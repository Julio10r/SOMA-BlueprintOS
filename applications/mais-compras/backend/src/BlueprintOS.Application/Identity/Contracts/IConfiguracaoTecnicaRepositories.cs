using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Repositório de Identity Providers por Unidade de Negócio (O1.11). Operação administrativa
/// corporativa: a Unidade de Negócio referenciada vem sempre do path (recurso operado), nunca da sessão de
/// quem administra — a autorização vem da permissão RBAC (<c>Sistema.Gerenciar</c>), não do escopo do
/// usuário.</summary>
public interface IIdentityProviderRepository
{
    Task<IReadOnlyList<IdentityProvider>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct);

    Task<IdentityProvider?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct);

    Task AdicionarAsync(IdentityProvider identityProvider, CancellationToken ct);

    Task SalvarAlteracoesAsync(CancellationToken ct);
}

/// <summary>Repositório de Configuração de ERP por Unidade de Negócio (O1.11) — relação 1:1.</summary>
public interface IConfiguracaoErpRepository
{
    Task<ConfiguracaoErp?> ObterPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct);

    Task AdicionarAsync(ConfiguracaoErp configuracaoErp, CancellationToken ct);

    Task SalvarAlteracoesAsync(CancellationToken ct);
}

/// <summary>Repositório de Configuração de Notificações por Unidade de Negócio (O1.11, item #24) —
/// relação 1:1, mesmo padrão de <see cref="IConfiguracaoErpRepository"/>.</summary>
public interface IConfiguracaoNotificacaoRepository
{
    Task<ConfiguracaoNotificacao?> ObterPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct);

    Task AdicionarAsync(ConfiguracaoNotificacao configuracaoNotificacao, CancellationToken ct);

    Task SalvarAlteracoesAsync(CancellationToken ct);
}

/// <summary>Repositório de Parâmetros gerais (O1.11) — globais (<c>UnidadeNegocioId == null</c>) ou por
/// Unidade de Negócio.</summary>
public interface IParametroRepository
{
    Task<IReadOnlyList<Parametro>> ListarAsync(Guid? unidadeNegocioId, CancellationToken ct);

    Task<Parametro?> ObterPorIdAsync(Guid id, CancellationToken ct);

    Task<bool> ExisteComChaveAsync(string chave, Guid? unidadeNegocioId, Guid? excluirId, CancellationToken ct);

    Task AdicionarAsync(Parametro parametro, CancellationToken ct);

    Task RemoverAsync(Parametro parametro, CancellationToken ct);

    Task SalvarAlteracoesAsync(CancellationToken ct);
}

/// <summary>Repositório do catálogo de Feature Flags e do vínculo N:N por Unidade de Negócio (O1.11).</summary>
public interface IFeatureFlagRepository
{
    Task<IReadOnlyList<FeatureFlag>> ListarAsync(CancellationToken ct);

    Task<FeatureFlag?> ObterPorIdAsync(Guid id, CancellationToken ct);

    Task<bool> ExisteComNomeAsync(string nome, CancellationToken ct);

    Task<IReadOnlyList<FeatureFlagUnidadeNegocio>> ListarStatusPorFlagAsync(Guid featureFlagId, CancellationToken ct);

    Task<FeatureFlagUnidadeNegocio?> ObterStatusAsync(Guid featureFlagId, Guid unidadeNegocioId, CancellationToken ct);

    Task AdicionarAsync(FeatureFlag featureFlag, CancellationToken ct);

    Task AdicionarStatusAsync(FeatureFlagUnidadeNegocio status, CancellationToken ct);

    Task SalvarAlteracoesAsync(CancellationToken ct);
}
