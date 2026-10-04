using System.Xml;
using EmissorFiscal.Core.Domain;
using EmissorFiscal.Core.Protocols;
using EmissorFiscal.Core.Signature;
using EmissorFiscal.Core.Xml;
using EmissorFiscal.Samples;
using Xunit;

namespace EmissorFiscal.Core.Tests;

public sealed class ProtocolTests(OfflineProof proof) : IClassFixture<OfflineProof>
{
    [Theory]
    [InlineData("100", FiscalState.Authorized)]
    [InlineData("120", FiscalState.AuthorizedWithAlert)]
    [InlineData("150", FiscalState.AuthorizedOutsideDeadline)]
    public void KnownAuthorizationsRequireCorrelatedProtocol(string code, FiscalState expected)
    {
        var result = NfceProtocol.Interpret(proof.Signed, OfflineFixtures.Response(proof.Signed, code, alerts: true), proof.Validator);
        Assert.Equal(expected, result.State);
        Assert.True(result.IsAuthorized);
        Assert.Equal(OfflineFixtures.Protocol17, result.ProtocolNumber);
        Assert.Equal(17, result.ProtocolNumber!.Length);
        Assert.Equal(new[] { new FiscalMessage("1", "ALERTA SINTETICO A"), new FiscalMessage("2", "ALERTA SINTETICO B") }, result.Messages);
        Assert.Equal(code, result.Code);
    }

    [Theory]
    [InlineData("key")]
    [InlineData("environment")]
    [InlineData("digest")]
    [InlineData("number")]
    [InlineData("missing-number")]
    [InlineData("missing-digest")]
    public void IncompatibleOrIncompleteProtocolCannotAuthorize(string scenario)
    {
        var response = Mutate(OfflineFixtures.Response(proof.Signed), document =>
        {
            var info = (XmlElement)document.GetElementsByTagName("infProt", SafeXml.NfeNamespace)[0]!;
            switch (scenario)
            {
                case "key": SafeXml.Child(info, "chNFe").InnerText =
                    AccessKey.Create("2610", "12345678000195", "001", "000000002", "12345678").Value; break;
                case "environment": SafeXml.Child(info, "tpAmb").InnerText = "1"; break;
                case "digest": SafeXml.Child(info, "digVal").InnerText = Convert.ToBase64String(new byte[20]); break;
                case "number": SafeXml.Child(info, "nProt").InnerText = "1234567890123456"; break;
                case "missing-number": info.RemoveChild(SafeXml.Child(info, "nProt")); break;
                case "missing-digest": info.RemoveChild(SafeXml.Child(info, "digVal")); break;
            }
        });
        var result = NfceProtocol.Interpret(proof.Signed, response, proof.Validator);
        Assert.False(result.IsAuthorized);
        Assert.Null(result.CorrelatedProtocolXml);
        Assert.Throws<InvalidDataException>(() => NfceProtocol.BuildProcessed(proof.Signed, response, proof.Validator));
    }

    [Fact]
    public void MissingProtocolCannotAuthorizeEvenWithSuccessCode()
    {
        var response = Mutate(OfflineFixtures.Response(proof.Signed, consultation: true), document =>
            document.DocumentElement!.RemoveChild(SafeXml.Child(document.DocumentElement!, "protNFe")));
        var result = NfceProtocol.Interpret(proof.Signed, response, proof.Validator);
        Assert.False(result.IsAuthorized);
        Assert.Equal(FiscalState.Unknown, result.State);
    }

    [Fact]
    public void TruncatedResponseCannotAuthorize()
    {
        var result = NfceProtocol.Interpret(proof.Signed, OfflineFixtures.Fixture("truncated-response.xml"), proof.Validator);
        Assert.False(result.IsAuthorized);
        Assert.Equal(FiscalState.InvalidResponse, result.State);
    }

    [Theory]
    [InlineData("204", FiscalState.Unknown)]
    [InlineData("539", FiscalState.Unknown)]
    [InlineData("999", FiscalState.Unknown)]
    [InlineData("215", FiscalState.Rejected)]
    public void UnknownDuplicateAndRejectionCodesAreExplicit(string code, FiscalState expected)
    {
        var result = NfceProtocol.Interpret(proof.Signed, OfflineFixtures.Response(proof.Signed, code), proof.Validator);
        Assert.Equal(expected, result.State);
        Assert.False(result.IsAuthorized);
        Assert.Equal(code, result.Code);
    }

    [Theory]
    [InlineData("103", FiscalState.Processing)]
    [InlineData("105", FiscalState.Processing)]
    [InlineData("217", FiscalState.NotFound)]
    public void PendingAndNotFoundNeverAuthorize(string code, FiscalState expected)
    {
        var response = Mutate(OfflineFixtures.Response(proof.Signed, consultation: true), document =>
        {
            var root = document.DocumentElement!;
            SafeXml.Child(root, "cStat").InnerText = code;
            root.RemoveChild(SafeXml.Child(root, "protNFe"));
        });
        var result = NfceProtocol.Interpret(proof.Signed, response, proof.Validator);
        Assert.Equal(expected, result.State);
        Assert.False(result.IsAuthorized);
    }

    [Fact]
    public void ConsultationWithDifferentOuterStateNeedsReconciliation()
    {
        var response = Mutate(OfflineFixtures.Response(proof.Signed, consultation: true), document =>
            SafeXml.Child(document.DocumentElement!, "cStat").InnerText = "101");
        var result = NfceProtocol.Interpret(proof.Signed, response, proof.Validator);
        Assert.False(result.IsAuthorized);
        Assert.Equal(FiscalState.Unknown, result.State);
    }

    [Fact]
    public void AlertExtensionMissingInPinnedConsultationSchemaFailsClosed()
    {
        // Não editar XSD oficial para aceitar extensão nova: requer novo manifest revisado.
        var result = NfceProtocol.Interpret(proof.Signed,
            OfflineFixtures.Response(proof.Signed, "120", consultation: true, alerts: true), proof.Validator);
        Assert.Equal(FiscalState.InvalidResponse, result.State);
        Assert.False(result.IsAuthorized);
    }

    [Fact]
    public void ProcessedInvoiceMustContainTheOriginalSignedDocument()
    {
        var processed = NfceProtocol.BuildProcessed(proof.Signed, OfflineFixtures.Response(proof.Signed), proof.Validator);
        var document = SafeXml.Load(processed);
        var root = document.DocumentElement!;
        root.ReplaceChild(document.ImportNode(SafeXml.Load(proof.AlphaSigned).DocumentElement!, true), SafeXml.Child(root, "NFe"));
        Assert.False(NfceProtocol.Interpret(proof.Signed, SafeXml.Serialize(document), proof.Validator).IsAuthorized);
    }

    private static string Mutate(string xml, Action<XmlDocument> edit)
    {
        var document = SafeXml.Load(xml);
        edit(document);
        return SafeXml.Serialize(document);
    }
}
