using MAS.Core.Enums;

namespace MAS.Core.Messages;

/// <summary>
/// Wiadomość zmiany statusu agenta - event publikowany przez każdego agenta.
/// </summary>
public sealed record AgentStatusChangedMessage : AgentMessage
{
    /// <summary>Poprzedni status agenta.</summary>
    public required AgentStatus PreviousStatus { get; init; }

    /// <summary>Nowy status agenta.</summary>
    public required AgentStatus NewStatus { get; init; }

    /// <summary>Opcjonalny powód zmiany statusu.</summary>
    public string? Reason { get; init; }
}
