using System.Xml;
using System.Xml.Schema;
using EmissorFiscal.Core.Wire;
using EmissorFiscal.Core.Xml;

namespace EmissorFiscal.Core.Xsd;

public sealed class XsdValidator
{
    private readonly Dictionary<string, XmlSchemaSet> _sets = new(StringComparer.Ordinal);
    public SchemaManifest Manifest { get; }

    public XsdValidator(SchemaManifest manifest)
    {
        Manifest = manifest;
        // Cada entrypoint tem seu conjunto: os leiautes contêm tipos homônimos.
        foreach (var entry in manifest.Entrypoints)
        {
            var set = new XmlSchemaSet { XmlResolver = manifest.Resolver() };
            set.ValidationEventHandler += (_, args) => throw new XmlSchemaException(args.Message, args.Exception);
            using var stream = manifest.Open(entry.Value);
            using var reader = XmlReader.Create(stream, SafeXml.Settings(), SchemaManifest.VirtualBase + entry.Value);
            set.Add(null, reader);
            set.Compile();
            _sets.Add(entry.Key, set);
        }
    }

    public IReadOnlyList<string> CompiledEntrypoints => _sets.Keys.Order(StringComparer.Ordinal).ToArray();

    public void RequireValid(string entrypoint, string xml)
    {
        if (!_sets.TryGetValue(entrypoint, out var set))
            throw new ArgumentException("Entrypoint não suportado.", nameof(entrypoint));
        var document = SafeXml.Load(xml);
        if (document.GetElementsByTagName("*").OfType<XmlElement>().Any(e =>
            e.HasAttribute("schemaLocation", "http://www.w3.org/2001/XMLSchema-instance") ||
            e.HasAttribute("noNamespaceSchemaLocation", "http://www.w3.org/2001/XMLSchema-instance")))
            throw new InvalidDataException("Schemas fornecidos na instância são proibidos.");
        var settings = SafeXml.Settings();
        settings.ValidationType = ValidationType.Schema;
        settings.Schemas = set;
        settings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings |
                                   XmlSchemaValidationFlags.ProcessIdentityConstraints;
        var errors = new List<string>();
        settings.ValidationEventHandler += (_, args) => errors.Add(args.Message);
        using var input = new StringReader(xml);
        using var reader = XmlReader.Create(input, settings);
        while (reader.Read()) { }
        if (errors.Count != 0) throw new XmlSchemaValidationException(string.Join(" | ", errors));
        NfceMessages.RequireScope(entrypoint, document);
    }
}
