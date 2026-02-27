using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using UltimateApp.Application.Contracts;
using UltimateApp.Application.Models;

namespace UltimateApp.Infrastructure.AI;

/// <summary>
/// Dostawca AI oparty o lokalny serwer llama.cpp (HTTP).
/// Dokumentacja API: https://github.com/ggerganov/llama.cpp/blob/master/examples/server/README.md
/// </summary>
public sealed class GgufProvider : IAIProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<GgufProvider> _logger;
    private readonly string _baseUrl;

    public string ProviderName => "GGUF";

    public GgufProvider(HttpClient http, ILogger<GgufProvider> logger, string baseUrl = "http://127.0.0.1:8080")
    {
        _http = http;
        _logger = logger;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            var response = await _http.GetAsync($"{_baseUrl}/health", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<AIResponse> CompleteAsync(AIRequest request, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            prompt = BuildPrompt(request),
            n_predict = request.MaxTokens,
            temperature = request.Temperature,
            stop = new[] { "</s>", "[INST]" }
        };

        var httpResponse = await _http.PostAsJsonAsync($"{_baseUrl}/completion", payload, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var json = await httpResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        var text = json.GetProperty("content").GetString() ?? string.Empty;
        var tokensStopped = json.TryGetProperty("tokens_evaluated", out var tokEl) ? tokEl.GetInt32() : 0;

        return new AIResponse(text.Trim(), ProviderName, TokensUsed: tokensStopped);
    }

    private static string BuildPrompt(AIRequest request)
    {
        if (string.IsNullOrEmpty(request.SystemPrompt))
            return request.Prompt;
        return $"[INST] <<SYS>>\n{request.SystemPrompt}\n<</SYS>>\n\n{request.Prompt} [/INST]";
    }
}
