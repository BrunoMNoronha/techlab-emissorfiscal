using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using EmissorFiscal.Core.Domain;
using EmissorFiscal.Core.Events;
using EmissorFiscal.Core.Numbering;
using EmissorFiscal.Core.Signature;
using EmissorFiscal.Core.Xml;
using EmissorFiscal.Samples;
using Xunit;

namespace EmissorFiscal.Core.Tests;

public sealed class EventsTests(OfflineProof proof) : IClassFixture<OfflineProof>
{
    private const string Justification = "Cancelamento sintetico de teste offline sem valor fiscal";
    private static readonly DateTimeOffset EventTime = new(2026, 10, 18, 10, 0, 0, TimeSpan.FromHours(-3));

    private static T WithCertificate<T>(Func<X509Certificate2, T> action)
    {
        var pfx = OfflineFixtures.RsaPkcs12();
        try
        {
            using var certificate = A1Signer.ImportPkcs12(pfx, OfflineFixtures.Password.AsSpan());
            return action(certificate);
        }
        finally { CryptographicOperations.ZeroMemory(pfx); }
    }

    private CancellationRequest Request(int sequence = 1)
    {
        var key = A1Signer.Verify(proof.Signed).Key;
        return new(key, key.Cnpj, "153260000000001", Justification, EventTime, sequence);
    }

    private string SignedCancellation(int sequence = 1) =>
        WithCertificate(certificate => NfceEvents.BuildSignedCancellation(Request(sequence), certificate, proof.Validator));

    private static string EventResponse(string signedEvent, string code = "135", string batchCode = "128",
        bool withProtocol = true, Action<XmlElement>? mutate = null)
    {
        var info = SafeXml.Child(SafeXml.Load(signedEvent).DocumentElement!, "infEvento");
        var protocol = withProtocol ? "<nProt>153260000000099</nProt>" : "";
        var xml = $"<retEnvEvento xmlns=\"{SafeXml.NfeNamespace}\" versao=\"1.00\"><idLote>1</idLote><tpAmb>2</tpAmb>" +
            $"<verAplic>OFFLINE-1</verAplic><cOrgao>53</cOrgao><cStat>{batchCode}</cStat><xMotivo>LOTE SINTETICO</xMotivo>" +
            $"<retEvento versao=\"1.00\"><infEvento><tpAmb>2</tpAmb><verAplic>OFFLINE-1</verAplic><cOrgao>53</cOrgao>" +
            $"<cStat>{code}</cStat><xMotivo>RESULTADO SINTETICO SEM VALIDADE FISCAL</xMotivo>" +
            $"<chNFe>{SafeXml.Text(info, "chNFe")}</chNFe><tpEvento>110111</tpEvento><xEvento>Cancelamento</xEvento>" +
            $"<nSeqEvento>{SafeXml.Text(info, "nSeqEvento")}</nSeqEvento><dhRegEvento>2026-10-18T10:00:05-03:00</dhRegEvento>" +
            $"{protocol}</infEvento></retEvento></retEnvEvento>";
        if (mutate is null) return xml;
        var document = SafeXml.Load(xml);
        mutate((XmlElement)document.GetElementsByTagName("infEvento", SafeXml.NfeNamespace)[0]!);
        return SafeXml.Serialize(document);
    }

    private static readonly NumberPoolKey Pool = NumberPoolKey.Create("cnpj:12345678000195", "1");

    private static InutilizationRequest InutRequest(long from = 5, long to = 7) =>
        new(Pool, "12345678000195", "26", from, to, Justification);

    private string SignedInutilization(long from = 5, long to = 7) =>
        WithCertificate(certificate => NfceInutilization.BuildSigned(InutRequest(from, to), certificate, proof.Validator));

