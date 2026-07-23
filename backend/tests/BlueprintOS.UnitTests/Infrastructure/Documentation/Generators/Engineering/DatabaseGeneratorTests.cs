using BlueprintOS.Infrastructure.Documentation.Generators.Engineering;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Engineering;

public class DatabaseGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_Should_State_No_Schema_Defined_Honestly()
    {
        var result = await new DatabaseGenerator().GenerateAsync();

        Assert.Contains("Nenhum schema de banco de dados definido", result);
    }
}
