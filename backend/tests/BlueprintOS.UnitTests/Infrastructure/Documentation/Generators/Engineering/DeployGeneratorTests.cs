using BlueprintOS.Infrastructure.Documentation.Generators.Engineering;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Engineering;

public class DeployGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_Should_Reference_Dotnet_Run_And_External_SqlServer()
    {
        var result = await new DeployGenerator().GenerateAsync();

        Assert.Contains("dotnet run", result);
        Assert.Contains("npm run dev", result);
        Assert.Contains("SQL Server externo", result);
        Assert.Contains("ADR-0018", result);
        Assert.DoesNotContain("ADR-0019", result);
        Assert.DoesNotContain("Dockerfile", result);
        Assert.DoesNotContain("docker-compose", result);
    }
}
