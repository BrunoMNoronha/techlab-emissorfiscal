using EmissorFiscal.Core.Domain;
using EmissorFiscal.Core.Wire;
using EmissorFiscal.Core.Xml;

namespace EmissorFiscal.Core.QrCode;

public static class QrCode3
{
    // Receita DF, serviço 781, consultado em 04/10/2026. Apenas strings; nenhum acesso a URL.
    public const string DfBase = "http://www.fazenda.df.gov.br/nfce/qrcode?";
    public const string DfKeyConsultation = "http://www.fazenda.df.gov.br/nfce/consulta";

    public static string Online(AccessKey key, string environment = "2")
    {
        key.RequireOfflineScope();
        if (environment != "2") throw new InvalidDataException("Somente homologação 2.");
        return $"{DfBase}p={key.Value}|3|{environment}";
    }

    public static string AddToInvoice(string unsignedXml)
    {
        var document = SafeXml.Load(unsignedXml);
        var invoice = SafeXml.RequireRoot(document, "NFe");
        var key = NfceMessages.RequireInvoiceScope(invoice, requireSupplement: false);
        if (SafeXml.Children(invoice, "infNFeSupl").Count != 0 ||
            document.GetElementsByTagName("Signature", "http://www.w3.org/2000/09/xmldsig#").Count != 0)
            throw new InvalidDataException("Adicionar QR somente ao XML ainda sem suplemento e assinatura.");
        var supplement = document.CreateElement("infNFeSupl", SafeXml.NfeNamespace);
        SafeXml.Append(document, supplement, "qrCode", Online(key));
        SafeXml.Append(document, supplement, "urlChave", DfKeyConsultation);
        invoice.AppendChild(supplement);
        return SafeXml.Serialize(document);
    }
}
