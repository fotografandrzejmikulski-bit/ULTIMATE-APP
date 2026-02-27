using MAS.Core.Enums;
using MAS.Core.Messages;

namespace MAS.Core.Abstractions;

/// <summary>
/// Kontrakt każdego autonomicznego agenta w systemie MAS.
/// Wzorzec: Strategy + Observer.
/// </summary>
public interface IAgent
{
    /// <summary>Unikalny identyfikator agenta w systemie.</summary>
    string Id { get; }

    /// <summary>Czytelna dla człowieka nazwa agenta.</summary>
    string Name { get; }

    /// <summary>Aktualny status agenta.</summary>
    AgentStatus Status { get; }

    /// <summary>
    /// Inicjalizuje agenta i przygotowuje zasoby.
    /// Wywoływane raz przy starcie systemu.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Wysyła wiadomość do agenta.
    /// Metoda jest nieblokująca — wiadomość jest umieszczana w kolejce wewnętrznej.
    /// </summary>
    ValueTask EnqueueMessageAsync(AgentMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uruchamia pętlę przetwarzania wiadomości agenta.
    /// Wywoływane przez AgentRunner jako hosted service.
    /// </summary>
    Task RunAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Zatrzymuje agenta i zwalnia zasoby.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}
