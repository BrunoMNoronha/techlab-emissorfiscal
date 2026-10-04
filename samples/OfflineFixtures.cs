using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using EmissorFiscal.Core.Domain;
using EmissorFiscal.Core.QrCode;
using EmissorFiscal.Core.Signature;
using EmissorFiscal.Core.Xml;
using EmissorFiscal.Core.Xsd;

namespace EmissorFiscal.Samples;

// Compartilhado somente pela CLI e testes; não faz parte do núcleo.
public static class OfflineFixtures
{
    public const string Password = "synthetic-test-only";
    public const string Protocol17 = "15326000000000001";
    public static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name));
    public static XsdValidator Validator() => new(SchemaManifest.Load(
        Path.Combine(AppContext.BaseDirectory, "schemas", "nfe", "4.00", "manifest.json")));

    public static byte[] RsaPkcs12(bool includePrivateKey = true)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=OFFLINE SYNTHETIC A1 - NOT ICP-BRASIL", rsa,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
        if (includePrivateKey) return certificate.Export(X509ContentType.Pkcs12, Password);
        using var publicOnly = X509CertificateLoader.LoadCertificate(certificate.RawData);
        return publicOnly.Export(X509ContentType.Pkcs12, Password);
    }

    public static byte[] EcdsaPkcs12()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=OFFLINE SYNTHETIC EC", ecdsa, HashAlgorithmName.SHA256);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
        return certificate.Export(X509ContentType.Pkcs12, Password);
    }

    public static string SignedInvoice(bool alphanumeric = false)
    {
        var xml = Fixture("nfce-unsigned.xml");
        if (alphanumeric)
        {
            var document = SafeXml.Load(xml);
            var info = SafeXml.Child(document.DocumentElement!, "infNFe");
            var key = AccessKey.Create("2610", "12ABC34501DE35", "001", "000000001", "12345678");
            info.SetAttribute("Id", "NFe" + key.Value);
            SafeXml.Child(SafeXml.Child(info, "emit"), "CNPJ").InnerText = key.Cnpj;
            SafeXml.Child(SafeXml.Child(info, "ide"), "cDV").InnerText = key.Value[^1..];
            xml = SafeXml.Serialize(document);
        }
        xml = QrCode3.AddToInvoice(xml);
        var pfx = RsaPkcs12();
        try
        {
            using var certificate = A1Signer.ImportPkcs12(pfx, Password.AsSpan());
            return A1Signer.Sign(xml, certificate);
        }
        finally { CryptographicOperations.ZeroMemory(pfx); }
    }

    public static string Response(string original, string code = "100", bool consultation = false, bool alerts = false)
    {
        var proof = A1Signer.Verify(original);
        var messageXml = alerts ? "<cMsg>1</cMsg><xMsg>ALERTA SINTETICO A</xMsg><cMsg>2</cMsg><xMsg>ALERTA SINTETICO B</xMsg>" : "";
        var protocol = $"<protNFe versao=\"4.00\"><infProt><tpAmb>2</tpAmb><verAplic>OFFLINE-1</verAplic><chNFe>{proof.Key.Value}</chNFe>" +
            $"<dhRecbto>2026-10-04T12:00:01-03:00</dhRecbto><nProt>{Protocol17}</nProt><digVal>{proof.DigestBase64}</digVal>" +
            $"<cStat>{code}</cStat><xMotivo>RESULTADO SINTETICO SEM VALIDADE FISCAL</xMotivo>{messageXml}</infProt></protNFe>";
        var root = consultation ? "retConsSitNFe" : "retEnviNFe";
        var outerCode = consultation ? code : "104";
        var consultedKey = consultation ? $"<chNFe>{proof.Key.Value}</chNFe>" : "";
        return $"<{root} xmlns=\"{SafeXml.NfeNamespace}\" versao=\"4.00\"><tpAmb>2</tpAmb><verAplic>OFFLINE-1</verAplic>" +
            $"<cStat>{outerCode}</cStat><xMotivo>RETORNO SINTETICO</xMotivo><cUF>53</cUF><dhRecbto>2026-10-04T12:00:01-03:00</dhRecbto>" +
            $"{consultedKey}{protocol}</{root}>";
    }
}
