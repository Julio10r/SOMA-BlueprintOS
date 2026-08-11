using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Authorization;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Api.Administration;

public sealed record RegraOrcamentariaUpsertRequest(string? Nome, Guid CentroCustoMetadadoId, decimal ValorLimite, PeriodoOrcamentario Periodo);

public sealed record RegraOrcamentariaStatusRequest(bool Ativo);

/// <summary>O1.12 — Fundação de Administração de Controle Orçamentário (ADR-0020, revisão R1.1). CRUD
/// administrativo por Unidade de Negócio, sem exclusão física. <c>unidadeNegocioId</c> explícito no path,
/// nunca de claim de sessão do cliente — mesmo padrão de <see cref="ConfiguracaoNotificacaoController"/>.
/// APENAS o cadastro: nenhuma reserva contábil, consumo real ou bloqueio operacional é implementado por
/// estes endpoints.</summary>
public static class RegrasOrcamentariasController
{
    public static IEndpointRouteBuilder MapRegrasOrcamentarias(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(PerfisController.BaseRoute)
            .WithTags("Administração — Regras Orçamentárias")
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.OrcamentoGerenciar))
            .AddEndpointFilter<CsrfHeaderFilter>();

        group.MapGet("/unidades-negocio/{unidadeNegocioId:guid}/regras-orcamentarias", Listar);
        group.MapPost("/unidades-negocio/{unidadeNegocioId:guid}/regras-orcamentarias", Criar);
        group.MapPut("/unidades-negocio/{unidadeNegocioId:guid}/regras-orcamentarias/{id:guid}", Atualizar);
        group.MapPatch("/unidades-negocio/{unidadeNegocioId:guid}/regras-orcamentarias/{id:guid}/status", AlterarStatus);

        return endpoints;
    }

    private static async Task<IResult> Listar(Guid unidadeNegocioId, IListarRegrasOrcamentariasUseCase useCase, CancellationToken ct) =>
        Results.Ok(await useCase.ExecuteAsync(unidadeNegocioId, ct));

    private static async Task<IResult> Criar(
        Guid unidadeNegocioId, RegraOrcamentariaUpsertRequest? request, ICriarRegraOrcamentariaUseCase useCase, CancellationToken ct)
    {
        var resultado = await useCase.ExecuteAsync(ParaInput(request), unidadeNegocioId, ct);
        return resultado.Sucesso
            ? Results.Created($"{PerfisController.BaseRoute}/unidades-negocio/{unidadeNegocioId}/regras-orcamentarias/{resultado.Valor!.Id}", resultado.Valor)
            : Traduzir(resultado);
    }

    private static async Task<IResult> Atualizar(
        Guid unidadeNegocioId, Guid id, RegraOrcamentariaUpsertRequest? request, IAtualizarRegraOrcamentariaUseCase useCase, CancellationToken ct)
    {
        var resultado = await useCase.ExecuteAsync(id, ParaInput(request), unidadeNegocioId, ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
    }

    private static async Task<IResult> AlterarStatus(
        Guid unidadeNegocioId, Guid id, RegraOrcamentariaStatusRequest? request, IAlterarStatusRegraOrcamentariaUseCase useCase, CancellationToken ct)
    {
        if (request is null) return Results.BadRequest(new { code = "requisicao_invalida", message = "Informe o status desejado." });

        var resultado = await useCase.ExecuteAsync(id, request.Ativo, unidadeNegocioId, ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
    }

    private static RegraOrcamentariaInput ParaInput(RegraOrcamentariaUpsertRequest? request) => new(
        request?.Nome ?? string.Empty,
        request?.CentroCustoMetadadoId ?? Guid.Empty,
        request?.ValorLimite ?? 0,
        request?.Periodo ?? PeriodoOrcamentario.Mensal);

    private static IResult Traduzir<T>(RbacResultado<T> resultado) => resultado.Falha switch
    {
        RbacFalha.UnidadeNegocioNaoEncontrada => Results.NotFound(new { code = "unidade_negocio_nao_encontrada", message = resultado.Mensagem }),
        RbacFalha.RegraOrcamentariaNaoEncontrada => Results.NotFound(new { code = "regra_orcamentaria_nao_encontrada", message = resultado.Mensagem }),
        RbacFalha.NomeObrigatorio => Results.BadRequest(new { code = "nome_obrigatorio", message = resultado.Mensagem }),
        RbacFalha.ValorLimiteInvalido => Results.BadRequest(new { code = "valor_limite_invalido", message = resultado.Mensagem }),
        RbacFalha.CentroCustoInvalidoNaUnidadeDeNegocio => Results.BadRequest(new { code = "centro_custo_invalido", message = resultado.Mensagem }),
        _ => Results.BadRequest(new { code = "requisicao_invalida", message = resultado.Mensagem }),
    };
}
