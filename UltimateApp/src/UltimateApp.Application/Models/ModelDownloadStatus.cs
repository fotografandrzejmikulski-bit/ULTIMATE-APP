namespace UltimateApp.Application.Models;

/// <summary>Stan pobierania modelu.</summary>
public enum ModelDownloadState
{
    NotDownloaded,
    Downloading,
    Downloaded,
    ValidationFailed,
    Error
}

/// <summary>Pełny status pobierania modelu.</summary>
public record ModelDownloadStatus(
    string ModelName,
    ModelDownloadState State,
    double ProgressPercent = 0,
    string? LocalPath = null,
    string? ErrorMessage = null
);
