using BlueprintOS.Domain.Identity;
using BlueprintOS.Domain.Identity.Raw;
using BlueprintOS.Domain.Integrations.Occurrences;
using BlueprintOS.Domain.Knowledge.Linx;
using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Domain.Procurement.Suppliers.Raw;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Persistence;

public sealed class BlueprintOSDbContext(DbContextOptions<BlueprintOSDbContext> options) : DbContext(options)
{
    public DbSet<UnidadeNegocio> UnidadesNegocio => Set<UnidadeNegocio>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Perfil> Perfis => Set<Perfil>();
    public DbSet<Permissao> Permissoes => Set<Permissao>();
    public DbSet<PerfilPermissao> PerfisPermissoes => Set<PerfilPermissao>();
    public DbSet<UsuarioPerfil> UsuariosPerfis => Set<UsuarioPerfil>();
    public DbSet<UsuarioCentroCusto> UsuariosCentrosCusto => Set<UsuarioCentroCusto>();
    public DbSet<FilialMetadado> FiliaisMetadados => Set<FilialMetadado>();
    public DbSet<CentroCustoMetadado> CentrosCustoMetadados => Set<CentroCustoMetadado>();
    public DbSet<ContaContabilMetadado> ContasContabeisMetadados => Set<ContaContabilMetadado>();
    public DbSet<UnidadeMedidaMetadado> UnidadesMedidaMetadados => Set<UnidadeMedidaMetadado>();
    public DbSet<RawLinxContaContabilRegistro> RawLinxContasContabeisSnapshot => Set<RawLinxContaContabilRegistro>();
    public DbSet<RawLinxUnidadeMedidaRegistro> RawLinxUnidadesMedidaSnapshot => Set<RawLinxUnidadeMedidaRegistro>();
    public DbSet<RawLinxCentroCustoRegistro> RawLinxCentrosCustoSnapshot => Set<RawLinxCentroCustoRegistro>();
    public DbSet<RawLinxFilialRegistro> RawLinxFiliaisSnapshot => Set<RawLinxFilialRegistro>();
    public DbSet<RawLinxItemFiscalRegistro> RawLinxItensFiscaisSnapshot => Set<RawLinxItemFiscalRegistro>();
    public DbSet<RawLinxItemFiscalReferenciaFornecedorRegistro> RawLinxItensFiscaisReferenciasFornecedorSnapshot => Set<RawLinxItemFiscalReferenciaFornecedorRegistro>();
    public DbSet<ItemFiscal> ItensFiscais => Set<ItemFiscal>();
    public DbSet<ItemFiscalReferenciaFornecedor> ItensFiscaisReferenciasFornecedor => Set<ItemFiscalReferenciaFornecedor>();
    public DbSet<UnidadeAlocacao> UnidadesAlocacao => Set<UnidadeAlocacao>();
    public DbSet<CentroCustoUnidadeAlocacao> CentrosCustoUnidadesAlocacao => Set<CentroCustoUnidadeAlocacao>();
    public DbSet<CodigoVerificacaoOtp> CodigosVerificacaoOtp => Set<CodigoVerificacaoOtp>();
    public DbSet<OtpRequestThrottle> OtpRequestThrottles => Set<OtpRequestThrottle>();
    public DbSet<SessaoAutenticacao> SessoesAutenticacao => Set<SessaoAutenticacao>();
    public DbSet<BootstrapEstado> BootstrapEstados => Set<BootstrapEstado>();
    public DbSet<BootstrapSessao> BootstrapSessoes => Set<BootstrapSessao>();

    // O1.11 — Administração de Unidades de Negócio e Configuração Técnica.
    public DbSet<IdentityProvider> IdentityProviders => Set<IdentityProvider>();
    public DbSet<ConfiguracaoErp> ConfiguracoesErp => Set<ConfiguracaoErp>();
    public DbSet<ConfiguracaoNotificacao> ConfiguracoesNotificacao => Set<ConfiguracaoNotificacao>();
    public DbSet<Parametro> Parametros => Set<Parametro>();
    public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
    public DbSet<FeatureFlagUnidadeNegocio> FeatureFlagsUnidadesNegocio => Set<FeatureFlagUnidadeNegocio>();

