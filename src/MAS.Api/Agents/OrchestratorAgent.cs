using MAS.Core.Agents;
using MAS.Core.Abstractions;
using MAS.Core.Enums;
using MAS.Core.Messages;
using Microsoft.Extensions.Logging;

namespace MAS.Api.Agents;

/// <summary>
/// Agent Orkiestrator — centralny punkt koordynacji roju agentów.
/// Analizuje przychodzące zadania i deleguje je do właściwych agentów specjalistycznych.
/// Wzorzec: Mediator + Command.
/// </summary>
public sealed class OrchestratorAgent : BaseAgent
{
    private readonly IMessageBus _bus;
    private readonly IAgentRegistry _registry;
    private readonly ILogger<OrchestratorAgent> _logger;

    public OrchestratorAgent(
        IMessageBus bus,
        IAgentRegistry registry,
        ILogger<OrchestratorAgent> logger)
        : base("orchestrator", "Orchestrator Agent")
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[{AgentId}] Inicjalizacja Orkiestratora.", Id);
        await base.InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override async Task ProcessMessageAsync(AgentMessage message, CancellationToken cancellationToken)
    {
        switch (message)
        {
            case TaskMessage task:
                await HandleTaskAsync(task, cancellationToken).ConfigureAwait(false);
                break;

            case TaskResultMessage result:
                await HandleResultAsync(result, cancellationToken).ConfigureAwait(false);
                break;

            default:
                _logger.LogWarning("[{AgentId}] Nieobsługiwany typ wiadomości: {Type}", Id, message.GetType().Name);
                break;
        }
    }

    private async Task HandleTaskAsync(TaskMessage task, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{AgentId}] Analiza zadania: {Title}", Id, task.Title);

        // Wybierz wolnego agenta specjalistycznego (nie siebie)
        var available = _registry.GetByStatus(AgentStatus.Idle)
            .Where(a => a.Id != Id)
            .ToList();

        if (available.Count == 0)
        {
            _logger.LogWarning("[{AgentId}] Brak wolnych agentów – zadanie {TaskId} odrzucone.", Id, task.Id);
            await _bus.PublishAsync(new TaskResultMessage
            {
                SenderId = Id,
                ReceiverId = task.SenderId,
                TaskId = task.Id,
                Status = TaskExecutionStatus.Failed,
                Result = "Brak wolnych agentów w systemie."
            }, cancellationToken).ConfigureAwait(false);
            return;
        }

        // Round-robin: wybierz agenta z najniższym indeksem kolejki
        var target = available[0];
        var delegatedTask = task with { ReceiverId = target.Id };

        _logger.LogInformation("[{AgentId}] Delegowanie {TaskId} → {TargetId}", Id, task.Id, target.Id);
        await target.EnqueueMessageAsync(delegatedTask, cancellationToken).ConfigureAwait(false);
    }

    private async Task HandleResultAsync(TaskResultMessage result, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{AgentId}] Otrzymano wynik zadania {TaskId}: {Status}",
            Id, result.TaskId, result.Status);
        await _bus.PublishAsync(result, cancellationToken).ConfigureAwait(false);
    }
}
