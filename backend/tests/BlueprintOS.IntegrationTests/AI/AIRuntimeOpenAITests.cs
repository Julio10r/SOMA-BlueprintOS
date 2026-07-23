using BlueprintOS.Core.AI.Contracts;
using BlueprintOS.Core.AI.Models;
using BlueprintOS.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BlueprintOS.IntegrationTests.AI;

public sealed class AIRuntimeOpenAITests
{
    [Fact]
    public async Task ExecuteAsync_ComPromptSimples_DeveRetornarRespostaDoModelo()
    {
        var apiKey = Environment.GetEnvironmentVariable("AI__OpenAI__ApiKey")
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return; // Sem credencial disponível no ambiente: teste de integração é ignorado.
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:OpenAI:ApiKey"] = apiKey
            })
            .Build();

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);

        await using var provider = services.BuildServiceProvider();
        var runtime = provider.GetRequiredService<IAIRuntime>();

        var response = await runtime.ExecuteAsync(new AIRequest("Diga Olá"));

        Assert.False(string.IsNullOrWhiteSpace(response.Text));
    }
}
