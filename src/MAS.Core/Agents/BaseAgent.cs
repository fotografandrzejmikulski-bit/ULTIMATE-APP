using System.Threading.Channels;
using MAS.Core.Abstractions;
using MAS.Core.Enums;
using MAS.Core.Messages;

namespace MAS.Core.Agents;

/// <summary>
/// Abstrakcyjna klasa bazowa dla wszystkich agentów systemu MAS.
/// Implementuje wzorce: Template Method, Observer, Strategy.
/// Pętla przetwarzania wiadomości oparta jest na System.Threading.Channels
/// dla gwarantowanego wysokowydajnego asynchronicznego przetwarzania
/// bez blokowania wątków.
/// </summary>
public abstract class BaseAgent : IAgent
{
    private readonly Channel<AgentMessage> _mailbox;
    private AgentStatus _status = AgentStatus.Idle;

    protected BaseAgent(string id, string name, int mailboxCapacity = 1000)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));

        _mailbox = Channel.CreateBounded<AgentMessage>(new BoundedChannelOptions(mailboxCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    /// <inheritdoc/>
    public string Id { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public AgentStatus Status
    {
        get => _status;
        private set
        {
            if (_status == value) return;
            var previous = _status;
            _status = value;
            OnStatusChanged(previous, value);
        }
    }

    /// <inheritdoc/>
    public virtual Task InitializeAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public async ValueTask EnqueueMessageAsync(AgentMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        await _mailbox.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        Status = AgentStatus.Running;

        try
        {
            await foreach (var message in _mailbox.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                await ProcessMessageAsync(message, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // expected on shutdown
        }
        finally
        {
            Status = AgentStatus.Stopped;
        }
    }

    /// <inheritdoc/>
    public virtual Task StopAsync(CancellationToken cancellationToken = default)
    {
        _mailbox.Writer.TryComplete();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Przetwarza pojedynczą wiadomość ze skrzynki odbiorczej agenta.
    /// Metoda do nadpisania w klasach pochodnych – wzorzec Template Method.
    /// </summary>
    protected abstract Task ProcessMessageAsync(AgentMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Wywoływana przy zmianie statusu agenta. Nadpisz, aby reagować na zmiany statusu.
    /// </summary>
    protected virtual void OnStatusChanged(AgentStatus previous, AgentStatus current) { }

    /// <summary>
    /// Pomocnicza metoda ustawiająca status agenta z zewnątrz podklasy.
    /// </summary>
    protected void SetStatus(AgentStatus status) => Status = status;
}
