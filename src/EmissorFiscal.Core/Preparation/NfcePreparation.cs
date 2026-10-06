using System.Globalization;
using System.Xml;
using EmissorFiscal.Core.Domain;
using EmissorFiscal.Core.Xml;

namespace EmissorFiscal.Core.Preparation;

public static class NfcePreparation
{
    public const string HomologationProduct =
        "NOTA FISCAL EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL";
    public const string HomologationRecipient =
        "NF-E EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL";

    // Novo documento a partir dos três modelos; nenhuma leitura/clonagem de fixture,
    // soma tributária, classificação, assinatura ou operação de rede.
    public static PreparedNfce BuildUnsigned(PreparationSale sale,
        PreparationEmitterProfile emitter, PreparationTestContext context)
    {
        PreparationValidation.Validate(sale, emitter, context);
        var date = PreparationValidation.IssuedAt(sale.IssuedAt);
        var key = AccessKey.Create(date.ToString("yyMM", CultureInfo.InvariantCulture), emitter.Cnpj,
            emitter.Series.PadLeft(3, '0'), context.Number.PadLeft(9, '0'), context.NumericCode);
        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        var invoice = document.CreateElement("NFe", SafeXml.NfeNamespace);
        document.AppendChild(invoice);
        var info = Element(document, invoice, "infNFe");
        info.SetAttribute("Id", "NFe" + key.Value);
        info.SetAttribute("versao", "4.00");
        var identification = Element(document, info, "ide");
        Fields(document, identification,
            ("cUF", "53"), ("cNF", context.NumericCode), ("natOp", sale.Operation.Nature),
            ("mod", "65"), ("serie", emitter.Series), ("nNF", context.Number),
            ("dhEmi", sale.IssuedAt), ("tpNF", sale.Operation.InvoiceType),
            ("idDest", sale.Operation.Destination), ("cMunFG", emitter.FiscalMunicipality),
            ("tpImp", "4"), ("tpEmis", "1"), ("cDV", key.Value[^1..]), ("tpAmb", "2"),
            ("finNFe", sale.Operation.Purpose), ("indFinal", sale.Operation.FinalConsumer),
            ("indPres", sale.Operation.Presence), ("procEmi", "0"), ("verProc", emitter.ProcessVersion));
        var issuer = Element(document, info, "emit");
        Fields(document, issuer, ("CNPJ", emitter.Cnpj), ("xNome", emitter.Name), ("xFant", emitter.FantasyName));
        Address(document, Element(document, issuer, "enderEmit"), emitter.Address);
        Fields(document, issuer, ("IE", emitter.StateRegistration), ("CRT", emitter.Crt));
        if (sale.Recipient is not null)
        {
            var recipient = Element(document, info, "dest");
            Fields(document, recipient, (sale.Recipient.DocumentType, sale.Recipient.Document),
                ("xNome", sale.Recipient.Name));
            if (sale.Recipient.Address is not null)
                Address(document, Element(document, recipient, "enderDest"), sale.Recipient.Address);
            Fields(document, recipient, ("indIEDest", sale.Recipient.StateRegistrationIndicator),
                ("email", sale.Recipient.Email));
        }
        for (var index = 0; index < sale.Items.Length; index++)
        {
            var item = sale.Items[index];
            var detail = Element(document, info, "det");
            detail.SetAttribute("nItem", item.Number);
            var product = Element(document, detail, "prod");
            Fields(document, product, ("cProd", item.Code), ("cEAN", item.Gtin),
                ("xProd", item.Description), ("NCM", item.Ncm),
                ("CEST", item.Cest), ("cBenef", item.BenefitCode), ("CFOP", item.Cfop),
                ("uCom", item.CommercialUnit), ("qCom", item.CommercialQuantity),
                ("vUnCom", item.CommercialUnitPrice), ("vProd", item.ProductValue),
                ("cEANTrib", item.TaxableGtin), ("uTrib", item.TaxableUnit),
                ("qTrib", item.TaxableQuantity), ("vUnTrib", item.TaxableUnitPrice),
                ("vFrete", item.Freight), ("vSeg", item.Insurance), ("vDesc", item.Discount),
                ("vOutro", item.OtherValue), ("indTot", item.TotalIndicator));
            Taxes(document, Element(document, detail, "imposto"), item.Taxes);
            Fields(document, detail, ("infAdProd", item.AdditionalInfo), ("vItem", item.ItemTotal));
        }
        var total = Element(document, info, "total");
        var icmsTotal = Element(document, total, "ICMSTot");
        // Ordem e 19 campos obrigatórios de leiauteNFe_v4.00.xsd, ICMSTot.
        foreach (var name in IcmsTotalOrder)
            SafeXml.Append(document, icmsTotal, name,
                (string)typeof(PreparationIcmsTotals).GetProperty(name)!.GetValue(sale.Totals.Icms)!);
        if (sale.Totals.IbsCbs is not null)
            RtcTotals(document, Element(document, total, "IBSCBSTot"), sale.Totals.IbsCbs);
        Fields(document, total, ("vNFTot", sale.Totals.InvoiceTotal));
        Fields(document, Element(document, info, "transp"), ("modFrete", sale.Operation.FreightMode));
        var payments = Element(document, info, "pag");
        foreach (var payment in sale.Payments)
        {
            var detail = Element(document, payments, "detPag");
            Fields(document, detail, ("indPag", payment.Indicator), ("tPag", payment.Code),
                ("xPag", payment.Description), ("vPag", payment.Value));
            if (payment.Card is not null)
            {
                var card = Element(document, detail, "card");
                Fields(document, card, ("tpIntegra", payment.Card.Integration), ("CNPJ", payment.Card.Cnpj),
                    ("tBand", payment.Card.Brand), ("cAut", payment.Card.Authorization),
                    ("CNPJReceb", payment.Card.RecipientCnpj), ("idTermPag", payment.Card.Terminal));
            }
        }
        Fields(document, payments, ("vTroco", sale.Change));
        if (sale.AdditionalInfo is not null)
            Fields(document, Element(document, info, "infAdic"), ("infCpl", sale.AdditionalInfo));
        return new PreparedNfce(key, SafeXml.Serialize(document));
    }

