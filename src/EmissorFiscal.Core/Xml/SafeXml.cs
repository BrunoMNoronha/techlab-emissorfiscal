using System.Text;
using System.Xml;

namespace EmissorFiscal.Core.Xml;

public static class SafeXml
{
    public const string NfeNamespace = "http://www.portalfiscal.inf.br/nfe";
    public const int MaxDocumentCharacters = 4 * 1024 * 1024;

    public static XmlReaderSettings Settings() => new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        MaxCharactersInDocument = MaxDocumentCharacters,
        MaxCharactersFromEntities = 1024,
        IgnoreWhitespace = false,
        IgnoreComments = false,
        CloseInput = false
    };

    public static XmlDocument Load(string xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        if (xml.Length > MaxDocumentCharacters)
            throw new XmlException("XML excede o limite local.");
        using var input = new StringReader(xml);
        using var reader = XmlReader.Create(input, Settings());
        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        document.Load(reader);
        return document;
    }

    public static XmlDocument Load(byte[] xml)
    {
        ArgumentNullException.ThrowIfNull(xml);
        if (xml.Length > MaxDocumentCharacters)
            throw new XmlException("XML excede o limite local.");
        using var input = new MemoryStream(xml, writable: false);
        using var reader = XmlReader.Create(input, Settings());
        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        document.Load(reader);
        return document;
    }

    // Serializa sem pretty-print: whitespace novo dentro de infNFe quebra o digest.
    public static string Serialize(XmlDocument document)
    {
        using var output = new MemoryStream();
        using (var writer = XmlWriter.Create(output, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false), Indent = false,
            OmitXmlDeclaration = true, NewLineHandling = NewLineHandling.None
        }))
            document.WriteTo(writer);
        return Encoding.UTF8.GetString(output.ToArray());
    }

    public static XmlElement RequireRoot(XmlDocument document, string name)
    {
        var root = document.DocumentElement;
        if (root is null || root.LocalName != name || root.NamespaceURI != NfeNamespace)
            throw new InvalidDataException($"Raiz esperada: {name} no namespace NF-e.");
        var versionElement = name == "NFe" ? Child(root, "infNFe") : root;
        if (versionElement.GetAttribute("versao") != "4.00")
            throw new InvalidDataException("Versão XML não suportada.");
        return root;
    }

    public static XmlElement Child(XmlElement parent, string name, string ns = NfeNamespace)
    {
        var children = Children(parent, name, ns);
        if (children.Count != 1)
            throw new InvalidDataException($"Esperado exatamente um {name}.");
        return children[0];
    }

    public static IReadOnlyList<XmlElement> Children(XmlElement parent, string name, string ns = NfeNamespace) =>
        parent.ChildNodes.OfType<XmlElement>()
            .Where(e => e.LocalName == name && e.NamespaceURI == ns).ToArray();

    public static string Text(XmlElement parent, string name) => Child(parent, name).InnerText;

    public static void Append(XmlDocument document, XmlElement parent, string name, string value)
    {
        var child = document.CreateElement(name, NfeNamespace);
        child.InnerText = value;
        parent.AppendChild(child);
    }
}
