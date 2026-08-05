using BlueprintOS.Infrastructure.Documentation.Generators.Engineering;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Generators.Engineering;

public class ApiGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_Should_Document_Health_And_Negotiation_Recommendation_Endpoints()
    {
        var result = await new ApiGenerator().GenerateAsync();

        Assert.Contains("GET /health", result);
        Assert.Contains("POST /api/v1/negociacoes/recomendacoes", result);
        Assert.Contains("casos de uso Application", result);
    }

    [Fact]
    public async Task GenerateAsync_Should_Document_Real_Suppliers_Endpoints()
    {
        var result = await new ApiGenerator().GenerateAsync();

        Assert.Contains("/fornecedores", result);
        Assert.Contains("/api/fornecedores/descobrir", result);
        Assert.Contains("/api/fornecedores/sincronizar", result);
        Assert.Contains("consulta-cnpj", result);
    }
}
