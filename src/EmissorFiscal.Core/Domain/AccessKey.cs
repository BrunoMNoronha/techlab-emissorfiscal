using System.Globalization;
using System.Text.RegularExpressions;

namespace EmissorFiscal.Core.Domain;

// Chave e CNPJ permanecem strings, inclusive para CNPJ alfanumérico.
public sealed record AccessKey
{
    private static readonly Regex Shape = new(
        "^[0-9]{6}[A-Z0-9]{12}[0-9]{26}$", RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));
    public string Value { get; }
    public string Cnpj => Value.Substring(6, 14);
    public string Model => Value.Substring(20, 2);
    public string Uf => Value[..2];
    public string EmissionType => Value.Substring(34, 1);

    private AccessKey(string value) => Value = value;

    public static AccessKey Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length != 44 || !Shape.IsMatch(value) || CalculateDigit(value[..43]) != value[43] - '0')
            throw new InvalidDataException("Chave de acesso inválida.");
        ValidateCnpj(value.Substring(6, 14));
        var month = int.Parse(value.AsSpan(4, 2), CultureInfo.InvariantCulture);
        if (month is < 1 or > 12)
            throw new InvalidDataException("Mês inválido na chave.");
        return new AccessKey(value);
    }

    public static AccessKey Create(string yearMonth, string cnpj, string series, string number, string numericCode)
    {
        if (!Regex.IsMatch(yearMonth, "^[0-9]{4}$", RegexOptions.CultureInvariant) ||
            !Regex.IsMatch(series, "^[0-9]{3}$", RegexOptions.CultureInvariant) ||
            !Regex.IsMatch(number, "^[0-9]{9}$", RegexOptions.CultureInvariant) || number == "000000000" ||
            !Regex.IsMatch(numericCode, "^[0-9]{8}$", RegexOptions.CultureInvariant))
            throw new InvalidDataException("Componentes da chave inválidos.");
        ValidateCnpj(cnpj);
        var basis = $"53{yearMonth}{cnpj}65{series}{number}1{numericCode}";
        return Parse(basis + CalculateDigit(basis).ToString(CultureInfo.InvariantCulture));
    }

    public void RequireOfflineScope()
    {
        if (Uf != "53" || Model != "65" || EmissionType != "1")
            throw new InvalidDataException("Recorte local: NFC-e 65, DF 53, emissão normal 1.");
    }

    // NT conjunta CNPJ: valor ASCII - 48 para letras e dígitos; pesos 2..9 da direita.
    public static int CalculateDigit(string basis)
    {
        if (basis.Length != 43 || !Regex.IsMatch(basis, "^[0-9]{6}[A-Z0-9]{12}[0-9]{25}$", RegexOptions.CultureInvariant))
            throw new InvalidDataException("Base da chave deve ter 43 posições no leiaute oficial.");
        var sum = 0;
        var weight = 2;
        for (var i = basis.Length - 1; i >= 0; i--)
        {
            sum += (basis[i] - '0') * weight;
            weight = weight == 9 ? 2 : weight + 1;
        }
        var remainder = sum % 11;
        return remainder is 0 or 1 ? 0 : 11 - remainder;
    }

    public static void ValidateCnpj(string cnpj)
    {
        if (cnpj is null || cnpj.Length != 14 ||
            !Regex.IsMatch(cnpj, "^[A-Z0-9]{12}[0-9]{2}$", RegexOptions.CultureInvariant) ||
            cnpj.Distinct().Count() == 1 ||
            CnpjDigit(cnpj[..12]) != cnpj[12] - '0' || CnpjDigit(cnpj[..13]) != cnpj[13] - '0')
            throw new InvalidDataException("CNPJ numérico/alfanumérico inválido.");
    }

    private static int CnpjDigit(string basis)
    {
        var sum = 0;
        var weight = 2;
        for (var i = basis.Length - 1; i >= 0; i--)
        {
            sum += (basis[i] - '0') * weight;
            weight = weight == 9 ? 2 : weight + 1;
        }
        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
