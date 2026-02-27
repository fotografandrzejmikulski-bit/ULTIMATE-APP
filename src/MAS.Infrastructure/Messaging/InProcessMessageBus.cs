using System.Collections.Concurrent;
using MAS.Core.Abstractions;
using MAS.Core.Messages;
using Microsoft.Extensions.Logging;

namespace MAS.Infrastructure.Messaging;

/// <summary>
/// In-process implementacja magistrali wiadomości opartej na Task Parallel Library.
/// Dla środowisk rozproszonych zastąp implementacją opartą na RabbitMQ/Azure Service Bus.
/// Wzorzec: Mediator + Observer.
/// </summary>
public sealed class InProcessMessageBus : IMessageBus, IDisposable
{
    private readonly ILogger<InProcessMessageBus> _logger;

    // Subskrypcje: typ wiadomości → lista handlerów (agentId lub null dla broadcast)
    private readonly ConcurrentDictionary<Type, List<HandlerEntry>> _handlers = new();

    // Używamy lock dla operacji synchronicznych (Subscribe/Unsubscribe)
    // i SemaphoreSlim(1,1) tylko dla PublishAsync, gdzie potrzebujemy await.
    private readonly object _syncLock = new();
    private readonly SemaphoreSlim _publishLock = new(1, 1);

    public InProcessMessageBus(ILogger<InProcessMessageBus> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async ValueTask PublishAsync(AgentMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var messageType = message.GetType();
        _logger.LogDebug("Publishing {MessageType} from {SenderId} → {ReceiverId}",
            messageType.Name, message.SenderId, message.ReceiverId ?? "broadcast");

        if (!_handlers.TryGetValue(messageType, out var handlers))
            return;

        List<HandlerEntry> snapshot;
        await _publishLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            snapshot = [.. handlers];
        }
        finally
        {
            _publishLock.Release();
        }

        var tasks = snapshot
            .Where(h => h.AgentId is null || h.AgentId == message.ReceiverId)
            .Select(h => h.InvokeAsync(message, cancellationToken).AsTask());

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public IDisposable Subscribe<TMessage>(Func<TMessage, CancellationToken, ValueTask> handler)
        where TMessage : AgentMessage
        => SubscribeCore<TMessage>(agentId: null, handler);

    /// <inheritdoc/>
    public IDisposable Subscribe<TMessage>(string agentId, Func<TMessage, CancellationToken, ValueTask> handler)
        where TMessage : AgentMessage
        => SubscribeCore<TMessage>(agentId, handler);

    private IDisposable SubscribeCore<TMessage>(
        string? agentId,
        Func<TMessage, CancellationToken, ValueTask> handler)
        where TMessage : AgentMessage
    {
        var entry = new HandlerEntry(
            agentId,
            (msg, ct) => handler((TMessage)msg, ct));

        var list = _handlers.GetOrAdd(typeof(TMessage), _ => []);

        lock (_syncLock) { list.Add(entry); }

        return new Subscription(() =>
        {
            lock (_syncLock) { list.Remove(entry); }
        });
    }

    public void Dispose()
    {
        _publishLock.Dispose();
    }

    // ---------- private helpers ----------

    private sealed record HandlerEntry(
        string? AgentId,
        Func<AgentMessage, CancellationToken, ValueTask> Invoke)
    {
        public ValueTask InvokeAsync(AgentMessage message, CancellationToken ct) => Invoke(message, ct);
    }

    private sealed class Subscription(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
