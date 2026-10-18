using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using EmissorFiscal.Core.Domain;
using EmissorFiscal.Core.Wire;
using EmissorFiscal.Core.Xml;

namespace EmissorFiscal.Core.Signature;

public sealed record SignatureProof(AccessKey Key, string DigestBase64, string XmlSha256, string CertificateSha256);
public sealed record ElementSignatureProof(string Id, string DigestBase64, string XmlSha256, string CertificateSha256);

public static class A1Signer
{
    public const string DsNamespace = SignedXml.XmlDsigNamespaceUrl;

    public static X509Certificate2 ImportPkcs12(ReadOnlySpan<byte> pfx, ReadOnlySpan<char> password)
    {
        if (pfx.Length is 0 or > 1024 * 1024) throw new CryptographicException("PKCS#12 vazio/excessivo.");
        var certificate = X509CertificateLoader.LoadPkcs12(pfx, password, X509KeyStorageFlags.EphemeralKeySet);
        try { RequireSigningCertificate(certificate); return certificate; }
        catch { certificate.Dispose(); throw; }
    }

    public static string Sign(string unsignedXml, X509Certificate2 certificate)
    {
        RequireSigningCertificate(certificate);
        var document = SafeXml.Load(unsignedXml);
        var invoice = SafeXml.RequireRoot(document, "NFe");
        NfceMessages.RequireInvoiceScope(invoice);
        var serialized = SignCore(document, invoice, SafeXml.Child(invoice, "infNFe"), certificate);
        Verify(serialized, certificate);
        return serialized;
    }

    // Assinatura de evento/inutilização: o elemento identificado por Id (infEvento,
    // infInut) é assinado e a Signature anexada ao seu contêiner direto (evento,
    // inutNFe), com as mesmas regras do MOC usadas na NFC-e.
    public static string SignElement(string unsignedXml, X509Certificate2 certificate, string rootName, string infoName,
        string version)
    {
        RequireSigningCertificate(certificate);
        var document = SafeXml.Load(unsignedXml);
        var root = SafeXml.RequireRoot(document, rootName, version);
        var info = SafeXml.Child(root, infoName);
        if (info.GetAttribute("Id").Length == 0) throw new CryptographicException("Elemento a assinar sem Id.");
        var serialized = SignCore(document, root, info, certificate);
        VerifyElement(serialized, rootName, rootName, infoName, version, certificate);
        return serialized;
    }

