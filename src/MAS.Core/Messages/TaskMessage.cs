namespace MAS.Core.Messages;

/// <summary>
/// Wiadomość zadania wysyłana do agenta wykonawczego.
/// </summary>
public sealed record TaskMessage : AgentMessage
{
    /// <summary>Tytuł / krótki opis zadania.</summary>
    public required string Title { get; init; }

    /// <summary>Szczegółowy opis zadania w języku naturalnym lub strukturyzowany JSON.</summary>
    public required string Payload { get; init; }

    /// <summary>Maksymalny czas wykonania zadania.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>Opcjonalne parametry kontekstowe (klucz-wartość).</summary>
    public IReadOnlyDictionary<string, string> Context { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
