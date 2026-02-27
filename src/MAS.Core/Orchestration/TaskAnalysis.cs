using MAS.Core.Abstractions;

namespace MAS.Core.Orchestration;

/// <summary>
/// Model danych reprezentujący wynik analizy zadania przez orkiestratora
/// przed jego delegacją do agenta specjalistycznego.
/// </summary>
public sealed record TaskAnalysis
{
    public required string TaskId { get; init; }
    public required string RecommendedAgentType { get; init; }
    public required double ConfidenceScore { get; init; }
    public IReadOnlyList<string> RequiredCapabilities { get; init; } = [];
}
