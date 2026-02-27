namespace UltimateApp.Application.Models;

/// <summary>Informacje o modelu GGUF do pobrania.</summary>
public record ModelInfo(
    string Name,
    string DownloadUrl,
    string FileName,
    string? ExpectedSha256 = null
);
