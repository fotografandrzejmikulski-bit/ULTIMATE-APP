using UltimateApp.Application.Models;

namespace UltimateApp.Application.Contracts;

/// <summary>
/// Wspólny interfejs dla dostawców AI (GGUF, Ollama, Gemini).
/// </summary>
public interface IAIProvider
{
    /// <summary>Nazwa dostawcy (np. "GGUF", "Ollama", "Gemini").</summary>
    string ProviderName { get; }

    /// <summary>Czy dostawca jest dostępny (serwer uruchomiony, klucz skonfigurowany).</summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>Wyślij żądanie i zwróć odpowiedź.</summary>
    Task<AIResponse> CompleteAsync(AIRequest request, CancellationToken cancellationToken = default);
}
