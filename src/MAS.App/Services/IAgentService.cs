using MAS.App.Models;

namespace MAS.App.Services;

/// <summary>
/// Kontrakt serwisu komunikacji z backendem MAS przez SignalR i REST API.
/// Izoluje ViewModels od szczegółów protokołu transportowego.
/// </summary>
public interface IAgentService
{
    /// <summary>Czy połączenie z backendem jest aktywne.</summary>
    bool IsConnected { get; }

    /// <summary>Zdarzenie wywoływane przy zmianie statusu połączenia.</summary>
    event EventHandler<bool> ConnectionStatusChanged;

    /// <summary>Zdarzenie wywoływane przy otrzymaniu wyniku zadania.</summary>
    event EventHandler<TaskResultModel> TaskResultReceived;

    /// <summary>Zdarzenie wywoływane przy aktualizacji listy agentów.</summary>
    event EventHandler<IReadOnlyList<AgentViewModel>> AgentsUpdated;

    /// <summary>Nawiązuje połączenie z backendem.</summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>Rozłącza się z backendem.</summary>
    Task DisconnectAsync();

    /// <summary>Wysyła zadanie do systemu agentów.</summary>
    Task<Guid> SendTaskAsync(AgentTaskModel task, CancellationToken cancellationToken = default);

    /// <summary>Pobiera listę agentów i ich statusy.</summary>
    Task<IReadOnlyList<AgentViewModel>> GetAgentsAsync(CancellationToken cancellationToken = default);
}
