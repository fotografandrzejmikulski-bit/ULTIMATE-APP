using MAS.Core.Messages;

namespace MAS.Core.Abstractions;

/// <summary>
/// Koordynator (Orchestrator) odpowiedzialny za delegowanie zadań do agentów
/// i zarządzanie przepływem pracy (workflow) w systemie MAS.
/// Wzorzec: Mediator + Chain of Responsibility.
/// </summary>
public interface IOrchestrator
{
    /// <summary>
    /// Deleguje zadanie do najbardziej odpowiedniego wolnego agenta.
    /// Wybór agenta oparty jest na strategii load-balancingu.
    /// </summary>
    Task<TaskResultMessage> DispatchAsync(TaskMessage task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uruchamia zadanie złożone (pipeline) jako sekwencję kroków
    /// rozdzielonych pomiędzy specjalistyczne agenty.
    /// </summary>
    Task<IReadOnlyList<TaskResultMessage>> RunPipelineAsync(
        IEnumerable<TaskMessage> steps,
        CancellationToken cancellationToken = default);
}
