using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Schema;
using EmissorFiscal.Core.Preparation;
using EmissorFiscal.Core.QrCode;
using EmissorFiscal.Core.Signature;
using EmissorFiscal.Core.Wire;
using EmissorFiscal.Core.Xml;
using EmissorFiscal.Core.Xsd;
using EmissorFiscal.Samples;
using Xunit;

namespace EmissorFiscal.Core.Tests;

public sealed class PreparationProof : IDisposable
{
    public XsdValidator Validator { get; } = OfflineFixtures.Validator();
    public X509Certificate2 Certificate { get; }

    public PreparationProof()
    {
        var pfx = OfflineFixtures.RsaPkcs12();
        try { Certificate = A1Signer.ImportPkcs12(pfx, OfflineFixtures.Password.AsSpan()); }
        finally { CryptographicOperations.ZeroMemory(pfx); }
    }

    public void Dispose() => Certificate.Dispose();
}

public sealed class NfcePreparationTests(PreparationProof proof) : IClassFixture<PreparationProof>
{
    [Theory]
    [InlineData("crt1")]
    [InlineData("crt3")]
    [InlineData("st-crt1")]
    [InlineData("exempt-crt1")]
    [InlineData("basket-crt3")]
    public void DeclaredModelsProduceNewSignedNfeSubmissionAndConsultationValidatedByPinnedXsd(string scenario)
    {
        var (sale, emitter, context) = Models(scenario);
        var prepared = NfcePreparation.BuildUnsigned(sale, emitter, context);
        Assert.DoesNotContain("<Signature", prepared.UnsignedXml, StringComparison.Ordinal);
        Assert.DoesNotContain("<infNFeSupl", prepared.UnsignedXml, StringComparison.Ordinal);
        // O entrypoint NFe exige assinatura: XML unsigned não prova validação fiscal.
        Assert.Throws<XmlSchemaValidationException>(() => proof.Validator.RequireValid("nfe", prepared.UnsignedXml));
        var signed = Sign(prepared);
        proof.Validator.RequireValid("nfe", signed);
        var signature = A1Signer.Verify(SafeXml.Serialize(SafeXml.Load(signed)), proof.Certificate);
        Assert.Equal(prepared.AccessKey, signature.Key);
        var submission = NfceMessages.BuildSubmission(signed, "123", proof.Validator);
        proof.Validator.RequireValid("enviNFe", submission);
        proof.Validator.RequireValid("consSitNFe", NfceMessages.BuildConsultation(signature.Key, proof.Validator));
        Assert.Equal(signature.DigestBase64, A1Signer.Verify(submission).DigestBase64);
        Assert.Equal("53", signature.Key.Uf);
        Assert.Equal("65", signature.Key.Model);
        // Nenhum transporte, resposta/protocolo autorizado ou certificado de contribuinte.
    }

