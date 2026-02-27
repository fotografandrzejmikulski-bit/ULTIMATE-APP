namespace MAS.App.Models;

/// <summary>
/// Model danych zadania tworzonego przez użytkownika aplikacji.
/// </summary>
public sealed class AgentTaskModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Title { get; set; }
    public required string Payload { get; set; }
    public Dictionary<string, string> Context { get; set; } = [];
    public int TimeoutSeconds { get; set; } = 300;
}

/// <summary>
/// Model wyświetlania wyniku zadania w UI.
/// </summary>
public sealed class TaskResultModel
{
    public Guid TaskId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Result { get; set; }
    public double? DurationMs { get; set; }
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.Now;

    public string StatusIcon => Status switch
    {
        "Succeeded" => "✅",
        "Failed"    => "❌",
        "TimedOut"  => "⏱️",
        _           => "⏳"
    };
}

/// <summary>
/// Model agenta widoczny w dashboardzie aplikacji.
/// </summary>
public sealed class AgentViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public string StatusColor => Status switch
    {
        "Running"   => "#4CAF50",
        "Idle"      => "#2196F3",
        "Faulted"   => "#F44336",
        "Stopped"   => "#9E9E9E",
        _           => "#FF9800"
    };
}
