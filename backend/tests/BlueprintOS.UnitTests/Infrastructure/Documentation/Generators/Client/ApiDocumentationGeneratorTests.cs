using BlueprintOS.Infrastructure.Documentation.Generators.Client;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Client;

public class ApiDocumentationGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_Should_Document_Real_Health_Endpoint()
    {
        var result = await new ApiDocumentationGenerator().GenerateAsync();

        Assert.Contains("/health", result);
    }
}
