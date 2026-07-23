using BlueprintOS.Infrastructure.Documentation;
using BlueprintOS.Infrastructure.Documentation.Generators.Engineering;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Engineering;

public class MermaidGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_Should_Produce_Mermaid_FlowChart_With_Real_Project_Names()
    {
        var generator = new MermaidGenerator(new MermaidDiagramGenerator());

        var result = await generator.GenerateAsync();

        Assert.Contains("```mermaid", result);
        Assert.Contains("graph TD", result);
        Assert.Contains("BlueprintOS.Api", result);
        Assert.Contains("BlueprintOS.Infrastructure", result);
    }
}
