namespace UltimateApp.Application.Models;

/// <summary>Odpowiedź od modelu AI.</summary>
public record AIResponse(
    string Text,
    string ProviderUsed,
    int TokensUsed = 0,
    TimeSpan? Duration = null,
    bool IsSuccess = true,
    string? ErrorMessage = null
);
