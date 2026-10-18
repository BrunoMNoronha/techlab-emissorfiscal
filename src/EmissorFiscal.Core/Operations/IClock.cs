namespace EmissorFiscal.Core.Operations;

// Relógio injetável: o núcleo nunca lê a hora do sistema diretamente, para que
// expiração de lease e respostas tardias sejam reproduzíveis em teste.
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public static readonly SystemClock Instance = new();
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
