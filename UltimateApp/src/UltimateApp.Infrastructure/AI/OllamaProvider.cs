using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using UltimateApp.Application.Contracts;
using UltimateApp.Application.Models;

namespace UltimateApp.Infrastructure.AI;

/// <summary>
/// Dostawca AI oparty o lokalny serwer Ollama (HTTP).
/// </summary>
public sealed class OllamaProvider : IAIProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<OllamaProvider> _logger;
    private readonly string _baseUrl;
    private readonly string _model;

    public string ProviderName => "Ollama";

    public OllamaProvider(HttpClient http, ILogger<OllamaProvider> logger, string baseUrl = "http://127.0.0.1:11434", string model = "llama3")
    {
        _http = http;
        _logger = logger;
        _baseUrl = baseUrl.TrimEnd('/');
        _model = model;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            var response = await _http.GetAsync($"{_baseUrl}/api/tags", cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<AIResponse> CompleteAsync(AIRequest request, CancellationToken cancellationToken = default)
    {
        var messages = new List<object>();
        if (!string.IsNullOrEmpty(request.SystemPrompt))
            messages.Add(new { role = "system", content = request.SystemPrompt });
        messages.Add(new { role = "user", content = request.Prompt });

        var payload = new
        {
            model = _model,
            messages,
            stream = false,
            options = new { num_predict = request.MaxTokens, temperature = request.Temperature }
        };

        var httpResponse = await _http.PostAsJsonAsync($"{_baseUrl}/api/chat", payload, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var json = await httpResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        var text = json.GetProperty("message").GetProperty("content").GetString() ?? string.Empty;

        return new AIResponse(text.Trim(), ProviderName);
    }
}
