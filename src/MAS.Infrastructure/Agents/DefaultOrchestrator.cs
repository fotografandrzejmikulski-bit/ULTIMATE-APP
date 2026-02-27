using MAS.Core.Abstractions;
using MAS.Core.Enums;
using MAS.Core.Messages;
using Microsoft.Extensions.Logging;

namespace MAS.Infrastructure.Agents;

/// <summary>
/// Implementacja orkiestratora odpowiedzialnego za delegowanie zadań do agentów.
/// Strategia: round-robin z priorytetyzacją po statusie Idle.
/// Wzorzec: Strategy + Mediator.
/// </summary>
public sealed class DefaultOrchestrator : IOrchestrator
{
    private readonly IAgentRegistry _registry;
    private readonly IMessageBus _bus;
    private readonly ILogger<DefaultOrchestrator> _logger;

    public DefaultOrchestrator(
        IAgentRegistry registry,
        IMessageBus bus,
        ILogger<DefaultOrchestrator> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<TaskResultMessage> DispatchAsync(
        TaskMessage task,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        var idleAgents = _registry.GetByStatus(AgentStatus.Idle);
        if (idleAgents.Count == 0)
        {
            _logger.LogWarning("Brak dostępnych agentów dla zadania {TaskId}", task.Id);
            return new TaskResultMessage
            {
                SenderId = "orchestrator",
                ReceiverId = task.SenderId,
                TaskId = task.Id,
                Status = TaskExecutionStatus.Failed,
                Result = "Brak dostępnych agentów w systemie."
            };
        }

        // Wybierz pierwszego dostępnego agenta (najprostszy load-balancer)
        var selectedAgent = idleAgents[0];
        _logger.LogInformation("Delegowanie zadania {TaskId} ({Title}) → agent {AgentId}",
            task.Id, task.Title, selectedAgent.Id);

        // Przygotuj wiadomość z przypisanym odbiorcą
        var dispatchedTask = task with { ReceiverId = selectedAgent.Id };

        // Stwórz TaskCompletionSource do oczekiwania na odpowiedź
        var tcs = new TaskCompletionSource<TaskResultMessage>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(task.Timeout);

        // Subskrybuj odpowiedź agenta
        using var subscription = _bus.Subscribe<TaskResultMessage>(
            task.SenderId,
            (result, _) =>
            {
                if (result.TaskId == task.Id)
                    tcs.TrySetResult(result);
                return ValueTask.CompletedTask;
            });

        // Wyślij zadanie do agenta
        await selectedAgent.EnqueueMessageAsync(dispatchedTask, cancellationToken).ConfigureAwait(false);

        // Czekaj na wynik lub timeout
        try
        {
            return await tcs.Task.WaitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new TaskResultMessage
            {
                SenderId = "orchestrator",
                ReceiverId = task.SenderId,
                TaskId = task.Id,
                Status = TaskExecutionStatus.TimedOut,
                Result = $"Zadanie przekroczyło limit czasu {task.Timeout}."
            };
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TaskResultMessage>> RunPipelineAsync(
        IEnumerable<TaskMessage> steps,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(steps);

        var results = new List<TaskResultMessage>();
        foreach (var step in steps)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await DispatchAsync(step, cancellationToken).ConfigureAwait(false);
            results.Add(result);

            if (result.Status is TaskExecutionStatus.Failed or TaskExecutionStatus.TimedOut)
            {
                _logger.LogWarning("Pipeline przerwany na kroku {TaskId}: {Status}", step.Id, result.Status);
                break;
            }
        }
        return results;
    }
}