    private static string InutResponse(string signed, string code = "102", bool withProtocol = true, bool withRange = true,
        string ambient = "2")
    {
        var info = SafeXml.Child(SafeXml.Load(signed).DocumentElement!, "infInut");
        var range = withRange
            ? $"<ano>{SafeXml.Text(info, "ano")}</ano><CNPJ>{SafeXml.Text(info, "CNPJ")}</CNPJ><mod>65</mod><serie>{SafeXml.Text(info, "serie")}</serie>" +
              $"<nNFIni>{SafeXml.Text(info, "nNFIni")}</nNFIni><nNFFin>{SafeXml.Text(info, "nNFFin")}</nNFFin>"
            : "";
        var protocol = withProtocol ? "<nProt>153260000000077</nProt>" : "";
        return $"<retInutNFe xmlns=\"{SafeXml.NfeNamespace}\" versao=\"4.00\"><infInut><tpAmb>{ambient}</tpAmb><verAplic>OFFLINE-1</verAplic>" +
            $"<cStat>{code}</cStat><xMotivo>RESULTADO SINTETICO SEM VALIDADE FISCAL</xMotivo><cUF>53</cUF>{range}" +
            $"<dhRecbto>2026-10-18T10:00:05-03:00</dhRecbto>{protocol}</infInut></retInutNFe>";
    }

    private static string Resign(string xml, string rootName, string infoName, string version, Action<XmlElement> mutate)
    {
        var document = SafeXml.Load(xml);
        var root = document.DocumentElement!;
        root.RemoveChild(SafeXml.Child(root, "Signature", A1Signer.DsNamespace));
        mutate(SafeXml.Child(root, infoName));
        return WithCertificate(certificate => A1Signer.SignElement(SafeXml.Serialize(document), certificate, rootName, infoName, version));
    }

    [Fact]
    public void CancellationIsSignedValidatedAndCorrelatedIntoProcEventoNFe()
    {
        var signed = SignedCancellation();
        var signature = A1Signer.VerifyElement(signed, "evento", "evento", "infEvento", NfceEvents.Version);
        var key = A1Signer.Verify(proof.Signed).Key;
        Assert.Equal("ID110111" + key.Value + "01", signature.Id);
        var batch = NfceEvents.BuildBatch(signed, "42");
        proof.Validator.RequireValid("envEvento", batch);
        A1Signer.VerifyElement(batch, "envEvento", "evento", "infEvento", NfceEvents.Version);
        var result = NfceEvents.Interpret(signed, EventResponse(signed), proof.Validator);
        Assert.Equal(EventState.Registered, result.State);
        Assert.True(result.IsRegistered);
        Assert.Equal("153260000000099", result.ProtocolNumber);
        var processed = NfceEvents.BuildProcessed(signed, EventResponse(signed), proof.Validator);
        Assert.StartsWith("<procEventoNFe", processed);
        Assert.Equal(signature.DigestBase64,
            A1Signer.VerifyElement(processed, "procEventoNFe", "evento", "infEvento", NfceEvents.Version).DigestBase64);
    }

    [Theory]
    [InlineData("136", EventState.RegisteredNotLinked)]
    [InlineData("155", EventState.RegisteredOutsideDeadline)]
    [InlineData("573", EventState.Rejected)]
    [InlineData("594", EventState.Rejected)]
    [InlineData("999", EventState.Unknown)]
    public void EventCodesMapOnlyWhenKnown(string code, EventState expected)
    {
        var signed = SignedCancellation();
        var result = NfceEvents.Interpret(signed, EventResponse(signed, code), proof.Validator);
        Assert.Equal(expected, result.State);
        Assert.Equal(code, result.Code);
        if (expected == EventState.Rejected || expected == EventState.Unknown)
            Assert.Throws<InvalidDataException>(() => NfceEvents.BuildProcessed(signed, EventResponse(signed, code), proof.Validator));
    }

