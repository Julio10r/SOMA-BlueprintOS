using System.Net.Http.Headers;
using BlueprintOS.Application.Procurement.Negotiations;
using BlueprintOS.Application.Procurement.Negotiations.Contracts;
using BlueprintOS.Application.Procurement.Suppliers;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
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
using BlueprintOS.Core.Documentation.Contracts.Assets;
using BlueprintOS.Core.Documentation.Contracts.Client;
using BlueprintOS.Core.Documentation.Contracts.Engineering;
using BlueprintOS.Core.Documentation.Contracts.Executive;
using BlueprintOS.Core.Knowledge.Contracts;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Infrastructure.Documentation;
using BlueprintOS.Infrastructure.Documentation.Assets;
using BlueprintOS.Infrastructure.Documentation.Generators.Client;
using BlueprintOS.Infrastructure.Documentation.Generators.Engineering;
using BlueprintOS.Infrastructure.Documentation.Generators.Executive;
using BlueprintOS.Infrastructure.Documentation.Publishing;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;
using BlueprintOS.Infrastructure.Integrations.ERP.Soma;
using BlueprintOS.Infrastructure.Integrations.CnpjConsulta;
using BlueprintOS.Infrastructure.Integrations.OpenAI;
using BlueprintOS.Infrastructure.Knowledge;
using BlueprintOS.Infrastructure.Memory;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using BlueprintOS.Infrastructure.Publication;
using BlueprintOS.Infrastructure.Publication.Assets;
using BlueprintOS.Infrastructure.Publication.Health;
using BlueprintOS.Infrastructure.Publication.Publishers;
using BlueprintOS.Infrastructure.Publication.Rendering;
using BlueprintOS.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.DependencyInjection;

