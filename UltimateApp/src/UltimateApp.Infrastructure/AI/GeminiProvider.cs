using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using UltimateApp.Application.Contracts;
using UltimateApp.Application.Models;

namespace UltimateApp.Infrastructure.AI;

/// <summary>
/// Dostawca AI oparty o Google Gemini API (cloud).
/// Klucz API pobierany przez ISecretProvider.
/// </summary>
public sealed class GeminiProvider : IAIProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<GeminiProvider> _logger;
    private readonly ISecretProvider _secretProvider;
    private readonly string _model;
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    public string ProviderName => "Gemini";

    public GeminiProvider(HttpClient http, ILogger<GeminiProvider> logger, ISecretProvider secretProvider, string model = "gemini-1.5-flash")
    {
        _http = http;
        _logger = logger;
        _secretProvider = secretProvider;
        _model = model;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        var key = await _secretProvider.GetSecretAsync("GEMINI_API_KEY", cancellationToken);
        return !string.IsNullOrEmpty(key);
    }

    public async Task<AIResponse> CompleteAsync(AIRequest request, CancellationToken cancellationToken = default)
    {
        var key = await _secretProvider.GetSecretAsync("GEMINI_API_KEY", cancellationToken);
        if (string.IsNullOrEmpty(key))
            throw new InvalidOperationException("Brak klucza GEMINI_API_KEY.");

        var parts = new List<object> { new { text = request.Prompt } };
        if (!string.IsNullOrEmpty(request.SystemPrompt))
            parts.Insert(0, new { text = $"[System]: {request.SystemPrompt}\n\n" });

        var payload = new
        {
            contents = new[] { new { parts } },
            generationConfig = new { maxOutputTokens = request.MaxTokens, temperature = request.Temperature }
        };

        var url = $"{BaseUrl}/{_model}:generateContent?key={key}";
        var httpResponse = await _http.PostAsJsonAsync(url, payload, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var json = await httpResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        var text = json
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;

        return new AIResponse(text.Trim(), ProviderName);
    }
}