    [Theory]
    [InlineData("crt1")]
    [InlineData("crt3")]
    [InlineData("st-crt1")]
    [InlineData("exempt-crt1")]
    [InlineData("basket-crt3")]
    public void DeclaredValuesGroupsUnitsDiscountsPaymentsAndEmitterArePreserved(string scenario)
    {
        var (sale, emitter, context) = Models(scenario);
        var prepared = NfcePreparation.BuildUnsigned(sale, emitter, context);
        var info = Info(prepared.UnsignedXml);
        var issuer = SafeXml.Child(info, "emit");
        Assert.Equal(emitter.Cnpj, SafeXml.Text(issuer, "CNPJ"));
        Assert.Equal(emitter.Name, SafeXml.Text(issuer, "xNome"));
        Assert.Equal(emitter.StateRegistration, SafeXml.Text(issuer, "IE"));
        Assert.Equal(emitter.Address.Street, SafeXml.Text(SafeXml.Child(issuer, "enderEmit"), "xLgr"));
        Assert.Equal(sale.IssuedAt, SafeXml.Text(SafeXml.Child(info, "ide"), "dhEmi"));
        Assert.Equal(NfcePreparation.HomologationRecipient, SafeXml.Text(SafeXml.Child(info, "dest"), "xNome"));
        Assert.Equal("9", SafeXml.Text(SafeXml.Child(info, "dest"), "indIEDest"));
        var details = SafeXml.Children(info, "det");
        Assert.Equal(sale.Items.Length, details.Count);
        for (var index = 0; index < details.Count; index++)
        {
            var declared = sale.Items[index];
            var product = SafeXml.Child(details[index], "prod");
            Assert.Equal(declared.Number, details[index].GetAttribute("nItem"));
            Assert.Equal(declared.Code, SafeXml.Text(product, "cProd"));
            Assert.Equal(declared.Ncm, SafeXml.Text(product, "NCM"));
            Assert.Equal(declared.Cfop, SafeXml.Text(product, "CFOP"));
            Assert.Equal(index == 0 ? NfcePreparation.HomologationProduct : declared.Description, SafeXml.Text(product, "xProd"));
            Assert.Equal(declared.CommercialUnit, SafeXml.Text(product, "uCom"));
            Assert.Equal(declared.TaxableUnit, SafeXml.Text(product, "uTrib"));
            Assert.Equal(declared.CommercialQuantity, SafeXml.Text(product, "qCom"));
            Assert.Equal(declared.TaxableQuantity, SafeXml.Text(product, "qTrib"));
            Assert.Equal(declared.CommercialUnitPrice, SafeXml.Text(product, "vUnCom"));
            Assert.Equal(declared.TaxableUnitPrice, SafeXml.Text(product, "vUnTrib"));
            Assert.Equal(declared.ProductValue, SafeXml.Text(product, "vProd"));
            if (declared.Discount is not null) Assert.Equal(declared.Discount, SafeXml.Text(product, "vDesc"));
            var taxes = SafeXml.Child(details[index], "imposto");
            Assert.Single(SafeXml.Children(SafeXml.Child(taxes, "ICMS"), declared.Taxes.Icms.Group));
            Assert.Single(SafeXml.Children(SafeXml.Child(taxes, "PIS"), declared.Taxes.Pis.Group));
            Assert.Single(SafeXml.Children(SafeXml.Child(taxes, "COFINS"), declared.Taxes.Cofins.Group));
            var icms = SafeXml.Child(SafeXml.Child(taxes, "ICMS"), declared.Taxes.Icms.Group);
            Assert.Equal(declared.Taxes.Icms.Origin, SafeXml.Text(icms, "orig"));
            if (scenario == "crt3")
            {
                Assert.Equal(declared.Taxes.Icms.Base, SafeXml.Text(icms, "vBC"));
                Assert.Equal(declared.Taxes.Icms.Rate, SafeXml.Text(icms, "pICMS"));
                Assert.Equal(declared.Taxes.Icms.Value, SafeXml.Text(icms, "vICMS"));
                var pis = SafeXml.Child(SafeXml.Child(taxes, "PIS"), "PISAliq");
                Assert.Equal(declared.Taxes.Pis.Base, SafeXml.Text(pis, "vBC"));
                Assert.Equal(declared.Taxes.Pis.Rate, SafeXml.Text(pis, "pPIS"));
                Assert.Equal(declared.Taxes.Pis.Value, SafeXml.Text(pis, "vPIS"));
                var cofins = SafeXml.Child(SafeXml.Child(taxes, "COFINS"), "COFINSAliq");
                Assert.Equal(declared.Taxes.Cofins.Base, SafeXml.Text(cofins, "vBC"));
                Assert.Equal(declared.Taxes.Cofins.Rate, SafeXml.Text(cofins, "pCOFINS"));
                Assert.Equal(declared.Taxes.Cofins.Value, SafeXml.Text(cofins, "vCOFINS"));
                var rtc = SafeXml.Child(SafeXml.Child(taxes, "IBSCBS"), "gIBSCBS");
                Assert.Equal(declared.Taxes.IbsCbs!.Tax.Base, SafeXml.Text(rtc, "vBC"));
                Assert.Equal(declared.Taxes.IbsCbs.Tax.Uf.Rate, SafeXml.Text(SafeXml.Child(rtc, "gIBSUF"), "pIBSUF"));
                Assert.Equal(declared.Taxes.IbsCbs.Tax.Uf.Value, SafeXml.Text(SafeXml.Child(rtc, "gIBSUF"), "vIBSUF"));
                Assert.Equal(declared.Taxes.IbsCbs.Tax.Cbs.Value, SafeXml.Text(SafeXml.Child(rtc, "gCBS"), "vCBS"));
                Assert.Equal(declared.ItemTotal, SafeXml.Text(details[index], "vItem"));
            }
        }
        var total = SafeXml.Child(info, "total");
        var icmsTotals = SafeXml.Child(total, "ICMSTot");
        Assert.Equal(19, icmsTotals.ChildNodes.OfType<XmlElement>().Count());
        foreach (var property in typeof(PreparationIcmsTotals).GetProperties())
            Assert.Equal((string)property.GetValue(sale.Totals.Icms)!, SafeXml.Text(icmsTotals, property.Name));
        var payments = SafeXml.Child(info, "pag");
        var paymentDetails = SafeXml.Children(payments, "detPag");
        Assert.Equal(sale.Payments.Length, paymentDetails.Count);
        for (var index = 0; index < paymentDetails.Count; index++)
        {
            Assert.Equal(sale.Payments[index].Code, SafeXml.Text(paymentDetails[index], "tPag"));
            Assert.Equal(sale.Payments[index].Value, SafeXml.Text(paymentDetails[index], "vPag"));
        }
        Assert.Equal(sale.Change, SafeXml.Text(payments, "vTroco"));
        if (scenario == "crt3")
        {
            var rtcTotals = SafeXml.Child(total, "IBSCBSTot");
            Assert.Equal(sale.Totals.IbsCbs!.Base, SafeXml.Text(rtcTotals, "vBCIBSCBS"));
            Assert.Equal(sale.Totals.IbsCbs.Ibs.Value, SafeXml.Text(SafeXml.Child(rtcTotals, "gIBS"), "vIBS"));
            Assert.Equal(sale.Totals.IbsCbs.Cbs.Value, SafeXml.Text(SafeXml.Child(rtcTotals, "gCBS"), "vCBS"));
            Assert.Equal(sale.Totals.InvoiceTotal, SafeXml.Text(total, "vNFTot"));
            Assert.Equal("100.00", SafeXml.Text(total, "vNFTot")); // 2026: dado declarado, sem soma automática RTC.
        }
        Assert.Equal(sale.Items[0].Description, Models(scenario).Sale.Items[0].Description);
    }

    [Fact]
    public void ExplicitOffsetControlsKeyMonthWithoutUtcNormalization()
    {
        var (sale, emitter, context) = Models("crt1");
        sale = sale with { IssuedAt = "2026-10-31T23:59:59-03:00" };
        var prepared = NfcePreparation.BuildUnsigned(sale, emitter, context);
        Assert.StartsWith("532610", prepared.AccessKey.Value, StringComparison.Ordinal);
        Assert.Equal(sale.IssuedAt, SafeXml.Text(SafeXml.Child(Info(prepared.UnsignedXml), "ide"), "dhEmi"));
        proof.Validator.RequireValid("nfe", Sign(prepared));
    }

