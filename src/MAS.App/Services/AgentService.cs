using System.Net.Http.Json;
using MAS.App.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace MAS.App.Services;

/// <summary>
/// Implementacja serwisu agentów komunikująca się z backendem przez:
/// - SignalR (WebSocket) dla real-time powiadomień
/// - REST API dla operacji CRUD i statusów
/// </summary>
public sealed class AgentService : IAgentService, IAsyncDisposable
{
    private readonly string _baseUrl;
    private readonly HttpClient _http;
    private HubConnection? _hubConnection;

    public bool IsConnected { get; private set; }

    public event EventHandler<bool>? ConnectionStatusChanged;
    public event EventHandler<TaskResultModel>? TaskResultReceived;
    public event EventHandler<IReadOnlyList<AgentViewModel>>? AgentsUpdated;

    public AgentService(string baseUrl)
    {
        _baseUrl = baseUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseUrl));
        _http = new HttpClient { BaseAddress = new Uri(_baseUrl) };
    }

    /// <inheritdoc/>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{_baseUrl}/hubs/agents")
            .WithAutomaticReconnect()
            .Build();

        // Odbiór wyniku zadania
        _hubConnection.On<object>("ReceiveTaskResult", result =>
        {
            if (result is System.Text.Json.JsonElement element)
            {
                var model = new TaskResultModel
                {
                    TaskId = element.TryGetProperty("taskId", out var tid) && tid.TryGetGuid(out var g) ? g : Guid.Empty,
                    Status = element.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "",
                    Result = element.TryGetProperty("result", out var r) ? r.GetString() : null,
                    DurationMs = element.TryGetProperty("duration", out var d) ? d.GetDouble() : null
                };
                TaskResultReceived?.Invoke(this, model);
            }
        });

        _hubConnection.Closed += _ =>
        {
            IsConnected = false;
            ConnectionStatusChanged?.Invoke(this, false);
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += _ =>
        {
            IsConnected = true;
            ConnectionStatusChanged?.Invoke(this, true);
            return Task.CompletedTask;
        };

        await _hubConnection.StartAsync(cancellationToken).ConfigureAwait(false);
        IsConnected = true;
        ConnectionStatusChanged?.Invoke(this, true);
    }

    /// <inheritdoc/>
    public async Task DisconnectAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.StopAsync().ConfigureAwait(false);
            IsConnected = false;
        }
    }

    /// <inheritdoc/>
    public async Task<Guid> SendTaskAsync(AgentTaskModel task, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.SendAsync("SendTask", task.Title, task.Payload, cancellationToken)
                .ConfigureAwait(false);
            return task.Id;
        }

        // Fallback do REST API
        var response = await _http.PostAsJsonAsync("api/tasks", new
        {
            title = task.Title,
            payload = task.Payload,
            timeoutSeconds = task.TimeoutSeconds,
            context = task.Context
        }, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        return task.Id;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AgentViewModel>> GetAgentsAsync(CancellationToken cancellationToken = default)
    {
        var agents = await _http
            .GetFromJsonAsync<List<AgentViewModel>>("api/agents", cancellationToken)
            .ConfigureAwait(false);

        return agents ?? [];
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync().ConfigureAwait(false);
        }
        _http.Dispose();
    }
}
