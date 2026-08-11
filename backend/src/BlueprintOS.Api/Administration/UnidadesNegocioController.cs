using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Authorization;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Api.Administration;

public sealed record UnidadeNegocioCriarRequest(string? Nome, string? Slug);

public sealed record UnidadeNegocioRenomearRequest(string? Nome);

public sealed record UnidadeNegocioStatusRequest(bool Ativa);

/// <summary>O1.11 — Cadastro de Unidades de Negócio (CRUD real). Recurso CORPORATIVO: ao contrário de
/// <see cref="UnidadesAlocacaoController"/>/<see cref="CentrosCustoController"/>, NUNCA é escopado pela
/// Unidade de Negócio de quem administra — a UN sendo administrada é o próprio recurso da requisição
/// (todas, no <c>GET</c> de listagem; uma específica, via <c>id</c> no path das demais operações),
/// protegido pela permissão corporativa <c>UnidadeNegocio.Gerenciar</c>. Nunca há exclusão física —
/// apenas Criar, Editar (somente Nome — Slug é imutável após a criação) e Ativar/Inativar.</summary>
public static class UnidadesNegocioController
{
    public static IEndpointRouteBuilder MapUnidadesNegocio(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(PerfisController.BaseRoute)
            .WithTags("Administração — Unidades de Negócio")
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.UnidadeNegocioGerenciar))
            .AddEndpointFilter<CsrfHeaderFilter>();

        group.MapGet("/unidades-negocio", Listar);
        group.MapPost("/unidades-negocio", Criar);
        group.MapPut("/unidades-negocio/{id:guid}", Renomear);
        group.MapPatch("/unidades-negocio/{id:guid}/status", AlterarStatus);

        return endpoints;
    }

    private static async Task<IResult> Listar(IListarUnidadesNegocioUseCase useCase, CancellationToken ct) =>
        Results.Ok(await useCase.ExecuteAsync(ct));

    private static async Task<IResult> Criar(UnidadeNegocioCriarRequest? request, ICriarUnidadeNegocioUseCase useCase, CancellationToken ct)
    {
        var input = new UnidadeNegocioCriarInput(request?.Nome ?? string.Empty, request?.Slug ?? string.Empty);
        var resultado = await useCase.ExecuteAsync(input, ct);
        return resultado.Sucesso
            ? Results.Created($"{PerfisController.BaseRoute}/unidades-negocio/{resultado.Valor!.Id}", resultado.Valor)
            : Traduzir(resultado);
    }

    private static async Task<IResult> Renomear(
        Guid id, UnidadeNegocioRenomearRequest? request, IRenomearUnidadeNegocioUseCase useCase, CancellationToken ct)
    {
        var input = new UnidadeNegocioRenomearInput(request?.Nome ?? string.Empty);
        var resultado = await useCase.ExecuteAsync(id, input, ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
    }

    private static async Task<IResult> AlterarStatus(
        Guid id, UnidadeNegocioStatusRequest? request, IAlterarStatusUnidadeNegocioUseCase useCase, CancellationToken ct)
    {
        if (request is null) return Results.BadRequest(new { code = "requisicao_invalida", message = "Informe o status desejado." });

        var resultado = await useCase.ExecuteAsync(id, request.Ativa, ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
    }

    private static IResult Traduzir<T>(RbacResultado<T> resultado) => resultado.Falha switch
    {
        RbacFalha.UnidadeNegocioNaoEncontrada => Results.NotFound(new { code = "unidade_negocio_nao_encontrada", message = resultado.Mensagem }),
        RbacFalha.SlugDuplicado => Results.Conflict(new { code = "slug_duplicado", message = resultado.Mensagem }),
        RbacFalha.SlugObrigatorio => Results.BadRequest(new { code = "slug_obrigatorio", message = resultado.Mensagem }),
        RbacFalha.SlugInvalido => Results.BadRequest(new { code = "slug_invalido", message = resultado.Mensagem }),
        RbacFalha.NomeObrigatorio => Results.BadRequest(new { code = "nome_obrigatorio", message = resultado.Mensagem }),
        _ => Results.BadRequest(new { code = "requisicao_invalida", message = resultado.Mensagem }),
    };
}
