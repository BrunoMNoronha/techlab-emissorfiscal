namespace EmissorFiscal.Core.Operations;

// Lease de trabalhador sobre uma operação, com token de fencing monotônico por
// operação. Um trabalhador antigo que retoma após perder o lease continua com
// token inferior e tem toda transição recusada, inclusive resposta tardia.
public sealed record Lease(string OperationId, string Worker, long FencingToken, DateTimeOffset ExpiresAt);

public interface ILeaseRegistry
{
    Lease Acquire(string operationId, string worker, TimeSpan duration);
    Lease Renew(Lease lease, TimeSpan duration);
    bool IsCurrent(Lease lease);
}

public class LeaseException(string message) : InvalidOperationException(message);

public sealed class LeaseHeldException(string operationId, string holder)
    : LeaseException($"Operação {operationId} está sob lease válido de {holder}.");

public sealed class FencingException(string operationId, long token)
    : LeaseException($"Lease da operação {operationId} com token {token} não é o atual; transição recusada.");

public sealed class InMemoryLeaseRegistry(IClock clock) : ILeaseRegistry
{
    private readonly object gate = new();
    private readonly Dictionary<string, Lease> leases = new(StringComparer.Ordinal);

    public Lease Acquire(string operationId, string worker, TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(operationId);
        ArgumentNullException.ThrowIfNull(worker);
        if (duration <= TimeSpan.Zero) throw new InvalidDataException("Duração do lease deve ser positiva.");
        lock (gate)
        {
            var now = clock.UtcNow;
            leases.TryGetValue(operationId, out var current);
            if (current is not null && current.ExpiresAt > now && current.Worker != worker)
                throw new LeaseHeldException(operationId, current.Worker);
            // O token só cresce, mesmo quando o mesmo trabalhador readquire.
            var lease = new Lease(operationId, worker, (current?.FencingToken ?? 0) + 1, now + duration);
            leases[operationId] = lease;
            return lease;
        }
    }

    public Lease Renew(Lease lease, TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(lease);
        if (duration <= TimeSpan.Zero) throw new InvalidDataException("Duração do lease deve ser positiva.");
        lock (gate)
        {
            if (!IsCurrentLocked(lease)) throw new FencingException(lease.OperationId, lease.FencingToken);
            var renewed = lease with { ExpiresAt = clock.UtcNow + duration };
            leases[lease.OperationId] = renewed;
            return renewed;
        }
    }

    public bool IsCurrent(Lease lease)
    {
        ArgumentNullException.ThrowIfNull(lease);
        lock (gate) return IsCurrentLocked(lease);
    }

    private bool IsCurrentLocked(Lease lease) =>
        leases.TryGetValue(lease.OperationId, out var current) &&
        current.FencingToken == lease.FencingToken && current.ExpiresAt > clock.UtcNow;
}
