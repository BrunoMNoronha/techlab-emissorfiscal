using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml;
using EmissorFiscal.Core.Xml;

namespace EmissorFiscal.Core.Xsd;

public sealed record SchemaPackage(string Id, string Archive, string Sha256, string Origin,
    string Catalog, string PublishedOn, string Subtree);
public sealed record SchemaReplacement(string Name, string Path, string Sha256, string PackageId, string ArchiveEntry);
public sealed record SchemaFile(string Name, string Path, string Sha256, string PackageId, string ArchiveEntry,
    string[] Dependencies, SchemaReplacement[] Supersedes);
public sealed record ManifestData(int ManifestVersion, string BaselineId, string ReviewedOn,
    string[] Precedence, SchemaPackage[] Packages, Dictionary<string, string> Entrypoints, SchemaFile[] Files);

public sealed class SchemaManifest
{
    public const string Baseline = "nfe-4.00-offline-2026-10-04";
    internal const string VirtualBase = "https://schemas.offline.invalid/nfe4/";
    private readonly Dictionary<string, byte[]> _contents;
    public string BaselineId => Baseline;
    public string ManifestSha256 { get; }
    public IReadOnlyDictionary<string, string> Entrypoints { get; }
    public IReadOnlyList<SchemaFile> Files { get; }

