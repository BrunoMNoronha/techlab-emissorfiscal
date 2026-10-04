using EmissorFiscal.Core.Domain;
using EmissorFiscal.Core.QrCode;
using Xunit;

namespace EmissorFiscal.Core.Tests;

public sealed class AccessKeyAndQrTests
{
    [Fact]
    public void OfficialMocNumericExampleHasDigit5()
    {
        // MOC 7, visão geral, 2.2.6.2, pp.19-20: soma 644, resto 6, DV 5.
        const string basis = "5206043300991100250655012000000780026730161";
        Assert.Equal(43, basis.Length);
        Assert.Equal(5, AccessKey.CalculateDigit(basis));
    }

    [Theory]
    [InlineData("12345678000195")]
    [InlineData("12ABC34501DE35")]
    public void NumericAndOfficialRfbAlphanumericCnpjRemainStrings(string cnpj)
    {
        // RFB, manual DV CNPJ alfanumérico: 12.ABC.345/01DE-35; NT conjunta, Anexo I.
        AccessKey.ValidateCnpj(cnpj);
        var key = AccessKey.Create("2610", cnpj, "001", "000000001", "12345678");
        Assert.Equal(cnpj, key.Cnpj);
        Assert.Equal(key, AccessKey.Parse(key.Value));
        Assert.Equal(44, key.Value.Length);
    }

    [Theory]
    [InlineData("12abc34501de35")]
    [InlineData("12ABC34501DE36")]
    [InlineData("00000000000000")]
    [InlineData("１２ABC34501DE35")]
    public void InvalidCnpjFails(string value) => Assert.Throws<InvalidDataException>(() => AccessKey.ValidateCnpj(value));

    [Fact]
    public void WrongKeyDigitFails() => Assert.Throws<InvalidDataException>(() =>
        AccessKey.Parse("53261012345678000195650010000000011123456783"));

    [Fact]
    public void OfficialAlphanumericAlgorithmVectorHasExpectedDigit()
    {
        // Vetor derivado aplicando o código oficial do Anexo II da NT conjunta à fixture.
        const string basis = "53261012ABC34501DE3565001000000001112345678";
        Assert.Equal(43, basis.Length);
        // Resultado independente registrado em docs/VALIDATION.md.
        Assert.Equal(2, AccessKey.CalculateDigit(basis));
    }

    [Fact]
    public void Qr3OnlineHasExactlyThreeParametersAndNoCsc()
    {
        var key = AccessKey.Create("2610", "12ABC34501DE35", "001", "000000001", "12345678");
        Assert.Equal($"http://www.fazenda.df.gov.br/nfce/qrcode?p={key.Value}|3|2", QrCode3.Online(key));
        Assert.Equal(3, QrCode3.Online(key).Split("?p=", StringSplitOptions.None)[1].Split('|').Length);
        Assert.Throws<InvalidDataException>(() => QrCode3.Online(key, "1"));
    }

    [Theory]
    [InlineData("35", "65", "1")]
    [InlineData("53", "55", "1")]
    [InlineData("53", "65", "9")]
    public void QrDeniesOtherScopes(string uf, string model, string emission)
    {
        var original = AccessKey.Create("2610", "12345678000195", "001", "000000001", "12345678").Value;
        var basis = uf + original.Substring(2, 18) + model + original.Substring(22, 12) + emission + original.Substring(35, 8);
        var key = AccessKey.Parse(basis + AccessKey.CalculateDigit(basis));
        Assert.Throws<InvalidDataException>(() => QrCode3.Online(key));
    }
}
