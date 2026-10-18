using System.Xml;
using EmissorFiscal.Core.Idempotency;
using EmissorFiscal.Core.Numbering;
using EmissorFiscal.Core.Operations;
using EmissorFiscal.Core.Protocols;
using EmissorFiscal.Core.Xml;
using EmissorFiscal.Core.Xsd;
using EmissorFiscal.Samples;
using Xunit;

namespace EmissorFiscal.Core.Tests;

public sealed class OperationLifecycleTests(OfflineProof proof) : IClassFixture<OfflineProof>
{
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(30);

    private sealed class ManualClock : IClock
    {
        public DateTimeOffset UtcNow { get; private set; } = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);
        public void Advance(TimeSpan delta) => UtcNow += delta;
    }

    // Monta pool, store, leases e ciclo de vida sobre a mesma NFC-e sintética
    // assinada da fixture (série 001, nNF 1, CNPJ 12345678000195).
    private sealed class Harness
    {
        public const string OperationId = "venda-0001";
        public ManualClock Clock { get; } = new();
        public InMemoryOperationStore Store { get; } = new();
        public InMemoryLeaseRegistry Leases { get; }
        public InMemoryNumberPool Pool { get; } = new();
        public NumberPoolKey Key { get; } = NumberPoolKey.Create("cnpj:12345678000195", "1");
        public NumberReservation Reservation { get; }
        public OperationLifecycle Lifecycle { get; }

        public Harness(XsdValidator validator)
        {
            Leases = new InMemoryLeaseRegistry(Clock);
            Pool.Initialize(Key, 1);
            Reservation = Pool.Reserve(Key, OperationId, RequestHash.Compute("{\"sale\":\"1\"}", "{\"emitter\":\"1\"}"));
            Lifecycle = new OperationLifecycle(Store, Leases, Clock, validator);
        }

        public Lease Acquire(string worker = "worker-a") => Leases.Acquire(OperationId, worker, LeaseDuration);

        public Operation FrozenAndSubmitted(Lease lease, string signed)
        {
            Lifecycle.RecordIntent(lease, Reservation, SchemaManifest.Baseline);
            Lifecycle.FreezeXml(lease, signed);
            return Lifecycle.RecordSubmission(lease);
        }
    }

    private Harness NewHarness() => new(proof.Validator);

    [Fact]
    public void HappyPathAuthorizesWithCorrelatedProtocolAndAppendOnlyHistory()
    {
        var h = NewHarness();
        var lease = h.Acquire();
        var intent = h.Lifecycle.RecordIntent(lease, h.Reservation, SchemaManifest.Baseline);
        Assert.Equal(OperationState.Intended, intent.State);
        Assert.Equal(NextAction.FreezeXml, intent.NextAction);
        var frozen = h.Lifecycle.FreezeXml(lease, proof.Signed);
        Assert.Equal(NextAction.Submit, frozen.NextAction);
        Assert.Equal(64, frozen.XmlSha256!.Length);
        var submitted = h.Lifecycle.RecordSubmission(lease);
        Assert.Equal(OperationState.Submitted, submitted.State);
        Assert.Equal(1, submitted.Attempts);
        Assert.Equal(NextAction.Consult, submitted.NextAction);
        var authorized = h.Lifecycle.RecordResponse(lease, OfflineFixtures.Response(proof.Signed));
        Assert.Equal(OperationState.Authorized, authorized.State);
        Assert.True(authorized.IsAuthorized);
        Assert.Equal(OfflineFixtures.Protocol17, authorized.ProtocolNumber);
        Assert.Equal(NextAction.None, authorized.NextAction);
        Assert.Equal(4, authorized.Version);
        Assert.Equal(new[] { typeof(IntentRecorded), typeof(XmlFrozen), typeof(SubmissionRecorded), typeof(ResponseRecorded) },
            authorized.Events.Select(e => e.GetType()).ToArray());
        Assert.All(authorized.Events, e => Assert.Equal(lease.FencingToken, e.FencingToken));
        Assert.Equal(h.Reservation, authorized.Reservation);
        Assert.Equal(SchemaManifest.Baseline, authorized.BaselineId);
    }

    [Fact]
    public void AuthorizationWithAlertKeepsEveryMessage()
    {
        var h = NewHarness();
        var lease = h.Acquire();
        h.FrozenAndSubmitted(lease, proof.Signed);
        var result = h.Lifecycle.RecordResponse(lease, OfflineFixtures.Response(proof.Signed, "120", alerts: true));
        Assert.Equal(OperationState.AuthorizedWithAlert, result.State);
        Assert.Equal(new[] { new FiscalMessage("1", "ALERTA SINTETICO A"), new FiscalMessage("2", "ALERTA SINTETICO B") }, result.Messages);
        Assert.Equal(NextAction.None, result.NextAction);
    }

    [Fact]
    public void RejectionIsTerminalAndKeepsTheConsumedNumber()
    {
        var h = NewHarness();
        var lease = h.Acquire();
        h.FrozenAndSubmitted(lease, proof.Signed);
        var rejected = h.Lifecycle.RecordResponse(lease, OfflineFixtures.Response(proof.Signed, "225"));
        Assert.Equal(OperationState.Rejected, rejected.State);
        Assert.Equal(NextAction.None, rejected.NextAction);
        Assert.Throws<InvalidTransitionException>(() => h.Lifecycle.RecordSubmission(lease));
        Assert.Throws<InvalidTransitionException>(() => h.Lifecycle.FreezeXml(lease, proof.Signed));
        // H13: o número reservado não volta ao pool; a próxima operação recebe o seguinte.
        Assert.Equal(2, h.Pool.Reserve(h.Key, "venda-0002", h.Reservation.RequestHash).Number);
    }

    [Fact]
    public void CrashBeforeSubmissionResumesWithANewLeaseAndTheOldTokenIsFenced()
    {
        var h = NewHarness();
        var old = h.Acquire("worker-a");
        h.Lifecycle.RecordIntent(old, h.Reservation, SchemaManifest.Baseline);
        h.Lifecycle.FreezeXml(old, proof.Signed);
        h.Clock.Advance(LeaseDuration + TimeSpan.FromSeconds(1));
        var resumed = h.Acquire("worker-b");
        Assert.True(resumed.FencingToken > old.FencingToken);
        Assert.Throws<FencingException>(() => h.Lifecycle.RecordSubmission(old));
        var operation = h.Store.Load(Harness.OperationId)!;
        Assert.Equal(OperationState.XmlFrozen, operation.State);
        Assert.Equal(NextAction.Submit, operation.NextAction);
        Assert.Equal(1, h.Lifecycle.RecordSubmission(resumed).Attempts);
    }

    [Fact]
    public void TimeoutAfterSubmissionNeverResendsAndResolvesByConsultation()
    {
        var h = NewHarness();
        var lease = h.Acquire();
        var submitted = h.FrozenAndSubmitted(lease, proof.Signed);
        Assert.Equal(NextAction.Consult, submitted.NextAction);
        Assert.Throws<InvalidTransitionException>(() => h.Lifecycle.RecordSubmission(lease));
        var consulted = h.Lifecycle.RecordConsultation(lease, OfflineFixtures.Response(proof.Signed, consultation: true));
        Assert.Equal(OperationState.Authorized, consulted.State);
        Assert.Equal(1, consulted.Attempts);
        Assert.Equal(1, consulted.Consultations);
        Assert.Equal(OfflineFixtures.Protocol17, consulted.ProtocolNumber);
    }

    [Fact]
    public void ProcessingResponseLeadsToConsultationAndThenAuthorization()
    {
        var h = NewHarness();
        var lease = h.Acquire();
        h.FrozenAndSubmitted(lease, proof.Signed);
        var processing = h.Lifecycle.RecordResponse(lease, WithoutProtocol(OfflineFixtures.Response(proof.Signed), "105"));
        Assert.Equal(OperationState.Processing, processing.State);
        Assert.Equal(NextAction.Consult, processing.NextAction);
        Assert.Throws<InvalidTransitionException>(() => h.Lifecycle.RecordSubmission(lease));
        var authorized = h.Lifecycle.RecordConsultation(lease, OfflineFixtures.Response(proof.Signed, consultation: true));
        Assert.Equal(OperationState.Authorized, authorized.State);
        Assert.Equal(5, authorized.Version);
    }

    [Fact]
    public void LateResponseFromAWorkerThatLostItsLeaseIsRefusedAndTheStoreIsUnchanged()
    {
        var h = NewHarness();
        var a = h.Acquire("worker-a");
        h.FrozenAndSubmitted(a, proof.Signed);
        h.Clock.Advance(LeaseDuration + TimeSpan.FromSeconds(1));
        var b = h.Acquire("worker-b");
        var before = h.Store.Load(Harness.OperationId)!.Version;
        Assert.Throws<FencingException>(() => h.Lifecycle.RecordResponse(a, OfflineFixtures.Response(proof.Signed)));
        Assert.Equal(before, h.Store.Load(Harness.OperationId)!.Version);
        var reconciled = h.Lifecycle.RecordConsultation(b, OfflineFixtures.Response(proof.Signed, consultation: true));
        Assert.Equal(OperationState.Authorized, reconciled.State);
        Assert.Equal(b.FencingToken, reconciled.Events[^1].FencingToken);
    }

    [Theory]
    [InlineData("204")]
    [InlineData("539")]
    public void DuplicateCodesStayUnknownAndRequireConsultationNotResubmission(string code)
    {
        var h = NewHarness();
        var lease = h.Acquire();
        h.FrozenAndSubmitted(lease, proof.Signed);
        var duplicate = h.Lifecycle.RecordResponse(lease, OfflineFixtures.Response(proof.Signed, code));
        Assert.Equal(OperationState.Unknown, duplicate.State);
        Assert.Equal(NextAction.Consult, duplicate.NextAction);
        Assert.Throws<InvalidTransitionException>(() => h.Lifecycle.RecordSubmission(lease));
        Assert.Equal(h.Reservation, duplicate.Reservation);
    }

    [Fact]
    public void IsolatedNotFoundNeedsManualReconciliationWithoutNewReservationOrResend()
    {
        var h = NewHarness();
        var lease = h.Acquire();
        h.FrozenAndSubmitted(lease, proof.Signed);
        var notFound = h.Lifecycle.RecordConsultation(lease,
            WithoutProtocol(OfflineFixtures.Response(proof.Signed, consultation: true), "217"));
        Assert.Equal(OperationState.NotFound, notFound.State);
        Assert.Equal(NextAction.ManualReconciliation, notFound.NextAction);
        Assert.Throws<InvalidTransitionException>(() => h.Lifecycle.RecordSubmission(lease));
        Assert.Equal(h.Reservation, notFound.Reservation);
        Assert.Equal(1, h.Reservation.Number);
        // Uma consulta posterior ainda pode localizar e concluir a mesma operação.
        var later = h.Lifecycle.RecordConsultation(lease, OfflineFixtures.Response(proof.Signed, consultation: true));
        Assert.Equal(OperationState.Authorized, later.State);
        Assert.Equal(2, later.Consultations);
    }

    [Fact]
    public void FrozenXmlMustMatchTheReservationAndCannotBeReplaced()
    {
        var h = NewHarness();
        var lease = h.Acquire();
        h.Lifecycle.RecordIntent(lease, h.Reservation, SchemaManifest.Baseline);
        // A fixture alfanumérica tem outro CNPJ mas a mesma série/nNF: a chave só é
        // conferida contra a reserva; um XML com nNF diferente é recusado.
        var otherNumber = WithNumber(proof.Signed, "000000002");
        Assert.Throws<InvalidDataException>(() => h.Lifecycle.FreezeXml(lease, otherNumber));
        var frozen = h.Lifecycle.FreezeXml(lease, proof.Signed);
        Assert.Equal(OperationState.XmlFrozen, frozen.State);
        Assert.Throws<InvalidTransitionException>(() => h.Lifecycle.FreezeXml(lease, proof.Signed));
        Assert.Equal(proof.Signed, h.Store.Load(Harness.OperationId)!.SignedXml);
    }

    [Fact]
    public void EventsOutOfOrderAreRefused()
    {
        var h = NewHarness();
        var lease = h.Acquire();
        Assert.Throws<InvalidTransitionException>(() => h.Lifecycle.FreezeXml(lease, proof.Signed));
        h.Lifecycle.RecordIntent(lease, h.Reservation, SchemaManifest.Baseline);
        Assert.Throws<InvalidTransitionException>(() => h.Lifecycle.RecordIntent(lease, h.Reservation, SchemaManifest.Baseline));
        Assert.Throws<InvalidTransitionException>(() => h.Lifecycle.RecordSubmission(lease));
        Assert.Throws<InvalidTransitionException>(() => h.Lifecycle.RecordResponse(lease, OfflineFixtures.Response(proof.Signed)));
        Assert.Throws<InvalidTransitionException>(() => h.Lifecycle.RecordConsultation(lease, OfflineFixtures.Response(proof.Signed, consultation: true)));
        var otherLease = h.Leases.Acquire("outra-operacao", "worker-a", LeaseDuration);
        Assert.Throws<InvalidDataException>(() => h.Lifecycle.RecordIntent(otherLease, h.Reservation, SchemaManifest.Baseline));
    }

    [Fact]
    public void StoreIsAppendOnlyWithOptimisticConcurrency()
    {
        var store = new InMemoryOperationStore();
        var h = NewHarness();
        var intent = new IntentRecorded(1, h.Clock.UtcNow, h.Reservation, SchemaManifest.Baseline);
        Assert.Null(store.Load("op"));
        Assert.Equal(1, store.Append("op", intent, 0).Version);
        Assert.Throws<OperationConcurrencyException>(() => store.Append("op", intent, 0));
        // Histórico inválido não entra: o primeiro evento precisa ser IntentRecorded.
        Assert.Throws<InvalidDataException>(() => store.Append("outra",
            new SubmissionRecorded(1, h.Clock.UtcNow, 1), 0));
        Assert.Equal(new[] { "Append", "Load" }, typeof(IOperationStore).GetMethods().Select(m => m.Name).Order().ToArray());
    }

    [Fact]
    public void LeasesAreExclusiveWhileValidAndTokensOnlyGrow()
    {
        var clock = new ManualClock();
        var registry = new InMemoryLeaseRegistry(clock);
        var a = registry.Acquire("op", "worker-a", LeaseDuration);
        Assert.Throws<LeaseHeldException>(() => registry.Acquire("op", "worker-b", LeaseDuration));
        var renewed = registry.Renew(a, LeaseDuration);
        Assert.Equal(a.FencingToken, renewed.FencingToken);
        Assert.True(registry.IsCurrent(renewed));
        clock.Advance(LeaseDuration + TimeSpan.FromSeconds(1));
        Assert.False(registry.IsCurrent(renewed));
        Assert.Throws<FencingException>(() => registry.Renew(renewed, LeaseDuration));
        var b = registry.Acquire("op", "worker-b", LeaseDuration);
        Assert.Equal(renewed.FencingToken + 1, b.FencingToken);
        Assert.False(registry.IsCurrent(renewed));
        // O mesmo trabalhador readquirindo também avança o token.
        var again = registry.Acquire("op", "worker-b", LeaseDuration);
        Assert.Equal(b.FencingToken + 1, again.FencingToken);
        Assert.False(registry.IsCurrent(b));
        Assert.Throws<InvalidDataException>(() => registry.Acquire("op", "worker-b", TimeSpan.Zero));
    }

    private static string WithoutProtocol(string response, string outerCode)
    {
        var document = SafeXml.Load(response);
        var root = document.DocumentElement!;
        root.RemoveChild(SafeXml.Child(root, "protNFe"));
        SafeXml.Child(root, "cStat").InnerText = outerCode;
        return SafeXml.Serialize(document);
    }

    private static string WithNumber(string signed, string number)
    {
        // Só para provar a recusa: a assinatura deixa de verificar após a edição e isso
        // também é motivo de falha; o teste aceita qualquer InvalidDataException.
        var document = SafeXml.Load(signed);
        var info = (XmlElement)document.GetElementsByTagName("infNFe", SafeXml.NfeNamespace)[0]!;
        var id = info.GetAttribute("Id");
        info.SetAttribute("Id", id[..28] + number + id[37..]);
        return SafeXml.Serialize(document);
    }
}
