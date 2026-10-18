using EmissorFiscal.Core.Idempotency;

namespace EmissorFiscal.Core.Numbering;

// Porta de numeração transacional. Não há liberação de número: a interface
// só sabe inicializar, reservar e localizar. Persistência durável é EF-09.
public interface INumberPool
{
    // Próximo número aprovado pelo responsável, justificado pelo histórico real
    // da série. Uma vez por pool; nunca deduzido do maior número conhecido.
    void Initialize(NumberPoolKey key, long approvedNextNumber);

    // Atômica. Mesma operação com o mesmo requestHash devolve a mesma reserva;
    // com requestHash diferente, lança IdempotencyConflictException sem consumir número.
    NumberReservation Reserve(NumberPoolKey key, string operationId, RequestHash requestHash);

    NumberReservation? Find(NumberPoolKey key, string operationId);
}
