using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Authorization;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Api.Administration;

public sealed record RegraWorkflowUpsertRequest(string? Nome, string? TipoProcesso, int Ordem);

public sealed record RegraWorkflowStatusRequest(bool Ativo);

/// <summary>O1.12 — Fundação de Administração de Workflow (ADR-0020, revisão R1.1). CRUD administrativo
/// por Unidade de Negócio, sem exclusão física. <c>unidadeNegocioId</c> explícito no path (recurso
/// administrado), nunca de claim de sessão do cliente — mesmo padrão de
/// <see cref="ConfiguracaoNotificacaoController"/>. Nenhum motor de execução de workflow é acionado por
/// estes endpoints.</summary>
public static class RegrasWorkflowController
{
    public static IEndpointRouteBuilder MapRegrasWorkflow(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(PerfisController.BaseRoute)
            .WithTags("Administração — Regras de Workflow")
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.WorkflowGerenciar))
            .AddEndpointFilter<CsrfHeaderFilter>()
            .AddEndpointFilter<EscopoUnidadeNegocioPathFilter>();

        group.MapGet("/unidades-negocio/{unidadeNegocioId:guid}/regras-workflow", Listar);
        group.MapPost("/unidades-negocio/{unidadeNegocioId:guid}/regras-workflow", Criar);
        group.MapPut("/unidades-negocio/{unidadeNegocioId:guid}/regras-workflow/{id:guid}", Atualizar);
        group.MapPatch("/unidades-negocio/{unidadeNegocioId:guid}/regras-workflow/{id:guid}/status", AlterarStatus);

        return endpoints;
    }

    private static async Task<IResult> Listar(Guid unidadeNegocioId, IListarRegrasWorkflowUseCase useCase, CancellationToken ct) =>
        Results.Ok(await useCase.ExecuteAsync(unidadeNegocioId, ct));

    private static async Task<IResult> Criar(
        Guid unidadeNegocioId, RegraWorkflowUpsertRequest? request, ICriarRegraWorkflowUseCase useCase, CancellationToken ct)
    {
        var resultado = await useCase.ExecuteAsync(ParaInput(request), unidadeNegocioId, ct);
        return resultado.Sucesso
            ? Results.Created($"{PerfisController.BaseRoute}/unidades-negocio/{unidadeNegocioId}/regras-workflow/{resultado.Valor!.Id}", resultado.Valor)
            : Traduzir(resultado);
    }

    private static async Task<IResult> Atualizar(
        Guid unidadeNegocioId, Guid id, RegraWorkflowUpsertRequest? request, IAtualizarRegraWorkflowUseCase useCase, CancellationToken ct)
    {
        var resultado = await useCase.ExecuteAsync(id, ParaInput(request), unidadeNegocioId, ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
    }

    private static async Task<IResult> AlterarStatus(
        Guid unidadeNegocioId, Guid id, RegraWorkflowStatusRequest? request, IAlterarStatusRegraWorkflowUseCase useCase, CancellationToken ct)
    {
        if (request is null) return Results.BadRequest(new { code = "requisicao_invalida", message = "Informe o status desejado." });

        var resultado = await useCase.ExecuteAsync(id, request.Ativo, unidadeNegocioId, ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
    }

    private static RegraWorkflowInput ParaInput(RegraWorkflowUpsertRequest? request) => new(
        request?.Nome ?? string.Empty, request?.TipoProcesso ?? string.Empty, request?.Ordem ?? 0);

    private static IResult Traduzir<T>(RbacResultado<T> resultado) => resultado.Falha switch
    {
        RbacFalha.UnidadeNegocioNaoEncontrada => Results.NotFound(new { code = "unidade_negocio_nao_encontrada", message = resultado.Mensagem }),
        RbacFalha.RegraWorkflowNaoEncontrada => Results.NotFound(new { code = "regra_workflow_nao_encontrada", message = resultado.Mensagem }),
        RbacFalha.NomeObrigatorio => Results.BadRequest(new { code = "nome_obrigatorio", message = resultado.Mensagem }),
        RbacFalha.TipoProcessoObrigatorio => Results.BadRequest(new { code = "tipo_processo_obrigatorio", message = resultado.Mensagem }),
        RbacFalha.OrdemInvalida => Results.BadRequest(new { code = "ordem_invalida", message = resultado.Mensagem }),
        _ => Results.BadRequest(new { code = "requisicao_invalida", message = resultado.Mensagem }),
    };
}
