using MAS.Core.Enums;
using MAS.Infrastructure.Agents;

namespace MAS.Infrastructure.Tests;

/// <summary>
/// Testy jednostkowe rejestru agentów.
/// </summary>
public sealed class AgentRegistryTests
{
    private sealed class FakeAgent : MAS.Core.Agents.BaseAgent
    {
        public FakeAgent(string id) : base(id, $"Agent {id}") { }

        protected override Task ProcessMessageAsync(MAS.Core.Messages.AgentMessage message, CancellationToken ct)
            => Task.CompletedTask;
    }

    [Fact]
    public void Register_ThenFind_ReturnsAgent()
    {
        var registry = new AgentRegistry();
        var agent = new FakeAgent("agent-1");

        registry.Register(agent);

        Assert.Same(agent, registry.Find("agent-1"));
    }

    [Fact]
    public void Find_NonExistent_ReturnsNull()
    {
        var registry = new AgentRegistry();
        Assert.Null(registry.Find("non-existent"));
    }

    [Fact]
    public void Unregister_RemovesAgent()
    {
        var registry = new AgentRegistry();
        var agent = new FakeAgent("agent-1");
        registry.Register(agent);

        registry.Unregister("agent-1");

        Assert.Null(registry.Find("agent-1"));
    }

    [Fact]
    public void GetAll_ReturnsAllRegistered()
    {
        var registry = new AgentRegistry();
        registry.Register(new FakeAgent("a1"));
        registry.Register(new FakeAgent("a2"));
        registry.Register(new FakeAgent("a3"));

        Assert.Equal(3, registry.GetAll().Count);
    }

    [Fact]
    public void GetByStatus_FiltersCorrectly()
    {
        var registry = new AgentRegistry();
        registry.Register(new FakeAgent("idle-1"));
        registry.Register(new FakeAgent("idle-2"));

        // Wszystkie agenty zaczynają w statusie Idle
        var idle = registry.GetByStatus(AgentStatus.Idle);
        var running = registry.GetByStatus(AgentStatus.Running);

        Assert.Equal(2, idle.Count);
        Assert.Empty(running);
    }

    [Fact]
    public void Register_ThrowsOnNull()
    {
        var registry = new AgentRegistry();
        Assert.Throws<ArgumentNullException>(() => registry.Register(null!));
    }

    [Fact]
    public void Find_CaseInsensitive()
    {
        var registry = new AgentRegistry();
        registry.Register(new FakeAgent("Agent-One"));

        Assert.NotNull(registry.Find("agent-one"));
        Assert.NotNull(registry.Find("AGENT-ONE"));
    }
}
