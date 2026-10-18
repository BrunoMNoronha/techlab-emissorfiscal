using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using EmissorFiscal.Core.Domain;
using EmissorFiscal.Core.Signature;
using EmissorFiscal.Core.Xml;
using EmissorFiscal.Core.Xsd;

namespace EmissorFiscal.Core.Events;

// Pedido de cancelamento (tpEvento 110111) de uma NFC-e autorizada. Prazo,
// elegibilidade e decisão de cancelar são políticas externas (EF-17); aqui só
// se monta, assina e correlaciona o XML, no leiaute de eventos 1.00.
public sealed record CancellationRequest(AccessKey Key, string Cnpj, string ProtocolNumber, string Justification,
    DateTimeOffset EventTime, int Sequence = 1);

public enum EventState
{
    Registered,
    RegisteredNotLinked,
    RegisteredOutsideDeadline,
    Rejected,
    Unknown,
    InvalidResponse
}

public sealed record EventResult(EventState State, string? Code, string Reason, string? ProtocolNumber,
    string? CorrelatedReturnXml, string? Problem)
{
    public bool IsRegistered => State is EventState.Registered or EventState.RegisteredNotLinked
        or EventState.RegisteredOutsideDeadline;
}

public static class NfceEvents
{
    public const string Version = "1.00";
    public const string CancellationType = "110111";
    public const string CancellationDescription = "Cancelamento";
    private const string BatchProcessed = "128";

    // Rejeições de evento com semântica conhecida neste recorte (MOC 7, Anexo I,
    // tabela de cStat). Qualquer outro código não é inferido como rejeição.
    private static readonly HashSet<string> RejectionCodes = new(StringComparer.Ordinal)
    {
        "218", "220", "501", "502", "573", "594"
    };

