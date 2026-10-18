using EmissorFiscal.Core.Numbering;
using EmissorFiscal.Core.Protocols;

namespace EmissorFiscal.Core.Operations;

// Estados locais da operação. O estado fiscal vem exclusivamente de
// NfceProtocol.Interpret; aqui só se registra a transição permitida.
public enum OperationState
{
    Intended,
    XmlFrozen,
    Submitted,
    Processing,
    Authorized,
    AuthorizedWithAlert,
    AuthorizedOutsideDeadline,
    Rejected,
    NotFound,
    Unknown
}

// Próximo passo seguro. Nunca há reenvio automático: após o primeiro envio,
// toda incerteza se resolve por consulta ou por reconciliação humana.
public enum NextAction
{
    FreezeXml,
    Submit,
    Consult,
    ManualReconciliation,
    None
}

// Eventos imutáveis, em ordem de gravação. Cada um carrega o token de fencing
// do lease que o gravou e o instante do relógio injetado.
public abstract record OperationEvent(long FencingToken, DateTimeOffset RecordedAt);

public sealed record IntentRecorded(long FencingToken, DateTimeOffset RecordedAt,
    NumberReservation Reservation, string BaselineId) : OperationEvent(FencingToken, RecordedAt);

// XML assinado e seus hashes ficam congelados; uma segunda revisão é indisponível (H13).
public sealed record XmlFrozen(long FencingToken, DateTimeOffset RecordedAt,
    string SignedXml, string XmlSha256, string DigestBase64, string AccessKey) : OperationEvent(FencingToken, RecordedAt);

// Gravado ANTES de qualquer I/O: um crash após este evento deixa a operação
// em Submitted, e o caminho seguro passa a ser consultar, não reenviar.
public sealed record SubmissionRecorded(long FencingToken, DateTimeOffset RecordedAt, int Attempt)
    : OperationEvent(FencingToken, RecordedAt);

public sealed record ResponseRecorded(long FencingToken, DateTimeOffset RecordedAt,
    int Attempt, string ResponseXml, ProtocolResult Result) : OperationEvent(FencingToken, RecordedAt);

public sealed record ConsultationRecorded(long FencingToken, DateTimeOffset RecordedAt,
    int Consultation, string ResponseXml, ProtocolResult Result) : OperationEvent(FencingToken, RecordedAt);

public class OperationException(string message) : InvalidOperationException(message);

public sealed class InvalidTransitionException(string operationId, OperationState state, string attempted)
    : OperationException($"Operação {operationId} em {state} não admite {attempted}.");

public sealed class OperationConcurrencyException(string operationId, long expected, long actual)
    : OperationException($"Operação {operationId}: versão esperada {expected}, atual {actual}; recarregar antes de gravar.");
