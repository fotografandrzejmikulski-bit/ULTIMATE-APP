using MAS.Core.Agents;
using MAS.Core.Enums;
using MAS.Core.Messages;

namespace MAS.Core.Tests;

/// <summary>
/// Testy jednostkowe klasy bazowej BaseAgent.
/// </summary>
public sealed class BaseAgentTests
{
    // Testowy agent — minimalny, ale realistyczny
    private sealed class TestAgent : BaseAgent
    {
        public List<AgentMessage> ProcessedMessages { get; } = [];
        public List<(AgentStatus Previous, AgentStatus Current)> StatusChanges { get; } = [];

        public TestAgent() : base("test-agent", "Test Agent") { }

        protected override Task ProcessMessageAsync(AgentMessage message, CancellationToken cancellationToken)
        {
            ProcessedMessages.Add(message);
            return Task.CompletedTask;
        }

        protected override void OnStatusChanged(AgentStatus previous, AgentStatus current)
            => StatusChanges.Add((previous, current));

        public new void SetStatus(AgentStatus status) => base.SetStatus(status);
    }

    [Fact]
    public void Constructor_SetsIdAndName()
    {
        var agent = new TestAgent();
        Assert.Equal("test-agent", agent.Id);
        Assert.Equal("Test Agent", agent.Name);
    }

    [Fact]
    public void InitialStatus_IsIdle()
    {
        var agent = new TestAgent();
        Assert.Equal(AgentStatus.Idle, agent.Status);
    }

    [Fact]
    public void SetStatus_ChangesStatus()
    {
        var agent = new TestAgent();
        agent.SetStatus(AgentStatus.Running);
        Assert.Equal(AgentStatus.Running, agent.Status);
    }

    [Fact]
    public void SetStatus_SameValue_DoesNotFireStatusChanged()
    {
        var agent = new TestAgent();
        agent.SetStatus(AgentStatus.Idle); // same as initial
        Assert.Empty(agent.StatusChanges);
    }

    [Fact]
    public void SetStatus_DifferentValue_FiresStatusChanged()
    {
        var agent = new TestAgent();
        agent.SetStatus(AgentStatus.Running);
        Assert.Single(agent.StatusChanges);
        Assert.Equal((AgentStatus.Idle, AgentStatus.Running), agent.StatusChanges[0]);
    }

    [Fact]
    public async Task EnqueueMessageAsync_MessageIsProcessedByRunLoop()
    {
        var agent = new TestAgent();
        using var cts = new CancellationTokenSource();

        // Uruchom pętlę agenta
        var runTask = agent.RunAsync(cts.Token);

        var message = new TaskMessage
        {
            SenderId = "sender",
            Title = "Test",
            Payload = "data"
        };

        await agent.EnqueueMessageAsync(message);

        // Daj czas na przetworzenie
        await Task.Delay(100);

        await cts.CancelAsync();
        await agent.StopAsync();

        try { await runTask; } catch (OperationCanceledException) { }

        Assert.Single(agent.ProcessedMessages);
        Assert.Equal(message.Id, agent.ProcessedMessages[0].Id);
    }

    [Fact]
    public async Task RunAsync_SetsStatusToRunning()
    {
        var agent = new TestAgent();
        using var cts = new CancellationTokenSource();

        var runTask = agent.RunAsync(cts.Token);
        await Task.Delay(50);

        Assert.Equal(AgentStatus.Running, agent.Status);

        await cts.CancelAsync();
        await agent.StopAsync();

        try { await runTask; } catch (OperationCanceledException) { }
    }

    [Fact]
    public async Task StopAsync_SetsStatusToStopped()
    {
        var agent = new TestAgent();
        using var cts = new CancellationTokenSource();

        var runTask = agent.RunAsync(cts.Token);
        await Task.Delay(50);

        await cts.CancelAsync();
        await agent.StopAsync();

        try { await runTask; } catch (OperationCanceledException) { }

        Assert.Equal(AgentStatus.Stopped, agent.Status);
    }
}
