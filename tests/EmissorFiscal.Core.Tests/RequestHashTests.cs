using EmissorFiscal.Core.Idempotency;
using Xunit;

namespace EmissorFiscal.Core.Tests;

public sealed class RequestHashTests
{
    private const string Emitter = "{\"profileId\":\"p1\",\"cnpj\":\"12345678000195\"}";

    private static string Sample(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "preparation", name + ".json"));

    [Fact]
    public void PropertyOrderAndWhitespaceDoNotChangeTheHash()
    {
        var a = RequestHash.Compute("{\"b\":\"2\",\"a\":{\"y\":\"1\",\"x\":[\"1\",\"2\"]}}", Emitter);
        var b = RequestHash.Compute(" {\n \"a\" : { \"x\" : [ \"1\" , \"2\" ] , \"y\" : \"1\" } ,\n \"b\" : \"2\" }\n", Emitter);
        Assert.Equal(a, b);
        Assert.Matches("^rh1:[0-9a-f]{64}$", a.Value);
    }

    [Fact]
    public void ArrayOrderIsContent()
    {
        var a = RequestHash.Compute("{\"items\":[\"1\",\"2\"]}", Emitter);
        var b = RequestHash.Compute("{\"items\":[\"2\",\"1\"]}", Emitter);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void UnicodeEscapesNormalizeToTheSameContent()
    {
        var a = RequestHash.Compute("{\"xProd\":\"caf\\u00e9\"}", Emitter);
        var b = RequestHash.Compute("{\"xProd\":\"café\"}", Emitter);
        Assert.Equal(a, b);
        Assert.Equal("{\"xProd\":\"caf\\u00E9\"}", CanonicalJson.CanonicalizeToString("{\"xProd\":\"café\"}"));
    }

    [Fact]
    public void NumericLiteralsArePreservedSoScaleIsContent()
    {
        // O contrato usa strings para valores; números JSON, se aparecerem, não são reinterpretados.
        Assert.NotEqual(RequestHash.Compute("{\"n\":1.0}", Emitter), RequestHash.Compute("{\"n\":1.00}", Emitter));
        Assert.NotEqual(RequestHash.Compute("{\"n\":1}", Emitter), RequestHash.Compute("{\"n\":\"1\"}", Emitter));
        Assert.Equal("{\"n\":1.50}", CanonicalJson.CanonicalizeToString("{ \"n\" : 1.50 }"));
    }

    [Theory]
    [InlineData("{\"a\":\"1\",\"a\":\"2\"}")]
    [InlineData("{\"a\":null}")]
    [InlineData("{\"a\":\"1\",}")]
    [InlineData("{\"a\":\"1\"} // comentário")]
    [InlineData("[\"a\"]")]
    [InlineData("\"a\"")]
    [InlineData("")]
    [InlineData("{\"a\":{\"a\":null}}")]
    public void AmbiguousOrNonObjectJsonCannotBeHashed(string json) =>
        Assert.Throws<InvalidDataException>(() => RequestHash.Compute(json, Emitter));

    [Fact]
    public void EmitterAndSaleOccupyFixedPositionsInTheEnvelope()
    {
        var a = RequestHash.Compute("{\"k\":\"sale\"}", "{\"k\":\"emitter\"}");
        var b = RequestHash.Compute("{\"k\":\"emitter\"}", "{\"k\":\"sale\"}");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void SampleDocumentsHashDeterministicallyAndDifferByContent()
    {
        var crt1 = RequestHash.Compute(Sample("sale-crt1"), Sample("emitter-crt1"));
        Assert.Equal(crt1, RequestHash.Compute(Sample("sale-crt1"), Sample("emitter-crt1")));
        Assert.NotEqual(crt1, RequestHash.Compute(Sample("sale-crt3"), Sample("emitter-crt3")));
        Assert.NotEqual(crt1, RequestHash.Compute(Sample("sale-st-crt1"), Sample("emitter-crt1")));
        // O contexto de numeração não participa: a mesma venda recebe o mesmo hash com qualquer nNF.
        Assert.DoesNotContain("numericCode", CanonicalJson.CanonicalizeToString(Sample("sale-crt1")));
    }

    [Fact]
    public void ParseAcceptsOnlyTheVersionedLowercaseForm()
    {
        var computed = RequestHash.Compute("{\"a\":\"1\"}", Emitter);
        Assert.Equal(computed, RequestHash.Parse(computed.Value));
        Assert.Equal(computed.Value, computed.ToString());
        Assert.Throws<InvalidDataException>(() => RequestHash.Parse(computed.Value.ToUpperInvariant()));
        Assert.Throws<InvalidDataException>(() => RequestHash.Parse("rh2:" + computed.Value[4..]));
        Assert.Throws<InvalidDataException>(() => RequestHash.Parse(computed.Value[4..]));
    }

    [Fact]
    public void OversizedDocumentsAreRefusedBeforeParsing()
    {
        var huge = "{\"a\":\"" + new string('x', 1024 * 1024) + "\"}";
        Assert.Throws<InvalidDataException>(() => CanonicalJson.Canonicalize(huge));
        Assert.Equal("{\"a\":false,\"z\":true}", CanonicalJson.CanonicalizeToString("{\"z\":true,\"a\":false}"));
    }
}
