using System.Security.Cryptography;
using EmissorFiscal.Core.QrCode;
using EmissorFiscal.Core.Signature;
using EmissorFiscal.Core.Xml;
using EmissorFiscal.Samples;
using Xunit;

namespace EmissorFiscal.Core.Tests;

public sealed class A1SignatureTests(OfflineProof proof) : IClassFixture<OfflineProof>
{
    [Fact]
    public void SyntheticPkcs12IsImportedAndSignsAfterSerialization()
    {
        var pfx = OfflineFixtures.RsaPkcs12();
        try
        {
            using var certificate = A1Signer.ImportPkcs12(pfx, OfflineFixtures.Password.AsSpan());
            Assert.True(certificate.HasPrivateKey);
            var signed = A1Signer.Sign(QrCode3.AddToInvoice(OfflineFixtures.Fixture("nfce-unsigned.xml")), certificate);
            var result = A1Signer.Verify(SafeXml.Serialize(SafeXml.Load(signed)), certificate);
            Assert.Equal(20, Convert.FromBase64String(result.DigestBase64).Length);
        }
        finally { CryptographicOperations.ZeroMemory(pfx); }
    }

    [Theory]
    [InlineData("wrong-password")]
    [InlineData("public-only")]
    [InlineData("ecdsa")]
    public void InvalidPkcs12Fails(string scenario)
    {
        var pfx = scenario == "ecdsa" ? OfflineFixtures.EcdsaPkcs12() :
            OfflineFixtures.RsaPkcs12(includePrivateKey: scenario != "public-only");
        try
        {
            var password = scenario == "wrong-password" ? "incorrect" : OfflineFixtures.Password;
            Assert.Throws<CryptographicException>(() =>
            {
                using var rejected = A1Signer.ImportPkcs12(pfx, password.AsSpan());
            });
        }
        finally { CryptographicOperations.ZeroMemory(pfx); }
    }

    [Fact]
    public void SignedContentTamperingFails() => Assert.Throws<CryptographicException>(() =>
        A1Signer.Verify(proof.Signed.Replace("<vProd>10.00</vProd>", "<vProd>11.00</vProd>", StringComparison.Ordinal)));

    [Fact]
    public void DuplicateIdFailsEvenOutsideInfNfe()
    {
        var document = SafeXml.Load(proof.Signed);
        var extra = document.CreateElement("unexpected", SafeXml.NfeNamespace);
        extra.SetAttribute("id", SafeXml.Child(document.DocumentElement!, "infNFe").GetAttribute("Id"));
        document.DocumentElement!.AppendChild(extra);
        Assert.Throws<CryptographicException>(() => A1Signer.Verify(SafeXml.Serialize(document)));
    }

    [Fact]
    public void ExternalReferenceIsRejectedBeforeSignedXmlLoadsIt()
    {
        var document = SafeXml.Load(proof.Signed);
        var reference = document.GetElementsByTagName("Reference", A1Signer.DsNamespace)[0]!;
        ((System.Xml.XmlElement)reference).SetAttribute("URI", "https://127.0.0.1:1/never-read");
        Assert.Throws<CryptographicException>(() => A1Signer.Verify(SafeXml.Serialize(document)));
    }

    [Fact]
    public void AlgorithmSubstitutionFails() => Assert.Throws<CryptographicException>(() =>
        A1Signer.Verify(proof.Signed.Replace("http://www.w3.org/2000/09/xmldsig#rsa-sha1",
            "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256", StringComparison.Ordinal)));

    [Fact]
    public void ExpectedCertificateSubstitutionFails()
    {
        var pfx = OfflineFixtures.RsaPkcs12();
        try
        {
            using var different = A1Signer.ImportPkcs12(pfx, OfflineFixtures.Password.AsSpan());
            Assert.Throws<CryptographicException>(() => A1Signer.Verify(proof.Signed, different));
        }
        finally { CryptographicOperations.ZeroMemory(pfx); }
    }
}
