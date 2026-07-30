using BlueprintOS.Infrastructure.Documentation.Generators.Client;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Client;

public class ApiDocumentationGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_Should_Document_Health_And_Negotiation_Recommendation_Endpoints()
    {
        var result = await new ApiDocumentationGenerator().GenerateAsync();

        Assert.Contains("/health", result);
        Assert.Contains("/api/v1/negociacoes/recomendacoes", result);
    }
}
