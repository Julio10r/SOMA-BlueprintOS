using System.Net.Http.Headers;
using BlueprintOS.Application.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Knowledge.Linx;
using BlueprintOS.Application.Knowledge.Linx.Contracts;
using BlueprintOS.Application.Procurement.Negotiations;
using BlueprintOS.Application.Procurement.Negotiations.Contracts;
using BlueprintOS.Application.Procurement.Suppliers;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Core.AI.Contracts;
using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Memory;
using BlueprintOS.Core.AI.Memory.Contracts;
using BlueprintOS.Core.AI.Memory.Models;
using BlueprintOS.Core.AI.Negotiation;
using BlueprintOS.Core.AI.Negotiation.Contracts;
using BlueprintOS.Core.AI.Negotiation.Models;
using BlueprintOS.Core.AI.Negotiation.Rules;
using BlueprintOS.Core.Agents;
using BlueprintOS.Core.Documentation.Contracts;
using BlueprintOS.Core.Knowledge.Contracts;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Infrastructure.Documentation;
using BlueprintOS.Infrastructure.Publication.Docs;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;
using BlueprintOS.Infrastructure.Integrations.ERP.Soma;
using BlueprintOS.Infrastructure.Integrations;
using BlueprintOS.Infrastructure.Integrations.CnpjConsulta;
using BlueprintOS.Infrastructure.Integrations.CepConsulta;
using BlueprintOS.Infrastructure.Integrations.Ibge;
using BlueprintOS.Infrastructure.Integrations.OpenAI;
using BlueprintOS.Infrastructure.Knowledge;
using BlueprintOS.Infrastructure.Knowledge.Linx;
using BlueprintOS.Infrastructure.Memory;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using BlueprintOS.Infrastructure.Publication;
using BlueprintOS.Infrastructure.Publication.Assets;
using BlueprintOS.Infrastructure.Publication.Health;
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
        services.AddScoped<IFornecedorLinxVinculoRepository, FornecedorLinxVinculoRepository>();
        services.AddScoped<IFornecedorCnpjConsultaHistoricoRepository, FornecedorCnpjConsultaHistoricoRepository>();
        services.AddScoped<IFornecedorEnriquecimentoAnaliseRepository, FornecedorEnriquecimentoAnaliseRepository>();
        services.AddScoped<IErpFornecedorDiscoveryRepository, ErpFornecedorDiscoveryRepository>();
        services.AddScoped<IFornecedorDescobertoRepository, FornecedorDescobertoRepository>();
        services.AddScoped<IFornecedorSincronizacaoRepository, FornecedorSincronizacaoRepository>();
        services.AddScoped<ISincronizacaoFornecedorMonitorRepository, SincronizacaoFornecedorMonitorRepository>();
        services.AddScoped<IListarSincronizacoesFornecedoresUseCase, ListarSincronizacoesFornecedoresUseCase>();
        services.AddScoped<IObterSincronizacaoFornecedorUseCase, ObterSincronizacaoFornecedorUseCase>();
        services.AddScoped<IErpFornecedorAdapter, SomaDesenvolErpFornecedorAdapter>();
        services.AddScoped<IErpFornecedorAdapterResolver, ErpFornecedorAdapterResolver>();
        services.AddScoped<IGarantirFornecedorErpAdapter, SomaGarantirFornecedorErpAdapter>();
        services.AddScoped<IGarantirFornecedorErpAdapterResolver, GarantirFornecedorErpAdapterResolver>();
        services.AddScoped<IGarantirFornecedorNoErpUseCase, GarantirFornecedorNoErpUseCase>();
        services.AddScoped<IVerificarFornecedorNoErpUseCase, VerificarFornecedorNoErpUseCase>();
        services.AddScoped<ResolvedorBusinessUnit>();
        services.AddScoped<IFornecedorErpReader, SomaFornecedorReader>();
        services.AddScoped<ISincronizarFornecedorUseCase, SincronizarFornecedorUseCase>();
        services.AddScoped<ISincronizarFornecedoresErpUseCase, SincronizarFornecedoresErpUseCase>();
        services.AddScoped<IRecuperarSincronizacaoFornecedorAbandonadaUseCase, RecuperarSincronizacaoFornecedorAbandonadaUseCase>();
        services.AddScoped<IBackfillFornecedorLinxVinculosUseCase, BackfillFornecedorLinxVinculosUseCase>();
        services.AddScoped<IListarFornecedorLinxVinculosUseCase, ListarFornecedorLinxVinculosUseCase>();
        services.AddScoped<IDefinirFornecedorLinxVinculoPrincipalUseCase, DefinirFornecedorLinxVinculoPrincipalUseCase>();
        services.AddScoped<ICadastrarFornecedorUseCase, CadastrarFornecedorUseCase>();
        services.AddScoped<IAtualizarFornecedorUseCase, AtualizarFornecedorUseCase>();
        services.AddScoped<IInativarFornecedorUseCase, InativarFornecedorUseCase>();
        services.AddScoped<IAlterarStatusFornecedorUseCase, AlterarStatusFornecedorUseCase>();
        services.AddScoped<IObterFornecedorUseCase, ObterFornecedorUseCase>();

        // B3 — Bloco 5A: sincronização Linx -> +Compras de Item Fiscal e de Referências por Fornecedor
        // (docs/audits/B3-Bloco5A-*.md). Registrados aqui (não em IdentityServiceCollectionExtensions)
        // pelo mesmo motivo do bloco de Referências por Fornecedor logo abaixo: a resolução das
        // Referências depende de IFornecedorRepository (Suppliers).
        services.AddScoped<IItemFiscalErpReader, SomaItemFiscalReader>();
        services.AddScoped<ISincronizarItensFiscaisErpUseCase, SincronizarItensFiscaisErpUseCase>();
        services.AddScoped<IItemFiscalReferenciaFornecedorErpReader, SomaItemFiscalReferenciaFornecedorReader>();
        services.AddScoped<ISincronizarItemFiscalReferenciasFornecedorErpUseCase, SincronizarItemFiscalReferenciasFornecedorErpUseCase>();

        // B3 — Bloco 4: Referências de Item Fiscal por Fornecedor (Discovery homologado) — casos de uso que
        // dependem tanto de Identity (IItemFiscalRepository/IItemFiscalReferenciaFornecedorRepository,
        // registrados por AddIdentityAuthCore) quanto de Suppliers (IObterFornecedorUseCase, acima).
        // Registrados aqui, e não em IdentityServiceCollectionExtensions, porque Program.cs sempre chama
        // AddInfrastructure junto de AddIdentityAuthCore, mas um teste de composição mínima do host
        // (fail-closed do Bootstrap) chama apenas AddIdentityAuthCore.
        services.AddScoped<IListarReferenciasFornecedorUseCase, ListarReferenciasFornecedorUseCase>();
        services.AddScoped<IIncluirReferenciaFornecedorUseCase, IncluirReferenciaFornecedorUseCase>();
        services.AddScoped<IAtualizarReferenciaFornecedorUseCase, AtualizarReferenciaFornecedorUseCase>();
        services.AddScoped<IRemoverReferenciaFornecedorUseCase, RemoverReferenciaFornecedorUseCase>();
        services.AddScoped<IPesquisarFornecedorUseCase, PesquisarFornecedorUseCase>();
        services.AddScoped<IPesquisarFornecedorPaginadoUseCase, PesquisarFornecedorPaginadoUseCase>();
        services.AddScoped<IDescobrirFornecedoresUseCase, DescobrirFornecedoresUseCase>();
        services.AddScoped<IListarDescobertasUseCase, ListarDescobertasUseCase>();
        services.AddScoped<IConsultarCnpjFornecedorUseCase, ConsultarCnpjFornecedorUseCase>();
        // B2.7/ADR-0023 — expurgo de retencao (180 dias) do snapshot bruto de consultas de CNPJ.
        // Nenhum agendador automatico foi introduzido nesta sprint: o mecanismo fica pronto para
        // invocacao futura (rotina periodica/endpoint administrativo), sem Hangfire/Quartz.
        services.AddScoped<IExpurgarPayloadBrutoConsultaCnpjUseCase, ExpurgarPayloadBrutoConsultaCnpjUseCase>();
        services.AddScoped<IAnalisarEnriquecimentoFornecedorUseCase, AnalisarEnriquecimentoFornecedorUseCase>();
        services.AddScoped<IAprovarEnriquecimentoFornecedorUseCase, AprovarEnriquecimentoFornecedorUseCase>();
        services.AddScoped<IRejeitarEnriquecimentoFornecedorUseCase, RejeitarEnriquecimentoFornecedorUseCase>();

        // O1.7 — Filiais e Centros de Custo integrados ao ERP (leitura real + metadados locais +Compras) e
        // a validação de vínculo Usuário×Centro de Custo (O1.6-L2) são registradas em
        // IdentityServiceCollectionExtensions.AddRbacCore — reaproveitadas aqui via AddIdentityAuthCore,
        // chamado junto de AddInfrastructure na composição raiz (Program.cs).

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
        }).ConfigurePrimaryHttpMessageHandler(Ipv4PreferringHttpHandler.Create);

        // Gate de homologação de Fornecedores (2026-09-01), item 6 — consulta de CEP pelo backend,
        // mesmo padrão do CNPJ acima, mas via ViaCEP (fonte real usada pelo Linx, achado 2).
        services.Configure<CepConsultaOptions>(configuration.GetSection(CepConsultaOptions.SectionName));
        services.AddScoped<IConsultarCepFornecedorUseCase, ConsultarCepFornecedorUseCase>();
        services.AddHttpClient<ICepConsultaProvider, ViaCepProvider>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<CepConsultaOptions>>().Value;
            if (!string.Equals(options.Provider, "ViaCep", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"CepConsulta provider '{options.Provider}' is not supported.");
            }

            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds + 1));
        }).ConfigurePrimaryHttpMessageHandler(Ipv4PreferringHttpHandler.Create);

        // Gate de homologação de Fornecedores (2026-09-01): cidade como combo dependente da UF,
        // populado com municípios reais via IBGE — pelo backend, mesmo padrão de CEP/CNPJ acima.
        services.Configure<IbgeMunicipioOptions>(configuration.GetSection(IbgeMunicipioOptions.SectionName));
        services.AddScoped<IListarMunicipiosPorUfUseCase, ListarMunicipiosPorUfUseCase>();
        services.AddHttpClient<IMunicipioProvider, IbgeMunicipioProvider>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<IbgeMunicipioOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds + 1));
        }).ConfigurePrimaryHttpMessageHandler(Ipv4PreferringHttpHandler.Create);

        // Gate de homologação de Fornecedores (2026-09-01): catálogo pré-cadastrado de Categoria
        // (tabela própria do +Compras, não sincronizada do ERP — substitui o campo texto livre).
        services.AddScoped<ICategoriaFornecedorRepository, CategoriaFornecedorRepository>();
        services.AddScoped<IListarCategoriasFornecedorUseCase, ListarCategoriasFornecedorUseCase>();

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

        // AI Governance Onda 1 — nucleo deterministico de policy/approval/auditoria para futuras tools.
        // Nesta onda nao executa SQL nem substitui os runbooks existentes; fornece a base tecnica para que
        // actions propostas por agents sejam classificadas e vinculadas a aprovacoes especificas.
        services.AddSingleton<IAIGovernancePolicyEngine, AIGovernancePolicyEngine>();
        services.AddSingleton<IApprovalPolicy, ApprovalPolicy>();
        services.AddSingleton<IGovernanceAuditRecorder, InMemoryGovernanceAuditRecorder>();
        services.AddSingleton<GovernedActionDemoFlow>();
        services.AddScoped<SecurityLgpdAgent>();

        // O1.13.5 — Fundação dos Agents Especialistas Linx (base de conhecimento persistente e versionada).
        // TimeProvider.System já é registrado por AddIdentityAuthCore no host principal; registrado também
        // aqui para que composições que usam apenas AddInfrastructure (ex.: testes de integração isolados)
        // resolvam os casos de uso de conhecimento sem depender de outra chamada de registro.
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ILinxKnowledgeRepository, LinxKnowledgeRepository>();
        services.AddScoped<IRegistrarConhecimentoUseCase, RegistrarConhecimentoUseCase>();
        services.AddScoped<IPromoverConhecimentoUseCase, PromoverConhecimentoUseCase>();
        services.AddScoped<IBuscarConhecimentoUseCase, BuscarConhecimentoUseCase>();
        services.AddScoped<IObterHistoricoConhecimentoUseCase, ObterHistoricoConhecimentoUseCase>();
        services.AddScoped<ILinxSchemaDiscoveryReader, LinxSchemaDiscoveryReader>();

        // Os dois papéis de Agent (Work Order, seções 7/8) são resolvidos diretamente via DI — não pelo
        // AgentFactory reflection-based existente, que só reconhece o par (IAIRuntime, IKnowledgeService);
        // estender essa reflexão para reconhecer IBuscarConhecimentoUseCase exigiria que BlueprintOS.Core
        // referenciasse BlueprintOS.Application, invertendo a direção de dependência das camadas.
        services.AddScoped<LinxErpSpecialistAgent>();
        services.AddScoped<LinxDatabaseSpecialistAgent>();
        services.AddGovernedWriteStack(configuration);

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

        // Publication Engine — fonte docs/, destino dist/, sem lógica por audiência (ADR-0019)
        services.Configure<PublicationOptions>(configuration.GetSection(PublicationOptions.SectionName));
        services.AddSingleton<IQualityMetricsProvider, QualityMetricsProvider>();
        services.AddSingleton<IDocumentThemeProvider, DocumentThemeProvider>();
        services.AddSingleton<IDocumentationAssetsManager, DocumentationAssetsManager>();
        services.AddSingleton<IContentRenderer, MarkdownRenderer>();
        services.AddSingleton<IContentRenderer, HtmlRenderer>();
        services.AddSingleton<IContentRenderer, PdfRenderer>();
        services.AddSingleton<IDocsDiscoveryService>(provider =>
            new DocsDiscoveryService(provider.GetRequiredService<IOptions<PublicationOptions>>().Value.ExcludedTopLevelDirectories));
        services.AddSingleton<IPublicationService, DocsPublisher>();

        // Documentation Health Report (Sprint A7.2)
        services.Configure<DocumentationHealthOptions>(configuration.GetSection(DocumentationHealthOptions.SectionName));
        services.AddSingleton<IDocumentationHealthService, DocumentationHealthService>();

        return services;
    }
}