    // O1.12 — Fundação de Administração (Workflow, Alçadas, Controle Orçamentário).
    public DbSet<RegraWorkflow> RegrasWorkflow => Set<RegraWorkflow>();
    public DbSet<AlcadaAprovacao> AlcadasAprovacao => Set<AlcadaAprovacao>();
    public DbSet<RegraOrcamentaria> RegrasOrcamentarias => Set<RegraOrcamentaria>();

    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<FornecedorCnpjConsultaHistorico> FornecedoresCnpjConsultas => Set<FornecedorCnpjConsultaHistorico>();
    public DbSet<FornecedorEnriquecimentoAnalise> FornecedoresEnriquecimentoAnalises => Set<FornecedorEnriquecimentoAnalise>();
    public DbSet<FornecedorDominioErp> FornecedoresDominiosErp => Set<FornecedorDominioErp>();
    public DbSet<FornecedorDescoberto> FornecedoresDescobertos => Set<FornecedorDescoberto>();
    public DbSet<FornecedorSincronizacao> FornecedoresSincronizacoes => Set<FornecedorSincronizacao>();
    public DbSet<SincronizacaoFornecedor> SincronizacoesFornecedores => Set<SincronizacaoFornecedor>();
    public DbSet<ErroSincronizacaoFornecedor> ErrosSincronizacoesFornecedores => Set<ErroSincronizacaoFornecedor>();
    // B3 — Bloco 5A.9: vínculos Linx de um Fornecedor (1 CNPJ = 1 Fornecedor, N vínculos).
    public DbSet<FornecedorLinxVinculo> FornecedorLinxVinculos => Set<FornecedorLinxVinculo>();
    // Gate de homologação de Fornecedores (2026-09-01): catálogo pré-cadastrado de Categoria
    // (antes campo texto livre) — tabela própria do +Compras, não sincronizada do ERP.
    public DbSet<CategoriaFornecedor> CategoriasFornecedor => Set<CategoriaFornecedor>();

    // B3 — Bloco 5A.9, Gate A: staging RAW do LiveRead governado (dataset "linx.fornecedores.snapshot").
    // Truncate-and-reload por execução — identidade/completude vivem em RawLinxFornecedorSnapshotExecucao,
    // não linha a linha. Sem FK para o domínio: RAW nunca é lido diretamente por Fornecedor/FornecedorLinxVinculo.
    public DbSet<RawLinxFornecedorSnapshotExecucao> RawLinxFornecedoresSnapshotExecucoes => Set<RawLinxFornecedorSnapshotExecucao>();
    public DbSet<RawLinxFornecedorSnapshotRegistro> RawLinxFornecedoresSnapshot => Set<RawLinxFornecedorSnapshotRegistro>();
    public DbSet<RawLinxFornecedorDominioErpRegistro> RawLinxFornecedorDominiosSnapshot => Set<RawLinxFornecedorDominioErpRegistro>();
    public DbSet<LinxDatasetLoadState> LinxDatasetLoadStates => Set<LinxDatasetLoadState>();

    // B3 — Bloco 5A.9, complemento "PERSISTÊNCIA DE OCORRÊNCIAS/ERROS DE INTEGRAÇÃO": genérico, não
    // específico de Fornecedor — suporta qualquer dataset/pipeline futuro.
    public DbSet<IntegrationOccurrence> IntegrationOccurrences => Set<IntegrationOccurrence>();

    // O1.13.5 — Fundação dos Agents Especialistas Linx (base de conhecimento persistente e versionada).
    public DbSet<LinxKnowledgeEntry> LinxConhecimentoEntradas => Set<LinxKnowledgeEntry>();

    // NOTE: The AIGovernance* DbSets (approval requests/grants, audit events, write verification profiles,
    // knowledge gaps, recovery index, write execution audit, rollback audit) were removed. Governance
    // bookkeeping now lives entirely in file-based stores under runtime/governance/ (see
    // src/BlueprintOS.Infrastructure/Persistence/Governance/File*.cs) — MAISCOMPRAS, the +Compras business
    // database this context represents, is business-application infrastructure, not agent infrastructure,
    // and the Governed Write Stack must have zero dependency on it.

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BlueprintOSDbContext).Assembly);
    }
}
