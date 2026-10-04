using System.Xml;
using EmissorFiscal.Core.Xml;
using EmissorFiscal.Samples;
using Xunit;

namespace EmissorFiscal.Core.Tests;

public sealed class SafeXmlTests
{
    [Fact]
    public void ExternalEntityIsRejectedBeforeResolution() =>
        Assert.Throws<XmlException>(() => SafeXml.Load(OfflineFixtures.Fixture("xxe.xml")));

    [Theory]
    [InlineData("<!DOCTYPE x SYSTEM 'file:///C:/never-read'><x />")]
    [InlineData("<!DOCTYPE x [<!ENTITY a 'abc'>]><x>&a;</x>")]
    [InlineData("<!DOCTYPE x [<!ENTITY a SYSTEM 'http://127.0.0.1:1/never-read'>]><x>&a;</x>")]
    public void AllDtdsAreProhibited(string xml) => Assert.Throws<XmlException>(() => SafeXml.Load(xml));

    [Fact]
    public void TruncatedXmlIsRejected() => Assert.Throws<XmlException>(() =>
        SafeXml.Load(OfflineFixtures.Fixture("truncated-response.xml")));

    [Fact]
    public void ExcessiveDocumentIsRejected() => Assert.Throws<XmlException>(() =>
        SafeXml.Load("<x>" + new string('a', SafeXml.MaxDocumentCharacters) + "</x>"));
}
