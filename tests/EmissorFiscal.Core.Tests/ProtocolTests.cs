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

    [Theory]
    [InlineData(false, "153260000000001", true)]
    [InlineData(true, "153260000000001", true)]
    [InlineData(true, "1532600000000001", false)]
    [InlineData(false, "153260000000000001", false)]
    [InlineData(true, "153260000000000001", false)]
    public void ProtocolLengthBoundaryPreservesCorrelation(bool consultation, string number, bool accepted)
    {
        var response = Mutate(OfflineFixtures.Response(proof.Signed, consultation: consultation), document =>
            ((XmlElement)document.GetElementsByTagName("nProt", SafeXml.NfeNamespace)[0]!).InnerText = number);
        var result = NfceProtocol.Interpret(proof.Signed, response, proof.Validator);
        Assert.Equal(accepted, result.IsAuthorized);
        if (accepted)
        {
            Assert.Equal(number, result.ProtocolNumber);
            var processed = NfceProtocol.BuildProcessed(proof.Signed, response, proof.Validator);
            Assert.Equal(number, NfceProtocol.Interpret(proof.Signed, processed, proof.Validator).ProtocolNumber);
            Assert.Equal(A1Signer.Verify(proof.Signed).DigestBase64, A1Signer.Verify(processed).DigestBase64);
        }
        else
        {
            Assert.Equal(FiscalState.InvalidResponse, result.State);
            Assert.Throws<InvalidDataException>(() => NfceProtocol.BuildProcessed(proof.Signed, response, proof.Validator));
        }
    }

    [Theory]
    [InlineData(false, 0, true)]
    [InlineData(false, 1, true)]
    [InlineData(false, 5, true)]
    [InlineData(false, 6, false)]
    [InlineData(true, 1, false)]
    [InlineData(true, 5, false)]
    [InlineData(true, 6, false)]
    public void AlertCardinalityPreservesBaselineBoundary(bool consultation, int count, bool accepted)
    {
        // Combinações sintéticas verificam estrutura/comportamento; não são respostas oficiais.
        var response = Mutate(OfflineFixtures.Response(proof.Signed, "120", consultation: consultation), document =>
        {
            var info = (XmlElement)document.GetElementsByTagName("infProt", SafeXml.NfeNamespace)[0]!;
            for (var index = 1; index <= count; index++)
            {
                foreach (var pair in new[] { ("cMsg", index.ToString()), ("xMsg", $"ALERTA SINTETICO {index}") })
                {
                    var element = document.CreateElement(pair.Item1, SafeXml.NfeNamespace);
                    element.InnerText = pair.Item2;
                    info.AppendChild(element);
                }
            }
        });
        var result = NfceProtocol.Interpret(proof.Signed, response, proof.Validator);
        Assert.Equal(accepted, result.IsAuthorized);
        if (accepted)
        {
            Assert.Equal(FiscalState.AuthorizedWithAlert, result.State);
            Assert.Equal(Enumerable.Range(1, count).Select(index => new FiscalMessage(index.ToString(), $"ALERTA SINTETICO {index}")), result.Messages);
            var processed = NfceProtocol.BuildProcessed(proof.Signed, response, proof.Validator);
            Assert.Equal(result.Messages, NfceProtocol.Interpret(proof.Signed, processed, proof.Validator).Messages);
            Assert.Equal(A1Signer.Verify(proof.Signed).DigestBase64, A1Signer.Verify(processed).DigestBase64);
        }
        else
        {
            Assert.Equal(FiscalState.InvalidResponse, result.State);
            Assert.Throws<InvalidDataException>(() => NfceProtocol.BuildProcessed(proof.Signed, response, proof.Validator));
        }
    }

    [Theory]
    [InlineData("cMsg")]
    [InlineData("xMsg")]
    public void IncompleteAlertPairCannotAuthorize(string missing)
    {
        var response = Mutate(OfflineFixtures.Response(proof.Signed, "120", alerts: true), document =>
        {
            var element = document.GetElementsByTagName(missing, SafeXml.NfeNamespace)[0]!;
            element.ParentNode!.RemoveChild(element);
        });
        Assert.Equal(FiscalState.InvalidResponse, NfceProtocol.Interpret(proof.Signed, response, proof.Validator).State);
        Assert.Throws<InvalidDataException>(() => NfceProtocol.BuildProcessed(proof.Signed, response, proof.Validator));
    }

    private static string Mutate(string xml, Action<XmlDocument> edit)
    {
        var document = SafeXml.Load(xml);
        edit(document);
        return SafeXml.Serialize(document);
    }
}
