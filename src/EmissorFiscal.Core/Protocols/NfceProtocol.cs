using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using EmissorFiscal.Core.Signature;
using EmissorFiscal.Core.Wire;
using EmissorFiscal.Core.Xml;
using EmissorFiscal.Core.Xsd;

namespace EmissorFiscal.Core.Protocols;

public enum FiscalState
{
    Authorized,
    AuthorizedWithAlert,
    AuthorizedOutsideDeadline,
    Rejected,
    Processing,
    NotFound,
    Unknown,
    InvalidResponse
}

public sealed record FiscalMessage(string Code, string Text);
public sealed record ProtocolResult(FiscalState State, string? Code, string Reason,
    string? ProtocolNumber, IReadOnlyList<FiscalMessage> Messages, string? CorrelatedProtocolXml,
    string? Problem)
{
    public bool IsAuthorized => State is FiscalState.Authorized or FiscalState.AuthorizedWithAlert or
        FiscalState.AuthorizedOutsideDeadline;
}

public static class NfceProtocol
{
    private static readonly HashSet<string> RejectionCodes = new(StringComparer.Ordinal)
    {
        "203", "215", "225", "280", "281", "283", "297", "298", "502", "704", "778"
    };

    public static ProtocolResult Interpret(string originalSignedXml, string responseXml, XsdValidator validator)
    {
        // Um XML original inválido é falha local do chamador, nunca um resultado fiscal.
        validator.RequireValid("nfe", originalSignedXml);
        var original = A1Signer.Verify(originalSignedXml);
        try
        {
            var document = SafeXml.Load(responseXml);
            var root = document.DocumentElement ?? throw new InvalidDataException("Resposta vazia.");
            var entrypoint = root.LocalName switch
            {
                "retEnviNFe" => "retEnviNFe", "retConsSitNFe" => "retConsSitNFe", "nfeProc" => "procNFe",
                _ => throw new InvalidDataException("Retorno offline não suportado.")
            };
            validator.RequireValid(entrypoint, responseXml);
            var containerCode = entrypoint == "procNFe" ? null : SafeXml.Text(root, "cStat");
            var containerReason = entrypoint == "procNFe" ? "nfeProc técnico" : SafeXml.Text(root, "xMotivo");
            if (entrypoint == "retConsSitNFe" && SafeXml.Text(root, "chNFe") != original.Key.Value)
                return Unknown(containerCode, containerReason, "Chave consultada difere do XML original.");
            if (entrypoint == "procNFe")
            {
                var embedded = A1Signer.Verify(responseXml);
                if (embedded.Key != original.Key || embedded.DigestBase64 != original.DigestBase64 ||
                    embedded.CertificateSha256 != original.CertificateSha256)
                    return Unknown(null, containerReason, "NFC-e processada difere do original assinado.");
            }
            var protocols = SafeXml.Children(root, "protNFe");
            if (protocols.Count == 0)
            {
                return containerCode switch
                {
                    "103" or "105" => new(FiscalState.Processing, containerCode, containerReason, null, [], null,
                        "Processamento pendente; consulta/reconciliação futura, sem reenvio automático."),
                    "217" => new(FiscalState.NotFound, containerCode, containerReason, null, [], null,
                        "Não localizado; este resultado isolado não permite nova emissão/reenvio."),
                    _ when containerCode is not null && RejectionCodes.Contains(containerCode) =>
                        new(FiscalState.Rejected, containerCode, containerReason, null, [], null, null),
                    _ => Unknown(containerCode, containerReason, "Retorno sem protocolo correlacionado.")
                };
            }
            if (protocols.Count != 1) return Unknown(containerCode, containerReason, "Mais de um protocolo.");
            var protocol = protocols[0];
            var info = SafeXml.Child(protocol, "infProt");
            var code = SafeXml.Text(info, "cStat");
            var reason = SafeXml.Text(info, "xMotivo");
            var messages = ReadMessages(info);
            // Também correlacionar rejeições: nunca atribuir o resultado de outra chave.
            if (SafeXml.Text(info, "chNFe") != original.Key.Value || SafeXml.Text(info, "tpAmb") != "2")
                return Unknown(code, reason, "Chave/ambiente do protocolo difere do XML original.", messages);
            if (entrypoint == "retEnviNFe" && containerCode != "104")
                return Unknown(code, reason, "Resposta de envio com status externo incompatível com protocolo.", messages);
            if (entrypoint == "retConsSitNFe" && containerCode != code)
                return Unknown(code, reason, "Consulta e protocolo informam estados diferentes; reconciliar eventos.", messages);
            if (code is not ("100" or "120" or "150"))
            {
                if (RejectionCodes.Contains(code))
                    return new(FiscalState.Rejected, code, reason, OptionalText(info, "nProt"), messages, null, null);
                return Unknown(code, reason, code is "204" or "539"
                    ? "Duplicidade exige reconciliação; não alterar XML ou reenviar."
                    : "Código fiscal não mapeado; não inferir autorização nem rejeição.", messages);
            }
            var number = OptionalText(info, "nProt");
            var fiscalDigest = OptionalText(info, "digVal");
            if (number is null || !Regex.IsMatch(number, "^(?:[0-9]{15}|[0-9]{17})$", RegexOptions.CultureInvariant) ||
                fiscalDigest is null)
                return Unknown(code, reason, "Autorização sem nProt/digVal completo.", messages);
            var expectedDigest = Convert.FromBase64String(original.DigestBase64);
            var actualDigest = Convert.FromBase64String(fiscalDigest);
            if (actualDigest.Length != 20 || !CryptographicOperations.FixedTimeEquals(expectedDigest, actualDigest))
                return Unknown(code, reason, "digVal não corresponde ao digest fiscal do XML original.", messages);
            var state = code switch
            {
                "120" => FiscalState.AuthorizedWithAlert,
                "150" => FiscalState.AuthorizedOutsideDeadline,
                _ => FiscalState.Authorized
            };
            return new(state, code, reason, number, messages, protocol.OuterXml, null);
        }
        catch (Exception error) when (error is XmlException or XmlSchemaException or InvalidDataException or
                                            FormatException or CryptographicException)
        {
            return new(FiscalState.InvalidResponse, null, "Retorno local inválido/incompleto.", null, [], null,
                error.GetType().Name);
        }
    }

