using System.Globalization;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Schema;
using EmissorFiscal.Core.Domain;
using EmissorFiscal.Core.Protocols;
using EmissorFiscal.Core.QrCode;
using EmissorFiscal.Core.Xml;
using EmissorFiscal.Core.Xsd;

namespace EmissorFiscal.Core.Danfe;

public sealed record DanfeParty(string DocumentLabel, string Document, string Name, string? FantasyName,
    string? StateRegistration, IReadOnlyList<string> AddressLines);

public sealed record DanfeItem(string Number, string Code, string Description, string Quantity, string Unit,
    string UnitPrice, string Total, string? Discount);

public sealed record DanfeTotals(int ItemCount, string Products, string Discount, string Freight, string Insurance,
    string Other, string Invoice, string? ApproximateTaxes);

public sealed record DanfePayment(string Code, string? Description, string Value);

public sealed record DanfeProtocol(string Number, string ReceivedAt, string Code, string Reason);

// Dados do DANFE NFC-e extraídos de um nfeProc autorizado. Nenhum valor é
// recalculado: tudo vem do XML e do protocolo correlacionado por NfceProtocol.
public sealed record DanfeModel(
    AccessKey Key, string Environment, string Number, string Series, string IssuedAt,
    DanfeParty Emitter, DanfeParty? Recipient, IReadOnlyList<DanfeItem> Items, DanfeTotals Totals,
    IReadOnlyList<DanfePayment> Payments, string? Change, DanfeProtocol Protocol, FiscalState State,
    IReadOnlyList<FiscalMessage> Alerts, string QrCodeText, string ConsultationUrl, string? AdditionalInfo)
{
    public bool IsHomologation => Environment == "2";

    public string FormattedKey => string.Join(' ', Enumerable.Range(0, 11).Select(i => Key.Value.Substring(i * 4, 4)));

    // O nfeProc é a única entrada: a NFe embutida é reinterpretada contra o
    // próprio protocolo (assinatura, XSD, chave, ambiente 2, cStat, digVal).
    public static DanfeModel FromProcessed(string processedXml, XsdValidator validator)
    {
        ArgumentNullException.ThrowIfNull(validator);
        var document = SafeXml.Load(processedXml);
        var root = SafeXml.RequireRoot(document, "nfeProc");
        var invoice = SafeXml.Child(root, "NFe");
        var original = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        original.AppendChild(original.ImportNode(invoice, deep: true));
        ProtocolResult result;
        try { result = NfceProtocol.Interpret(SafeXml.Serialize(original), processedXml, validator); }
        catch (Exception error) when (error is CryptographicException or XmlSchemaException)
        {
            throw new InvalidDataException($"nfeProc recusado para DANFE: {error.Message}", error);
        }
        if (!result.IsAuthorized || result.ProtocolNumber is null)
            throw new InvalidDataException(
                $"DANFE exige nfeProc autorizado (cStat 100/120/150); estado {result.State}: {result.Problem ?? result.Reason}");
        var info = SafeXml.Child(invoice, "infNFe");
        var key = AccessKey.Parse(info.GetAttribute("Id")["NFe".Length..]);
        key.RequireOfflineScope();
        var identification = SafeXml.Child(info, "ide");
        var environment = SafeXml.Text(identification, "tpAmb");
        if (environment != "2" || SafeXml.Text(identification, "mod") != "65")
            throw new InvalidDataException("DANFE restrito à NFC-e modelo 65 em homologação (tpAmb 2).");
        var supplement = SafeXml.Child(invoice, "infNFeSupl");
        var qr = SafeXml.Text(supplement, "qrCode");
        var consultation = SafeXml.Text(supplement, "urlChave");
        if (qr != QrCode3.Online(key, environment) || consultation != QrCode3.DfKeyConsultation)
            throw new InvalidDataException("QR Code/URL de consulta do XML divergem do QR v3 online DF da chave.");
        var protocolInfo = SafeXml.Child(SafeXml.Child(root, "protNFe"), "infProt");
        var protocol = new DanfeProtocol(result.ProtocolNumber, SafeXml.Text(protocolInfo, "dhRecbto"),
            result.Code ?? SafeXml.Text(protocolInfo, "cStat"), result.Reason);
        var details = SafeXml.Children(info, "det");
        if (details.Count == 0) throw new InvalidDataException("NFC-e sem itens.");
        var items = details.Select(Item).ToArray();
        var totals = SafeXml.Child(SafeXml.Child(info, "total"), "ICMSTot");
        var payments = SafeXml.Child(info, "pag");
        var additional = SafeXml.Children(info, "infAdic");
        return new DanfeModel(
            key, environment, SafeXml.Text(identification, "nNF"), SafeXml.Text(identification, "serie"),
            SafeXml.Text(identification, "dhEmi"), ReadEmitter(SafeXml.Child(info, "emit")),
            ReadRecipient(SafeXml.Children(info, "dest")), items,
            new DanfeTotals(items.Length, SafeXml.Text(totals, "vProd"), SafeXml.Text(totals, "vDesc"),
                SafeXml.Text(totals, "vFrete"), SafeXml.Text(totals, "vSeg"), SafeXml.Text(totals, "vOutro"),
                SafeXml.Text(totals, "vNF"), Optional(totals, "vTotTrib")),
            SafeXml.Children(payments, "detPag").Select(p =>
                new DanfePayment(SafeXml.Text(p, "tPag"), Optional(p, "xPag"), SafeXml.Text(p, "vPag"))).ToArray(),
            Optional(payments, "vTroco"), protocol, result.State, result.Messages, qr, consultation,
            additional.Count == 1 ? Optional(additional[0], "infCpl") : null);
    }

    private static DanfeItem Item(XmlElement detail)
    {
        var product = SafeXml.Child(detail, "prod");
        return new DanfeItem(detail.GetAttribute("nItem"), SafeXml.Text(product, "cProd"), SafeXml.Text(product, "xProd"),
            SafeXml.Text(product, "qCom"), SafeXml.Text(product, "uCom"), SafeXml.Text(product, "vUnCom"),
            SafeXml.Text(product, "vProd"), Optional(product, "vDesc"));
    }

    private static DanfeParty ReadEmitter(XmlElement issuer) =>
        new("CNPJ", SafeXml.Text(issuer, "CNPJ"), SafeXml.Text(issuer, "xNome"), Optional(issuer, "xFant"),
            Optional(issuer, "IE"), Address(SafeXml.Child(issuer, "enderEmit")));

    private static DanfeParty? ReadRecipient(IReadOnlyList<XmlElement> recipients)
    {
        if (recipients.Count == 0) return null;
        var recipient = recipients[0];
        var (label, document) = Optional(recipient, "CNPJ") is { } cnpj ? ("CNPJ", cnpj)
            : Optional(recipient, "CPF") is { } cpf ? ("CPF", cpf)
            : Optional(recipient, "idEstrangeiro") is { } foreign ? ("ID ESTRANGEIRO", foreign)
            : throw new InvalidDataException("Destinatário sem identificação.");
        var addresses = SafeXml.Children(recipient, "enderDest");
        return new DanfeParty(label, document, Optional(recipient, "xNome") ?? string.Empty, null,
            Optional(recipient, "IE"), addresses.Count == 1 ? Address(addresses[0]) : []);
    }

    private static IReadOnlyList<string> Address(XmlElement address)
    {
        var first = string.Join(", ", new[]
        {
            Optional(address, "xLgr"), Optional(address, "nro"), Optional(address, "xCpl")
        }.Where(part => !string.IsNullOrWhiteSpace(part)));
        var postal = Optional(address, "CEP");
        var second = string.Join(" - ", new[]
        {
            Optional(address, "xBairro"),
            string.Join("/", new[] { Optional(address, "xMun"), Optional(address, "UF") }.Where(p => p is not null)),
            postal is null ? null : "CEP " + FormatPostalCode(postal)
        }.Where(part => !string.IsNullOrWhiteSpace(part)));
        return new[] { first, second }.Where(line => line.Length > 0).ToArray();
    }

    private static string FormatPostalCode(string postal) =>
        postal.Length == 8 ? postal[..5] + "-" + postal[5..] : postal;

    private static string? Optional(XmlElement parent, string name)
    {
        var children = SafeXml.Children(parent, name);
        if (children.Count > 1) throw new InvalidDataException($"{name} repetido.");
        return children.Count == 0 ? null : children[0].InnerText;
    }

    // Formatação de apresentação; os valores permanecem strings do XML.
    public static string Money(string value)
    {
        var parsed = decimal.Parse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        return parsed.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',');
    }

    public static string Quantity(string value)
    {
        var parsed = decimal.Parse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
        var text = parsed.ToString("0.00##########", CultureInfo.InvariantCulture);
        return text.Replace('.', ',');
    }

    public static string Document(string label, string value) => (label, value.Length) switch
    {
        ("CNPJ", 14) => $"{value[..2]}.{value[2..5]}.{value[5..8]}/{value[8..12]}-{value[12..]}",
        ("CPF", 11) => $"{value[..3]}.{value[3..6]}.{value[6..9]}-{value[9..]}",
        _ => value
    };

    public static string Timestamp(string iso)
    {
        var parsed = DateTimeOffset.ParseExact(iso, "yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal);
        return parsed.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
    }
}
