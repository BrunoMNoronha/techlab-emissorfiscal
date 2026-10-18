using System.Text;
using System.Text.Json;
using EmissorFiscal.Core.Preparation;

namespace EmissorFiscal.Core.Idempotency;

// Forma canônica determinística de um documento JSON do contrato de preparação:
// propriedades ordenadas por código UTF-16 (como o JCS, RFC 8785), sem espaços,
// strings reescritas pelo escritor padrão (escapes normalizados) e números
// mantidos com o texto literal recebido — "1.0" e "1.00" são conteúdos distintos,
// porque o contrato preserva escala em strings e não interpreta números.
public static class CanonicalJson
{
    private const int SafeLimit = 1024 * 1024;
    private static readonly JsonDocumentOptions ReadOptions = new() { MaxDepth = 32 };
    private static readonly JsonWriterOptions WriteOptions = new() { Indented = false, SkipValidation = false };

    public static byte[] Canonicalize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        if (json.Length > SafeLimit) throw new InvalidDataException("JSON excede o limite local de 1.048.576 caracteres.");
        try
        {
            using var document = JsonDocument.Parse(json, ReadOptions);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidDataException("A raiz JSON deve ser um objeto.");
            PreparationJson.RejectAmbiguity(document.RootElement);
            using var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer, WriteOptions))
                Write(writer, document.RootElement);
            return buffer.ToArray();
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("JSON inválido para canonicalização: " + exception.Message, exception);
        }
    }

    public static string CanonicalizeToString(string json) => Encoding.UTF8.GetString(Canonicalize(json));

    private static void Write(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject()
                             .OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    Write(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var child in element.EnumerateArray()) Write(writer, child);
                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;
            case JsonValueKind.Number:
                writer.WriteRawValue(element.GetRawText(), skipInputValidation: false);
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            default:
                throw new InvalidDataException("Valor JSON não suportado na forma canônica.");
        }
    }
}