    public static string BuildSignedCancellation(CancellationRequest request, X509Certificate2 certificate, XsdValidator validator)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Key.RequireOfflineScope();
        if (request.Cnpj != request.Key.Cnpj) throw new InvalidDataException("CNPJ do autor difere do emitente da chave.");
        // Mesma admissão de nProt do NfceProtocol (15 ou 17 posições); detEvento é
        // processContents="skip" no XSD, então esta é a única checagem local.
        if (!Regex.IsMatch(request.ProtocolNumber ?? "", "^(?:[0-9]{15}|[0-9]{17})$", RegexOptions.CultureInvariant))
            throw new InvalidDataException("nProt da autorização deve ter 15 ou 17 dígitos.");
        if (request.Sequence is < 1 or > 99) throw new InvalidDataException("nSeqEvento deve estar entre 1 e 99.");
        Justification(request.Justification);
        var sequence = request.Sequence.ToString(CultureInfo.InvariantCulture);
        var document = NewDocument("evento");
        var evento = document.DocumentElement!;
        var info = document.CreateElement("infEvento", SafeXml.NfeNamespace);
        info.SetAttribute("Id", "ID" + CancellationType + request.Key.Value + request.Sequence.ToString("00", CultureInfo.InvariantCulture));
        evento.AppendChild(info);
        SafeXml.Append(document, info, "cOrgao", "53");
        SafeXml.Append(document, info, "tpAmb", "2");
        SafeXml.Append(document, info, "CNPJ", request.Cnpj);
        SafeXml.Append(document, info, "chNFe", request.Key.Value);
        SafeXml.Append(document, info, "dhEvento", request.EventTime.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture));
        SafeXml.Append(document, info, "tpEvento", CancellationType);
        SafeXml.Append(document, info, "nSeqEvento", sequence);
        SafeXml.Append(document, info, "verEvento", Version);
        var detail = document.CreateElement("detEvento", SafeXml.NfeNamespace);
        detail.SetAttribute("versao", Version);
        info.AppendChild(detail);
        SafeXml.Append(document, detail, "descEvento", CancellationDescription);
        SafeXml.Append(document, detail, "nProt", request.ProtocolNumber!);
        SafeXml.Append(document, detail, "xJust", request.Justification);
        var signed = A1Signer.SignElement(SafeXml.Serialize(document), certificate, "evento", "infEvento", Version);
        // evento isolado não é entrypoint: a prova XSD é feita no lote de um evento.
        validator.RequireValid("envEvento", BuildBatch(signed, "1"));
        return signed;
    }

    // Lote de exatamente um evento (indSinc não existe no leiaute; o serviço é síncrono por lote).
    public static string BuildBatch(string signedEventXml, string batchId)
    {
        if (!Regex.IsMatch(batchId ?? "", "^[0-9]{1,15}$", RegexOptions.CultureInvariant))
            throw new InvalidDataException("idLote deve ter 1 a 15 dígitos.");
        var evento = SafeXml.Load(signedEventXml);
        SafeXml.RequireRoot(evento, "evento", Version);
        var document = NewDocument("envEvento");
        var root = document.DocumentElement!;
        SafeXml.Append(document, root, "idLote", batchId!);
        root.AppendChild(document.ImportNode(evento.DocumentElement!, deep: true));
        return SafeXml.Serialize(document);
    }

    public static EventResult Interpret(string signedEventXml, string responseXml, XsdValidator validator)
    {
        ArgumentNullException.ThrowIfNull(signedEventXml);
        ArgumentNullException.ThrowIfNull(responseXml);
        // Evento original inválido é falha local do chamador, nunca um resultado fiscal.
        A1Signer.VerifyElement(signedEventXml, "evento", "evento", "infEvento", Version);
        var original = SafeXml.Child(SafeXml.Load(signedEventXml).DocumentElement!, "infEvento");
        try
        {
            validator.RequireValid("retEnvEvento", responseXml);
            var root = SafeXml.Load(responseXml).DocumentElement!;
            var batchCode = SafeXml.Text(root, "cStat");
            var batchReason = SafeXml.Text(root, "xMotivo");
            var returns = SafeXml.Children(root, "retEvento");
            if (returns.Count == 0)
            {
                return RejectionCodes.Contains(batchCode) || batchCode is "225" or "215"
                    ? new(EventState.Rejected, batchCode, batchReason, null, null, null)
                    : Unknown(batchCode, batchReason, "Lote sem retEvento correlacionado.");
            }
            if (batchCode != BatchProcessed) return Unknown(batchCode, batchReason, "Lote com status incompatível com retEvento.");
            if (returns.Count != 1) return Unknown(batchCode, batchReason, "Mais de um retEvento para um lote unitário.");
            var info = SafeXml.Child(returns[0], "infEvento");
            var code = SafeXml.Text(info, "cStat");
            var reason = SafeXml.Text(info, "xMotivo");
            if (SafeXml.Text(info, "tpAmb") != "2" || OptionalText(info, "chNFe") != SafeXml.Text(original, "chNFe") ||
                OptionalText(info, "tpEvento") != CancellationType ||
                OptionalText(info, "nSeqEvento") != SafeXml.Text(original, "nSeqEvento"))
                return Unknown(code, reason, "Chave/ambiente/evento/sequência do retorno diferem do evento assinado.");
            if (code is "135" or "136" or "155")
            {
                var number = OptionalText(info, "nProt");
                if (number is null) return Unknown(code, reason, "Evento registrado sem nProt.");
                var state = code switch
                {
                    "136" => EventState.RegisteredNotLinked,
                    "155" => EventState.RegisteredOutsideDeadline,
                    _ => EventState.Registered
                };
                return new(state, code, reason, number, returns[0].OuterXml, null);
            }
            return RejectionCodes.Contains(code)
                ? new(EventState.Rejected, code, reason, null, null, null)
                : Unknown(code, reason, "Código de evento não mapeado; não inferir registro nem rejeição.");
        }
        catch (Exception error) when (error is XmlException or XmlSchemaException or InvalidDataException or
                                            FormatException or CryptographicException)
        {
            return new(EventState.InvalidResponse, null, "Retorno local inválido/incompleto.", null, null, error.GetType().Name);
        }
    }

    public static string BuildProcessed(string signedEventXml, string responseXml, XsdValidator validator)
    {
        var result = Interpret(signedEventXml, responseXml, validator);
        if (!result.IsRegistered || result.CorrelatedReturnXml is null)
            throw new InvalidDataException("procEventoNFe exige evento registrado com retEvento correlacionado.");
        var document = NewDocument("procEventoNFe");
        var root = document.DocumentElement!;
        root.AppendChild(document.ImportNode(SafeXml.Load(signedEventXml).DocumentElement!, deep: true));
        root.AppendChild(document.ImportNode(SafeXml.Load(result.CorrelatedReturnXml).DocumentElement!, deep: true));
        var xml = SafeXml.Serialize(document);
        validator.RequireValid("procEventoNFe", xml);
        A1Signer.VerifyElement(xml, "procEventoNFe", "evento", "infEvento", Version);
        return xml;
    }

    internal static void Justification(string? value)
    {
        if (value is null || value.Length is < 15 or > 255 || value != value.Trim())
            throw new InvalidDataException("xJust deve ter de 15 a 255 caracteres, sem espaços nas pontas.");
        try { XmlConvert.VerifyXmlChars(value); }
        catch (XmlException exception) { throw new InvalidDataException("Caractere XML inválido em xJust.", exception); }
    }

    internal static XmlDocument NewDocument(string rootName, string version = Version)
    {
        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        var root = document.CreateElement(rootName, SafeXml.NfeNamespace);
        root.SetAttribute("versao", version);
        document.AppendChild(root);
        return document;
    }

    internal static string? OptionalText(XmlElement parent, string name)
    {
        var children = SafeXml.Children(parent, name);
        if (children.Count > 1) throw new InvalidDataException($"{name} repetido.");
        return children.Count == 0 ? null : children[0].InnerText;
    }

    private static EventResult Unknown(string? code, string reason, string problem) =>
        new(EventState.Unknown, code, reason, null, null, problem);
}
