using System.Text.Json.Nodes;
using EmissorFiscal.Core.Danfe;
using EmissorFiscal.Core.Preparation;
using EmissorFiscal.Core.Protocols;
using EmissorFiscal.Core.QrCode;
using EmissorFiscal.Core.Signature;
using EmissorFiscal.Core.Xml;
using EmissorFiscal.Samples;
using Xunit;

namespace EmissorFiscal.Core.Tests;

public sealed class DanfeTests(PreparationProof proof) : IClassFixture<PreparationProof>
{
    public static TheoryData<string> Scenarios => ["crt1", "crt3", "st-crt1", "exempt-crt1", "red-crt3", "basket-crt3"];

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void TextMatchesSnapshotForEveryDeclaredModel(string scenario)
    {
        var danfe = DanfeRenderer.Render(Processed(scenario), proof.Validator);
        var expected = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "danfe", scenario + ".txt"))
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        Assert.Equal(expected, danfe.Text);
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void TextFitsEightyMillimetreColumnsAndCarriesMandatoryContent(string scenario)
    {
        var (sale, emitter, _) = Models(scenario);
        var processed = Processed(scenario);
        var danfe = DanfeRenderer.Render(processed, proof.Validator);
        var lines = danfe.Text.TrimEnd('\n').Split('\n');
        Assert.All(lines, line => Assert.InRange(line.Length, 0, DanfeText.Columns));
        Assert.All(lines, line => Assert.DoesNotContain('\r', line));
        Assert.Contains(DanfeText.HomologationBanner[..34], danfe.Text, StringComparison.Ordinal);
        Assert.Contains("SEM VALOR", danfe.Text, StringComparison.Ordinal);
        // Primeiro item carrega a descrição de homologação exigida pela regra do preparador.
        Assert.Equal(NfcePreparation.HomologationProduct, danfe.Model.Items[0].Description);
        Assert.Contains("001 " + sale.Items[0].Code + " NOTA FISCAL EMITIDA", danfe.Text, StringComparison.Ordinal);
        Assert.Contains("Documento Auxiliar da Nota Fiscal", danfe.Text, StringComparison.Ordinal);
        Assert.Contains("Nao permite aproveitamento de credito de ICMS", danfe.Text, StringComparison.Ordinal);
        Assert.Contains(emitter.Name, danfe.Text, StringComparison.Ordinal);
        Assert.Contains("Consulte pela Chave de Acesso em\n" + QrCode3.DfKeyConsultation + "\n", danfe.Text, StringComparison.Ordinal);
        Assert.Contains(danfe.Model.FormattedKey[..29] + "\n" + danfe.Model.FormattedKey[30..], danfe.Text, StringComparison.Ordinal);
        Assert.Contains("Protocolo de Autorizacao: " + OfflineFixtures.Protocol17, danfe.Text, StringComparison.Ordinal);
        Assert.Contains("Data de Autorizacao: 04/10/2026 12:00:01", danfe.Text, StringComparison.Ordinal);
        Assert.Contains("CONSUMIDOR - " + sale.Recipient!.DocumentType + " ", danfe.Text, StringComparison.Ordinal);
        Assert.Contains("QTD. TOTAL DE ITENS", danfe.Text, StringComparison.Ordinal);
        Assert.Contains("VALOR A PAGAR R$", danfe.Text, StringComparison.Ordinal);
        Assert.Contains("Dinheiro", danfe.Text, StringComparison.Ordinal);
        Assert.Contains(QrCode3.Online(danfe.Model.Key), danfe.Text.Replace("\n", "", StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.DoesNotContain("cStat", danfe.Text, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void ModelIsBoundToKeyEnvironmentProtocolAndDeclaredValuesOfTheSameProcessedXml(string scenario)
    {
        var (sale, emitter, _) = Models(scenario);
        var processed = Processed(scenario);
        var signature = A1Signer.Verify(processed);
        var model = DanfeModel.FromProcessed(processed, proof.Validator);
        var info = SafeXml.Child(SafeXml.Child(SafeXml.Load(processed).DocumentElement!, "NFe"), "infNFe");
        Assert.Equal(signature.Key, model.Key);
        Assert.Equal("2", model.Environment);
        Assert.True(model.IsHomologation);
        Assert.Equal(FiscalState.Authorized, model.State);
        Assert.Empty(model.Alerts);
        Assert.Equal(OfflineFixtures.Protocol17, model.Protocol.Number);
        Assert.Equal("100", model.Protocol.Code);
        Assert.Equal("2026-10-04T12:00:01-03:00", model.Protocol.ReceivedAt);
        Assert.Equal("123", model.Number);
        Assert.Equal(emitter.Series, model.Series);
        Assert.Equal(sale.IssuedAt, model.IssuedAt);
        Assert.Equal(emitter.Cnpj, model.Emitter.Document);
        Assert.Equal(emitter.Name, model.Emitter.Name);
        Assert.Equal(emitter.StateRegistration, model.Emitter.StateRegistration);
        Assert.Equal(sale.Items.Length, model.Items.Count);
        Assert.Equal(sale.Items.Length, model.Totals.ItemCount);
        var details = SafeXml.Children(info, "det");
        for (var index = 0; index < details.Count; index++)
        {
            var product = SafeXml.Child(details[index], "prod");
            Assert.Equal(SafeXml.Text(product, "cProd"), model.Items[index].Code);
            Assert.Equal(SafeXml.Text(product, "xProd"), model.Items[index].Description);
            Assert.Equal(SafeXml.Text(product, "qCom"), model.Items[index].Quantity);
            Assert.Equal(SafeXml.Text(product, "uCom"), model.Items[index].Unit);
            Assert.Equal(SafeXml.Text(product, "vUnCom"), model.Items[index].UnitPrice);
            Assert.Equal(SafeXml.Text(product, "vProd"), model.Items[index].Total);
        }
        var totals = SafeXml.Child(SafeXml.Child(info, "total"), "ICMSTot");
        Assert.Equal(SafeXml.Text(totals, "vProd"), model.Totals.Products);
        Assert.Equal(SafeXml.Text(totals, "vDesc"), model.Totals.Discount);
        Assert.Equal(SafeXml.Text(totals, "vNF"), model.Totals.Invoice);
        Assert.Null(model.Totals.ApproximateTaxes);
        Assert.Equal(sale.Payments.Select(p => (p.Code, p.Value)), model.Payments.Select(p => (p.Code, p.Value)));
        Assert.Equal(sale.Change, model.Change);
        Assert.Equal(sale.Recipient!.Document, model.Recipient!.Document);
        Assert.Equal(NfcePreparation.HomologationRecipient, model.Recipient.Name);
        Assert.Equal(QrCode3.Online(model.Key), model.QrCodeText);
        Assert.Equal(QrCode3.DfKeyConsultation, model.ConsultationUrl);
        Assert.Equal(sale.AdditionalInfo, model.AdditionalInfo);
        Assert.Equal(44, model.FormattedKey.Replace(" ", "", StringComparison.Ordinal).Length);
        Assert.Equal(model.Key.Value, model.FormattedKey.Replace(" ", "", StringComparison.Ordinal));
    }

    [Fact]
    public void QrImageDecodesToTheQrV3StringOfTheSameKey()
    {
        var danfe = DanfeRenderer.Render(Processed("crt1"), proof.Validator);
        Assert.Equal(6, danfe.QrCode.Version);
        Assert.Equal(41, danfe.QrCode.Size);
        QrTestDecoder.AssertFunctionPatterns(danfe.QrCode);
        Assert.Equal(danfe.Model.QrCodeText, QrTestDecoder.Decode(danfe.QrCode));
        Assert.Equal(QrCode3.Online(danfe.Model.Key), QrTestDecoder.Decode(danfe.QrCode));
        Assert.Equal(danfe.QrCode.ToSvg(), danfe.QrSvg);
        Assert.Contains("viewBox=\"0 0 49 49\"", danfe.QrSvg, StringComparison.Ordinal);
    }

    [Fact]
    public void AuthorizedWithAlertsPrintsStatusAndEveryMessage()
    {
        var signed = Signed("crt3");
        var processed = NfceProtocol.BuildProcessed(signed, OfflineFixtures.Response(signed, "120", alerts: true), proof.Validator);
        var danfe = DanfeRenderer.Render(processed, proof.Validator);
        Assert.Equal(FiscalState.AuthorizedWithAlert, danfe.Model.State);
        Assert.Equal(2, danfe.Model.Alerts.Count);
        Assert.Contains("cStat 120 - RESULTADO SINTETICO SEM VALIDADE\nFISCAL\n", danfe.Text, StringComparison.Ordinal);
        Assert.Contains("Alerta 1: ALERTA SINTETICO A\n", danfe.Text, StringComparison.Ordinal);
        Assert.Contains("Alerta 2: ALERTA SINTETICO B\n", danfe.Text, StringComparison.Ordinal);
        Assert.Contains("Protocolo de Autorizacao: " + OfflineFixtures.Protocol17, danfe.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void AuthorizedOutsideDeadlineIsRenderedWithItsStatus()
    {
        var signed = Signed("crt1");
        var processed = NfceProtocol.BuildProcessed(signed, OfflineFixtures.Response(signed, "150"), proof.Validator);
        var danfe = DanfeRenderer.Render(processed, proof.Validator);
        Assert.Equal(FiscalState.AuthorizedOutsideDeadline, danfe.Model.State);
        Assert.Contains("cStat 150 - ", danfe.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("203")] // rejeição mapeada
    [InlineData("110")] // denegação: não existe mais para NFC-e; não é autorização
    [InlineData("301")] // código não mapeado
    [InlineData("204")] // duplicidade: reconciliação, não autorização
    public void ProcessedXmlWhoseProtocolIsNotAuthorizedIsRefused(string code)
    {
        var processed = Processed("crt1").Replace("<cStat>100</cStat>", $"<cStat>{code}</cStat>", StringComparison.Ordinal);
        var error = Assert.Throws<InvalidDataException>(() => DanfeModel.FromProcessed(processed, proof.Validator));
        Assert.Contains("DANFE exige nfeProc autorizado", error.Message, StringComparison.Ordinal);
        Assert.Throws<InvalidDataException>(() => DanfeRenderer.Render(processed, proof.Validator));
    }

    [Fact]
    public void ProtocolOfAnotherKeyOrAnotherEnvironmentIsRefused()
    {
        var processed = Processed("crt1");
        var key = A1Signer.Verify(processed).Key.Value;
        var otherKey = A1Signer.Verify(Processed("crt3")).Key.Value;
        Assert.NotEqual(key, otherKey);
        var swappedKey = processed.Replace($"<chNFe>{key}</chNFe>", $"<chNFe>{otherKey}</chNFe>", StringComparison.Ordinal);
        Assert.Throws<InvalidDataException>(() => DanfeModel.FromProcessed(swappedKey, proof.Validator));
        var protocolStart = processed.IndexOf("<protNFe", StringComparison.Ordinal);
        var production = processed[..protocolStart] +
            processed[protocolStart..].Replace("<tpAmb>2</tpAmb>", "<tpAmb>1</tpAmb>", StringComparison.Ordinal);
        Assert.Throws<InvalidDataException>(() => DanfeModel.FromProcessed(production, proof.Validator));
    }

    [Fact]
    public void TamperedInvoiceInsideProcessedXmlIsRefused()
    {
        var processed = Processed("crt1");
        var tampered = processed.Replace("<vNF>93.00</vNF>", "<vNF>1.00</vNF>", StringComparison.Ordinal);
        Assert.NotEqual(processed, tampered);
        var error = Assert.Throws<InvalidDataException>(() => DanfeModel.FromProcessed(tampered, proof.Validator));
        Assert.Contains("nfeProc recusado para DANFE", error.Message, StringComparison.Ordinal);
        Assert.IsType<System.Security.Cryptography.CryptographicException>(error.InnerException);
    }

    [Fact]
    public void SignedInvoiceWithoutProtocolAndUnrelatedXmlAreRefused()
    {
        Assert.Throws<InvalidDataException>(() => DanfeModel.FromProcessed(Signed("crt1"), proof.Validator));
        Assert.Throws<InvalidDataException>(() => DanfeModel.FromProcessed(
            OfflineFixtures.Response(Signed("crt1")), proof.Validator));
    }

    [Fact]
    public void UnidentifiedConsumerIsPrintedWhenThereIsNoRecipient()
    {
        var (_, emitter, context) = Models("crt1");
        var json = JsonNode.Parse(Sample("sale-crt1"))!.AsObject();
        json.Remove("recipient");
        var sale = PreparationJson.ParseSale(json.ToJsonString());
        var signed = Sign(NfcePreparation.BuildUnsigned(sale, emitter, context));
        var danfe = DanfeRenderer.Render(NfceProtocol.BuildProcessed(signed, OfflineFixtures.Response(signed), proof.Validator), proof.Validator);
        Assert.Null(danfe.Model.Recipient);
        Assert.Contains("\nCONSUMIDOR NAO IDENTIFICADO\n", danfe.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void WrapKeepsWordsRespectsIndentAndCutsOversizedTokens()
    {
        Assert.Equal(["abc"], DanfeText.Wrap("abc"));
        var url = new string('x', 100);
        Assert.Equal([url[..48], url[48..96], url[96..]], DanfeText.Wrap(url));
        var wrapped = DanfeText.Wrap(string.Join(' ', Enumerable.Repeat("palavra", 12)), "    ");
        Assert.All(wrapped, line => Assert.InRange(line.Length, 1, DanfeText.Columns));
        Assert.StartsWith("palavra", wrapped[0], StringComparison.Ordinal);
        Assert.All(wrapped.Skip(1), line => Assert.StartsWith("    palavra", line, StringComparison.Ordinal));
        Assert.Equal(12, wrapped.Sum(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length));
    }

    [Theory]
    [InlineData("70.25", "70,25")]
    [InlineData("0.00", "0,00")]
    [InlineData("1234.5", "1234,50")]
    public void MoneyUsesCommaAndTwoDecimals(string value, string expected) => Assert.Equal(expected, DanfeModel.Money(value));

    [Theory]
    [InlineData("2.0000", "2,00")]
    [InlineData("35.1250000000", "35,125")]
    [InlineData("11.7083333333", "11,7083333333")]
    public void QuantityTrimsTrailingZerosButKeepsTwoDecimals(string value, string expected) =>
        Assert.Equal(expected, DanfeModel.Quantity(value));

    [Fact]
    public void DocumentsAndTimestampsAreFormattedForPrinting()
    {
        Assert.Equal("93.847.261/0001-22", DanfeModel.Document("CNPJ", "93847261000122"));
        Assert.Equal("12.ABC.345/01DE-35", DanfeModel.Document("CNPJ", "12ABC34501DE35"));
        Assert.Equal("123.456.789-09", DanfeModel.Document("CPF", "12345678909"));
        Assert.Equal("X123", DanfeModel.Document("ID ESTRANGEIRO", "X123"));
        Assert.Equal("05/10/2026 10:11:12", DanfeModel.Timestamp("2026-10-05T10:11:12-03:00"));
    }

    private string Processed(string scenario)
    {
        var signed = Signed(scenario);
        return NfceProtocol.BuildProcessed(signed, OfflineFixtures.Response(signed), proof.Validator);
    }

    private string Signed(string scenario)
    {
        var (sale, emitter, context) = Models(scenario);
        return Sign(NfcePreparation.BuildUnsigned(sale, emitter, context));
    }

    private string Sign(PreparedNfce prepared) =>
        A1Signer.Sign(QrCode3.AddToInvoice(prepared.UnsignedXml), proof.Certificate);

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
