using BlueprintOS.Infrastructure.Documentation;
using BlueprintOS.Infrastructure.Documentation.Generators.Engineering;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Engineering;

public class ArchitectureGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_Should_Include_Documentation_And_Agents_Modules()
    {
        var generator = new ArchitectureGenerator(new TechnicalDocumentationGenerator());

        var result = await generator.GenerateAsync();

        Assert.Contains("Documentation", result);
        Assert.Contains("Agents", result);
        Assert.Contains("AI.Negotiation", result);
    }
}
