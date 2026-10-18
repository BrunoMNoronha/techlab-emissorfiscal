namespace EmissorFiscal.Core.Operations;

// Porta de histórico append-only. Não há atualização nem remoção: toda
// mudança é um evento novo, gravado com a versão esperada (controle otimista).
// Durabilidade, isolamento e restauração são a EF-09.
public interface IOperationStore
{
    Operation? Load(string operationId);

    // expectedVersion 0 cria a operação; qualquer divergência lança
    // OperationConcurrencyException sem gravar.
    Operation Append(string operationId, OperationEvent @event, long expectedVersion);
}

public sealed class InMemoryOperationStore : IOperationStore
{
    private readonly object gate = new();
    private readonly Dictionary<string, List<OperationEvent>> histories = new(StringComparer.Ordinal);

    public Operation? Load(string operationId)
    {
        ArgumentNullException.ThrowIfNull(operationId);
        lock (gate)
            return histories.TryGetValue(operationId, out var events)
                ? Operation.Fold(operationId, events.ToArray())
                : null;
    }

    public Operation Append(string operationId, OperationEvent @event, long expectedVersion)
    {
        ArgumentNullException.ThrowIfNull(operationId);
        ArgumentNullException.ThrowIfNull(@event);
        lock (gate)
        {
            histories.TryGetValue(operationId, out var events);
            var actual = events?.Count ?? 0;
            if (actual != expectedVersion) throw new OperationConcurrencyException(operationId, expectedVersion, actual);
            var next = events is null ? new List<OperationEvent>() : new List<OperationEvent>(events);
            next.Add(@event);
            // Valida a transição antes de persistir: histórico inválido nunca entra.
            var operation = Operation.Fold(operationId, next.ToArray());
            histories[operationId] = next;
            return operation;
        }
    }
}
