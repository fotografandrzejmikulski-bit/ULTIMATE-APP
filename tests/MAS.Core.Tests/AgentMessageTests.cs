using MAS.Core.Enums;
using MAS.Core.Messages;

namespace MAS.Core.Tests;

/// <summary>
/// Testy modeli wiadomości systemu MAS.
/// </summary>
public sealed class AgentMessageTests
{
    [Fact]
    public void TaskMessage_DefaultTimeout_IsFiveMinutes()
    {
        var msg = new TaskMessage
        {
            SenderId = "sender",
            Title = "Test",
            Payload = "data"
        };
        Assert.Equal(TimeSpan.FromMinutes(5), msg.Timeout);
    }

    [Fact]
    public void TaskMessage_Id_IsUnique()
    {
        var msg1 = new TaskMessage { SenderId = "s", Title = "T", Payload = "P" };
        var msg2 = new TaskMessage { SenderId = "s", Title = "T", Payload = "P" };
        Assert.NotEqual(msg1.Id, msg2.Id);
    }

    [Fact]
    public void TaskResultMessage_Duration_CalculatesCorrectly()
    {
        var start = DateTimeOffset.UtcNow;
        var end = start.AddSeconds(2);

        var result = new TaskResultMessage
        {
            SenderId = "agent",
            TaskId = Guid.NewGuid(),
            Status = TaskExecutionStatus.Succeeded,
            StartedAt = start,
            CompletedAt = end
        };

        Assert.Equal(TimeSpan.FromSeconds(2), result.Duration);
    }

    [Fact]
    public void TaskResultMessage_Duration_IsNullWhenTimesNotSet()
    {
        var result = new TaskResultMessage
        {
            SenderId = "agent",
            TaskId = Guid.NewGuid(),
            Status = TaskExecutionStatus.Pending
        };

        Assert.Null(result.Duration);
    }

    [Fact]
    public void AgentMessage_DefaultPriority_IsNormal()
    {
        var msg = new TaskMessage { SenderId = "s", Title = "T", Payload = "P" };
        Assert.Equal(MessagePriority.Normal, msg.Priority);
    }

    [Fact]
    public void AgentMessage_Context_IsCaseInsensitive()
    {
        var msg = new TaskMessage
        {
            SenderId = "s",
            Title = "T",
            Payload = "P",
            Context = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["KEY"] = "value"
            }
        };

        Assert.Equal("value", msg.Context["key"]);
        Assert.Equal("value", msg.Context["KEY"]);
    }
}
