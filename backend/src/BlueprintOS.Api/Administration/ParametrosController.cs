using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Authorization;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Api.Administration;

public sealed record ParametroCriarRequest(string? Chave, string? Valor, string? Descricao, Guid? UnidadeNegocioId);

public sealed record ParametroAtualizarRequest(string? Valor, string? Descricao);

/// <summary>O1.11 — Parâmetros gerais, globais (<c>unidadeNegocioId</c> nulo) ou por Unidade de Negócio,
/// protegidos pela permissão corporativa <c>Sistema.Gerenciar</c>. Único não-transacional-de-ERP desta
/// Work Order com exclusão física: Parâmetro não é dado mestre de ERP nem possui histórico externo a
/// preservar (decisão registrada explicitamente na Work Order O1.11).</summary>
public static class ParametrosController
{
    public static IEndpointRouteBuilder MapParametros(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(PerfisController.BaseRoute)
            .WithTags("Administração — Parâmetros")
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.SistemaGerenciar))
            .AddEndpointFilter<CsrfHeaderFilter>();

        group.MapGet("/parametros", Listar);
        group.MapPost("/parametros", Criar);
        group.MapPut("/parametros/{id:guid}", Atualizar);
        group.MapDelete("/parametros/{id:guid}", Excluir);

        return endpoints;
    }

    private static async Task<IResult> Listar(Guid? unidadeNegocioId, IListarParametrosUseCase useCase, CancellationToken ct) =>
        Results.Ok(await useCase.ExecuteAsync(unidadeNegocioId, ct));

    private static async Task<IResult> Criar(ParametroCriarRequest? request, ICriarParametroUseCase useCase, CancellationToken ct)
    {
        var input = new ParametroCriarInput(
            request?.Chave ?? string.Empty, request?.Valor ?? string.Empty, request?.Descricao ?? string.Empty, request?.UnidadeNegocioId);
        var resultado = await useCase.ExecuteAsync(input, ct);
        return resultado.Sucesso
            ? Results.Created($"{PerfisController.BaseRoute}/parametros/{resultado.Valor!.Id}", resultado.Valor)
            : Traduzir(resultado);
    }

    private static async Task<IResult> Atualizar(
        Guid id, ParametroAtualizarRequest? request, IAtualizarParametroUseCase useCase, CancellationToken ct)
    {
        var input = new ParametroAtualizarInput(request?.Valor ?? string.Empty, request?.Descricao ?? string.Empty);
        var resultado = await useCase.ExecuteAsync(id, input, ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
    }

    private static async Task<IResult> Excluir(Guid id, IExcluirParametroUseCase useCase, CancellationToken ct)
    {
        var resultado = await useCase.ExecuteAsync(id, ct);
        return resultado.Sucesso ? Results.NoContent() : Traduzir(resultado);
    }

    private static IResult Traduzir<T>(RbacResultado<T> resultado) => resultado.Falha switch
    {
        RbacFalha.ParametroNaoEncontrado => Results.NotFound(new { code = "parametro_nao_encontrado", message = resultado.Mensagem }),
        RbacFalha.UnidadeNegocioNaoEncontrada => Results.NotFound(new { code = "unidade_negocio_nao_encontrada", message = resultado.Mensagem }),
        RbacFalha.ChaveObrigatoria => Results.BadRequest(new { code = "chave_obrigatoria", message = resultado.Mensagem }),
        RbacFalha.ParametroDuplicado => Results.Conflict(new { code = "parametro_duplicado", message = resultado.Mensagem }),
        _ => Results.BadRequest(new { code = "requisicao_invalida", message = resultado.Mensagem }),
    };
}
