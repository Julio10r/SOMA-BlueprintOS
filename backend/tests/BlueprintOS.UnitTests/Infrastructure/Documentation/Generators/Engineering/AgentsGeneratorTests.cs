using BlueprintOS.Infrastructure.Documentation.Generators.Engineering;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Engineering;

public class AgentsGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_Should_Describe_Real_Agent_Classes()
    {
        var result = await new AgentsGenerator().GenerateAsync();

        Assert.Contains("BaseAgent", result);
        Assert.Contains("EchoAgent", result);
        Assert.Contains("KnowledgeAgent", result);
        Assert.Contains("AgentFactory", result);
    }
}
