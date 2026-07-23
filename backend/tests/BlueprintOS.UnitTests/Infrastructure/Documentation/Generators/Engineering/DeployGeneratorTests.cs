using BlueprintOS.Infrastructure.Documentation.Generators.Engineering;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Engineering;

public class DeployGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_Should_Reference_Real_Dockerfile_Path()
    {
        var result = await new DeployGenerator().GenerateAsync();

        Assert.Contains("BlueprintOS.Api/Dockerfile", result);
        Assert.Contains("docker-compose", result);
    }
}
