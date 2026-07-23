using System.Net.Http.Headers;
using BlueprintOS.Core.AI.Contracts;
using BlueprintOS.Core.AI.Memory;
using BlueprintOS.Core.AI.Memory.Contracts;
using BlueprintOS.Core.AI.Memory.Models;
using BlueprintOS.Core.Agents;
using BlueprintOS.Core.Knowledge.Contracts;
using BlueprintOS.Infrastructure.Integrations.OpenAI;
using BlueprintOS.Infrastructure.Knowledge;
using BlueprintOS.Infrastructure.Memory;
using BlueprintOS.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.DependencyInjection;

/// <summary>
/// Registra os serviços de infraestrutura da aplicação, incluindo o runtime de IA e seus provedores.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenAIOptions>(configuration.GetSection(OpenAIOptions.SectionName));

        services.AddHttpClient<IAIProvider, OpenAIProvider>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<OpenAIOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        });

        services.AddSingleton<IAIRuntime, AIRuntime>();

        services.Configure<KnowledgeOptions>(configuration.GetSection(KnowledgeOptions.SectionName));
        services.AddSingleton<IKnowledgeProvider, MarkdownKnowledgeProvider>();
        services.AddSingleton<IKnowledgeService, KnowledgeService>();

        services.AddSingleton<AgentFactory>();

        services.Configure<NegotiationScoreOptions>(configuration.GetSection(NegotiationScoreOptions.SectionName));
        services.AddSingleton<INegotiationMemoryStore, InMemoryNegotiationMemoryStore>();
        services.AddSingleton<INegotiationMemory>(provider => new NegotiationMemory(
            provider.GetRequiredService<INegotiationMemoryStore>(),
            provider.GetRequiredService<IOptions<NegotiationScoreOptions>>().Value));

        return services;
    }
}
