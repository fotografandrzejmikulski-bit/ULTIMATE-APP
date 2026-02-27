using System.Collections.Concurrent;
using MAS.Core.Abstractions;
using MAS.Core.Enums;

namespace MAS.Infrastructure.Agents;

/// <summary>
/// Thread-safe rejestr agentów systemu.
/// </summary>
public sealed class AgentRegistry : IAgentRegistry
{
    private readonly ConcurrentDictionary<string, IAgent> _agents = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public void Register(IAgent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);
        _agents[agent.Id] = agent;
    }

    /// <inheritdoc/>
    public void Unregister(string agentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        _agents.TryRemove(agentId, out _);
    }

    /// <inheritdoc/>
    public IAgent? Find(string agentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        return _agents.TryGetValue(agentId, out var agent) ? agent : null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<IAgent> GetByStatus(AgentStatus status)
        => [.. _agents.Values.Where(a => a.Status == status)];

    /// <inheritdoc/>
    public IReadOnlyList<IAgent> GetAll()
        => [.. _agents.Values];
}