    private static string SignCore(XmlDocument document, XmlElement container, XmlElement info, X509Certificate2 certificate)
    {
        if (document.GetElementsByTagName("Signature", DsNamespace).Count != 0)
            throw new CryptographicException("XML já contém assinatura.");
        RequireUniqueIds(document);
        using var key = certificate.GetRSAPrivateKey() ?? throw new CryptographicException("Chave RSA ausente.");
        var signedXml = new InvoiceSignedXml(document, info) { SigningKey = key, Resolver = new RejectingXmlResolver() };
        var signedInfo = signedXml.SignedInfo ?? throw new CryptographicException("SignedInfo ausente.");
        signedInfo.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl;
        signedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA1Url;
        var reference = new Reference("#" + info.GetAttribute("Id")) { DigestMethod = SignedXml.XmlDsigSHA1Url };
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigC14NTransform());
        signedXml.AddReference(reference);
        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(certificate));
        signedXml.KeyInfo = keyInfo;
        signedXml.ComputeSignature();
        container.AppendChild(document.ImportNode(signedXml.GetXml(), deep: true));
        return SafeXml.Serialize(document);
    }

    // Somente prova criptográfica local. verifySignatureOnly evita chain/revocation I/O.
    // Sem validação ICP-Brasil, titularidade, cadeia ou consulta fiscal neste incremento.
    public static SignatureProof Verify(string xml, X509Certificate2? expectedCertificate = null)
    {
        var document = SafeXml.Load(xml);
        RequireUniqueIds(document);
        var invoice = NfceMessages.InvoiceIn(document);
        var key = NfceMessages.RequireInvoiceScope(invoice);
        var (digest, certificateSha256) = VerifyCore(document, invoice, SafeXml.Child(invoice, "infNFe"), expectedCertificate);
        return new(key, digest, Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(xml))), certificateSha256);
    }

    // Verifica a única assinatura de um documento de evento/inutilização. O elemento
    // assinado (signedName) é a raiz ou um filho direto dela (evento dentro de
    // envEvento/procEventoNFe); o Id vem do elemento infoName.
    public static ElementSignatureProof VerifyElement(string xml, string rootName, string signedName, string infoName,
        string version, X509Certificate2? expectedCertificate = null)
    {
        var document = SafeXml.Load(xml);
        RequireUniqueIds(document);
        var root = SafeXml.RequireRoot(document, rootName, version);
        var signed = rootName == signedName ? root : SafeXml.Child(root, signedName);
        var info = SafeXml.Child(signed, infoName);
        var (digest, certificateSha256) = VerifyCore(document, signed, info, expectedCertificate);
        return new(info.GetAttribute("Id"), digest,
            Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(xml))), certificateSha256);
    }

    private static (string DigestBase64, string CertificateSha256) VerifyCore(XmlDocument document, XmlElement invoice,
        XmlElement info, X509Certificate2? expectedCertificate)
    {
        if (document.GetElementsByTagName("Signature", DsNamespace).Count != 1)
            throw new CryptographicException("Esperada uma única assinatura de NFC-e.");
        var signature = SafeXml.Child(invoice, "Signature", DsNamespace);
        RequireSignatureShape(signature, info.GetAttribute("Id"));
        var certificateElement = SafeXml.Child(SafeXml.Child(SafeXml.Child(signature, "KeyInfo", DsNamespace),
            "X509Data", DsNamespace), "X509Certificate", DsNamespace);
        using var certificate = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(certificateElement.InnerText));
        using var publicKey = certificate.GetRSAPublicKey();
        if (publicKey is null || publicKey.KeySize < 2048)
            throw new CryptographicException("Certificado de assinatura deve usar RSA >= 2048 bits.");
        if (expectedCertificate is not null && !certificate.RawData.AsSpan().SequenceEqual(expectedCertificate.RawData))
            throw new CryptographicException("Certificado embutido difere do esperado.");
        var signedXml = new InvoiceSignedXml(document, info) { Resolver = new RejectingXmlResolver() };
        signedXml.LoadXml(signature);
        if (!signedXml.CheckSignature(certificate, verifySignatureOnly: true))
            throw new CryptographicException("Assinatura/digest da NFC-e inválido.");
        var digest = SafeXml.Child(SafeXml.Child(SafeXml.Child(signature, "SignedInfo", DsNamespace),
            "Reference", DsNamespace), "DigestValue", DsNamespace).InnerText;
        var digestBytes = Convert.FromBase64String(digest);
        if (digestBytes.Length != 20) throw new CryptographicException("Digest fiscal SHA-1 deve ter 20 bytes.");
        return (Convert.ToBase64String(digestBytes), Convert.ToHexStringLower(SHA256.HashData(certificate.RawData)));
    }

    private static void RequireSigningCertificate(X509Certificate2 certificate)
    {
        if (!certificate.HasPrivateKey) throw new CryptographicException("A1 sem chave privada.");
        using var rsa = certificate.GetRSAPrivateKey();
        if (rsa is null || rsa.KeySize < 2048) throw new CryptographicException("A1 deve conter chave RSA >= 2048 bits.");
        var now = DateTime.UtcNow;
        if (certificate.NotBefore.ToUniversalTime() > now || certificate.NotAfter.ToUniversalTime() < now)
            throw new CryptographicException("Certificado fora da validade temporal.");
        foreach (var usage in certificate.Extensions.OfType<X509KeyUsageExtension>())
            if ((usage.KeyUsages & X509KeyUsageFlags.DigitalSignature) == 0)
                throw new CryptographicException("Certificado não permite assinatura digital.");
    }

    private static void RequireUniqueIds(XmlDocument document)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var element in document.GetElementsByTagName("*").OfType<XmlElement>())
            foreach (var attribute in element.Attributes.OfType<XmlAttribute>())
                if (attribute.LocalName.Equals("Id", StringComparison.OrdinalIgnoreCase) &&
                    (attribute.Value.Length == 0 || !ids.Add(attribute.Value)))
                    throw new CryptographicException("ID vazio/duplicado no XML.");
    }

    private static void RequireSignatureShape(XmlElement signature, string id)
    {
        var signedInfo = SafeXml.Child(signature, "SignedInfo", DsNamespace);
        if (SafeXml.Child(signedInfo, "CanonicalizationMethod", DsNamespace).GetAttribute("Algorithm") != SignedXml.XmlDsigC14NTransformUrl ||
            SafeXml.Child(signedInfo, "SignatureMethod", DsNamespace).GetAttribute("Algorithm") != SignedXml.XmlDsigRSASHA1Url)
            throw new CryptographicException("Algoritmos de assinatura fora do MOC 7.");
        var reference = SafeXml.Child(signedInfo, "Reference", DsNamespace);
        if (reference.GetAttribute("URI") != "#" + id || reference.HasAttribute("Type") ||
            SafeXml.Child(reference, "DigestMethod", DsNamespace).GetAttribute("Algorithm") != SignedXml.XmlDsigSHA1Url)
            throw new CryptographicException("Referência externa/incorreta ou digest fora do MOC.");
        var transforms = SafeXml.Child(reference, "Transforms", DsNamespace).ChildNodes.OfType<XmlElement>().ToArray();
        if (transforms.Length != 2 || transforms.Any(e => e.LocalName != "Transform" || e.NamespaceURI != DsNamespace || e.HasChildNodes) ||
            transforms[0].GetAttribute("Algorithm") != SignedXml.XmlDsigEnvelopedSignatureTransformUrl ||
            transforms[1].GetAttribute("Algorithm") != SignedXml.XmlDsigC14NTransformUrl)
            throw new CryptographicException("Transforms fora do MOC 7.");
        var keyInfo = SafeXml.Child(signature, "KeyInfo", DsNamespace);
        var x509Data = SafeXml.Child(keyInfo, "X509Data", DsNamespace);
        if (keyInfo.ChildNodes.OfType<XmlElement>().Count() != 1 || x509Data.ChildNodes.OfType<XmlElement>().Count() != 1 ||
            signature.ChildNodes.OfType<XmlElement>().Count() != 3)
            throw new CryptographicException("KeyInfo/Signature deve conter somente o certificado e os grupos do MOC.");
    }

    private sealed class InvoiceSignedXml(XmlDocument document, XmlElement target) : SignedXml(document)
    {
        public override XmlElement? GetIdElement(XmlDocument? document, string idValue) =>
            target.GetAttribute("Id") == idValue ? target : null;
    }

    private sealed class RejectingXmlResolver : XmlResolver
    {
        public override System.Net.ICredentials? Credentials { set { } }

        public override object GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn) =>
            throw new XmlException("Resolução externa proibida na assinatura offline.");
    }
}