    [Theory]
    [InlineData("key")]
    [InlineData("environment")]
    [InlineData("type")]
    [InlineData("sequence")]
    [InlineData("missing-protocol")]
    [InlineData("batch")]
    public void UncorrelatedOrIncompleteEventReturnsNeverRegister(string scenario)
    {
        var signed = SignedCancellation();
        var otherKey = AccessKey.Create("2610", "12345678000195", "001", "000000002", "12345678").Value;
        var response = scenario switch
        {
            "missing-protocol" => EventResponse(signed, withProtocol: false),
            "batch" => EventResponse(signed, batchCode: "104"),
            _ => EventResponse(signed, mutate: info =>
            {
                switch (scenario)
                {
                    case "key": SafeXml.Child(info, "chNFe").InnerText = otherKey; break;
                    case "environment": SafeXml.Child(info, "tpAmb").InnerText = "1"; break;
                    case "type": SafeXml.Child(info, "tpEvento").InnerText = "110110"; break;
                    case "sequence": SafeXml.Child(info, "nSeqEvento").InnerText = "2"; break;
                }
            })
        };
        var result = NfceEvents.Interpret(signed, response, proof.Validator);
        // tpAmb=1 é barrado pelo gate de escopo do validador (InvalidResponse); o resto não correlaciona (Unknown).
        Assert.Equal(scenario == "environment" ? EventState.InvalidResponse : EventState.Unknown, result.State);
        Assert.False(result.IsRegistered);
        Assert.Throws<InvalidDataException>(() => NfceEvents.BuildProcessed(signed, response, proof.Validator));
    }

    [Fact]
    public void TruncatedOrTamperedEventReturnIsInvalidNotRegistered()
    {
        var signed = SignedCancellation();
        var truncated = EventResponse(signed)[..^20];
        Assert.Equal(EventState.InvalidResponse, NfceEvents.Interpret(signed, truncated, proof.Validator).State);
        var tampered = signed.Replace(Justification, Justification + " alterado", StringComparison.Ordinal);
        Assert.Throws<CryptographicException>(() => NfceEvents.Interpret(tampered, EventResponse(signed), proof.Validator));
    }

    [Fact]
    public void SignedEventOutsideCancellationScopeIsRefusedBeforeInterpretingResponse()
    {
        var signed = SignedCancellation();
        var otherEvent = Resign(signed, "evento", "infEvento", NfceEvents.Version,
            info => SafeXml.Child(info, "tpEvento").InnerText = "110110");
        Assert.Throws<InvalidDataException>(() => NfceEvents.Interpret(otherEvent, EventResponse(signed), proof.Validator));
    }

    [Fact]
    public void EventReturnFromDifferentAuthorityDoesNotRegister()
    {
        var signed = SignedCancellation();
        var response = EventResponse(signed, mutate: info => SafeXml.Child(info, "cOrgao").InnerText = "35");
        Assert.Equal(EventState.Unknown, NfceEvents.Interpret(signed, response, proof.Validator).State);
        Assert.Throws<InvalidDataException>(() => NfceEvents.BuildProcessed(signed, response, proof.Validator));
    }

    [Fact]
    public void SequenceIsPartOfTheIdentityAndRepeatsDeterministically()
    {
        var first = SignedCancellation(1);
        var second = SignedCancellation(2);
        Assert.EndsWith("01", A1Signer.VerifyElement(first, "evento", "evento", "infEvento", NfceEvents.Version).Id);
        Assert.EndsWith("02", A1Signer.VerifyElement(second, "evento", "evento", "infEvento", NfceEvents.Version).Id);
        // Mesmo pedido, mesma identidade (Id) — a idempotência do evento é por chave+tipo+sequência.
        Assert.Equal(A1Signer.VerifyElement(first, "evento", "evento", "infEvento", NfceEvents.Version).Id,
            A1Signer.VerifyElement(SignedCancellation(1), "evento", "evento", "infEvento", NfceEvents.Version).Id);
    }

    [Theory]
    [InlineData("cnpj")]
    [InlineData("protocol")]
    [InlineData("justification-short")]
    [InlineData("justification-long")]
    [InlineData("sequence")]
    public void InvalidCancellationRequestsAreRefusedBeforeSigning(string scenario)
    {
        var request = Request();
        request = scenario switch
        {
            "cnpj" => request with { Cnpj = "12ABC34501DE35" },
            "protocol" => request with { ProtocolNumber = "1532600000000001" },
            "justification-short" => request with { Justification = "curta demais" },
            "justification-long" => request with { Justification = new string('x', 256) },
            _ => request with { Sequence = 100 }
        };
        Assert.Throws<InvalidDataException>(() => WithCertificate(certificate =>
            NfceEvents.BuildSignedCancellation(request, certificate, proof.Validator)));
    }

