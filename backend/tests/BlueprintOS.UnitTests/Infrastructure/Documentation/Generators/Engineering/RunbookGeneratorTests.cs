using BlueprintOS.Infrastructure.Documentation.Generators.Engineering;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Engineering;

public class RunbookGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_Should_State_No_Runbook_Registered_Honestly()
    {
        var result = await new RunbookGenerator().GenerateAsync();

        Assert.Contains("Nenhum procedimento operacional", result);
    }
}
