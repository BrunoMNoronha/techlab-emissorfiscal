using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using EmissorFiscal.Core.Domain;
using EmissorFiscal.Core.Numbering;
using EmissorFiscal.Core.Signature;
using EmissorFiscal.Core.Xml;
using EmissorFiscal.Core.Xsd;

namespace EmissorFiscal.Core.Events;

// Pedido de inutilização de uma faixa de nNF nunca autorizada. Qual faixa
// inutilizar, quando e com que justificativa é política aprovada fora daqui
// (EF-18, ficha seção 2); nenhuma faixa é deduzida de lacunas.
public sealed record InutilizationRequest(NumberPoolKey Pool, string Cnpj, string Year, long From, long To, string Justification);

public enum InutilizationState
{
    Homologated,
    Rejected,
    Unknown,
    InvalidResponse
}

public sealed record InutilizationResult(InutilizationState State, string? Code, string Reason, string? ProtocolNumber,
    string? CorrelatedReturnXml, string? Problem)
{
    public bool IsHomologated => State == InutilizationState.Homologated;
}

public static class NfceInutilization
{
    public const string Version = "4.00";
    public const string ProcessedRoot = "ProcInutNFe";
    private const string Homologated = "102";
    // Rejeições de inutilização com semântica conhecida (MOC 7, Anexo I); demais códigos não são inferidos.
    private static readonly HashSet<string> RejectionCodes = new(StringComparer.Ordinal)
    {
        "215", "241", "256", "563"
    };

    public static string BuildSigned(InutilizationRequest request, X509Certificate2 certificate, XsdValidator validator)
    {
        ArgumentNullException.ThrowIfNull(request);
        AccessKey.ValidateCnpj(request.Cnpj);
        if (!Regex.IsMatch(request.Year ?? "", "^[0-9]{2}$", RegexOptions.CultureInvariant))
            throw new InvalidDataException("ano deve ter dois dígitos.");
        if (request.From < 1 || request.To < request.From || request.To > NumberReservation.MaxNumber)
            throw new InvalidDataException("Faixa de inutilização inválida: 1 <= nNFIni <= nNFFin <= 999999999.");
        NfceEvents.Justification(request.Justification);
        var series = request.Pool.Series.TrimStart('0');
        if (series.Length == 0) series = "0";
        var id = "ID53" + request.Year + request.Cnpj + NumberPoolKey.Model + request.Pool.Series +
                 request.From.ToString("000000000", CultureInfo.InvariantCulture) +
                 request.To.ToString("000000000", CultureInfo.InvariantCulture);
        var document = NfceEvents.NewDocument("inutNFe", Version);
        var root = document.DocumentElement!;
        var info = document.CreateElement("infInut", SafeXml.NfeNamespace);
        info.SetAttribute("Id", id);
        root.AppendChild(info);
        SafeXml.Append(document, info, "tpAmb", NumberPoolKey.Environment);
        SafeXml.Append(document, info, "xServ", "INUTILIZAR");
        SafeXml.Append(document, info, "cUF", "53");
        SafeXml.Append(document, info, "ano", request.Year!);
        SafeXml.Append(document, info, "CNPJ", request.Cnpj);
        SafeXml.Append(document, info, "mod", NumberPoolKey.Model);
        SafeXml.Append(document, info, "serie", series);
        SafeXml.Append(document, info, "nNFIni", request.From.ToString(CultureInfo.InvariantCulture));
        SafeXml.Append(document, info, "nNFFin", request.To.ToString(CultureInfo.InvariantCulture));
        SafeXml.Append(document, info, "xJust", request.Justification);
        var signed = A1Signer.SignElement(SafeXml.Serialize(document), certificate, "inutNFe", "infInut", Version);
        validator.RequireValid("inutNFe", signed);
        return signed;
    }

