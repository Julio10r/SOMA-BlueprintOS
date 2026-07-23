using System.Net.Http.Headers;
using BlueprintOS.Core.AI.Contracts;
using BlueprintOS.Core.AI.Memory;
using BlueprintOS.Core.AI.Memory.Contracts;
using BlueprintOS.Core.AI.Memory.Models;
using BlueprintOS.Core.AI.Negotiation;
using BlueprintOS.Core.AI.Negotiation.Contracts;
using BlueprintOS.Core.AI.Negotiation.Models;
using BlueprintOS.Core.AI.Negotiation.Rules;
using BlueprintOS.Core.Agents;
using BlueprintOS.Core.Documentation.Contracts;
using BlueprintOS.Core.Documentation.Contracts.Client;
using BlueprintOS.Core.Documentation.Contracts.Engineering;
using BlueprintOS.Core.Documentation.Contracts.Executive;
using BlueprintOS.Core.Knowledge.Contracts;
using BlueprintOS.Infrastructure.Documentation;
using BlueprintOS.Infrastructure.Documentation.Generators.Client;
using BlueprintOS.Infrastructure.Documentation.Generators.Engineering;
using BlueprintOS.Infrastructure.Documentation.Generators.Executive;
using BlueprintOS.Infrastructure.Documentation.Publishing;
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

        services.Configure<NegotiationStrategyOptions>(configuration.GetSection(NegotiationStrategyOptions.SectionName));
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<NegotiationStrategyOptions>>().Value);
        services.AddSingleton<INegotiationStrategyRule, EmergencyUrgencyRule>();
        services.AddSingleton<INegotiationStrategyRule, PartnershipHighScoreRecurringRule>();
        services.AddSingleton<INegotiationStrategyRule, CompetitiveExpensiveSupplierRule>();
        services.AddSingleton<INegotiationStrategyRule, AggressivePriceAboveHistoryRule>();
        services.AddSingleton<INegotiationStrategyRule, BalancedNewSupplierRule>();
        services.AddSingleton<INegotiationStrategyRule, ConservativeFallbackRule>();
        services.AddSingleton<INegotiationStrategy>(provider => new NegotiationStrategy(
            provider.GetRequiredService<IEnumerable<INegotiationStrategyRule>>(),
            provider.GetRequiredService<IOptions<NegotiationStrategyOptions>>().Value));

        services.Configure<DocumentationOptions>(configuration.GetSection(DocumentationOptions.SectionName));
        services.AddSingleton<IDocumentationRepository, InMemoryDocumentationRepository>();
        services.AddSingleton<IDocumentVersioningService, DocumentVersioningService>();
        services.AddSingleton<IChangeLogService, ChangeLogService>();
        services.AddSingleton<IAdrService, MarkdownAdrService>();
        services.AddSingleton<ITechnicalDocumentationGenerator, TechnicalDocumentationGenerator>();
        services.AddSingleton<IFunctionalDocumentationGenerator, FunctionalDocumentationGenerator>();
        services.AddSingleton<IAiDocumentationGenerator, AiDocumentationGenerator>();
        services.AddSingleton<IDeveloperDocumentationGenerator, DeveloperDocumentationGenerator>();
        services.AddSingleton<IMermaidDiagramGenerator, MermaidDiagramGenerator>();
        services.AddSingleton<IDocumentationSyncService, DocumentationSyncService>();
        services.AddSingleton<IStaleDocumentationDetector, StaleDocumentationDetector>();
        services.AddSingleton<IGitLogReader, GitCliDocumentationService>();
        services.AddSingleton<IDocumentationMemoryNotifier, NoOpDocumentationMemoryNotifier>();

        // Portal de Documentação Viva (Sprint A8)
        services.AddSingleton<IDocumentPublisher, MarkdownPublisher>();
        services.AddSingleton<DocumentationPublisher>();

        services.AddSingleton<IDashboardGenerator, DashboardGenerator>();
        services.AddSingleton<IKpiGenerator, KpiGenerator>();
        services.AddSingleton<IRoadmapGenerator, RoadmapGenerator>();
        services.AddSingleton<ISprintStatusGenerator, SprintStatusGenerator>();
        services.AddSingleton<IReleaseGenerator, ReleaseGenerator>();

        services.AddSingleton<IProductOverviewGenerator, ProductOverviewGenerator>();
        services.AddSingleton<IUserGuideGenerator, UserGuideGenerator>();
        services.AddSingleton<IFunctionalGuideGenerator, FunctionalGuideGenerator>();
        services.AddSingleton<IApiDocumentationGenerator, ApiDocumentationGenerator>();
        services.AddSingleton<IChangelogGenerator, ChangelogGenerator>();
        services.AddSingleton<IFaqGenerator, FaqGenerator>();

        services.AddSingleton<IArchitectureGenerator, ArchitectureGenerator>();
        services.AddSingleton<IDatabaseGenerator, DatabaseGenerator>();
        services.AddSingleton<IAgentsGenerator, AgentsGenerator>();
        services.AddSingleton<IApiGenerator, ApiGenerator>();
        services.AddSingleton<IDeployGenerator, DeployGenerator>();
        services.AddSingleton<IRunbookGenerator, RunbookGenerator>();
        services.AddSingleton<IMermaidGenerator, MermaidGenerator>();
        services.AddSingleton<IDecisionsGenerator, DecisionsGenerator>();

        services.AddSingleton<IDocumentationPublishService, DocumentationPublishService>();

        return services;
    }
}