    private SchemaManifest(ManifestData data, Dictionary<string, byte[]> contents, string manifestHash)
    {
        _contents = contents;
        Entrypoints = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(data.Entrypoints, StringComparer.Ordinal));
        Files = Array.AsReadOnly(data.Files);
        ManifestSha256 = manifestHash;
    }

    // Apenas I/O local: leitura limitada, sem links/reparse points; bytes verificados ficam em memória.
    public static SchemaManifest Load(string manifestPath)
    {
        var absolutePath = Path.GetFullPath(manifestPath);
        RejectLinks(absolutePath);
        var root = Path.GetDirectoryName(absolutePath) ?? throw new InvalidDataException("Manifest sem diretório.");
        var manifestBytes = ReadLimited(absolutePath);
        var data = JsonSerializer.Deserialize<ManifestData>(manifestBytes, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        }) ?? throw new InvalidDataException("Manifest vazio.");
        CheckMetadata(data);
        var contents = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var file in data.Files)
        {
            if (!ValidName(file.Name) || file.Path != $"files/{file.Name}")
                throw new InvalidDataException("Caminho XSD fora do recorte do manifest.");
            var path = Path.GetFullPath(Path.Combine(root, file.Path));
            RejectLinks(path);
            var bytes = ReadLimited(path);
            if (Convert.ToHexStringLower(SHA256.HashData(bytes)) != file.Sha256)
                throw new InvalidDataException($"SHA-256 divergente: {file.Name}.");
            if (!contents.TryAdd(file.Name, bytes))
                throw new InvalidDataException("Arquivo XSD duplicado no manifest.");
        }
        foreach (var file in data.Files)
        {
            var document = SafeXml.Load(contents[file.Name]);
            if (document.DocumentElement?.LocalName != "schema" ||
                document.DocumentElement.NamespaceURI != "http://www.w3.org/2001/XMLSchema")
                throw new InvalidDataException("Arquivo não é um schema XSD.");
            var actual = new HashSet<string>(StringComparer.Ordinal);
            foreach (var element in document.DocumentElement.ChildNodes.OfType<XmlElement>())
            {
                if (element.NamespaceURI != "http://www.w3.org/2001/XMLSchema" ||
                    element.LocalName is not ("include" or "import" or "redefine")) continue;
                var dependency = element.GetAttribute("schemaLocation");
                if (!ValidName(dependency) || !contents.ContainsKey(dependency))
                    throw new InvalidDataException($"Dependência ausente/externa: {file.Name}.");
                actual.Add(dependency);
            }
            if (file.Dependencies is null || !actual.SetEquals(file.Dependencies) ||
                file.Dependencies.Distinct(StringComparer.Ordinal).Count() != file.Dependencies.Length)
                throw new InvalidDataException($"Dependências divergem do manifest: {file.Name}.");
        }
        foreach (var entry in data.Entrypoints.Values)
            if (!contents.ContainsKey(entry)) throw new InvalidDataException("Entrypoint ausente.");
        return new(data, contents, Convert.ToHexStringLower(SHA256.HashData(manifestBytes)));
    }

    private static void CheckMetadata(ManifestData data)
    {
        var packageHashes = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["PL009n"] = "8e269cd5d5d60532b709c8a88bf590323b94002efd3a49935fb5fd2db5957a62",
            ["PL010d"] = "45ceefe4dfbbfec93958283b650a2f1e1734784f4770d070b9907754de081d9b",
            ["PL010f"] = "b8589490a58a09a993a80e6ac4d7ed10f20892061ecfc56719337098d4b95998"
        };
        var names = new[] { "nfe", "enviNFe", "retEnviNFe", "consSitNFe", "retConsSitNFe", "procNFe" };
        if (data.ManifestVersion != 1 || data.BaselineId != Baseline || data.ReviewedOn != "2026-10-04" ||
            data.Precedence is null || !data.Precedence.SequenceEqual(new[] { "PL009n", "PL010d", "PL010f" }) ||
            data.Packages is null || data.Packages.Length != 3 ||
            data.Packages.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count() != 3 ||
            data.Files is null || data.Files.Length != 11 || data.Entrypoints is null ||
            !new HashSet<string>(names, StringComparer.Ordinal).SetEquals(data.Entrypoints.Keys))
            throw new InvalidDataException("Baseline não revisada; pacote novo exige revisão explícita.");
        foreach (var name in names)
            if (data.Entrypoints[name] != $"{name}_v4.00.xsd")
                throw new InvalidDataException("Entrypoint divergente.");
        foreach (var package in data.Packages)
            if (!packageHashes.TryGetValue(package.Id, out var hash) || hash != package.Sha256 ||
                !Uri.TryCreate(package.Origin, UriKind.Absolute, out var origin) || origin.Scheme != "https" ||
                origin.Host is not ("www.nfe.fazenda.gov.br" or "dfe-portal.svrs.rs.gov.br"))
                throw new InvalidDataException("Pacote/origem não revisado.");
        foreach (var file in data.Files)
        {
            var package = data.Packages.SingleOrDefault(p => p.Id == file.PackageId);
            if (package is null || file.Supersedes is null ||
                file.ArchiveEntry != package.Subtree + file.Name ||
                !Regex.IsMatch(file.Sha256 ?? "", "^[0-9a-f]{64}$", RegexOptions.CultureInvariant))
                throw new InvalidDataException("Proveniência/hash XSD inválido.");
        }
    }

    private static bool ValidName(string name) => name is not null &&
        Regex.IsMatch(name, "^[A-Za-z0-9_-][A-Za-z0-9_.-]*\\.xsd$", RegexOptions.CultureInvariant) &&
        !name.Contains("..", StringComparison.Ordinal);

    private static byte[] ReadLimited(string path)
    {
        using var stream = File.OpenRead(path);
        if (stream.Length > SafeXml.MaxDocumentCharacters) throw new InvalidDataException("Arquivo local excessivo.");
        var bytes = new byte[checked((int)stream.Length)];
        stream.ReadExactly(bytes);
        return bytes;
    }

    private static void RejectLinks(string path)
    {
        for (string? current = path; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Links/reparse points não são aceitos nos schemas.");
    }

    internal Stream Open(string name) => _contents.TryGetValue(name, out var bytes)
        ? new MemoryStream(bytes, writable: false)
        : throw new XmlException("Schema não consta do manifest.");

    internal XmlResolver Resolver() => new ManifestResolver(this);

    private sealed class ManifestResolver(SchemaManifest manifest) : XmlResolver
    {
        public override System.Net.ICredentials? Credentials { set { } }
        public override Uri ResolveUri(Uri? baseUri, string? relativeUri)
        {
            var resolved = base.ResolveUri(baseUri, relativeUri);
            RequireListed(resolved);
            return resolved;
        }
        public override object GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
        {
            var name = RequireListed(absoluteUri);
            if (ofObjectToReturn is not null && ofObjectToReturn != typeof(Stream))
                throw new XmlException("Resolver somente fornece streams locais.");
            return manifest.Open(name);
        }
        private string RequireListed(Uri uri)
        {
            var name = uri.Segments.LastOrDefault() ?? "";
            if (uri.AbsoluteUri != VirtualBase + name || !manifest._contents.ContainsKey(name))
                throw new XmlException("Resolução externa ou não manifestada proibida.");
            return name;
        }
    }
}
