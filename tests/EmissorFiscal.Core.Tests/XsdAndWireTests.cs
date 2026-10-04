using System.Xml.Schema;
using EmissorFiscal.Core.Domain;
using EmissorFiscal.Core.Protocols;
using EmissorFiscal.Core.Signature;
using EmissorFiscal.Core.Wire;
using EmissorFiscal.Core.Xml;
using EmissorFiscal.Samples;
using Xunit;

namespace EmissorFiscal.Core.Tests;

public sealed class XsdAndWireTests(OfflineProof proof) : IClassFixture<OfflineProof>
{
    [Fact]
    public void AllSixEntrypointsCompile()
    {
        Assert.Equal(new[] { "consSitNFe", "enviNFe", "nfe", "procNFe", "retConsSitNFe", "retEnviNFe" },
            proof.Validator.CompiledEntrypoints);
        Assert.Equal(11, proof.Validator.Manifest.Files.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SixPositiveSyntheticDocumentsAreValid(bool alpha)
    {
        var signed = alpha ? proof.AlphaSigned : proof.Signed;
        var signature = A1Signer.Verify(signed);
        var send = NfceMessages.BuildSubmission(signed, "1", proof.Validator);
        var consult = NfceMessages.BuildConsultation(signature.Key, proof.Validator);
        var response = OfflineFixtures.Response(signed);
        var returned = OfflineFixtures.Response(signed, consultation: true);
        var processed = NfceProtocol.BuildProcessed(signed, response, proof.Validator);
        proof.Validator.RequireValid("nfe", signed);
        proof.Validator.RequireValid("enviNFe", send);
        proof.Validator.RequireValid("consSitNFe", consult);
        proof.Validator.RequireValid("retEnviNFe", response);
        proof.Validator.RequireValid("retConsSitNFe", returned);
        proof.Validator.RequireValid("procNFe", processed);
        Assert.Equal(signature.DigestBase64, A1Signer.Verify(send).DigestBase64);
        Assert.Equal(signature.DigestBase64, A1Signer.Verify(processed).DigestBase64);
        Assert.True(NfceProtocol.Interpret(signed, processed, proof.Validator).IsAuthorized);
    }

    [Theory]
    [InlineData("<mod>65</mod>", "<mod>55</mod>")]
    [InlineData("<cUF>53</cUF>", "<cUF>35</cUF>")]
    [InlineData("<tpAmb>2</tpAmb>", "<tpAmb>1</tpAmb>")]
    [InlineData("<tpEmis>1</tpEmis>", "<tpEmis>9</tpEmis>")]
    public void OutsideScopeFailsEvenWhenXsdAccepts(string before, string after) =>
        Assert.Throws<InvalidDataException>(() => proof.Validator.RequireValid("nfe",
            proof.Signed.Replace(before, after, StringComparison.Ordinal)));

    [Fact]
    public void XmlDatatypeFailureIsReported() => Assert.Throws<XmlSchemaValidationException>(() =>
        proof.Validator.RequireValid("nfe", proof.Signed.Replace("<qCom>1.0000</qCom>", "<qCom>invalid</qCom>", StringComparison.Ordinal)));

    [Fact]
    public void ExternalSchemaHintIsRejected() => Assert.Throws<InvalidDataException>(() =>
        proof.Validator.RequireValid("nfe", proof.Signed.Replace("<NFe ",
            "<NFe xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation=\"http://www.portalfiscal.inf.br/nfe https://invalid.example/nfe.xsd\" ",
            StringComparison.Ordinal)));

    [Fact]
    public void TwoInvoicesInOneSubmissionAreRejected()
    {
        var envelope = SafeXml.Load(NfceMessages.BuildSubmission(proof.Signed, "1", proof.Validator));
        var second = SafeXml.Load(proof.AlphaSigned);
        envelope.DocumentElement!.AppendChild(envelope.ImportNode(second.DocumentElement!, true));
        Assert.Throws<InvalidDataException>(() => proof.Validator.RequireValid("enviNFe", SafeXml.Serialize(envelope)));
    }

    [Fact]
    public void AsynchronousSubmissionIsRejected() => Assert.Throws<InvalidDataException>(() =>
        proof.Validator.RequireValid("enviNFe", NfceMessages.BuildSubmission(proof.Signed, "1", proof.Validator)
            .Replace("<indSinc>1</indSinc>", "<indSinc>0</indSinc>", StringComparison.Ordinal)));

    [Fact]
    public void TaxFieldsArePreserved()
    {
        var unsignedInfo = SafeXml.Child(SafeXml.Load(OfflineFixtures.Fixture("nfce-unsigned.xml")).DocumentElement!, "infNFe");
        var signedInfo = SafeXml.Child(SafeXml.Load(proof.Signed).DocumentElement!, "infNFe");
        var supplied = SafeXml.Child(SafeXml.Child(unsignedInfo, "det"), "imposto").OuterXml;
        var preserved = SafeXml.Child(SafeXml.Child(signedInfo, "det"), "imposto").OuterXml;
        Assert.Equal(supplied, preserved);
    }

    [Fact]
    public void ConsultationOfAnotherModelIsRejected()
    {
        const string basis = "5326101234567800019555001000000001112345678";
        var key = AccessKey.Parse(basis + AccessKey.CalculateDigit(basis));
        Assert.Throws<InvalidDataException>(() => NfceMessages.BuildConsultation(key, proof.Validator));
    }
}
