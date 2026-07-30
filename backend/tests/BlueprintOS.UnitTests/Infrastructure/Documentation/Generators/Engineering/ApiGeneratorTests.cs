using BlueprintOS.Infrastructure.Documentation.Generators.Engineering;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Engineering;

public class ApiGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_Should_Document_Health_And_Negotiation_Endpoints()
    {
        var result = await new ApiGenerator().GenerateAsync();

        Assert.Contains("GET /health", result);
        Assert.Contains("POST /api/v1/negotiations/recommendations", result);
        Assert.Contains("humanDecisionRequired: true", result);
    }
}
