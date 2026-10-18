using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace EmissorFiscal.Core.Idempotency;

// Hash versionado do conteúdo de uma solicitação de emissão: venda e perfil do
// emitente, em forma canônica, dentro de um envelope de posições fixas. O
// contexto de numeração (nNF/cNF) fica fora: é resultado da reserva, não entrada.
// Mudar a canonicalização exige nova versão; "rh1" nunca muda de significado.
public sealed record RequestHash
{
    public const string Version = "rh1";
    private static readonly Regex Shape = new(
        "^rh1:[0-9a-f]{64}$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    public string Value { get; }

    private RequestHash(string value) => Value = value;

    public static RequestHash Compute(string saleJson, string emitterJson)
    {
        var sale = CanonicalJson.Canonicalize(saleJson);
        var emitter = CanonicalJson.Canonicalize(emitterJson);
        using var buffer = new MemoryStream();
        buffer.Write("{\"emitter\":"u8);
        buffer.Write(emitter);
        buffer.Write(",\"sale\":"u8);
        buffer.Write(sale);
        buffer.Write("}"u8);
        var digest = SHA256.HashData(buffer.ToArray());
        return new RequestHash(Version + ":" + Convert.ToHexStringLower(digest));
    }

    public static RequestHash Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!Shape.IsMatch(value)) throw new InvalidDataException("requestHash inválido; esperado rh1:<sha-256 em hex minúsculo>.");
        return new RequestHash(value);
    }

    public override string ToString() => Value;
}
