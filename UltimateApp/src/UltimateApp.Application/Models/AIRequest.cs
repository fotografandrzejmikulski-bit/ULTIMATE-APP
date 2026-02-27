namespace UltimateApp.Application.Models;

/// <summary>Żądanie do modelu AI.</summary>
public record AIRequest(
    string Prompt,
    string? SystemPrompt = null,
    int MaxTokens = 512,
    double Temperature = 0.7,
    string? PreferredProvider = null
);