    [Theory]
    [InlineData(15)]
    [InlineData(255)]
    public void JustificationBoundariesAreAcceptedForBothRequests(int length)
    {
        var justification = new string('a', length);
        var cancellation = WithCertificate(certificate => NfceEvents.BuildSignedCancellation(
            Request() with { Justification = justification }, certificate, proof.Validator));
        var inutilization = WithCertificate(certificate => NfceInutilization.BuildSigned(
            InutRequest() with { Justification = justification }, certificate, proof.Validator));
        Assert.NotEmpty(cancellation);
        Assert.NotEmpty(inutilization);
    }

    [Fact]
    public void InutilizationIsSignedValidatedAndCorrelatedIntoProcInutNFe()
    {
        var signed = SignedInutilization();
        var signature = A1Signer.VerifyElement(signed, "inutNFe", "inutNFe", "infInut", NfceInutilization.Version);
        Assert.Equal("ID53" + "26" + "12345678000195" + "65" + "001" + "000000005" + "000000007", signature.Id);
        var info = SafeXml.Child(SafeXml.Load(signed).DocumentElement!, "infInut");
        Assert.Equal("1", SafeXml.Text(info, "serie"));
        Assert.Equal("INUTILIZAR", SafeXml.Text(info, "xServ"));
        var result = NfceInutilization.Interpret(signed, InutResponse(signed), proof.Validator);
        Assert.Equal(InutilizationState.Homologated, result.State);
        Assert.Equal("153260000000077", result.ProtocolNumber);
        var processed = NfceInutilization.BuildProcessed(signed, InutResponse(signed), proof.Validator);
        Assert.StartsWith("<ProcInutNFe", processed);
        A1Signer.VerifyElement(processed, NfceInutilization.ProcessedRoot, "inutNFe", "infInut", NfceInutilization.Version);
    }

    [Theory]
    [InlineData("241", InutilizationState.Rejected)]
    [InlineData("563", InutilizationState.Rejected)]
    [InlineData("999", InutilizationState.Unknown)]
    public void InutilizationCodesMapOnlyWhenKnown(string code, InutilizationState expected)
    {
        var signed = SignedInutilization();
        Assert.Equal(expected, NfceInutilization.Interpret(signed, InutResponse(signed, code), proof.Validator).State);
        Assert.Throws<InvalidDataException>(() => NfceInutilization.BuildProcessed(signed, InutResponse(signed, code), proof.Validator));
    }

    [Fact]
    public void InutilizationReturnMustMatchTheSignedRangeAndEnvironment()
    {
        var signed = SignedInutilization();
        Assert.Equal(InutilizationState.Unknown,
            NfceInutilization.Interpret(signed, InutResponse(signed, withProtocol: false), proof.Validator).State);
        Assert.Equal(InutilizationState.Unknown,
            NfceInutilization.Interpret(signed, InutResponse(signed, withRange: false), proof.Validator).State);
        // Ambiente de produção é barrado pelo gate de escopo do validador, antes da correlação.
        Assert.Equal(InutilizationState.InvalidResponse,
            NfceInutilization.Interpret(signed, InutResponse(signed, ambient: "1"), proof.Validator).State);
        var other = SignedInutilization(8, 9);
        Assert.Equal(InutilizationState.Unknown,
            NfceInutilization.Interpret(signed, InutResponse(other), proof.Validator).State);
        Assert.Equal(InutilizationState.InvalidResponse,
            NfceInutilization.Interpret(signed, InutResponse(signed)[..^15], proof.Validator).State);
    }

