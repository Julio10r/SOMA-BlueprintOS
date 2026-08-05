using BlueprintOS.Infrastructure.Documentation.Generators.Executive;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Executive;

public class KpiGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_Should_Distinguish_Formal_Kpis_From_Existing_Technical_Evidence()
    {
        var result = await new KpiGenerator().GenerateAsync();

        Assert.Contains("Nenhum KPI de negócio formalizado", result);
        Assert.Contains("PROJECT_STATE.md", result);
        Assert.Contains("BACKLOG.md", result);
    }
}
