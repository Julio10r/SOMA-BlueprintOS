using BlueprintOS.Infrastructure.Documentation.Generators.Engineering;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Engineering;

public class DatabaseGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_Should_Describe_Real_DbContext_And_Persistence()
    {
        var result = await new DatabaseGenerator().GenerateAsync();

        Assert.Contains("BlueprintOSDbContext", result);
        Assert.Contains("Entity Framework Core", result);
        Assert.Contains("SQL Server", result);
        Assert.DoesNotContain("ainda não possui nenhum", result);
        Assert.DoesNotContain("Nenhum schema de banco de dados definido", result);
    }
}