    [Theory]
    [InlineData("product-literal")]
    [InlineData("recipient-literal")]
    [InlineData("reversed-items")]
    [InlineData("missing-first-number")]
    public void HomologationLiteralsAndSequentialItemsAreRequiredWithoutSilentOverrides(string scenario)
    {
        var json = JsonNode.Parse(Sample("sale-crt1"))!.AsObject();
        switch (scenario)
        {
            case "product-literal": json["items"]![0]!["xProd"] = "PRODUTO DIFERENTE"; break;
            case "recipient-literal": json["recipient"]!["name"] = "NOME DIFERENTE"; break;
            case "reversed-items":
                json["items"]![0]!["nItem"] = "2";
                json["items"]![1]!["nItem"] = "1";
                break;
            case "missing-first-number":
                json["items"]![0]!["nItem"] = "2";
                json["items"]![1]!["nItem"] = "3";
                break;
        }
        Assert.Throws<InvalidDataException>(() => PreparationJson.ParseSale(json.ToJsonString()));
    }

    [Fact]
    public void DeclaredDecimalsArePreservedWithoutFixtureMultiplicationEquality()
    {
        var (sale, emitter, context) = Models("crt1");
        var items = sale.Items.ToArray();
        items[0] = items[0] with { CommercialQuantity = "3.0000", CommercialUnitPrice = "23.4166666667" };
        sale = sale with { Items = items };
        var prepared = NfcePreparation.BuildUnsigned(sale, emitter, context);
        var product = SafeXml.Child(SafeXml.Children(Info(prepared.UnsignedXml), "det")[0], "prod");
        Assert.Equal("70.25", SafeXml.Text(product, "vProd"));
        Assert.Equal("23.4166666667", SafeXml.Text(product, "vUnCom"));
        proof.Validator.RequireValid("nfe", Sign(prepared));
        // Prova estrutural; equações/tolerâncias MOC/NT exigem futura validação fiscal.
    }

