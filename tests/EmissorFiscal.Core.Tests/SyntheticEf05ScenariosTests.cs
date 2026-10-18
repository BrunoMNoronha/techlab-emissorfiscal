using EmissorFiscal.Core.Preparation;
using EmissorFiscal.Core.Protocols;
using EmissorFiscal.Core.QrCode;
using EmissorFiscal.Core.Signature;
using EmissorFiscal.Samples;
using Xunit;

namespace EmissorFiscal.Core.Tests;

// As vendas e respostas são somente hipóteses sintéticas; não representam
// enquadramento aprovado, resposta da SEFAZ ou homologação fiscal.
public sealed class SyntheticEf05ScenariosTests(PreparationProof proof) : IClassFixture<PreparationProof>
{
    [Theory]
    [InlineData("crt1", "crt1")]
    [InlineData("crt3", "crt3")]
    [InlineData("st-crt1", "crt1")]
    [InlineData("exempt-crt1", "crt1")]
    [InlineData("red-crt3", "crt3")]
    [InlineData("basket-crt3", "crt3")]
    public void SyntheticSalesCorrelateProtocolAndRejectConsultationAlerts(string saleName, string emitterName)
    {
        var sale = PreparationJson.ParseSale(Sample("sale-" + saleName));
        var emitter = PreparationJson.ParseEmitter(Sample("emitter-" + emitterName));
        var context = PreparationJson.ParseContext(Sample("test-context"));
        var prepared = NfcePreparation.BuildUnsigned(sale, emitter, context);
        var signed = A1Signer.Sign(QrCode3.AddToInvoice(prepared.UnsignedXml), proof.Certificate);
        var signature = A1Signer.Verify(signed);

        // Código 100: envio, consulta e nfeProc correlacionados na baseline atual.
        var send100 = OfflineFixtures.Response(signed);
        var query100 = OfflineFixtures.Response(signed, consultation: true);
        foreach (var response in new[] { send100, query100 })
        {
            var result = NfceProtocol.Interpret(signed, response, proof.Validator);
            Assert.Equal(FiscalState.Authorized, result.State);
            Assert.Equal(OfflineFixtures.Protocol17, result.ProtocolNumber);
            var processed = NfceProtocol.BuildProcessed(signed, response, proof.Validator);
            Assert.Equal(FiscalState.Authorized, NfceProtocol.Interpret(signed, processed, proof.Validator).State);
            Assert.Equal(signature.DigestBase64, A1Signer.Verify(processed).DigestBase64);
        }

        // Código 120 com alertas: envio e nfeProc são aceitos; consulta falha
        // fechada porque o XSD oficial fixado ainda não contém essa extensão.
        var send120 = OfflineFixtures.Response(signed, "120", alerts: true);
        var sentWithAlert = NfceProtocol.Interpret(signed, send120, proof.Validator);
        Assert.Equal(FiscalState.AuthorizedWithAlert, sentWithAlert.State);
        Assert.Equal(OfflineFixtures.Protocol17, sentWithAlert.ProtocolNumber);
        Assert.Equal(2, sentWithAlert.Messages.Count);
        var processed120 = NfceProtocol.BuildProcessed(signed, send120, proof.Validator);
        var processedResult = NfceProtocol.Interpret(signed, processed120, proof.Validator);
        Assert.Equal(sentWithAlert.Messages, processedResult.Messages);
        Assert.Equal(FiscalState.AuthorizedWithAlert, processedResult.State);
        Assert.Equal(signature.DigestBase64, A1Signer.Verify(processed120).DigestBase64);

        var query120 = OfflineFixtures.Response(signed, "120", consultation: true, alerts: true);
        var invalidQuery = NfceProtocol.Interpret(signed, query120, proof.Validator);
        Assert.Equal(FiscalState.InvalidResponse, invalidQuery.State);
        Assert.False(invalidQuery.IsAuthorized);
        Assert.Null(invalidQuery.CorrelatedProtocolXml);
        Assert.Throws<InvalidDataException>(() => NfceProtocol.BuildProcessed(signed, query120, proof.Validator));
    }

    private static string Sample(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "preparation", name + ".json"));
}
