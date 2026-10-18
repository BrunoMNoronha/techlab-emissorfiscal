using System.Globalization;
using EmissorFiscal.Core.Idempotency;

namespace EmissorFiscal.Core.Numbering;

// Reserva de um nNF para uma operação. É imutável e nunca é devolvida ao pool:
// rejeição, abandono ou falha preservam o número consumido (decisão H13).
public sealed record NumberReservation(NumberPoolKey Key, string OperationId, RequestHash RequestHash, long Number)
{
    public const long MaxNumber = 999_999_999;

    // nNF sem zeros à esquerda, como o contrato de preparação exige.
    public string Nnf => Number.ToString(CultureInfo.InvariantCulture);

    // Nove posições para a chave de acesso.
    public string PaddedNumber => Number.ToString("000000000", CultureInfo.InvariantCulture);
}

public class NumberingException(string message) : InvalidOperationException(message);

public sealed class NumberPoolNotInitializedException(NumberPoolKey key)
    : NumberingException($"Pool {key} sem próximo número aprovado; a inicialização é entrada obrigatória e nunca é inferida do histórico.");

public sealed class NumberPoolAlreadyInitializedException(NumberPoolKey key)
    : NumberingException($"Pool {key} já inicializado; reinicializar permitiria reutilizar numeração.");

public sealed class NumberPoolExhaustedException(NumberPoolKey key)
    : NumberingException($"Pool {key} esgotou o nNF máximo {NumberReservation.MaxNumber}.");

public sealed class IdempotencyConflictException(NumberPoolKey key, string operationId)
    : NumberingException($"Operação {operationId} em {key} já reservada com conteúdo diferente; mesma operação exige o mesmo requestHash.");
