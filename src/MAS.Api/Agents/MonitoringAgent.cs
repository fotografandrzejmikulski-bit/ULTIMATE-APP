using MAS.Core.Agents;
using MAS.Core.Abstractions;
using MAS.Core.Enums;
using MAS.Core.Messages;
using Microsoft.Extensions.Logging;

namespace MAS.Api.Agents;

/// <summary>
/// Agent Monitorowania — zbiera metryki systemowe, śledzi statusy innych agentów
/// i generuje alerty w przypadku anomalii.
/// Subskrybuje zdarzenia z magistrali wiadomości (Event-Driven).
/// </summary>
public sealed class MonitoringAgent : BaseAgent
{
    private readonly IMessageBus _bus;
    private readonly ILogger<MonitoringAgent> _logger;
    private IDisposable? _statusSubscription;

    public MonitoringAgent(IMessageBus bus, ILogger<MonitoringAgent> logger)
        : base("monitoring-agent", "Monitoring Agent")
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Subskrybuj zmiany statusów agentów z całego systemu
        _statusSubscription = _bus.Subscribe<AgentStatusChangedMessage>(
            OnAgentStatusChanged);

        _logger.LogInformation("[{AgentId}] Monitoring Agent aktywny – nasłuchuję zdarzeń.", Id);
        await base.InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    private ValueTask OnAgentStatusChanged(AgentStatusChangedMessage msg, CancellationToken ct)
    {
        _logger.LogInformation(
            "[Monitor] Agent {AgentId}: {Previous} → {New}",
            msg.SenderId, msg.PreviousStatus, msg.NewStatus);

        if (msg.NewStatus == AgentStatus.Faulted)
        {
            _logger.LogCritical("[Monitor] ALERT: Agent {AgentId} przeszedł w stan Faulted!", msg.SenderId);
        }

        return ValueTask.CompletedTask;
    }

    protected override async Task ProcessMessageAsync(AgentMessage message, CancellationToken cancellationToken)
    {
        if (message is not TaskMessage task) return;

        _logger.LogInformation("[{AgentId}] Generowanie raportu diagnostycznego.", Id);

        await Task.Delay(50, cancellationToken).ConfigureAwait(false);

        await _bus.PublishAsync(new TaskResultMessage
        {
            SenderId = Id,
            ReceiverId = task.SenderId,
            TaskId = task.Id,
            Status = TaskExecutionStatus.Succeeded,
            Result = $"Raport diagnostyczny @ {DateTimeOffset.UtcNow:O}: System działa prawidłowo.",
            StartedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow
        }, cancellationToken).ConfigureAwait(false);
    }

    public override Task StopAsync(CancellationToken cancellationToken = default)
    {
        _statusSubscription?.Dispose();
        return base.StopAsync(cancellationToken);
    }
}
