using System.Text.RegularExpressions;

namespace EmissorFiscal.Core.Numbering;

// Pool canônico de numeração: identidade fiscal + modelo + ambiente + série.
// A identidade é opaca e já canônica, resolvida pela camada de identidade
// (EF-08): aliases de um mesmo emitente chegam aqui como a mesma string e,
// por isso, nunca criam pool próprio. Modelo e ambiente seguem o recorte local.
public sealed record NumberPoolKey
{
    public const string Model = "65";
    public const string Environment = "2";

    private static readonly Regex IdentityShape = new(
        "^[\\x21-\\x7E]{1,128}$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
    // Aceita a forma do XML (sem zeros à esquerda) e a da chave de acesso (três dígitos).
    private static readonly Regex SeriesShape = new(
        "^(0|[1-9][0-9]{0,2}|[0-9]{3})$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    public string FiscalIdentity { get; }
    // Sempre três dígitos, como na chave de acesso; "1" e "001" são o mesmo pool.
    public string Series { get; }

    private NumberPoolKey(string fiscalIdentity, string series)
    {
        FiscalIdentity = fiscalIdentity;
        Series = series;
    }

    public static NumberPoolKey Create(string fiscalIdentity, string series)
    {
        ArgumentNullException.ThrowIfNull(fiscalIdentity);
        ArgumentNullException.ThrowIfNull(series);
        if (!IdentityShape.IsMatch(fiscalIdentity))
            throw new InvalidDataException("Identidade fiscal canônica inválida: 1-128 caracteres ASCII visíveis, sem espaços.");
        if (!SeriesShape.IsMatch(series))
            throw new InvalidDataException("Série inválida: 0-999 sem zeros à esquerda ou com exatamente três dígitos.");
        return new NumberPoolKey(fiscalIdentity, series.PadLeft(3, '0'));
    }

    public override string ToString() => $"{FiscalIdentity}/{Model}/{Environment}/{Series}";
}
