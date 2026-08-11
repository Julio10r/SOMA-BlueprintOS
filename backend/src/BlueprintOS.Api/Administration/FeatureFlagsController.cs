using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Authorization;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Api.Administration;

public sealed record FeatureFlagCriarRequest(string? Nome, string? Descricao);

public sealed record FeatureFlagStatusRequest(bool Ativa);

/// <summary>O1.11 — Feature Flags, protegidas pela permissão corporativa <c>Sistema.Gerenciar</c>.
/// Catálogo nasce vazio — nenhuma flag fictícia é semeada por migration. O vínculo N:N com Unidade de
/// Negócio (`ComprasDataModel.md`) é ativado/desativado por <c>unidadeNegocioId</c> explícito no path.</summary>
public static class FeatureFlagsController
{
    public static IEndpointRouteBuilder MapFeatureFlags(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(PerfisController.BaseRoute)
            .WithTags("Administração — Feature Flags")
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.SistemaGerenciar))
            .AddEndpointFilter<CsrfHeaderFilter>();

        group.MapGet("/feature-flags", Listar);
        group.MapPost("/feature-flags", Criar);
        group.MapPatch("/feature-flags/{id:guid}/unidades-negocio/{unidadeNegocioId:guid}", AlterarStatus);

        return endpoints;
    }

    private static async Task<IResult> Listar(IListarFeatureFlagsUseCase useCase, CancellationToken ct) =>
        Results.Ok(await useCase.ExecuteAsync(ct));

    private static async Task<IResult> Criar(FeatureFlagCriarRequest? request, ICriarFeatureFlagUseCase useCase, CancellationToken ct)
    {
        var input = new FeatureFlagCriarInput(request?.Nome ?? string.Empty, request?.Descricao ?? string.Empty);
        var resultado = await useCase.ExecuteAsync(input, ct);
        return resultado.Sucesso
            ? Results.Created($"{PerfisController.BaseRoute}/feature-flags/{resultado.Valor!.Id}", resultado.Valor)
            : Traduzir(resultado);
    }

    private static async Task<IResult> AlterarStatus(
        Guid id, Guid unidadeNegocioId, FeatureFlagStatusRequest? request,
        IAlterarStatusFeatureFlagUnidadeUseCase useCase, CancellationToken ct)
    {
        if (request is null) return Results.BadRequest(new { code = "requisicao_invalida", message = "Informe o status desejado." });

        var resultado = await useCase.ExecuteAsync(id, unidadeNegocioId, request.Ativa, ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
    }

    private static IResult Traduzir<T>(RbacResultado<T> resultado) => resultado.Falha switch
    {
        RbacFalha.FeatureFlagNaoEncontrada => Results.NotFound(new { code = "feature_flag_nao_encontrada", message = resultado.Mensagem }),
        RbacFalha.UnidadeNegocioNaoEncontrada => Results.NotFound(new { code = "unidade_negocio_nao_encontrada", message = resultado.Mensagem }),
        RbacFalha.NomeObrigatorio => Results.BadRequest(new { code = "nome_obrigatorio", message = resultado.Mensagem }),
        RbacFalha.FeatureFlagDuplicada => Results.Conflict(new { code = "feature_flag_duplicada", message = resultado.Mensagem }),
        _ => Results.BadRequest(new { code = "requisicao_invalida", message = resultado.Mensagem }),
    };
}
