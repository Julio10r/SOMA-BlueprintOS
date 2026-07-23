using BlueprintOS.Infrastructure.Documentation.Generators.Executive;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Executive;

public class KpiGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_Should_State_No_Kpis_Registered_Honestly()
    {
        var result = await new KpiGenerator().GenerateAsync();

        Assert.Contains("Nenhum KPI de negócio registrado", result);
    }
}
