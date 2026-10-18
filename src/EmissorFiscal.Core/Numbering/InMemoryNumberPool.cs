using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using EmissorFiscal.Core.Idempotency;

namespace EmissorFiscal.Core.Numbering;

// Implementação em memória da porta de numeração: prova o algoritmo de reserva
// atômica e de idempotência sem escolher banco, transação ou processo. Não
// sobrevive ao processo e não é a persistência da EF-09.
public sealed class InMemoryNumberPool : INumberPool
{
    private static readonly Regex OperationShape = new(
        "^[\\x21-\\x7E]{1,128}$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));

    private readonly ConcurrentDictionary<NumberPoolKey, Pool> pools = new();

    public void Initialize(NumberPoolKey key, long approvedNextNumber)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (approvedNextNumber is < 1 or > NumberReservation.MaxNumber)
            throw new InvalidDataException($"Próximo número aprovado deve estar entre 1 e {NumberReservation.MaxNumber}.");
        if (!pools.TryAdd(key, new Pool(approvedNextNumber)))
            throw new NumberPoolAlreadyInitializedException(key);
    }

    public NumberReservation Reserve(NumberPoolKey key, string operationId, RequestHash requestHash)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(operationId);
        ArgumentNullException.ThrowIfNull(requestHash);
        if (!OperationShape.IsMatch(operationId))
            throw new InvalidDataException("Identificador de operação inválido: 1-128 caracteres ASCII visíveis, sem espaços.");
        if (!pools.TryGetValue(key, out var pool)) throw new NumberPoolNotInitializedException(key);
        lock (pool.Gate)
        {
            if (pool.ByOperation.TryGetValue(operationId, out var existing))
            {
                if (existing.RequestHash == requestHash) return existing;
                throw new IdempotencyConflictException(key, operationId);
            }
            if (pool.Next > NumberReservation.MaxNumber) throw new NumberPoolExhaustedException(key);
            var reservation = new NumberReservation(key, operationId, requestHash, pool.Next);
            pool.ByOperation.Add(operationId, reservation);
            pool.Next++;
            return reservation;
        }
    }

    public NumberReservation? Find(NumberPoolKey key, string operationId)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(operationId);
        if (!pools.TryGetValue(key, out var pool)) return null;
        lock (pool.Gate)
            return pool.ByOperation.TryGetValue(operationId, out var reservation) ? reservation : null;
    }

    private sealed class Pool(long next)
    {
        public readonly object Gate = new();
        public long Next = next;
        public readonly Dictionary<string, NumberReservation> ByOperation = new(StringComparer.Ordinal);
    }
}
