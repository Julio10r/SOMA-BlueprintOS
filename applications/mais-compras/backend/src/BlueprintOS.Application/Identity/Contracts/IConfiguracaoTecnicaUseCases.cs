using BlueprintOS.Application.Identity.Models;

namespace BlueprintOS.Application.Identity.Contracts;

// ---- O1.11 — Seleção de Unidade de Negócio (leitura, qualquer usuário autenticado) ----

/// <summary>Sistema hoje é single-BU-por-usuário — devolve sempre a única Unidade de Negócio do usuário
/// atual (a claim de sessão `unidade_negocio_id`), como base para o frontend decidir se mostra ou não a
/// tela de seleção. Nenhuma mudança de sessão/claims.</summary>
public interface IListarMinhasUnidadesNegocioUseCase
{
    Task<IReadOnlyList<UnidadeNegocioDto>> ExecuteAsync(Guid unidadeNegocioDaSessao, CancellationToken ct);
}

// ---- O1.11 — Cadastro de Unidades de Negócio (CRUD real) ----

public interface IListarUnidadesNegocioUseCase
{
    Task<IReadOnlyList<UnidadeNegocioDto>> ExecuteAsync(CancellationToken ct);
}

public interface ICriarUnidadeNegocioUseCase
{
    Task<RbacResultado<UnidadeNegocioDto>> ExecuteAsync(UnidadeNegocioCriarInput input, CancellationToken ct);
}

public interface IRenomearUnidadeNegocioUseCase
{
    Task<RbacResultado<UnidadeNegocioDto>> ExecuteAsync(Guid id, UnidadeNegocioRenomearInput input, CancellationToken ct);
}

public interface IAlterarStatusUnidadeNegocioUseCase
{
    Task<RbacResultado<UnidadeNegocioDto>> ExecuteAsync(Guid id, bool ativa, CancellationToken ct);
}

// ---- O1.11 — Identity Providers por Unidade de Negócio ----

public interface IListarIdentityProvidersUseCase
{
    Task<RbacResultado<IReadOnlyList<IdentityProviderDto>>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct);
}

public interface ICriarIdentityProviderUseCase
{
    Task<RbacResultado<IdentityProviderDto>> ExecuteAsync(Guid unidadeNegocioId, IdentityProviderInput input, CancellationToken ct);
}

public interface IAtualizarIdentityProviderUseCase
{
    Task<RbacResultado<IdentityProviderDto>> ExecuteAsync(Guid unidadeNegocioId, Guid id, IdentityProviderInput input, CancellationToken ct);
}

public interface IAlterarStatusIdentityProviderUseCase
{
    Task<RbacResultado<IdentityProviderDto>> ExecuteAsync(Guid unidadeNegocioId, Guid id, bool ativo, CancellationToken ct);
}

// ---- O1.11 — Configuração de ERP por Unidade de Negócio ----

public interface IObterConfiguracaoErpUseCase
{
    Task<RbacResultado<ConfiguracaoErpDto?>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct);
}

public interface ISalvarConfiguracaoErpUseCase
{
    Task<RbacResultado<ConfiguracaoErpDto>> ExecuteAsync(Guid unidadeNegocioId, ConfiguracaoErpInput input, CancellationToken ct);
}

public interface IAlterarStatusConfiguracaoErpUseCase
{
    Task<RbacResultado<ConfiguracaoErpDto>> ExecuteAsync(Guid unidadeNegocioId, bool ativo, CancellationToken ct);
}

// ---- O1.11 — Configuração de Notificações por Unidade de Negócio (item #24) ----

public interface IObterConfiguracaoNotificacaoUseCase
{
    Task<RbacResultado<ConfiguracaoNotificacaoDto?>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct);
}

public interface ISalvarConfiguracaoNotificacaoUseCase
{
    Task<RbacResultado<ConfiguracaoNotificacaoDto>> ExecuteAsync(Guid unidadeNegocioId, ConfiguracaoNotificacaoInput input, CancellationToken ct);
}

// ---- O1.11 — Parâmetros gerais ----

public interface IListarParametrosUseCase
{
    Task<IReadOnlyList<ParametroDto>> ExecuteAsync(Guid? unidadeNegocioId, CancellationToken ct);
}

public interface ICriarParametroUseCase
{
    Task<RbacResultado<ParametroDto>> ExecuteAsync(ParametroCriarInput input, CancellationToken ct);
}

public interface IAtualizarParametroUseCase
{
    Task<RbacResultado<ParametroDto>> ExecuteAsync(Guid id, ParametroAtualizarInput input, CancellationToken ct);
}

public interface IExcluirParametroUseCase
{
    Task<RbacResultado<bool>> ExecuteAsync(Guid id, CancellationToken ct);
}

// ---- O1.11 — Feature Flags ----

public interface IListarFeatureFlagsUseCase
{
    Task<IReadOnlyList<FeatureFlagDto>> ExecuteAsync(CancellationToken ct);
}

public interface ICriarFeatureFlagUseCase
{
    Task<RbacResultado<FeatureFlagDto>> ExecuteAsync(FeatureFlagCriarInput input, CancellationToken ct);
}

public interface IAlterarStatusFeatureFlagUnidadeUseCase
{
    Task<RbacResultado<FeatureFlagDto>> ExecuteAsync(Guid featureFlagId, Guid unidadeNegocioId, bool ativa, CancellationToken ct);
}
