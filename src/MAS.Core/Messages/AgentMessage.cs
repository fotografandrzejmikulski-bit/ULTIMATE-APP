using MAS.Core.Enums;

namespace MAS.Core.Messages;

/// <summary>
/// Kontrakt wiadomości przesyłanej między agentami.
/// Implementuje wzorzec Command/Event do komunikacji asynchronicznej.
/// </summary>
public abstract record AgentMessage
{
    /// <summary>Unikalny identyfikator wiadomości.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Czas utworzenia wiadomości (UTC).</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Identyfikator agenta nadawcy.</summary>
    public required string SenderId { get; init; }

    /// <summary>Identyfikator agenta odbiorcy; null = broadcast.</summary>
    public string? ReceiverId { get; init; }

    /// <summary>Priorytet wiadomości w kolejce.</summary>
    public MessagePriority Priority { get; init; } = MessagePriority.Normal;

    /// <summary>Identyfikator korzenia konwersacji (correlation ID).</summary>
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
}
