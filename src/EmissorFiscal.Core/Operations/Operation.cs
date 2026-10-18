using EmissorFiscal.Core.Numbering;
using EmissorFiscal.Core.Protocols;

namespace EmissorFiscal.Core.Operations;

// Visão derivada do histórico de eventos; não é persistida nem editável.
// Version é o número de eventos e serve de controle otimista no store.
public sealed class Operation
{
    private Operation(string operationId, IReadOnlyList<OperationEvent> events)
    {
        OperationId = operationId;
        Events = events;
        foreach (var @event in events) Apply(@event);
    }

    public string OperationId { get; }
    public IReadOnlyList<OperationEvent> Events { get; }
    public long Version => Events.Count;
    public OperationState State { get; private set; } = OperationState.Intended;
    public NumberReservation? Reservation { get; private set; }
    public string? BaselineId { get; private set; }
    public string? SignedXml { get; private set; }
    public string? XmlSha256 { get; private set; }
    public string? AccessKey { get; private set; }
    public int Attempts { get; private set; }
    public int Consultations { get; private set; }
    public ProtocolResult? LastResult { get; private set; }

    public string? ProtocolNumber => LastResult?.ProtocolNumber;
    public IReadOnlyList<FiscalMessage> Messages => LastResult?.Messages ?? [];
    public bool IsAuthorized => State is OperationState.Authorized or OperationState.AuthorizedWithAlert
        or OperationState.AuthorizedOutsideDeadline;

    public NextAction NextAction => State switch
    {
        OperationState.Intended => NextAction.FreezeXml,
        OperationState.XmlFrozen => NextAction.Submit,
        OperationState.Submitted or OperationState.Processing or OperationState.Unknown => NextAction.Consult,
        // 217 isolado: nem nova reserva nem reenvio; alguém precisa reconciliar.
        OperationState.NotFound => NextAction.ManualReconciliation,
        _ => NextAction.None
    };

    public static Operation Fold(string operationId, IReadOnlyList<OperationEvent> events)
    {
        ArgumentNullException.ThrowIfNull(operationId);
        ArgumentNullException.ThrowIfNull(events);
        if (events.Count == 0 || events[0] is not IntentRecorded)
            throw new InvalidDataException("Histórico de operação deve começar por IntentRecorded.");
        return new Operation(operationId, events);
    }

    private void Apply(OperationEvent @event)
    {
        switch (@event)
        {
            case IntentRecorded intent:
                Reservation = intent.Reservation;
                BaselineId = intent.BaselineId;
                State = OperationState.Intended;
                break;
            case XmlFrozen frozen:
                SignedXml = frozen.SignedXml;
                XmlSha256 = frozen.XmlSha256;
                AccessKey = frozen.AccessKey;
                State = OperationState.XmlFrozen;
                break;
            case SubmissionRecorded submission:
                Attempts = submission.Attempt;
                State = OperationState.Submitted;
                break;
            case ResponseRecorded response:
                LastResult = response.Result;
                State = Map(response.Result.State);
                break;
            case ConsultationRecorded consultation:
                Consultations = consultation.Consultation;
                LastResult = consultation.Result;
                State = Map(consultation.Result.State);
                break;
            default:
                throw new InvalidDataException("Evento de operação desconhecido.");
        }
    }

    internal static OperationState Map(FiscalState state) => state switch
    {
        FiscalState.Authorized => OperationState.Authorized,
        FiscalState.AuthorizedWithAlert => OperationState.AuthorizedWithAlert,
        FiscalState.AuthorizedOutsideDeadline => OperationState.AuthorizedOutsideDeadline,
        FiscalState.Rejected => OperationState.Rejected,
        FiscalState.Processing => OperationState.Processing,
        FiscalState.NotFound => OperationState.NotFound,
        // Unknown e InvalidResponse não autorizam nem rejeitam: só consulta resolve.
        _ => OperationState.Unknown
    };
}
