using BlueprintOS.Core.AI.Models;

namespace BlueprintOS.UnitTests.Core.AI;

public class ChatMessageTests
{
    [Fact]
    public void Should_Create_Message_Without_Optional_Fields()
    {
        var message = new ChatMessage(ChatRole.User, "Olá, mundo.");

        Assert.Equal(ChatRole.User, message.Role);
        Assert.Equal("Olá, mundo.", message.Content);
        Assert.Null(message.ToolCalls);
        Assert.Null(message.ToolCallId);
        Assert.Null(message.Name);
    }

    [Fact]
    public void Should_Create_Tool_Result_Message_With_ToolCallId()
    {
        var message = new ChatMessage(ChatRole.Tool, "{\"result\":42}", ToolCallId: "call-1");

        Assert.Equal(ChatRole.Tool, message.Role);
        Assert.Equal("call-1", message.ToolCallId);
    }

    [Fact]
    public void Should_Create_Assistant_Message_With_ToolCalls()
    {
        var toolCalls = new[] { new ToolCall("call-1", "get_weather", "{\"city\":\"São Paulo\"}") };

        var message = new ChatMessage(ChatRole.Assistant, string.Empty, ToolCalls: toolCalls);

        Assert.Single(message.ToolCalls!);
        Assert.Equal("get_weather", message.ToolCalls![0].ToolName);
    }
}