    [Fact]
    public void BoundaryPrecisionsAreAcceptedByPreparationAndPinnedSchema()
    {
        var (sale, emitter, context) = Models("crt1");
        var items = sale.Items.ToArray();
        items[0] = items[0] with
        {
            CommercialQuantity = "99999999999.9999", TaxableQuantity = "99999999999.9999",
            CommercialUnitPrice = "99999999999.9999999999", TaxableUnitPrice = "99999999999.9999999999",
            ProductValue = "9999999999999.99"
        };
        var prepared = NfcePreparation.BuildUnsigned(sale with { Items = items }, emitter, context);
        proof.Validator.RequireValid("nfe", Sign(prepared));
        Assert.Contains("<vProd>9999999999999.99</vProd>", prepared.UnsignedXml, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("missing-version")]
    [InlineData("unknown-root")]
    [InlineData("unknown-item")]
    [InlineData("unknown-tax")]
    [InlineData("missing-tax")]
    [InlineData("missing-money")]
    [InlineData("null")]
    [InlineData("numeric-code")]
    [InlineData("numeric-decimal")]
    [InlineData("unknown-rtc")]
    [InlineData("unsupported-class")]
    [InlineData("extra-nt-field")]
    public void StrictJsonRejectsMissingUnknownWrongTypesAndUnmappedFields(string scenario)
    {
        var json = JsonNode.Parse(Sample(scenario is "unknown-rtc" or "unsupported-class" ? "sale-crt3" : "sale-crt1"))!.AsObject();
        var item = json["items"]![0]!.AsObject();
        switch (scenario)
        {
            case "missing-version": json.Remove("version"); break;
            case "unknown-root": json["transportEndpoint"] = "https://example.invalid"; break;
            case "unknown-item": item["invented"] = "field"; break;
            case "unknown-tax": item["taxes"]!["IS"] = new JsonObject(); break;
            case "missing-tax": item["taxes"]!.AsObject().Remove("ICMS"); break;
            case "missing-money": json["totals"]!["ICMSTot"]!.AsObject().Remove("vICMSDeson"); break;
            case "null": json["reference"] = null; break;
            case "numeric-code": item["CFOP"] = 5102; break;
            case "numeric-decimal": item["qCom"] = 2.0000; break;
            case "unknown-rtc": item["taxes"]!["IBSCBS"]!["gIBSCBS"]!["gDif"] = new JsonObject(); break;
            case "unsupported-class": item["taxes"]!["IBSCBS"]!["cClassTrib"] = "200001"; break;
            case "extra-nt-field": item["taxes"]!["PIS"]!["vBC"] = "70.25"; break;
        }
        Assert.Throws<InvalidDataException>(() => PreparationJson.ParseSale(json.ToJsonString()));
    }

    [Theory]
    [InlineData("qCom", "1.00001")]
    [InlineData("qTrib", "100000000000")]
    [InlineData("vUnCom", "0.00000000001")]
    [InlineData("vUnTrib", "100000000000")]
    [InlineData("vProd", "70.251")]
    [InlineData("vProd", "70,25")]
    [InlineData("vProd", "7.025e1")]
    [InlineData("vProd", "70.25\n")]
    [InlineData("qCom", "2.0000\n")]
    [InlineData("vDesc", "0.00")]
    [InlineData("nItem", "991")]
    public void InvalidRepresentationAndPrecisionAreRejected(string field, string value)
    {
        var json = JsonNode.Parse(Sample("sale-crt1"))!.AsObject();
        json["items"]![0]![field] = value;
        Assert.Throws<InvalidDataException>(() => PreparationJson.ParseSale(json.ToJsonString()));
    }

    [Theory]
    [InlineData("2026-10-05T10:11:12Z")]
    [InlineData("2026-10-05T10:11:12")]
    [InlineData("2026-10-05T10:11:12.123-03:00")]
    [InlineData("2026-10-05T10:11:12-03:30")]
    [InlineData("2026-02-30T10:11:12-03:00")]
    public void DateRequiresAnExplicitOffsetAndOfficialLexicalForm(string date)
    {
        var json = JsonNode.Parse(Sample("sale-crt1"))!.AsObject();
        json["issuedAt"] = date;
        Assert.Throws<InvalidDataException>(() => PreparationJson.ParseSale(json.ToJsonString()));
    }

    [Theory]
    [InlineData("ICMS", "ICMS20")]
    [InlineData("PIS", "PISQtde")]
    [InlineData("COFINS", "COFINSOutr")]
    public void UnsupportedTaxGroupsAreRejectedRatherThanDropped(string tax, string group)
    {
        var json = JsonNode.Parse(Sample("sale-crt1"))!.AsObject();
        json["items"]![0]!["taxes"]![tax]!["group"] = group;
        Assert.Throws<InvalidDataException>(() => PreparationJson.ParseSale(json.ToJsonString()));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    public void NfceRecipientStateRegistrationIndicatorMustBeExplicitNine(string indicator)
    {
        var json = JsonNode.Parse(Sample("sale-crt3"))!.AsObject();
        json["recipient"]!["indIEDest"] = indicator;
        Assert.Throws<InvalidDataException>(() => PreparationJson.ParseSale(json.ToJsonString()));
    }

    [Fact]
    public void DuplicatePropertiesAndOtherContractVersionsAreRejected()
    {
        var json = Sample("sale-crt1");
        Assert.Throws<InvalidDataException>(() => PreparationJson.ParseSale(json.Replace("\"version\":",
            "\"version\": \"nfce-preparation/0.1\", \"version\":", StringComparison.Ordinal)));
        Assert.Throws<InvalidDataException>(() => PreparationJson.ParseSale(json.Replace(
            PreparationVersion.Current, "nfce-df-homologacao/0.2", StringComparison.Ordinal)));
        Assert.Throws<InvalidDataException>(() => PreparationJson.ParseSale("[]"));
        Assert.Throws<InvalidDataException>(() => PreparationJson.ParseSale(json + "// comment"));
    }

    [Fact]
    public void ServerProfileRequiresEmitterPostalCodeSupportedCrtAndMatchingIdentity()
    {
        var json = JsonNode.Parse(Sample("emitter-crt1"))!.AsObject();
        json["address"]!.AsObject().Remove("postalCode");
        Assert.Throws<InvalidDataException>(() => PreparationJson.ParseEmitter(json.ToJsonString()));
        foreach (var crt in new[] { "2", "4" })
        {
            json = JsonNode.Parse(Sample("emitter-crt1"))!.AsObject();
            json["crt"] = crt;
            Assert.Throws<InvalidDataException>(() => PreparationJson.ParseEmitter(json.ToJsonString()));
        }
        var (sale, emitter, context) = Models("crt1");
        Assert.Throws<InvalidDataException>(() => NfcePreparation.BuildUnsigned(
            sale with { EmitterProfileId = "other" }, emitter, context));
    }

    [Fact]
    public void CrtAndTaxGroupsCannotBeSubstitutedAndMissingRtcNeverBecomesZero()
    {
        var (sale1, emitter1, context) = Models("crt1");
        var (sale3, emitter3, _) = Models("crt3");
        Assert.Throws<InvalidDataException>(() => NfcePreparation.BuildUnsigned(
            sale1 with { EmitterProfileId = emitter3.ProfileId }, emitter3, context));
        Assert.Throws<InvalidDataException>(() => NfcePreparation.BuildUnsigned(
            sale3 with { EmitterProfileId = emitter1.ProfileId }, emitter1, context));
        var item = sale3.Items[0];
        var withoutRtc = sale3 with { Items = [item with { Taxes = item.Taxes with { IbsCbs = null } }] };
        Assert.Throws<InvalidDataException>(() => NfcePreparation.BuildUnsigned(withoutRtc, emitter3, context));
        Assert.Throws<InvalidDataException>(() => NfcePreparation.BuildUnsigned(
            sale3 with { Totals = sale3.Totals with { InvoiceTotal = null } }, emitter3, context));
        Assert.Throws<InvalidDataException>(() => NfcePreparation.BuildUnsigned(
            sale3 with { Items = [item with { ItemTotal = null }] }, emitter3, context));
    }

    [Fact]
    public void ContextRejectsMissingComponentsProductionIntentAndInvalidCodeTypes()
    {
        var json = JsonNode.Parse(Sample("test-context"))!.AsObject();
        json["syntheticOnly"] = false;
        Assert.Throws<InvalidDataException>(() => PreparationJson.ParseContext(json.ToJsonString()));
        json = JsonNode.Parse(Sample("test-context"))!.AsObject();
        json.Remove("number");
        Assert.Throws<InvalidDataException>(() => PreparationJson.ParseContext(json.ToJsonString()));
        json = JsonNode.Parse(Sample("test-context"))!.AsObject();
        json["numericCode"] = 87654321;
        Assert.Throws<InvalidDataException>(() => PreparationJson.ParseContext(json.ToJsonString()));
        json = JsonNode.Parse(Sample("test-context"))!.AsObject();
        json["endpoint"] = "https://example.invalid";
        Assert.Throws<InvalidDataException>(() => PreparationJson.ParseContext(json.ToJsonString()));
    }

    [Fact]
    public void RecipientMayBeOmittedAndCreditIntentIsNeverInferredFromCnpj()
    {
        var (sale, emitter, context) = Models("crt3");
        Assert.Equal("unknown", sale.Recipient!.CreditIntent);
        var prepared = NfcePreparation.BuildUnsigned(sale with { Recipient = null }, emitter, context);
        Assert.Empty(SafeXml.Children(Info(prepared.UnsignedXml), "dest"));
        proof.Validator.RequireValid("nfe", Sign(prepared));
    }

    [Fact]
    public void XmlEscapesDeclaredTextAndInvalidRequiredTypedModelsFailBeforeWriting()
    {
        var (sale, emitter, context) = Models("crt1");
        var prepared = NfcePreparation.BuildUnsigned(sale, emitter with { Name = "SINTETICO & LABORATORIO <TESTE>" }, context);
        Assert.Contains("&amp;", prepared.UnsignedXml, StringComparison.Ordinal);
        Assert.Equal("SINTETICO & LABORATORIO <TESTE>", SafeXml.Text(SafeXml.Child(Info(prepared.UnsignedXml), "emit"), "xNome"));
        Assert.Throws<InvalidDataException>(() => NfcePreparation.BuildUnsigned(sale with { Operation = null! }, emitter, context));
    }

    [Fact]
    public void StRetainedDeclaredFieldsAndCestAreSerializedInXsdOrderWithoutRecalculation()
    {
        var (sale, emitter, context) = Models("st-crt1");
        var prepared = NfcePreparation.BuildUnsigned(sale, emitter, context);
        var detail = SafeXml.Children(Info(prepared.UnsignedXml), "det")[0];
        Assert.Equal(sale.Items[0].Cest, SafeXml.Text(SafeXml.Child(detail, "prod"), "CEST"));
        var icms = SafeXml.Child(SafeXml.Child(SafeXml.Child(detail, "imposto"), "ICMS"), "ICMSSN500");
        Assert.Equal(new[]
        {
            "orig", "CSOSN", "vBCSTRet", "pST", "vICMSSubstituto", "vICMSSTRet",
            "vBCFCPSTRet", "pFCPSTRet", "vFCPSTRet", "pRedBCEfet", "vBCEfet", "pICMSEfet", "vICMSEfet"
        }, icms.ChildNodes.OfType<XmlElement>().Select(element => element.LocalName).ToArray());
        var declared = sale.Items[0].Taxes.Icms;
        foreach (var (value, name) in new (string?, string)[]
        {
            (declared.StRetainedBase, "vBCSTRet"), (declared.FinalConsumerRate, "pST"),
            (declared.SubstituteValue, "vICMSSubstituto"), (declared.StRetainedValue, "vICMSSTRet"),
            (declared.FcpStRetainedBase, "vBCFCPSTRet"), (declared.FcpStRetainedRate, "pFCPSTRet"),
            (declared.FcpStRetainedValue, "vFCPSTRet"), (declared.EffectiveBaseReductionRate, "pRedBCEfet"),
            (declared.EffectiveBase, "vBCEfet"), (declared.EffectiveRate, "pICMSEfet"),
            (declared.EffectiveValue, "vICMSEfet")
        }) Assert.Equal(value, SafeXml.Text(icms, name));
        // Valores declarados preservados; nenhuma soma, MVA ou alíquota é calculada.
    }

    [Fact]
    public void Icms60DeclaredStFieldsPrepareSignAndValidateAgainstPinnedXsd()
    {
        var sale = PreparationJson.ParseSale(Icms60Sale().ToJsonString());
        var (_, emitter, context) = Models("crt3");
        var prepared = NfcePreparation.BuildUnsigned(sale, emitter, context);
        var icms = SafeXml.Child(SafeXml.Child(SafeXml.Child(
            SafeXml.Children(Info(prepared.UnsignedXml), "det")[0], "imposto"), "ICMS"), "ICMS60");
        Assert.Equal("60", SafeXml.Text(icms, "CST"));
        Assert.Equal("90.00", SafeXml.Text(icms, "vBCSTRet"));
        Assert.Equal("20.00", SafeXml.Text(icms, "pST"));
        Assert.Equal("18.00", SafeXml.Text(icms, "vICMSSTRet"));
        proof.Validator.RequireValid("nfe", Sign(prepared));
    }

    [Fact]
    public void StRetainedSequencesMayBeEntirelyOmittedAndStillValidateAgainstPinnedXsd()
    {
        var json = JsonNode.Parse(Sample("sale-st-crt1"))!.AsObject();
        var icms = json["items"]![0]!["taxes"]!["ICMS"]!.AsObject();
        foreach (var field in new[]
        {
            "vBCSTRet", "pST", "vICMSSubstituto", "vICMSSTRet", "vBCFCPSTRet", "pFCPSTRet",
            "vFCPSTRet", "pRedBCEfet", "vBCEfet", "pICMSEfet", "vICMSEfet"
        }) icms.Remove(field);
        var sale = PreparationJson.ParseSale(json.ToJsonString());
        var (_, emitter, context) = Models("st-crt1");
        proof.Validator.RequireValid("nfe", Sign(NfcePreparation.BuildUnsigned(sale, emitter, context)));
    }

    [Fact]
    public void StGroupsRequireTheMatchingCrtWithoutSubstitution()
    {
        var sale60 = PreparationJson.ParseSale(Icms60Sale().ToJsonString());
        var (saleSn500, _, context) = Models("st-crt1");
        var (_, emitter1, _) = Models("crt1");
        var (_, emitter3, _) = Models("crt3");
        Assert.Throws<InvalidDataException>(() => NfcePreparation.BuildUnsigned(
            sale60 with { EmitterProfileId = emitter1.ProfileId }, emitter1, context));
        Assert.Throws<InvalidDataException>(() => NfcePreparation.BuildUnsigned(
            saleSn500 with { EmitterProfileId = emitter3.ProfileId }, emitter3, context));
    }

    [Theory]
    [InlineData("missing-cest")]
    [InlineData("cst-in-sn500")]
    [InlineData("wrong-csosn")]
    [InlineData("partial-retention")]
    [InlineData("substitute-alone")]
    [InlineData("partial-fcp")]
    [InlineData("partial-effective")]
    [InlineData("zero-pst")]
    [InlineData("one-decimal-pst")]
    [InlineData("comma-decimal")]
    [InlineData("modbc-in-sn500")]
    [InlineData("st-field-in-sn102")]
    public void StRetainedRepresentationViolationsAreRejected(string scenario)
    {
        var json = JsonNode.Parse(Sample(scenario == "st-field-in-sn102" ? "sale-crt1" : "sale-st-crt1"))!.AsObject();
        var icms = json["items"]![0]!["taxes"]!["ICMS"]!.AsObject();
        switch (scenario)
        {
            case "missing-cest": json["items"]![0]!.AsObject().Remove("CEST"); break;
            case "cst-in-sn500": icms["CST"] = "60"; break;
            case "wrong-csosn": icms["CSOSN"] = "102"; break;
            case "partial-retention": icms.Remove("vICMSSTRet"); break;
            case "substitute-alone":
                icms.Remove("vBCSTRet");
                icms.Remove("pST");
                icms.Remove("vICMSSTRet");
                break;
            case "partial-fcp": icms.Remove("vFCPSTRet"); break;
            case "partial-effective": icms.Remove("vICMSEfet"); break;
            case "zero-pst": icms["pST"] = "0"; break;
            case "one-decimal-pst": icms["pST"] = "18.0"; break;
            case "comma-decimal": icms["vBCSTRet"] = "40,00"; break;
            case "modbc-in-sn500": icms["modBC"] = "0"; break;
            case "st-field-in-sn102": icms["vBCSTRet"] = "40.00"; break;
        }
        Assert.Throws<InvalidDataException>(() => PreparationJson.ParseSale(json.ToJsonString()));
    }

    [Theory]
    [InlineData("40")]
    [InlineData("41")]
    public void Icms40DeclaredCstPreparesSignsAndValidatesAgainstPinnedXsd(string cst)
    {
        var sale = PreparationJson.ParseSale(Icms40Sale(cst).ToJsonString());
        var (_, emitter, context) = Models("crt3");
        var prepared = NfcePreparation.BuildUnsigned(sale, emitter, context);
        var icms = SafeXml.Child(SafeXml.Child(SafeXml.Child(
            SafeXml.Children(Info(prepared.UnsignedXml), "det")[0], "imposto"), "ICMS"), "ICMS40");
        Assert.Equal(new[] { "orig", "CST" }, icms.ChildNodes.OfType<XmlElement>()
            .Select(element => element.LocalName).ToArray());
        Assert.Equal(cst, SafeXml.Text(icms, "CST"));
        proof.Validator.RequireValid("nfe", Sign(prepared));
        // Valores declarados preservados; desoneração fica fora do recorte P2.
    }

    [Fact]
    public void ExemptDeclaredCsosnCodesAreSerializedWithoutExtraFields()
    {
        var (sale, emitter, context) = Models("exempt-crt1");
        var prepared = NfcePreparation.BuildUnsigned(sale, emitter, context);
        var details = SafeXml.Children(Info(prepared.UnsignedXml), "det");
        Assert.Equal(new[] { "103", "300", "400" }, details.Select(detail => SafeXml.Text(
            SafeXml.Child(SafeXml.Child(SafeXml.Child(detail, "imposto"), "ICMS"), "ICMSSN102"), "CSOSN")).ToArray());
        foreach (var detail in details)
        {
            var icms = SafeXml.Child(SafeXml.Child(SafeXml.Child(detail, "imposto"), "ICMS"), "ICMSSN102");
            Assert.Equal(new[] { "orig", "CSOSN" }, icms.ChildNodes.OfType<XmlElement>()
                .Select(element => element.LocalName).ToArray());
        }
    }

    [Theory]
    [InlineData("csosn-500-in-sn102")]
    [InlineData("cst-in-sn102")]
    [InlineData("cst50-in-icms40")]
    [InlineData("csosn-in-icms40")]
    [InlineData("own-tax-in-icms40")]
    [InlineData("st-field-in-icms40")]
    [InlineData("deson-in-icms40")]
    public void ExemptRepresentationViolationsAreRejected(string scenario)
    {
        var json = scenario.EndsWith("icms40", StringComparison.Ordinal)
            ? Icms40Sale("40")
            : JsonNode.Parse(Sample("sale-crt1"))!.AsObject();
        var icms = json["items"]![0]!["taxes"]!["ICMS"]!.AsObject();
        switch (scenario)
        {
            case "csosn-500-in-sn102": icms["CSOSN"] = "500"; break;
            case "cst-in-sn102": icms["CST"] = "40"; break;
            case "cst50-in-icms40": icms["CST"] = "50"; break;
            case "csosn-in-icms40": icms["CSOSN"] = "400"; break;
            case "own-tax-in-icms40": icms["vBC"] = "10.00"; break;
            case "st-field-in-icms40": icms["vBCSTRet"] = "10.00"; break;
            case "deson-in-icms40": icms["vICMSDeson"] = "1.00"; break;
        }
        Assert.Throws<InvalidDataException>(() => PreparationJson.ParseSale(json.ToJsonString()));
    }

    [Fact]
    public void ExemptGroupsRequireTheMatchingCrtWithoutSubstitution()
    {
        var sale40 = PreparationJson.ParseSale(Icms40Sale("40").ToJsonString());
        var (saleExempt, _, context) = Models("exempt-crt1");
        var (_, emitter1, _) = Models("crt1");
        var (_, emitter3, _) = Models("crt3");
        Assert.Throws<InvalidDataException>(() => NfcePreparation.BuildUnsigned(
            sale40 with { EmitterProfileId = emitter1.ProfileId }, emitter1, context));
        Assert.Throws<InvalidDataException>(() => NfcePreparation.BuildUnsigned(
            saleExempt with { EmitterProfileId = emitter3.ProfileId }, emitter3, context));
    }

    [Fact]
    public void Icms20DeclaredReductionPreparesSignsAndValidatesAgainstPinnedXsd()
    {
        var (sale, emitter, context) = Models("red-crt3");
        var prepared = NfcePreparation.BuildUnsigned(sale, emitter, context);
        var icms = SafeXml.Child(SafeXml.Child(SafeXml.Child(
            SafeXml.Children(Info(prepared.UnsignedXml), "det")[0], "imposto"), "ICMS"), "ICMS20");
        Assert.Equal(new[] { "orig", "CST", "modBC", "pRedBC", "vBC", "pICMS", "vICMS" },
            icms.ChildNodes.OfType<XmlElement>().Select(element => element.LocalName).ToArray());
        var declared = sale.Items[0].Taxes.Icms;
        Assert.Equal(declared.BaseReductionRate, SafeXml.Text(icms, "pRedBC"));
        Assert.Equal(declared.Base, SafeXml.Text(icms, "vBC"));
        Assert.Equal(declared.Rate, SafeXml.Text(icms, "pICMS"));
        Assert.Equal(declared.Value, SafeXml.Text(icms, "vICMS"));
        proof.Validator.RequireValid("nfe", Sign(prepared));
        // Valores declarados preservados; FCP e desoneração ficam fora do recorte P3.
    }

    [Theory]
    [InlineData("csosn-in-icms20")]
    [InlineData("wrong-cst-in-icms20")]
    [InlineData("missing-predbc")]
    [InlineData("missing-modbc")]
    [InlineData("one-decimal-predbc")]
    [InlineData("comma-decimal-predbc")]
    [InlineData("st-field-in-icms20")]
    [InlineData("fcp-in-icms20")]
    [InlineData("predbc-in-icms00")]
    [InlineData("predbc-in-icms40")]
    public void Icms20RepresentationViolationsAreRejected(string scenario)
    {
        var json = scenario switch
        {
            "predbc-in-icms00" => JsonNode.Parse(Sample("sale-crt3"))!.AsObject(),
            "predbc-in-icms40" => Icms40Sale("40"),
            _ => JsonNode.Parse(Sample("sale-red-crt3"))!.AsObject()
        };
        var icms = json["items"]![0]!["taxes"]!["ICMS"]!.AsObject();
        switch (scenario)
        {
            case "csosn-in-icms20": icms["CSOSN"] = "102"; break;
            case "wrong-cst-in-icms20": icms["CST"] = "00"; break;
            case "missing-predbc": icms.Remove("pRedBC"); break;
            case "missing-modbc": icms.Remove("modBC"); break;
            case "one-decimal-predbc": icms["pRedBC"] = "41.6"; break;
            case "comma-decimal-predbc": icms["pRedBC"] = "41,67"; break;
            case "st-field-in-icms20": icms["vBCSTRet"] = "10.00"; break;
            case "fcp-in-icms20": icms["vBCFCP"] = "10.00"; break;
            case "predbc-in-icms00": icms["pRedBC"] = "10.0000"; break;
            case "predbc-in-icms40": icms["pRedBC"] = "10.0000"; break;
        }
        Assert.Throws<InvalidDataException>(() => PreparationJson.ParseSale(json.ToJsonString()));
    }

    [Fact]
    public void Icms20RequiresCrt3WithoutSubstitution()
    {
        var (saleRed, _, context) = Models("red-crt3");
        var (_, emitter1, _) = Models("crt1");
        Assert.Throws<InvalidDataException>(() => NfcePreparation.BuildUnsigned(
            saleRed with { EmitterProfileId = emitter1.ProfileId }, emitter1, context));
    }

    [Fact]
    public void BasketRtcDeclaredReductionsPrepareSignAndValidateAgainstPinnedXsd()
    {
        var (sale, emitter, context) = Models("basket-crt3");
        var prepared = NfcePreparation.BuildUnsigned(sale, emitter, context);
        var ibsCbs = SafeXml.Child(SafeXml.Child(
            SafeXml.Children(Info(prepared.UnsignedXml), "det")[0], "imposto"), "IBSCBS");
        Assert.Equal("200", SafeXml.Text(ibsCbs, "CST"));
        Assert.Equal("200003", SafeXml.Text(ibsCbs, "cClassTrib"));
        var rtc = SafeXml.Child(ibsCbs, "gIBSCBS");
        var declared = sale.Items[0].Taxes.IbsCbs!.Tax;
        foreach (var (group, rate, value, reduction) in new[]
        {
            ("gIBSUF", "pIBSUF", "vIBSUF", declared.Uf.Reduction),
            ("gIBSMun", "pIBSMun", "vIBSMun", declared.Municipality.Reduction),
            ("gCBS", "pCBS", "vCBS", declared.Cbs.Reduction)
        })
        {
            var element = SafeXml.Child(rtc, group);
            Assert.Equal(new[] { rate, "gRed", value }, element.ChildNodes.OfType<XmlElement>()
                .Select(child => child.LocalName).ToArray());
            var red = SafeXml.Child(element, "gRed");
            Assert.Equal(new[] { "pRedAliq", "pAliqEfet" }, red.ChildNodes.OfType<XmlElement>()
                .Select(child => child.LocalName).ToArray());
            Assert.Equal(reduction!.Rate, SafeXml.Text(red, "pRedAliq"));
            Assert.Equal(reduction.EffectiveRate, SafeXml.Text(red, "pAliqEfet"));
        }
        proof.Validator.RequireValid("nfe", Sign(prepared));
        // Valores declarados preservados; percentuais de redução são hipóteses de teste.
    }

    [Theory]
    [InlineData("cst-000-with-basket-class")]
    [InlineData("class-000001-with-cst-200")]
    [InlineData("unmapped-basket-class")]
    [InlineData("missing-red-uf")]
    [InlineData("missing-red-mun")]
    [InlineData("missing-red-cbs")]
    [InlineData("partial-red")]
    [InlineData("partial-red-effective-only")]
    [InlineData("red-in-regular")]
    [InlineData("comma-decimal-red")]
    [InlineData("five-decimals-red")]
    [InlineData("unknown-red-field")]
    public void RtcPairRepresentationViolationsAreRejected(string scenario)
    {
        var json = JsonNode.Parse(Sample(scenario == "red-in-regular" ? "sale-crt3" : "sale-basket-crt3"))!.AsObject();
        var ibsCbs = json["items"]![0]!["taxes"]!["IBSCBS"]!.AsObject();
        var group = ibsCbs["gIBSCBS"]!.AsObject();
        switch (scenario)
        {
            case "cst-000-with-basket-class": ibsCbs["CST"] = "000"; break;
            case "class-000001-with-cst-200": ibsCbs["cClassTrib"] = "000001"; break;
            case "unmapped-basket-class": ibsCbs["cClassTrib"] = "200001"; break;
            case "missing-red-uf": group["gIBSUF"]!.AsObject().Remove("gRed"); break;
            case "missing-red-mun": group["gIBSMun"]!.AsObject().Remove("gRed"); break;
            case "missing-red-cbs": group["gCBS"]!.AsObject().Remove("gRed"); break;
            case "partial-red": group["gIBSUF"]!["gRed"]!.AsObject().Remove("pAliqEfet"); break;
            case "partial-red-effective-only": group["gCBS"]!["gRed"]!.AsObject().Remove("pRedAliq"); break;
            case "red-in-regular":
                group["gIBSUF"]!["gRed"] = new JsonObject { ["pRedAliq"] = "100.00", ["pAliqEfet"] = "0.00" };
                break;
            case "comma-decimal-red": group["gIBSUF"]!["gRed"]!["pRedAliq"] = "100,00"; break;
            case "five-decimals-red": group["gCBS"]!["gRed"]!["pAliqEfet"] = "0.00000"; break;
            case "unknown-red-field": group["gCBS"]!["gRed"]!["pRed"] = "1.00"; break;
        }
        Assert.Throws<InvalidDataException>(() => PreparationJson.ParseSale(json.ToJsonString()));
    }

    [Fact]
    public void RegularRtcPairStillSerializesWithoutReduction()
    {
        var (sale, emitter, context) = Models("crt3");
        var prepared = NfcePreparation.BuildUnsigned(sale, emitter, context);
        Assert.DoesNotContain("<gRed", prepared.UnsignedXml, StringComparison.Ordinal);
    }

    [Fact]
    public void BasketRtcRequiresCrt3WithoutSubstitution()
    {
        var (saleBasket, _, context) = Models("basket-crt3");
        var (_, emitter1, _) = Models("crt1");
        Assert.Throws<InvalidDataException>(() => NfcePreparation.BuildUnsigned(
            saleBasket with { EmitterProfileId = emitter1.ProfileId }, emitter1, context));
    }

    private static JsonObject Icms40Sale(string cst)
    {
        var json = JsonNode.Parse(Sample("sale-crt3"))!.AsObject();
        var icms = json["items"]![0]!["taxes"]!["ICMS"]!.AsObject();
        icms["group"] = "ICMS40";
        icms["CST"] = cst;
        foreach (var field in new[] { "modBC", "vBC", "pICMS", "vICMS" }) icms.Remove(field);
        return json;
    }

    private static JsonObject Icms60Sale()
    {
        var json = JsonNode.Parse(Sample("sale-crt3"))!.AsObject();
        var icms = json["items"]![0]!["taxes"]!["ICMS"]!.AsObject();
        icms["group"] = "ICMS60";
        icms["CST"] = "60";
        foreach (var field in new[] { "modBC", "vBC", "pICMS", "vICMS" }) icms.Remove(field);
        icms["vBCSTRet"] = "90.00";
        icms["pST"] = "20.00";
        icms["vICMSSTRet"] = "18.00";
        json["items"]![0]!["CEST"] = "0302103";
        return json;
    }

    private string Sign(PreparedNfce prepared) =>
        A1Signer.Sign(QrCode3.AddToInvoice(prepared.UnsignedXml), proof.Certificate);

    private static XmlElement Info(string xml) => SafeXml.Child(SafeXml.Load(xml).DocumentElement!, "infNFe");

    private static string Sample(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "preparation", name + ".json"));

    private static (PreparationSale Sale, PreparationEmitterProfile Emitter, PreparationTestContext Context) Models(string scenario) =>
        (PreparationJson.ParseSale(Sample("sale-" + scenario)),
         PreparationJson.ParseEmitter(Sample("emitter-" + scenario switch
         {
             "st-crt1" or "exempt-crt1" => "crt1",
             "red-crt3" or "basket-crt3" => "crt3",
             _ => scenario
         })),
         PreparationJson.ParseContext(Sample("test-context")));
}