    public static string BuildProcessed(string originalSignedXml, string responseXml, XsdValidator validator)
    {
        var result = Interpret(originalSignedXml, responseXml, validator);
        if (!result.IsAuthorized || result.CorrelatedProtocolXml is null)
            throw new InvalidDataException("nfeProc exige autorização com protocolo correlacionado.");
        var original = SafeXml.Load(originalSignedXml);
        var protocol = SafeXml.Load(result.CorrelatedProtocolXml);
        var document = NfceMessages.NewMessage("nfeProc");
        var root = document.DocumentElement!;
        root.AppendChild(document.ImportNode(original.DocumentElement!, deep: true));
        root.AppendChild(document.ImportNode(protocol.DocumentElement!, deep: true));
        var xml = SafeXml.Serialize(document);
        validator.RequireValid("procNFe", xml);
        A1Signer.Verify(xml);
        return xml;
    }

    private static ProtocolResult Unknown(string? code, string reason, string problem,
        IReadOnlyList<FiscalMessage>? messages = null) =>
        new(FiscalState.Unknown, code, reason, null, messages ?? [], null, problem);

    private static string? OptionalText(XmlElement parent, string name)
    {
        var children = SafeXml.Children(parent, name);
        if (children.Count > 1) throw new InvalidDataException($"{name} repetido.");
        return children.Count == 0 ? null : children[0].InnerText;
    }

    private static IReadOnlyList<FiscalMessage> ReadMessages(XmlElement info)
    {
        var codes = SafeXml.Children(info, "cMsg");
        var texts = SafeXml.Children(info, "xMsg");
        if (codes.Count != texts.Count) throw new InvalidDataException("Par cMsg/xMsg incompleto.");
        return codes.Select((code, index) => new FiscalMessage(code.InnerText, texts[index].InnerText)).ToArray();
    }
}
