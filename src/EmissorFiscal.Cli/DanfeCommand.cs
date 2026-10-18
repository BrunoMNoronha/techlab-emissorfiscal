using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EmissorFiscal.Core.Danfe;
using EmissorFiscal.Core.Protocols;
using EmissorFiscal.Core.Xml;
using EmissorFiscal.Samples;

namespace EmissorFiscal.Cli;

// danfe <nfeProc.xml | NFe-assinada.xml> <diretorio-novo>
// Com um nfeProc, renderiza o DANFE daquele protocolo. Com uma NFe assinada
// (saída `signed.xml` do `prepare`), simula a autorização com o protocolo
// sintético de OfflineFixtures (cStat 100) e grava também o nfeProc gerado,
// rotulado como sintético. Nenhum transporte; nada é consultado.
internal static class DanfeCommand
{
    public static int Run(string inputPath, string outputPath, Action<string>? progress = null, Action<string>? output = null)
    {
        progress?.Invoke("Lendo o XML e carregando os schemas");
        var target = Path.GetFullPath(outputPath);
        if (Directory.Exists(target) || File.Exists(target))
            throw new IOException("Escolha um diretório novo; os artefatos existentes não são sobrescritos.");
        var input = ReadInput(inputPath);
        var validator = OfflineFixtures.Validator();
        var root = SafeXml.Load(input).DocumentElement?.LocalName;
        var synthetic = root == "NFe";
        string processed;
        if (synthetic)
        {
            validator.RequireValid("nfe", input);
            processed = NfceProtocol.BuildProcessed(input, OfflineFixtures.Response(input), validator);
        }
        else if (root == "nfeProc") processed = input;
        else throw new InvalidDataException("Entrada deve ser nfeProc ou NFe assinada.");
        progress?.Invoke("Gerando o DANFE e o QR Code");
        var danfe = DanfeRenderer.Render(processed, validator);
        var artifacts = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["danfe.txt"] = danfe.Text,
            ["danfe-qr.svg"] = danfe.QrSvg
        };
        if (synthetic) artifacts["processed.xml"] = processed;
        var summary = new
        {
            kind = "NFCE_DANFE_OFFLINE",
            status = synthetic
                ? "DANFE DE PROVA LOCAL - PROTOCOLO SINTETICO - SEM VALOR FISCAL"
                : "DANFE RENDERIZADO LOCALMENTE - AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL",
            recordedAtUtc = DateTimeOffset.UtcNow,
            baseline = validator.Manifest.BaselineId,
            manifestSha256 = validator.Manifest.ManifestSha256,
            key = danfe.Model.Key.Value,
            environment = danfe.Model.Environment, model = "65", uf = "53",
            number = danfe.Model.Number, series = danfe.Model.Series,
            fiscalState = danfe.Model.State.ToString(),
            protocol = danfe.Model.Protocol.Number,
            protocolCode = danfe.Model.Protocol.Code,
            alerts = danfe.Model.Alerts,
            syntheticProtocol = synthetic,
            columns = DanfeText.Columns,
            textLines = danfe.Text.Count(c => c == '\n'),
            qrText = danfe.Model.QrCodeText,
            qrVersion = danfe.QrCode.Version,
            qrSize = danfe.QrCode.Size,
            qrErrorCorrection = "M",
            artifacts = artifacts.Select(x => new { name = x.Key, sha256 = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(x.Value))) }),
            outputDirectory = target,
            fiscalIo = false, taxpayerCertificate = false, fiscalHomologationObtained = false
        };
        var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
        progress?.Invoke("Gravando o DANFE de prova");
        Directory.CreateDirectory(target);
        foreach (var artifact in artifacts) WriteNew(Path.Combine(target, artifact.Key), artifact.Value);
        WriteNew(Path.Combine(target, "summary.json"), json);
        if (output is null) Console.WriteLine(json); else output(json);
        return 0;
    }

    private static string ReadInput(string path)
    {
        if (new FileInfo(path).Length > 8 * 1024 * 1024)
            throw new InvalidDataException("Arquivo de entrada excede o limite local de leitura de 8 MiB.");
        return File.ReadAllText(path, Encoding.UTF8);
    }

    private static void WriteNew(string path, string content)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(content);
    }
}
