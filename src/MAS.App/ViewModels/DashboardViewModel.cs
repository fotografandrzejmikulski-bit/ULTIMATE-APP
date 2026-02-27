using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MAS.App.Models;
using MAS.App.Services;

namespace MAS.App.ViewModels;

/// <summary>
/// Główny ViewModel dashboardu agentów.
/// Implementuje MVVM z INotifyPropertyChanged dla .NET MAUI data-binding.
/// Wzorzec: MVVM + Observer.
/// </summary>
public sealed class DashboardViewModel : INotifyPropertyChanged, IAsyncDisposable
{
    private readonly IAgentService _agentService;
    private bool _isConnected;
    private bool _isBusy;
    private string _taskTitle = string.Empty;
    private string _taskPayload = string.Empty;
    private string _connectionStatus = "Rozłączono";

    public DashboardViewModel(IAgentService agentService)
    {
        _agentService = agentService ?? throw new ArgumentNullException(nameof(agentService));

        Agents = [];
        TaskResults = [];

        _agentService.ConnectionStatusChanged += OnConnectionStatusChanged;
        _agentService.TaskResultReceived += OnTaskResultReceived;
        _agentService.AgentsUpdated += OnAgentsUpdated;
    }

    // ─── Właściwości bindowane ─────────────────────────────────────────────────

    public ObservableCollection<AgentViewModel> Agents { get; }
    public ObservableCollection<TaskResultModel> TaskResults { get; }

    public bool IsConnected
    {
        get => _isConnected;
        private set => SetField(ref _isConnected, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetField(ref _isBusy, value);
    }

    public string ConnectionStatus
    {
        get => _connectionStatus;
        private set => SetField(ref _connectionStatus, value);
    }

    public string TaskTitle
    {
        get => _taskTitle;
        set => SetField(ref _taskTitle, value);
    }

    public string TaskPayload
    {
        get => _taskPayload;
        set => SetField(ref _taskPayload, value);
    }

    // ─── Komendy / akcje ──────────────────────────────────────────────────────

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        IsBusy = true;
        try
        {
            await _agentService.ConnectAsync(ct).ConfigureAwait(false);
            await RefreshAgentsAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task SendTaskAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(TaskTitle) || string.IsNullOrWhiteSpace(TaskPayload))
            return;

        IsBusy = true;
        try
        {
            var task = new AgentTaskModel
            {
                Title = TaskTitle,
                Payload = TaskPayload
            };

            await _agentService.SendTaskAsync(task, ct).ConfigureAwait(false);
            TaskTitle = string.Empty;
            TaskPayload = string.Empty;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RefreshAgentsAsync(CancellationToken ct = default)
    {
        var agents = await _agentService.GetAgentsAsync(ct).ConfigureAwait(false);
        OnMainThread(() =>
        {
            Agents.Clear();
            foreach (var agent in agents)
                Agents.Add(agent);
        });
    }

    // ─── Obsługa zdarzeń serwisu ──────────────────────────────────────────────

    private void OnConnectionStatusChanged(object? sender, bool connected)
    {
        OnMainThread(() =>
        {
            IsConnected = connected;
            ConnectionStatus = connected ? "Połączono ✅" : "Rozłączono ❌";
        });
    }

    private void OnTaskResultReceived(object? sender, TaskResultModel result)
    {
        OnMainThread(() => TaskResults.Insert(0, result));
    }

    private void OnAgentsUpdated(object? sender, IReadOnlyList<AgentViewModel> agents)
    {
        OnMainThread(() =>
        {
            Agents.Clear();
            foreach (var agent in agents)
                Agents.Add(agent);
        });
    }

    // ─── INotifyPropertyChanged ───────────────────────────────────────────────

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    /// <summary>
    /// Przełącza na główny wątek UI – w MAUI używa MainThread.BeginInvokeOnMainThread.
    /// W testach jest to no-op.
    /// </summary>
    private static void OnMainThread(Action action)
    {
        // W aplikacji MAUI: Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(action)
        // W kontekście testów: wykonaj bezpośrednio
        action();
    }

    public async ValueTask DisposeAsync()
    {
        _agentService.ConnectionStatusChanged -= OnConnectionStatusChanged;
        _agentService.TaskResultReceived -= OnTaskResultReceived;
        _agentService.AgentsUpdated -= OnAgentsUpdated;

        if (_agentService is IAsyncDisposable disposable)
            await disposable.DisposeAsync().ConfigureAwait(false);
    }
}
