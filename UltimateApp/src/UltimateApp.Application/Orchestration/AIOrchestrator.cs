using Microsoft.Extensions.Logging;
using UltimateApp.Application.Contracts;
using UltimateApp.Application.Models;

namespace UltimateApp.Application.Orchestration;

/// <summary>
/// Orkiestrator DUAL GGUF + GEMINI.
/// Routuje zadania do odpowiedniego dostawcy z timeoutem, CancellationToken i limitem równoległości.
/// </summary>
public sealed class AIOrchestrator
{
    private readonly IReadOnlyList<IAIProvider> _providers;
    private readonly ILogger<AIOrchestrator> _logger;
    private readonly SemaphoreSlim _parallelismLimiter;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    public AIOrchestrator(
        IEnumerable<IAIProvider> providers,
        ILogger<AIOrchestrator> logger,
        int maxParallelism = 4)
    {
        _providers = providers.ToList();
        _logger = logger;
        _parallelismLimiter = new SemaphoreSlim(maxParallelism, maxParallelism);
    }

    /// <summary>
    /// Wybierz dostawcę i wyślij żądanie.
    /// Kolejność: preferowany (jeśli podany) → GGUF → Ollama → Gemini.
    /// </summary>
    public async Task<AIResponse> CompleteAsync(
        AIRequest request,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout ?? DefaultTimeout);

        await _parallelismLimiter.WaitAsync(cts.Token);
        try
        {
            var provider = await SelectProviderAsync(request.PreferredProvider, cts.Token);
            if (provider is null)
            {
                _logger.LogError("Brak dostępnego dostawcy AI.");
                return new AIResponse(string.Empty, "none", IsSuccess: false, ErrorMessage: "Brak dostępnego dostawcy AI.");
            }

            _logger.LogInformation("Wysyłam żądanie do dostawcy: {Provider}", provider.ProviderName);
            var started = DateTime.UtcNow;

            try
            {
                var response = await provider.CompleteAsync(request, cts.Token);
                var duration = DateTime.UtcNow - started;
                _logger.LogInformation("Odpowiedź od {Provider} w {Ms}ms, tokeny: {Tokens}",
                    provider.ProviderName, (int)duration.TotalMilliseconds, response.TokensUsed);
                return response with { Duration = duration };
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Timeout żądania do {Provider}", provider.ProviderName);
                return new AIResponse(string.Empty, provider.ProviderName, IsSuccess: false, ErrorMessage: "Timeout.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd żądania do {Provider}", provider.ProviderName);
                return new AIResponse(string.Empty, provider.ProviderName, IsSuccess: false, ErrorMessage: ex.Message);
            }
        }
        finally
        {
            _parallelismLimiter.Release();
        }
    }

    private async Task<IAIProvider?> SelectProviderAsync(string? preferred, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(preferred))
        {
            var p = _providers.FirstOrDefault(x => x.ProviderName.Equals(preferred, StringComparison.OrdinalIgnoreCase));
            if (p is not null && await p.IsAvailableAsync(ct))
                return p;
        }

        foreach (var provider in _providers)
        {
            if (await provider.IsAvailableAsync(ct))
                return provider;
        }

        return null;
    }
}
