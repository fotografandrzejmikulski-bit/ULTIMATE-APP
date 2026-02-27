namespace MAS.Core.Enums;

/// <summary>
/// Reprezentuje aktualny status agenta w systemie wieloagentowym.
/// </summary>
public enum AgentStatus
{
    /// <summary>Agent jest gotowy do przyjęcia zadania.</summary>
    Idle,

    /// <summary>Agent przetwarza aktualnie przydzielone zadanie.</summary>
    Running,

    /// <summary>Agent jest wstrzymany i czeka na wznowienie.</summary>
    Paused,

    /// <summary>Agent zakończył pracę i oczekuje na nowe zadania.</summary>
    Completed,

    /// <summary>Agent napotknął błąd podczas wykonywania zadania.</summary>
    Faulted,

    /// <summary>Agent jest wyłączony i nie przyjmuje zadań.</summary>
    Stopped
}
