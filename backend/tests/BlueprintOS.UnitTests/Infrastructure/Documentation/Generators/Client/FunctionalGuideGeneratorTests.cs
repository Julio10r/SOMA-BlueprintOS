using BlueprintOS.Infrastructure.Documentation.Generators.Client;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Client;

public class FunctionalGuideGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_Should_Describe_Real_Existing_Modules()
    {
        var result = await new FunctionalGuideGenerator().GenerateAsync();

        Assert.Contains("Documentação viva", result);
        Assert.Contains("Agentes de IA", result);
        Assert.Contains("Negociação", result);
    }
}
