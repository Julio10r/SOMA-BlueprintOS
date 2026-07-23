using BlueprintOS.Infrastructure.Documentation.Generators.Engineering;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Engineering;

public class ApiGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_Should_Document_Real_Health_Endpoint_Only()
    {
        var result = await new ApiGenerator().GenerateAsync();

        Assert.Contains("GET /health", result);
        Assert.Contains("Nenhum controller de negócio", result);
    }
}
