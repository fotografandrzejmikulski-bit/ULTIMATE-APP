namespace MAS.Core.Enums;

/// <summary>
/// Status wykonania zadania przez agenta.
/// </summary>
public enum TaskExecutionStatus
{
    Pending,
    InProgress,
    Succeeded,
    Failed,
    Cancelled,
    TimedOut
}
