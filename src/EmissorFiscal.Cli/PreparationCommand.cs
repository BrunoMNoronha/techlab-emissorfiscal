using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EmissorFiscal.Core.Preparation;
using EmissorFiscal.Core.QrCode;
using EmissorFiscal.Core.Signature;
using EmissorFiscal.Core.Wire;
using EmissorFiscal.Samples;

namespace EmissorFiscal.Cli;

internal static class PreparationCommand
{
    public static int Run(string salePath, string emitterPath, string contextPath, string outputPath, Action<string>? progress = null, Action<string>? output = null)
    {
        progress?.Invoke("Lendo e validando os dados de entrada");
        var sale = PreparationJson.ParseSale(ReadInput(salePath));
        var emitter = PreparationJson.ParseEmitter(ReadInput(emitterPath));
        var context = PreparationJson.ParseContext(ReadInput(contextPath));
        var target = Path.GetFullPath(outputPath);
        if (Directory.Exists(target) || File.Exists(target))
            throw new IOException("Escolha um diretório novo; os artefatos existentes não são sobrescritos.");
        progress?.Invoke("Preparando o XML e o QR Code");
        var prepared = NfcePreparation.BuildUnsigned(sale, emitter, context);
        var unsigned = QrCode3.AddToInvoice(prepared.UnsignedXml);
        progress?.Invoke("Carregando os schemas e assinando com certificado sintético");
        var validator = OfflineFixtures.Validator();
        var pfx = OfflineFixtures.RsaPkcs12();
        string signed;
        try
        {
            using var certificate = A1Signer.ImportPkcs12(pfx, OfflineFixtures.Password.AsSpan());
            signed = A1Signer.Sign(unsigned, certificate);
            validator.RequireValid("nfe", signed);
        }
        finally { CryptographicOperations.ZeroMemory(pfx); }
        progress?.Invoke("Verificando assinatura e montando mensagens");
        var proof = A1Signer.Verify(signed);
        var submission = NfceMessages.BuildSubmission(signed, "1", validator);
        var consultation = NfceMessages.BuildConsultation(proof.Key, validator);
        if (A1Signer.Verify(submission).DigestBase64 != proof.DigestBase64)
            throw new CryptographicException("Digest alterado no envelope de envio.");
        var artifacts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["unsigned.xml"] = unsigned,
            ["signed.xml"] = signed,
            ["submission.xml"] = submission,
            ["consultation.xml"] = consultation
        };
        var summary = new
        {
            kind = "NFCE_INPUT_MODEL_PREPARATION",
            modelVersion = "nfce-preparation/0.1",
            status = "MODELO PREPARADO LOCALMENTE - SEM AUTORIZACAO FISCAL",
            recordedAtUtc = DateTimeOffset.UtcNow,
            baseline = validator.Manifest.BaselineId,
            manifestSha256 = validator.Manifest.ManifestSha256,
            key = proof.Key.Value,
            environment = "2", model = "65", uf = "53", emissionType = "1",
            xmlSha256 = proof.XmlSha256,
            fiscalDigestBase64 = proof.DigestBase64,
            signatureInSubmissionVerified = true,
            artifacts = artifacts.Select(x => new { name = x.Key, sha256 = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(x.Value))) }),
            outputDirectory = target,
            fiscalIo = false, taxpayerCertificate = false, fiscalHomologationObtained = false
        };
        var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
        // Todas as entradas e todos os XMLs são validados antes de criar a saída.
        progress?.Invoke("Gravando os artefatos validados");
        Directory.CreateDirectory(target);
        foreach (var artifact in artifacts) WriteNew(Path.Combine(target, artifact.Key), artifact.Value);
        WriteNew(Path.Combine(target, "summary.json"), json);
        if (output is null) Console.WriteLine(json); else output(json);
        return 0;
    }

    private static string ReadInput(string path)
    {
        if (new FileInfo(path).Length > 8 * 1024 * 1024)
            throw new InvalidDataException("Arquivo de modelo excede o limite local de leitura de 8 MiB.");
        return File.ReadAllText(path, Encoding.UTF8);
    }

    private static void WriteNew(string path, string content)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(content);
    }
}
