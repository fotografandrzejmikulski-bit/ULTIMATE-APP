using MAS.Core.Agents;
using MAS.Core.Abstractions;
using MAS.Core.Enums;
using MAS.Core.Messages;
using Microsoft.Extensions.Logging;

namespace MAS.Api.Agents;

/// <summary>
/// Agent Badawczy — odpowiada za zadania wymagające analizy danych,
/// zbierania informacji lub generowania raportów.
/// W środowisku produkcyjnym integruje się z LLM (GPT-4, LLaMA) przez API lub lokalnie.
/// </summary>
public sealed class ResearchAgent : BaseAgent
{
    private readonly IMessageBus _bus;
    private readonly ILogger<ResearchAgent> _logger;

    public ResearchAgent(IMessageBus bus, ILogger<ResearchAgent> logger)
        : base("research-agent", "Research Agent")
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[{AgentId}] Research Agent gotowy.", Id);
        await base.InitializeAsync(cancellationToken).ConfigureAwait(false);
    }

    protected override async Task ProcessMessageAsync(AgentMessage message, CancellationToken cancellationToken)
    {
        if (message is not TaskMessage task) return;

        SetStatus(AgentStatus.Running);
        var startedAt = DateTimeOffset.UtcNow;

        _logger.LogInformation("[{AgentId}] Przetwarzanie zadania: {Title}", Id, task.Title);

        try
        {
            // Symulacja pracy analitycznej — w produkcji: wywołanie LLM API
            var result = await SimulateResearchAsync(task, cancellationToken).ConfigureAwait(false);

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
            _logger.LogError(ex, "[{AgentId}] Błąd podczas przetwarzania zadania {TaskId}", Id, task.Id);

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

    private static async Task<string> SimulateResearchAsync(TaskMessage task, CancellationToken ct)
    {
        // Symulacja opóźnienia analizy (50-200ms)
        await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(50, 200)), ct)
            .ConfigureAwait(false);

        return $"[Wynik analizy] Zadanie: '{task.Title}'. " +
               $"Przetworzone {task.Payload.Length} znaków danych. " +
               $"Znaczniki: {string.Join(", ", task.Context.Keys.Take(3))}";
    }
}
