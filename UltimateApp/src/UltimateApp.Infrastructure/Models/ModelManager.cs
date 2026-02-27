using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using UltimateApp.Application.Contracts;
using UltimateApp.Application.Models;

namespace UltimateApp.Infrastructure.Models;

/// <summary>
/// Zarządza pobieraniem, cachowaniem i walidacją modeli GGUF.
/// Obsługuje atomowe podmiany plików i walidację SHA256.
/// </summary>
public sealed class ModelManager : IModelManager
{
    private readonly HttpClient _http;
    private readonly ILogger<ModelManager> _logger;
    private readonly ModelInfo _llmModel;
    private readonly ModelInfo _specialistModel;
    private readonly string _cacheDirectory;

    public ModelManager(
        HttpClient http,
        ILogger<ModelManager> logger,
        ModelInfo llmModel,
        ModelInfo specialistModel,
        string? cacheDirectory = null)
    {
        _http = http;
        _logger = logger;
        _llmModel = llmModel;
        _specialistModel = specialistModel;
        _cacheDirectory = cacheDirectory ?? Path.Combine(AppContext.BaseDirectory, "cache");
        Directory.CreateDirectory(_cacheDirectory);
    }

    public async Task<(ModelDownloadStatus Llm, ModelDownloadStatus Specialist)> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return (GetModelStatus(_llmModel), GetModelStatus(_specialistModel));
    }

    public async Task DownloadModelsAsync(
        IProgress<(string ModelName, double Percent)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Rozpoczynam równoległe pobieranie modeli...");

        var llmTask = DownloadModelAsync(_llmModel, progress, cancellationToken);
        var specTask = DownloadModelAsync(_specialistModel, progress, cancellationToken);

        await Task.WhenAll(llmTask, specTask);

        _logger.LogInformation("Pobieranie modeli zakończone.");
    }

    public string? GetLlmModelPath()
    {
        var path = GetModelCachePath(_llmModel);
        return File.Exists(path) ? path : null;
    }

    public string? GetSpecialistModelPath()
    {
        var path = GetModelCachePath(_specialistModel);
        return File.Exists(path) ? path : null;
    }

    private ModelDownloadStatus GetModelStatus(ModelInfo model)
    {
        var path = GetModelCachePath(model);
        if (!File.Exists(path))
            return new ModelDownloadStatus(model.Name, ModelDownloadState.NotDownloaded);

        return new ModelDownloadStatus(model.Name, ModelDownloadState.Downloaded, 100, path);
    }

    private string GetModelCachePath(ModelInfo model) =>
        Path.Combine(_cacheDirectory, model.FileName);

    private async Task DownloadModelAsync(
        ModelInfo model,
        IProgress<(string ModelName, double Percent)>? progress,
        CancellationToken cancellationToken)
    {
        var finalPath = GetModelCachePath(model);
        var tempPath = finalPath + ".tmp";

        if (File.Exists(finalPath))
        {
            if (await ValidateSha256Async(finalPath, model.ExpectedSha256, cancellationToken))
            {
                _logger.LogInformation("Model {Name} już pobrany i zwalidowany.", model.Name);
                progress?.Report((model.Name, 100));
                return;
            }
            _logger.LogWarning("Model {Name} nie przeszedł walidacji SHA256, ponowne pobieranie.", model.Name);
        }

        _logger.LogInformation("Pobieram model {Name} z {Url}", model.Name, model.DownloadUrl);

        try
        {
            using var response = await _http.GetAsync(model.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

            var buffer = new byte[81920];
            long downloadedBytes = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                downloadedBytes += bytesRead;

                if (totalBytes > 0)
                {
                    var percent = (double)downloadedBytes / totalBytes * 100;
                    progress?.Report((model.Name, percent));
                }
            }
        }
        catch
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            throw;
        }

        if (!await ValidateSha256Async(tempPath, model.ExpectedSha256, cancellationToken))
        {
            File.Delete(tempPath);
            throw new InvalidOperationException($"Walidacja SHA256 nieudana dla modelu {model.Name}.");
        }

        // Atomowa podmiana pliku
        File.Move(tempPath, finalPath, overwrite: true);
        _logger.LogInformation("Model {Name} pobrany i zapisany do {Path}", model.Name, finalPath);
        progress?.Report((model.Name, 100));
    }

    /// <summary>Walidacja SHA256. Jeśli expected jest null/empty, walidacja jest pomijana (zwraca true).</summary>
    public static async Task<bool> ValidateSha256Async(string filePath, string? expectedSha256, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(expectedSha256))
            return true;

        using var sha256 = SHA256.Create();
        await using var stream = File.OpenRead(filePath);
        var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
        var actualHash = Convert.ToHexString(hashBytes);
        return string.Equals(actualHash, expectedSha256, StringComparison.OrdinalIgnoreCase);
    }
}
