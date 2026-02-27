using UltimateApp.Application.Contracts;

namespace UltimateApp.Infrastructure.Secrets;

/// <summary>
/// Implementacja ISecretProvider pobierająca sekrety ze zmiennych środowiskowych.
/// Interfejs umożliwia podłączenie Windows Credential Manager/DPAPI w przyszłości.
/// </summary>
public sealed class EnvironmentSecretProvider : ISecretProvider
{
    public Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return Task.FromResult(value);
    }
}
