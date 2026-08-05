using BlueprintOS.Infrastructure.Documentation.Generators.Engineering;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Engineering;

public class RunbookGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_Should_Distinguish_Formal_Catalog_From_Existing_Operational_Knowledge()
    {
        var result = await new RunbookGenerator().GenerateAsync();

        Assert.Contains("catálogo formal", result);
        Assert.Contains("completed_sprints.md", result);
        Assert.Contains("known_issues.md", result);
    }
}
