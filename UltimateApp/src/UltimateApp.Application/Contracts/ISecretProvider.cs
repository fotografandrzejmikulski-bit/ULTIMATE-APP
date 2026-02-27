namespace UltimateApp.Application.Contracts;

/// <summary>
/// Interfejs do pobierania sekretów (klucze API, hasła).
/// </summary>
public interface ISecretProvider
{
    /// <summary>Pobierz sekret po nazwie. Zwraca null jeśli nie znaleziono.</summary>
    Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default);
}
