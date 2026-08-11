namespace BlueprintOS.Application.Identity.Models;

// ---- O1.11 — Seleção/Cadastro de Unidades de Negócio ----

/// <summary>Projeção mínima de Unidade de Negócio usada tanto por `GET /me/unidades-negocio` quanto pelo
/// CRUD administrativo.</summary>
public sealed record UnidadeNegocioDto(Guid Id, string Nome, string Slug, bool Ativa);

/// <summary>Entrada de criação. <c>Slug</c> é imutável após a criação — não existe input de edição de
/// slug.</summary>
public sealed record UnidadeNegocioCriarInput(string Nome, string Slug);

public sealed record UnidadeNegocioRenomearInput(string Nome);

public sealed record UnidadeNegocioStatusInput(bool Ativa);

// ---- O1.11 — Identity Providers por Unidade de Negócio ----

/// <summary><c>Parametros</c> é o segredo em texto claro vindo do cliente apenas na entrada — nunca
/// devolvido. Nulo em edição preserva o segredo já salvo.</summary>
public sealed record IdentityProviderInput(string Tipo, IReadOnlyList<string>? DominiosAutorizados, string? Parametros);

public sealed record IdentityProviderStatusInput(bool Ativo);

public sealed record IdentityProviderDto(
    Guid Id,
    Guid UnidadeNegocioId,
    string Tipo,
    IReadOnlyList<string> DominiosAutorizados,
    bool ParametrosConfigurados,
    bool Ativo,
    DateTimeOffset CriadoEm,
    DateTimeOffset AtualizadoEm);

// ---- O1.11 — Configuração de ERP por Unidade de Negócio ----

public sealed record ConfiguracaoErpInput(string SistemaErp, string? ParametrosConexao);

public sealed record ConfiguracaoErpStatusInput(bool Ativo);

public sealed record ConfiguracaoErpDto(
    Guid Id,
    Guid UnidadeNegocioId,
    string SistemaErp,
    bool ParametrosConfigurados,
    bool Ativo,
    DateTimeOffset CriadoEm,
    DateTimeOffset AtualizadoEm);

// ---- O1.11 — Configuração de Notificações por Unidade de Negócio (item #24) ----

/// <summary>Escopo mínimo de fundação aprovado pelo Product Owner: apenas o canal e-mail
/// (ativado/inativado, remetente e nome do remetente). Sem catálogo de eventos nesta sprint — não existe
/// documentação formal aprovada com o conjunto de eventos (verificado em docs/product/, work orders e
/// ADRs); será endereçado quando os workflows operacionais correspondentes existirem.</summary>
public sealed record ConfiguracaoNotificacaoInput(bool EmailAtivado, string? EmailRemetente, string? NomeRemetente);

public sealed record ConfiguracaoNotificacaoDto(
    Guid Id,
    Guid UnidadeNegocioId,
    bool EmailAtivado,
    string? EmailRemetente,
    string? NomeRemetente,
    DateTimeOffset CriadoEm,
    DateTimeOffset AtualizadoEm);

// ---- O1.11 — Parâmetros gerais ----

public sealed record ParametroCriarInput(string Chave, string Valor, string Descricao, Guid? UnidadeNegocioId);

public sealed record ParametroAtualizarInput(string Valor, string Descricao);

public sealed record ParametroDto(Guid Id, string Chave, string Valor, string Descricao, Guid? UnidadeNegocioId);

// ---- O1.11 — Feature Flags ----

public sealed record FeatureFlagCriarInput(string Nome, string Descricao);

public sealed record FeatureFlagStatusInput(bool Ativa);

public sealed record FeatureFlagStatusUnidadeDto(Guid UnidadeNegocioId, string UnidadeNegocioNome, bool Ativa);

public sealed record FeatureFlagDto(Guid Id, string Nome, string Descricao, IReadOnlyList<FeatureFlagStatusUnidadeDto> Status);
