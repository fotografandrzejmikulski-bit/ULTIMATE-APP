using MAS.Core.Abstractions;
using MAS.Core.Messages;
using Microsoft.AspNetCore.SignalR;

namespace MAS.Api.Hubs;

/// <summary>
/// SignalR Hub — kanał real-time dla aplikacji klienckich (desktop, mobile).
/// Umożliwia dwukierunkową komunikację: klient → agenty i agenty → klient.
/// Aplikacja mobilna/desktopowa subskrybuje zdarzenia przez WebSocket.
/// </summary>
public sealed class AgentHub : Hub
{
    private readonly IOrchestrator _orchestrator;

    public AgentHub(IOrchestrator orchestrator)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
    }

    /// <summary>
    /// Klient wysyła zadanie do systemu agentów przez WebSocket.
    /// Wynik jest zwracany asynchronicznie metodą "ReceiveTaskResult".
    /// </summary>
    public async Task SendTask(string title, string payload)
    {
        var task = new TaskMessage
        {
            SenderId = Context.ConnectionId,
            Title = title,
            Payload = payload
        };

        // Wyślij natychmiastowe potwierdzenie przyjęcia zadania
        await Clients.Caller.SendAsync("TaskAccepted", task.Id).ConfigureAwait(false);

        // Przetwarzaj asynchronicznie – nie blokuj WebSocket
        _ = Task.Run(async () =>
        {
            var result = await _orchestrator.DispatchAsync(task).ConfigureAwait(false);
            await Clients.Caller.SendAsync("ReceiveTaskResult", new
            {
                taskId = result.TaskId,
                status = result.Status.ToString(),
                result = result.Result,
                duration = result.Duration?.TotalMilliseconds
            }).ConfigureAwait(false);
        });
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", Context.ConnectionId).ConfigureAwait(false);
        await base.OnConnectedAsync().ConfigureAwait(false);
    }
}