    [Theory]
    [InlineData("ano")]
    [InlineData("CNPJ")]
    [InlineData("mod")]
    [InlineData("serie")]
    [InlineData("nNFIni")]
    [InlineData("nNFFin")]
    public void HomologationRequiresEveryIdentifyingField(string missingField)
    {
        var signed = SignedInutilization();
        var document = SafeXml.Load(InutResponse(signed));
        var info = SafeXml.Child(document.DocumentElement!, "infInut");
        info.RemoveChild(SafeXml.Child(info, missingField));
        var response = SafeXml.Serialize(document);
        Assert.Equal(InutilizationState.Unknown, NfceInutilization.Interpret(signed, response, proof.Validator).State);
        Assert.Throws<InvalidDataException>(() => NfceInutilization.BuildProcessed(signed, response, proof.Validator));
    }

    [Theory]
    [InlineData("ano", "25")]
    [InlineData("CNPJ", "87654321000198")]
    [InlineData("mod", "55")]
    [InlineData("serie", "2")]
    [InlineData("nNFIni", "6")]
    [InlineData("nNFFin", "8")]
    public void HomologationRequiresAllIdentifiersToMatch(string field, string value)
    {
        var signed = SignedInutilization();
        var document = SafeXml.Load(InutResponse(signed));
        SafeXml.Child(SafeXml.Child(document.DocumentElement!, "infInut"), field).InnerText = value;
        var response = SafeXml.Serialize(document);
        Assert.Equal(field == "mod" ? InutilizationState.InvalidResponse : InutilizationState.Unknown,
            NfceInutilization.Interpret(signed, response, proof.Validator).State);
        Assert.Throws<InvalidDataException>(() => NfceInutilization.BuildProcessed(signed, response, proof.Validator));
    }

    [Fact]
    public void SignedInutilizationOutsideModelScopeIsRefusedBeforeInterpretingResponse()
    {
        var signed = SignedInutilization();
        var otherModel = Resign(signed, "inutNFe", "infInut", NfceInutilization.Version,
            info => SafeXml.Child(info, "mod").InnerText = "55");
        Assert.Throws<InvalidDataException>(() => NfceInutilization.Interpret(otherModel, InutResponse(signed), proof.Validator));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(7, 5)]
    [InlineData(1, 1_000_000_000)]
    public void InvalidRangesAreRefused(long from, long to) =>
        Assert.Throws<InvalidDataException>(() => WithCertificate(certificate =>
            NfceInutilization.BuildSigned(InutRequest(from, to), certificate, proof.Validator)));

    [Fact]
    public async Task OverlappingIntervalsAreLockedOutPerPoolEvenUnderConcurrency()
    {
        var registry = new InutilizationIntervalRegistry();
        Assert.Throws<InvalidDataException>(() => registry.Lock(Pool, 1, 1_000_000_000));
        registry.Lock(Pool, 5, 7);
        Assert.Throws<IntervalOverlapException>(() => registry.Lock(Pool, 7, 9));
        Assert.Throws<IntervalOverlapException>(() => registry.Lock(Pool, 1, 5));
        registry.Lock(Pool, 8, 9);
        registry.Lock(NumberPoolKey.Create(Pool.FiscalIdentity, "2"), 5, 7);
        var fresh = new InutilizationIntervalRegistry();
        var outcomes = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() =>
        {
            try { fresh.Lock(Pool, 100, 110); return true; }
            catch (IntervalOverlapException) { return false; }
        })));
        Assert.Equal(1, outcomes.Count(ok => ok));
    }

    [Fact]
    public void ExistingInvoiceSignatureApiIsUnchanged()
    {
        // O refactor do assinador não altera a prova da NFC-e nem aceita evento como NFe.
        var invoiceProof = A1Signer.Verify(proof.Signed);
        Assert.Equal(20, Convert.FromBase64String(invoiceProof.DigestBase64).Length);
        Assert.ThrowsAny<Exception>(() => A1Signer.Verify(SignedCancellation()));
        Assert.Throws<InvalidDataException>(() =>
            A1Signer.VerifyElement(proof.Signed, "evento", "evento", "infEvento", NfceEvents.Version));
    }
}