    private static readonly string[] IcmsTotalOrder =
    [
        "vBC", "vICMS", "vICMSDeson", "vFCP", "vBCST", "vST", "vFCPST", "vFCPSTRet",
        "vProd", "vFrete", "vSeg", "vDesc", "vII", "vIPI", "vIPIDevol", "vPIS", "vCOFINS", "vOutro", "vNF"
    ];

    private static void Address(XmlDocument document, XmlElement parent, PreparationAddress address) =>
        Fields(document, parent, ("xLgr", address.Street), ("nro", address.Number),
            ("xCpl", address.Complement), ("xBairro", address.District),
            ("cMun", address.MunicipalityCode), ("xMun", address.MunicipalityName), ("UF", address.Uf),
            ("CEP", address.PostalCode), ("cPais", address.CountryCode), ("xPais", address.CountryName),
            ("fone", address.Phone));

    private static void Taxes(XmlDocument document, XmlElement parent, PreparationTaxes taxes)
    {
        Fields(document, parent, ("vTotTrib", taxes.TotalTaxes));
        var icms = Element(document, Element(document, parent, "ICMS"), taxes.Icms.Group);
        Fields(document, icms, ("orig", taxes.Icms.Origin), ("CST", taxes.Icms.Cst), ("CSOSN", taxes.Icms.Csosn),
            ("modBC", taxes.Icms.BaseMode), ("vBC", taxes.Icms.Base), ("pICMS", taxes.Icms.Rate),
            ("vICMS", taxes.Icms.Value));
        var pis = Element(document, Element(document, parent, "PIS"), taxes.Pis.Group);
        Fields(document, pis, ("CST", taxes.Pis.Cst), ("vBC", taxes.Pis.Base),
            ("pPIS", taxes.Pis.Rate), ("vPIS", taxes.Pis.Value));
        var cofins = Element(document, Element(document, parent, "COFINS"), taxes.Cofins.Group);
        Fields(document, cofins, ("CST", taxes.Cofins.Cst), ("vBC", taxes.Cofins.Base),
            ("pCOFINS", taxes.Cofins.Rate), ("vCOFINS", taxes.Cofins.Value));
        if (taxes.IbsCbs is not null)
        {
            var ibsCbs = Element(document, parent, "IBSCBS");
            Fields(document, ibsCbs, ("CST", taxes.IbsCbs.Cst), ("cClassTrib", taxes.IbsCbs.Classification));
            var rtc = taxes.IbsCbs.Tax;
            var group = Element(document, ibsCbs, "gIBSCBS");
            Fields(document, group, ("vBC", rtc.Base));
            Fields(document, Element(document, group, "gIBSUF"), ("pIBSUF", rtc.Uf.Rate), ("vIBSUF", rtc.Uf.Value));
            Fields(document, Element(document, group, "gIBSMun"),
                ("pIBSMun", rtc.Municipality.Rate), ("vIBSMun", rtc.Municipality.Value));
            Fields(document, group, ("vIBS", rtc.Ibs));
            Fields(document, Element(document, group, "gCBS"), ("pCBS", rtc.Cbs.Rate), ("vCBS", rtc.Cbs.Value));
        }
    }

    private static void RtcTotals(XmlDocument document, XmlElement parent, PreparationIbsCbsTotals totals)
    {
        Fields(document, parent, ("vBCIBSCBS", totals.Base));
        var ibs = Element(document, parent, "gIBS");
        Fields(document, Element(document, ibs, "gIBSUF"), ("vDif", totals.Ibs.Uf.Deferral),
            ("vDevTrib", totals.Ibs.Uf.ReturnedTax), ("vIBSUF", totals.Ibs.Uf.Value));
        Fields(document, Element(document, ibs, "gIBSMun"), ("vDif", totals.Ibs.Municipality.Deferral),
            ("vDevTrib", totals.Ibs.Municipality.ReturnedTax), ("vIBSMun", totals.Ibs.Municipality.Value));
        Fields(document, ibs, ("vIBS", totals.Ibs.Value), ("vCredPres", totals.Ibs.PresumedCredit),
            ("vCredPresCondSus", totals.Ibs.ConditionalPresumedCredit));
        Fields(document, Element(document, parent, "gCBS"), ("vDif", totals.Cbs.Deferral),
            ("vDevTrib", totals.Cbs.ReturnedTax), ("vCBS", totals.Cbs.Value),
            ("vCredPres", totals.Cbs.PresumedCredit), ("vCredPresCondSus", totals.Cbs.ConditionalPresumedCredit));
    }

    private static XmlElement Element(XmlDocument document, XmlElement parent, string name)
    {
        var child = document.CreateElement(name, SafeXml.NfeNamespace);
        parent.AppendChild(child);
        return child;
    }

    private static void Fields(XmlDocument document, XmlElement parent, params (string Name, string? Value)[] fields)
    {
        foreach (var (name, value) in fields)
            if (value is not null) SafeXml.Append(document, parent, name, value);
    }
}
