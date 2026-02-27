using MAS.Core.Agents;
using MAS.Core.Abstractions;
using MAS.Core.Enums;
using MAS.Core.Messages;
using Microsoft.Extensions.Logging;

namespace MAS.Api.Agents;

/// <summary>
/// Agent Wykonawczy — odpowiada za realizację zadań operacyjnych:
/// wywołania zewnętrznych API, operacje na plikach, uruchamianie procesów.
/// </summary>
public sealed class ExecutionAgent : BaseAgent
{
    private readonly IMessageBus _bus;
    private readonly ILogger<ExecutionAgent> _logger;

    public ExecutionAgent(IMessageBus bus, ILogger<ExecutionAgent> logger)
        : base("execution-agent", "Execution Agent")
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[{AgentId}] Execution Agent gotowy.", Id);
        await base.InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override async Task ProcessMessageAsync(AgentMessage message, CancellationToken cancellationToken)
    {
        if (message is not TaskMessage task) return;

        SetStatus(AgentStatus.Running);
        var startedAt = DateTimeOffset.UtcNow;

        _logger.LogInformation("[{AgentId}] Wykonywanie zadania: {Title}", Id, task.Title);

        try
        {
            var result = await ExecuteOperationAsync(task, cancellationToken).ConfigureAwait(false);

            await _bus.PublishAsync(new TaskResultMessage
            {
                SenderId = Id,
                ReceiverId = task.SenderId,
                TaskId = task.Id,
                Status = TaskExecutionStatus.Succeeded,
                Result = result,
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[{AgentId}] Błąd wykonania zadania {TaskId}", Id, task.Id);

            await _bus.PublishAsync(new TaskResultMessage
            {
                SenderId = Id,
                ReceiverId = task.SenderId,
                TaskId = task.Id,
                Status = TaskExecutionStatus.Failed,
                Result = ex.Message,
                StartedAt = startedAt,
                CompletedAt = DateTimeOffset.UtcNow
            }, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            SetStatus(AgentStatus.Idle);
        }
    }

    private static async Task<string> ExecuteOperationAsync(TaskMessage task, CancellationToken ct)
    {
        // Symulacja operacji I/O (100-500ms)
        await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(100, 500)), ct)
            .ConfigureAwait(false);

        return $"[Wynik wykonania] Zadanie '{task.Title}' zakończone pomyślnie. " +
               $"Operacja I/O: OK. Timestamp: {DateTimeOffset.UtcNow:O}";
    }
}