/// <summary>
/// Registra os serviços de infraestrutura da aplicação, incluindo o runtime de IA e seus provedores.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);
        // B1 uses only the application-owned +Compras database. ERP is an external integration boundary.
        var connectionString = configuration.GetConnectionString("MaisComprasConnection");
        if (string.IsNullOrWhiteSpace(connectionString) || connectionString.StartsWith("__SET_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("ConnectionStrings:MaisComprasConnection must be configured through User Secrets or the ConnectionStrings__MaisComprasConnection environment variable.");
        }
        services.AddDbContext<BlueprintOSDbContext>(options => options.UseSqlServer(connectionString));
        services.AddSingleton<B1ConnectivityValidator>();
        services.AddScoped<IFornecedorRepository, FornecedorRepository>();
        services.AddScoped<IFornecedorCnpjConsultaHistoricoRepository, FornecedorCnpjConsultaHistoricoRepository>();
        services.AddScoped<IFornecedorEnriquecimentoAnaliseRepository, FornecedorEnriquecimentoAnaliseRepository>();
        services.AddScoped<IErpFornecedorDiscoveryRepository, ErpFornecedorDiscoveryRepository>();
        services.AddScoped<IFornecedorDescobertoRepository, FornecedorDescobertoRepository>();
        services.AddScoped<IFornecedorSincronizacaoRepository, FornecedorSincronizacaoRepository>();
        services.AddScoped<IErpFornecedorAdapter, SomaDesenvolErpFornecedorAdapter>();
        services.AddScoped<IErpFornecedorAdapterResolver, ErpFornecedorAdapterResolver>();
        services.AddScoped<IFornecedorErpReader, SomaFornecedorReader>();
        services.AddScoped<ISincronizarFornecedorUseCase, SincronizarFornecedorUseCase>();
        services.AddScoped<ISincronizarFornecedoresErpUseCase, SincronizarFornecedoresErpUseCase>();
        services.AddScoped<ICadastrarFornecedorUseCase, CadastrarFornecedorUseCase>();
        services.AddScoped<IAtualizarFornecedorUseCase, AtualizarFornecedorUseCase>();
        services.AddScoped<IExcluirFornecedorUseCase, ExcluirFornecedorUseCase>();
        services.AddScoped<IObterFornecedorUseCase, ObterFornecedorUseCase>();
        services.AddScoped<IPesquisarFornecedorUseCase, PesquisarFornecedorUseCase>();
        services.AddScoped<IDescobrirFornecedoresUseCase, DescobrirFornecedoresUseCase>();
        services.AddScoped<IListarDescobertasUseCase, ListarDescobertasUseCase>();
        services.AddScoped<IConsultarCnpjFornecedorUseCase, ConsultarCnpjFornecedorUseCase>();
        services.AddScoped<IAnalisarEnriquecimentoFornecedorUseCase, AnalisarEnriquecimentoFornecedorUseCase>();
        services.AddScoped<IAprovarEnriquecimentoFornecedorUseCase, AprovarEnriquecimentoFornecedorUseCase>();
        services.AddScoped<IRejeitarEnriquecimentoFornecedorUseCase, RejeitarEnriquecimentoFornecedorUseCase>();

        services.Configure<CnpjConsultaOptions>(configuration.GetSection(CnpjConsultaOptions.SectionName));
        services.AddHttpClient<ICnpjConsultaProvider, BrasilApiCnpjProvider>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<CnpjConsultaOptions>>().Value;
            if (!string.Equals(options.Provider, "BrasilApi", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"CnpjConsulta provider '{options.Provider}' is not supported.");
            }

            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds + 1));
        });

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
        services.AddScoped<INegotiationRecommendationUseCase, NegotiationRecommendationUseCase>();

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
        services.AddSingleton<IExecutiveContentLoader, ExecutiveContentLoader>();

        services.AddSingleton<IProductOverviewGenerator, ProductOverviewGenerator>();
        services.AddSingleton<IUserGuideGenerator, UserGuideGenerator>();
        services.AddSingleton<IFunctionalGuideGenerator, FunctionalGuideGenerator>();
        services.AddSingleton<IApiDocumentationGenerator, ApiDocumentationGenerator>();
        services.AddSingleton<IChangelogGenerator, ChangelogGenerator>();
        services.AddSingleton<IFaqGenerator, FaqGenerator>();
        services.AddSingleton<IClientContentLoader, ClientContentLoader>();

        services.AddSingleton<IArchitectureGenerator, ArchitectureGenerator>();
        services.AddSingleton<IDatabaseGenerator, DatabaseGenerator>();
        services.AddSingleton<IAgentsGenerator, AgentsGenerator>();
        services.AddSingleton<IApiGenerator, ApiGenerator>();
        services.AddSingleton<IDeployGenerator, DeployGenerator>();
        services.AddSingleton<IRunbookGenerator, RunbookGenerator>();
        services.AddSingleton<IMermaidGenerator, MermaidGenerator>();
        services.AddSingleton<IDecisionsGenerator, DecisionsGenerator>();
        services.AddSingleton<IEngineeringContentLoader, EngineeringContentLoader>();

        // Asset Generator (Sprint A7.3)
        services.AddSingleton<IDocumentationAssetGenerator, DocumentationAssetGenerator>();
        services.AddSingleton<IAssetPublisher, AssetFilePublisher>();

        services.AddSingleton<IDocumentationPublishService, DocumentationPublishService>();

        // Publication Engine (Sprint A9)
        services.Configure<PublicationOptions>(configuration.GetSection(PublicationOptions.SectionName));
        services.AddSingleton<IQualityMetricsProvider, QualityMetricsProvider>();
        services.AddSingleton<IDocumentThemeProvider, DocumentThemeProvider>();
        services.AddSingleton<IDocumentationAssetsManager, DocumentationAssetsManager>();
        services.AddSingleton<IContentRenderer, MarkdownRenderer>();
        services.AddSingleton<IContentRenderer, HtmlRenderer>();
        services.AddSingleton<IContentRenderer, PdfRenderer>();
        services.AddSingleton<IReportPublisher, ExecutivePublisher>();
        services.AddSingleton<IReportPublisher, ClientPublisher>();
        services.AddSingleton<IReportPublisher, EngineeringPublisher>();
        services.AddSingleton<IPublicationService, PublicationService>();

        // Documentation Health Report (Sprint A7.2)
        services.Configure<DocumentationHealthOptions>(configuration.GetSection(DocumentationHealthOptions.SectionName));
        services.AddSingleton<IDocumentationHealthService, DocumentationHealthService>();

        return services;
    }
}
