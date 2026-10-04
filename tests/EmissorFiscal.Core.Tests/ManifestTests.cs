using System.Security.Cryptography;
using System.Text.Json.Nodes;
using EmissorFiscal.Core.Xsd;
using Xunit;

namespace EmissorFiscal.Core.Tests;

public sealed class ManifestTests
{
    [Fact]
    public void ChangedXsdHashFails()
    {
        using var copy = new ManifestCopy();
        File.AppendAllText(Path.Combine(copy.Root, "files", "leiauteNFe_v4.00.xsd"), "\n");
        Assert.Throws<InvalidDataException>(() => SchemaManifest.Load(copy.Manifest));
    }

    [Fact]
    public void MissingDependencyFails()
    {
        using var copy = new ManifestCopy();
        File.Delete(Path.Combine(copy.Root, "files", "tiposBasico_v4.00.xsd"));
        Assert.Throws<FileNotFoundException>(() => SchemaManifest.Load(copy.Manifest));
    }

    [Fact]
    public void ExternalDependencyFailsEvenWhenItsLocalHashIsUpdated()
    {
        using var copy = new ManifestCopy();
        var path = Path.Combine(copy.Root, "files", "nfe_v4.00.xsd");
        File.WriteAllText(path, File.ReadAllText(path).Replace("schemaLocation=\"leiauteNFe_v4.00.xsd\"",
            "schemaLocation=\"https://invalid.example/external.xsd\"", StringComparison.Ordinal));
        copy.Edit(node =>
        {
            var file = node["files"]!.AsArray().Single(n => n!["name"]!.GetValue<string>() == "nfe_v4.00.xsd")!;
            file["sha256"] = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
        });
        Assert.Throws<InvalidDataException>(() => SchemaManifest.Load(copy.Manifest));
    }

    [Fact]
    public void DependencyDeclarationMustMatchActualSchema()
    {
        using var copy = new ManifestCopy();
        copy.Edit(node => node["files"]!.AsArray().Single(n => n!["name"]!.GetValue<string>() == "nfe_v4.00.xsd")!["dependencies"] = new JsonArray());
        Assert.Throws<InvalidDataException>(() => SchemaManifest.Load(copy.Manifest));
    }

    [Fact]
    public void PathTraversalIsRejected()
    {
        using var copy = new ManifestCopy();
        copy.Edit(node => node["files"]![0]!["path"] = "../../outside.xsd");
        Assert.Throws<InvalidDataException>(() => SchemaManifest.Load(copy.Manifest));
    }

    [Fact]
    public void NewPackageRequiresExplicitBaselineReview()
    {
        using var copy = new ManifestCopy();
        copy.Edit(node => node["packages"]![2]!["sha256"] = new string('0', 64));
        Assert.Throws<InvalidDataException>(() => SchemaManifest.Load(copy.Manifest));
    }

    private sealed class ManifestCopy : IDisposable
    {
        public string Root { get; } = Path.Combine(AppContext.BaseDirectory, "schema-proof-" + Guid.NewGuid().ToString("N"));
        public string Manifest => Path.Combine(Root, "manifest.json");
        public ManifestCopy()
        {
            var source = Path.Combine(AppContext.BaseDirectory, "schemas", "nfe", "4.00");
            Directory.CreateDirectory(Path.Combine(Root, "files"));
            File.Copy(Path.Combine(source, "manifest.json"), Manifest);
            foreach (var file in Directory.EnumerateFiles(Path.Combine(source, "files"), "*.xsd"))
                File.Copy(file, Path.Combine(Root, "files", Path.GetFileName(file)));
        }
        public void Edit(Action<JsonNode> edit)
        {
            var node = JsonNode.Parse(File.ReadAllText(Manifest))!;
            edit(node);
            File.WriteAllText(Manifest, node.ToJsonString());
        }
        public void Dispose()
        {
            var full = Path.GetFullPath(Root);
            if (Path.GetDirectoryName(full) != Path.TrimEndingDirectorySeparator(Path.GetFullPath(AppContext.BaseDirectory)) ||
                !Path.GetFileName(full).StartsWith("schema-proof-", StringComparison.Ordinal))
                throw new InvalidOperationException("Limpeza restrita ao diretório temporário desta prova.");
            Directory.Delete(full, recursive: true);
        }
    }
}
