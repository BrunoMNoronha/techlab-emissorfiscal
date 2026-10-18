using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using EmissorFiscal.Core.Domain;
using EmissorFiscal.Core.QrCode;
using EmissorFiscal.Core.Signature;
using EmissorFiscal.Core.Xml;
using EmissorFiscal.Core.Xsd;

namespace EmissorFiscal.Core.Wire;

public static class NfceMessages
{
    public static string BuildSubmission(string signedXml, string batchId, XsdValidator validator)
    {
        if (!Regex.IsMatch(batchId, "^[0-9]{1,15}$", RegexOptions.CultureInvariant))
            throw new InvalidDataException("idLote deve ter 1 a 15 dígitos; não representa reserva de numeração.");
        validator.RequireValid("nfe", signedXml);
        A1Signer.Verify(signedXml);
        var invoice = SafeXml.Load(signedXml);
        var document = NewMessage("enviNFe");
        var root = document.DocumentElement!;
        SafeXml.Append(document, root, "idLote", batchId);
        SafeXml.Append(document, root, "indSinc", "1");
        root.AppendChild(document.ImportNode(invoice.DocumentElement!, deep: true));
        var envelope = SafeXml.Serialize(document);
        validator.RequireValid("enviNFe", envelope);
        A1Signer.Verify(envelope);
        return envelope;
    }

    public static string BuildConsultation(AccessKey key, XsdValidator validator)
    {
        key.RequireOfflineScope();
        var document = NewMessage("consSitNFe");
        var root = document.DocumentElement!;
        SafeXml.Append(document, root, "tpAmb", "2");
        SafeXml.Append(document, root, "xServ", "CONSULTAR");
        SafeXml.Append(document, root, "chNFe", key.Value);
        var xml = SafeXml.Serialize(document);
        validator.RequireValid("consSitNFe", xml);
        return xml;
    }

    internal static XmlDocument NewMessage(string rootName)
    {
        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        var root = document.CreateElement(rootName, SafeXml.NfeNamespace);
        root.SetAttribute("versao", "4.00");
        document.AppendChild(root);
        return document;
    }

    internal static XmlElement InvoiceIn(XmlDocument document)
    {
        var root = document.DocumentElement ?? throw new InvalidDataException("XML vazio.");
        if (root.NamespaceURI != SafeXml.NfeNamespace) throw new InvalidDataException("Namespace inválido.");
        var invoice = root.LocalName switch
        {
            "NFe" => SafeXml.RequireRoot(document, "NFe"),
            "enviNFe" or "nfeProc" => SafeXml.Child(root, "NFe"),
            _ => throw new InvalidDataException("Contêiner de NFC-e não suportado.")
        };
        if (document.GetElementsByTagName("infNFe", SafeXml.NfeNamespace).Count != 1 ||
            document.GetElementsByTagName("NFe", SafeXml.NfeNamespace).Count != 1)
            throw new InvalidDataException("Somente uma NFC-e por documento/envelope.");
        return invoice;
    }

    internal static AccessKey RequireInvoiceScope(XmlElement invoice, bool requireSupplement = true)
    {
        var info = SafeXml.Child(invoice, "infNFe");
        if (info.GetAttribute("versao") != "4.00" || !info.GetAttribute("Id").StartsWith("NFe", StringComparison.Ordinal))
            throw new InvalidDataException("infNFe/Id/versão inválido.");
        var key = AccessKey.Parse(info.GetAttribute("Id")[3..]);
        key.RequireOfflineScope();
        var ide = SafeXml.Child(info, "ide");
        if (SafeXml.Text(ide, "mod") != "65" || SafeXml.Text(ide, "cUF") != "53" ||
            SafeXml.Text(ide, "tpAmb") != "2" || SafeXml.Text(ide, "tpEmis") != "1")
            throw new InvalidDataException("Recorte: modelo 65, cUF 53, tpAmb 2, tpEmis 1.");
        var emit = SafeXml.Child(info, "emit");
        if (SafeXml.Text(SafeXml.Child(emit, "enderEmit"), "UF") != "DF")
            throw new InvalidDataException("Emitente fora do DF.");
        var date = XmlConvert.ToDateTimeOffset(SafeXml.Text(ide, "dhEmi"));
        var series = PadDigits(SafeXml.Text(ide, "serie"), 3);
        var number = PadDigits(SafeXml.Text(ide, "nNF"), 9);
        var expected = AccessKey.Create(date.ToString("yyMM", CultureInfo.InvariantCulture),
            SafeXml.Text(emit, "CNPJ"), series, number, SafeXml.Text(ide, "cNF"));
        if (key != expected || SafeXml.Text(ide, "cDV") != key.Value[^1..])
            throw new InvalidDataException("Chave difere dos campos originais de infNFe.");
        var supplements = SafeXml.Children(invoice, "infNFeSupl");
        if (requireSupplement || supplements.Count != 0)
        {
            var supplement = SafeXml.Child(invoice, "infNFeSupl");
            if (SafeXml.Text(supplement, "qrCode") != QrCode3.Online(key) ||
                SafeXml.Text(supplement, "urlChave") != QrCode3.DfKeyConsultation)
                throw new InvalidDataException("QR Code 3/consulta fora do contrato DF online.");
        }
        return key;
    }

    private static string PadDigits(string value, int width)
    {
        if (value.Length < 1 || value.Length > width || value.Any(c => c is < '0' or > '9'))
            throw new InvalidDataException("Série/número inválido.");
        return value.PadLeft(width, '0');
    }

    internal static void RequireScope(string entrypoint, XmlDocument document)
    {
        // procInutNFe_v4.00.xsd declara a raiz oficial como "ProcInutNFe" (P maiúsculo).
        var rootName = entrypoint switch
        {
            "nfe" => "NFe", "procNFe" => "nfeProc", "procInutNFe" => "ProcInutNFe", _ => entrypoint
        };
        var version = entrypoint is "envEvento" or "retEnvEvento" or "procEventoNFe" ? "1.00" : "4.00";
        var root = SafeXml.RequireRoot(document, rootName, version);
        if (entrypoint is "nfe" or "enviNFe" or "procNFe")
        {
            RequireInvoiceScope(InvoiceIn(document));
            if (entrypoint == "enviNFe" && SafeXml.Text(root, "indSinc") != "1")
                throw new InvalidDataException("NFC-e local exige indSinc=1.");
        }
        foreach (var element in document.GetElementsByTagName("*").OfType<XmlElement>())
        {
            if (element.NamespaceURI != SafeXml.NfeNamespace) continue;
            if (element.LocalName == "tpAmb" && element.InnerText != "2")
                throw new InvalidDataException("Ambiente fora de homologação.");
            if (element.LocalName == "cUF" && element.InnerText != "53")
                throw new InvalidDataException("UF fora do DF.");
            if ((entrypoint is "inutNFe" or "retInutNFe" or "procInutNFe") &&
                element.LocalName == "mod" && element.InnerText != "65")
                throw new InvalidDataException("Inutilização fora do modelo NFC-e 65.");
            if (element.LocalName == "chNFe") AccessKey.Parse(element.InnerText).RequireOfflineScope();
        }
    }
}
