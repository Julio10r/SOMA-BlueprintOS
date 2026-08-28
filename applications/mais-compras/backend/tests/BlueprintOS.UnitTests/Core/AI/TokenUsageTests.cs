using BlueprintOS.Core.AI.Models;

namespace BlueprintOS.UnitTests.Core.AI;

public class TokenUsageTests
{
    [Fact]
    public void TotalTokens_Should_Sum_Prompt_And_Completion_Tokens()
    {
        var usage = new TokenUsage(PromptTokens: 120, CompletionTokens: 80);

        Assert.Equal(200, usage.TotalTokens);
    }

    [Fact]
    public void Records_With_Same_Values_Should_Be_Equal()
    {
        var first = new TokenUsage(10, 5);
        var second = new TokenUsage(10, 5);

        Assert.Equal(first, second);
    }
}
