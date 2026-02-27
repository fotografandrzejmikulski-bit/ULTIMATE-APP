namespace MAS.Api.Models;

/// <summary>Żądanie REST do uruchomienia zadania przez agentów.</summary>
public sealed class DispatchTaskRequest
{
    public required string Title { get; init; }
    public required string Payload { get; init; }
    public int TimeoutSeconds { get; init; } = 300;
    public Dictionary<string, string> Context { get; init; } = [];
}

/// <summary>Odpowiedź REST zawierająca wynik zadania.</summary>
public sealed class TaskResultResponse
{
    public Guid TaskId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Result { get; init; }
    public double? DurationMs { get; init; }
}

/// <summary>Informacje o agencie w systemie (dla dashboardu).</summary>
public sealed class AgentInfoResponse
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
}
