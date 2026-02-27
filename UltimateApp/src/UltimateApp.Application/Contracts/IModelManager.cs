using UltimateApp.Application.Models;

namespace UltimateApp.Application.Contracts;

/// <summary>
/// Zarządza pobieraniem i cachowaniem modeli GGUF.
/// </summary>
public interface IModelManager
{
    /// <summary>Sprawdź status obu modeli (LLM i Specialist).</summary>
    Task<(ModelDownloadStatus Llm, ModelDownloadStatus Specialist)> GetStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>Pobierz oba modele równolegle (LLM i Specialist).</summary>
    Task DownloadModelsAsync(IProgress<(string ModelName, double Percent)>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>Zwróć ścieżkę do modelu LLM (null jeśli nie pobrany).</summary>
    string? GetLlmModelPath();

    /// <summary>Zwróć ścieżkę do modelu Specialist (null jeśli nie pobrany).</summary>
    string? GetSpecialistModelPath();
}
