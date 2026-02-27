using MAS.Core.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MAS.Infrastructure.Agents;

/// <summary>
/// Hosted service uruchamiający pętle przetwarzania dla wszystkich zarejestrowanych agentów.
/// Każdy agent działa na dedykowanym TaskSchedulerze, nie blokując żadnych wątków UI.
/// </summary>
public sealed class AgentRunner : BackgroundService
{
    private readonly IAgentRegistry _registry;
    private readonly ILogger<AgentRunner> _logger;
    private readonly List<Task> _agentTasks = [];

    public AgentRunner(IAgentRegistry registry, ILogger<AgentRunner> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AgentRunner: inicjalizacja agentów…");

        var agents = _registry.GetAll();

        foreach (var agent in agents)
        {
            _logger.LogInformation("Inicjalizacja agenta: {AgentId} ({AgentName})", agent.Id, agent.Name);
            await agent.InitializeAsync(stoppingToken).ConfigureAwait(false);
        }

        _logger.LogInformation("AgentRunner: uruchamianie {Count} agentów…", agents.Count);

        foreach (var agent in agents)
        {
            var capturedAgent = agent;
            var task = Task.Run(
                () => capturedAgent.RunAsync(stoppingToken),
                stoppingToken);
            _agentTasks.Add(task);
        }

        _logger.LogInformation("AgentRunner: wszystkie agenty uruchomione.");

        await Task.WhenAll(_agentTasks).ConfigureAwait(false);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("AgentRunner: zatrzymywanie agentów…");

        foreach (var agent in _registry.GetAll())
        {
            await agent.StopAsync(cancellationToken).ConfigureAwait(false);
        }

        await base.StopAsync(cancellationToken).ConfigureAwait(false);
    }
}