    public static InutilizationResult Interpret(string signedXml, string responseXml, XsdValidator validator)
    {
        ArgumentNullException.ThrowIfNull(signedXml);
        ArgumentNullException.ThrowIfNull(responseXml);
        A1Signer.VerifyElement(signedXml, "inutNFe", "inutNFe", "infInut", Version);
        var original = SafeXml.Child(SafeXml.Load(signedXml).DocumentElement!, "infInut");
        try
        {
            validator.RequireValid("retInutNFe", responseXml);
            var root = SafeXml.Load(responseXml).DocumentElement!;
            var info = SafeXml.Child(root, "infInut");
            var code = SafeXml.Text(info, "cStat");
            var reason = SafeXml.Text(info, "xMotivo");
            if (SafeXml.Text(info, "tpAmb") != "2" || SafeXml.Text(info, "cUF") != "53")
                return Unknown(code, reason, "Ambiente/UF do retorno diferem do pedido.");
            // Campos opcionais do retorno, quando presentes, precisam coincidir com o pedido assinado.
            foreach (var name in new[] { "ano", "CNPJ", "mod", "serie", "nNFIni", "nNFFin" })
            {
                var returned = NfceEvents.OptionalText(info, name);
                if (returned is not null && returned != SafeXml.Text(original, name))
                    return Unknown(code, reason, $"{name} do retorno difere do pedido assinado.");
            }
            if (code == Homologated)
            {
                var number = NfceEvents.OptionalText(info, "nProt");
                if (number is null) return Unknown(code, reason, "Inutilização homologada sem nProt.");
                if (NfceEvents.OptionalText(info, "nNFIni") is null || NfceEvents.OptionalText(info, "nNFFin") is null)
                    return Unknown(code, reason, "Homologação sem a faixa confirmada.");
                return new(InutilizationState.Homologated, code, reason, number, root.OuterXml, null);
            }
            return RejectionCodes.Contains(code)
                ? new(InutilizationState.Rejected, code, reason, null, null, null)
                : Unknown(code, reason, "Código de inutilização não mapeado; não inferir homologação nem rejeição.");
        }
        catch (Exception error) when (error is XmlException or XmlSchemaException or InvalidDataException or
                                            FormatException or CryptographicException)
        {
            return new(InutilizationState.InvalidResponse, null, "Retorno local inválido/incompleto.", null, null, error.GetType().Name);
        }
    }

    public static string BuildProcessed(string signedXml, string responseXml, XsdValidator validator)
    {
        var result = Interpret(signedXml, responseXml, validator);
        if (!result.IsHomologated || result.CorrelatedReturnXml is null)
            throw new InvalidDataException("procInutNFe exige inutilização homologada com retorno correlacionado.");
        // Raiz "ProcInutNFe" (P maiúsculo), como declarada em procInutNFe_v4.00.xsd.
        var document = NfceEvents.NewDocument(ProcessedRoot, Version);
        var root = document.DocumentElement!;
        root.AppendChild(document.ImportNode(SafeXml.Load(signedXml).DocumentElement!, deep: true));
        root.AppendChild(document.ImportNode(SafeXml.Load(result.CorrelatedReturnXml).DocumentElement!, deep: true));
        var xml = SafeXml.Serialize(document);
        validator.RequireValid("procInutNFe", xml);
        A1Signer.VerifyElement(xml, ProcessedRoot, "inutNFe", "infInut", Version);
        return xml;
    }

    private static InutilizationResult Unknown(string? code, string reason, string problem) =>
        new(InutilizationState.Unknown, code, reason, null, null, problem);
}

// Bloqueio concorrente de faixas por pool: duas inutilizações em andamento não
// podem se sobrepor, e uma faixa já homologada nunca é reutilizada. Em memória,
// como o pool; durabilidade é EF-09.
public sealed class InutilizationIntervalRegistry
{
    private readonly object gate = new();
    private readonly Dictionary<NumberPoolKey, List<(long From, long To)>> intervals = new();

    public void Lock(NumberPoolKey pool, long from, long to)
    {
        ArgumentNullException.ThrowIfNull(pool);
        if (from < 1 || to < from) throw new InvalidDataException("Faixa inválida.");
        lock (gate)
        {
            if (!intervals.TryGetValue(pool, out var list)) intervals[pool] = list = [];
            if (list.Any(existing => from <= existing.To && existing.From <= to))
                throw new IntervalOverlapException(pool, from, to);
            list.Add((from, to));
        }
    }
}

public sealed class IntervalOverlapException(NumberPoolKey pool, long from, long to)
    : InvalidOperationException($"Faixa {from}-{to} de {pool} sobrepõe faixa já bloqueada ou homologada.");
