using MAS.Core.Enums;

namespace MAS.Core.Messages;

/// <summary>
/// Odpowiedź agenta na przydzielone zadanie.
/// </summary>
public sealed record TaskResultMessage : AgentMessage
{
    /// <summary>Identyfikator pierwotnego zadania.</summary>
    public required Guid TaskId { get; init; }

    /// <summary>Status wykonania zadania.</summary>
    public required TaskExecutionStatus Status { get; init; }

    /// <summary>Wynik lub komunikat błędu w formacie tekstowym / JSON.</summary>
    public string? Result { get; init; }

    /// <summary>Czas rozpoczęcia wykonania zadania.</summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>Czas zakończenia wykonania zadania.</summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>Czas trwania wykonania zadania.</summary>
    public TimeSpan? Duration => (StartedAt.HasValue && CompletedAt.HasValue)
        ? CompletedAt.Value - StartedAt.Value
        : null;
}
