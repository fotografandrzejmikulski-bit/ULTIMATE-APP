using MAS.Core.Messages;
using MAS.Infrastructure.Messaging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MAS.Infrastructure.Tests;

/// <summary>
/// Testy jednostkowe InProcessMessageBus.
/// </summary>
public sealed class InProcessMessageBusTests
{
    private static InProcessMessageBus CreateBus()
        => new(NullLogger<InProcessMessageBus>.Instance);

    [Fact]
    public async Task Publish_NoSubscribers_DoesNotThrow()
    {
        var bus = CreateBus();
        var msg = new TaskMessage { SenderId = "s", Title = "T", Payload = "P" };
        await bus.PublishAsync(msg); // powinno zakończyć się bez błędu
    }

    [Fact]
    public async Task Subscribe_ReceivesPublishedMessage()
    {
        var bus = CreateBus();
        TaskMessage? received = null;

        bus.Subscribe<TaskMessage>((msg, _) =>
        {
            received = msg;
            return ValueTask.CompletedTask;
        });

        var sent = new TaskMessage { SenderId = "s", Title = "T", Payload = "P" };
        await bus.PublishAsync(sent);

        Assert.Same(sent, received);
    }

    [Fact]
    public async Task Subscribe_WithAgentId_OnlyReceivesMatchingMessages()
    {
        var bus = CreateBus();
        var receivedByAgent1 = new List<AgentMessage>();
        var receivedByAgent2 = new List<AgentMessage>();

        bus.Subscribe<TaskMessage>("agent-1", (msg, _) =>
        {
            receivedByAgent1.Add(msg);
            return ValueTask.CompletedTask;
        });

        bus.Subscribe<TaskMessage>("agent-2", (msg, _) =>
        {
            receivedByAgent2.Add(msg);
            return ValueTask.CompletedTask;
        });

        var forAgent1 = new TaskMessage { SenderId = "s", ReceiverId = "agent-1", Title = "T", Payload = "P" };
        var forAgent2 = new TaskMessage { SenderId = "s", ReceiverId = "agent-2", Title = "T", Payload = "P" };

        await bus.PublishAsync(forAgent1);
        await bus.PublishAsync(forAgent2);

        Assert.Single(receivedByAgent1);
        Assert.Single(receivedByAgent2);
        Assert.Equal(forAgent1.Id, receivedByAgent1[0].Id);
        Assert.Equal(forAgent2.Id, receivedByAgent2[0].Id);
    }

    [Fact]
    public async Task Broadcast_DeliveredToAllSubscribers()
    {
        var bus = CreateBus();
        var count = 0;

        bus.Subscribe<TaskMessage>((_, _) => { count++; return ValueTask.CompletedTask; });
        bus.Subscribe<TaskMessage>((_, _) => { count++; return ValueTask.CompletedTask; });

        // Wiadomość bez ReceiverId = broadcast
        var broadcast = new TaskMessage { SenderId = "s", ReceiverId = null, Title = "T", Payload = "P" };
        await bus.PublishAsync(broadcast);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Unsubscribe_StopsReceivingMessages()
    {
        var bus = CreateBus();
        var count = 0;

        var subscription = bus.Subscribe<TaskMessage>((_, _) =>
        {
            count++;
            return ValueTask.CompletedTask;
        });

        var msg1 = new TaskMessage { SenderId = "s", Title = "T", Payload = "P" };
        await bus.PublishAsync(msg1);
        Assert.Equal(1, count);

        subscription.Dispose(); // anuluj subskrypcję

        var msg2 = new TaskMessage { SenderId = "s", Title = "T", Payload = "P" };
        await bus.PublishAsync(msg2);
        Assert.Equal(1, count); // handler nie powinien zostać wywołany ponownie
    }

    [Fact]
    public async Task Publish_NullMessage_ThrowsArgumentNullException()
    {
        var bus = CreateBus();
        await Assert.ThrowsAsync<ArgumentNullException>(() => bus.PublishAsync(null!).AsTask());
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var bus = CreateBus();
        bus.Dispose(); // nie powinno rzucić wyjątku
    }
}
