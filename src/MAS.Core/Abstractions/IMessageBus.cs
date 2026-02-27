using MAS.Core.Messages;

namespace MAS.Core.Abstractions;

/// <summary>
/// Magistrala wiadomości (Message Bus) łącząca agentów asynchronicznie.
/// Wzorzec: Mediator.
/// </summary>
public interface IMessageBus
{
    /// <summary>
    /// Publikuje wiadomość do odpowiednich odbiorców.
    /// Dostarczenie wiadomości odbywa się asynchronicznie i nie blokuje wątku UI.
    /// </summary>
    ValueTask PublishAsync(AgentMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subskrybuje handler na konkretny typ wiadomości.
    /// </summary>
    IDisposable Subscribe<TMessage>(Func<TMessage, CancellationToken, ValueTask> handler)
        where TMessage : AgentMessage;

    /// <summary>
    /// Subskrybuje handler na konkretny typ wiadomości dla konkretnego odbiorcy (agenta).
    /// </summary>
    IDisposable Subscribe<TMessage>(string agentId, Func<TMessage, CancellationToken, ValueTask> handler)
        where TMessage : AgentMessage;
}
