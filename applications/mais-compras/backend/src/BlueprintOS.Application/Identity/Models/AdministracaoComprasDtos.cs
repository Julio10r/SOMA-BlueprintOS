using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Models;

// ---- O1.12 — Fundação de Administração (Workflow, Alçadas, Controle Orçamentário) ----
// Mesmo cuidado das demais entradas administrativas desta base (UnidadeAlocacaoInput/UsuarioInput):
// UnidadeNegocioId nunca aparece nestes Inputs — vem sempre explícito do path da rota administrada
// (nunca de claim de sessão do cliente, nunca do corpo da requisição).

/// <summary>Projeção de leitura de uma Regra de Workflow (O1.12).</summary>
public sealed record RegraWorkflowDto(
    Guid Id,
    string Nome,
    Guid UnidadeNegocioId,
    string TipoProcesso,
    int Ordem,
    bool Ativo,
    DateTimeOffset CriadoEm,
    DateTimeOffset AtualizadoEm);

public sealed record RegraWorkflowInput(string Nome, string TipoProcesso, int Ordem);

/// <summary>Projeção de leitura de uma Alçada de Aprovação (O1.12). Exatamente um entre
/// <c>AprovadorUsuarioId</c>/<c>AprovadorPerfilId</c> é preenchido.</summary>
public sealed record AlcadaAprovacaoDto(
    Guid Id,
    string Nome,
    Guid UnidadeNegocioId,
    CriterioAlcada Criterio,
    decimal? ValorMinimo,
    decimal? ValorMaximo,
    Guid? CentroCustoMetadadoId,
    int Nivel,
    Guid? AprovadorUsuarioId,
    Guid? AprovadorPerfilId,
    bool Ativo,
    DateTimeOffset CriadoEm,
    DateTimeOffset AtualizadoEm);

public sealed record AlcadaAprovacaoInput(
    string Nome,
    CriterioAlcada Criterio,
    decimal? ValorMinimo,
    decimal? ValorMaximo,
    Guid? CentroCustoMetadadoId,
    int Nivel,
    Guid? AprovadorUsuarioId,
    Guid? AprovadorPerfilId);

/// <summary>Projeção de leitura de uma Regra Orçamentária (O1.12). Apenas o cadastro — nenhum saldo,
/// consumo ou reserva é calculado aqui.</summary>
public sealed record RegraOrcamentariaDto(
    Guid Id,
    string Nome,
    Guid UnidadeNegocioId,
    Guid CentroCustoMetadadoId,
    decimal ValorLimite,
    PeriodoOrcamentario Periodo,
    bool Ativo,
    DateTimeOffset CriadoEm,
    DateTimeOffset AtualizadoEm);

public sealed record RegraOrcamentariaInput(string Nome, Guid CentroCustoMetadadoId, decimal ValorLimite, PeriodoOrcamentario Periodo);
