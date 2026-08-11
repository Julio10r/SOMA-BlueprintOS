using BlueprintOS.Api.Auth;
using BlueprintOS.Api.Authorization;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Api.Administration;

public sealed record IdentityProviderRequest(string? Tipo, IReadOnlyList<string>? DominiosAutorizados, string? Parametros);

public sealed record IdentityProviderStatusRequest(bool Ativo);

/// <summary>O1.11 — Identity Providers por Unidade de Negócio. Operação administrativa corporativa:
/// <c>unidadeNegocioId</c> vem explicitamente do path (é a UN sendo operada, não necessariamente a da
/// sessão de quem administra) — protegida pela permissão corporativa <c>Sistema.Gerenciar</c>. Os
/// parâmetros sensíveis (<c>Parametros</c> no request) nunca são devolvidos pela API depois de salvos —
/// apenas <c>parametrosConfigurados: bool</c> na projeção de leitura; são cifrados via
/// <c>IDataProtector</c> antes de persistidos e nunca aparecem em log.</summary>
public static class IdentityProvidersController
{
    public static IEndpointRouteBuilder MapIdentityProviders(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(PerfisController.BaseRoute)
            .WithTags("Administração — Identity Providers")
            .RequireAuthorization(RbacPolicies.For(PermissaoCatalogo.SistemaGerenciar))
            .AddEndpointFilter<CsrfHeaderFilter>();

        group.MapGet("/unidades-negocio/{unidadeNegocioId:guid}/identity-providers", Listar);
        group.MapPost("/unidades-negocio/{unidadeNegocioId:guid}/identity-providers", Criar);
        group.MapPut("/unidades-negocio/{unidadeNegocioId:guid}/identity-providers/{id:guid}", Atualizar);
        group.MapPatch("/unidades-negocio/{unidadeNegocioId:guid}/identity-providers/{id:guid}/status", AlterarStatus);

        return endpoints;
    }

    private static async Task<IResult> Listar(
        Guid unidadeNegocioId, IListarIdentityProvidersUseCase useCase, CancellationToken ct)
    {
        var resultado = await useCase.ExecuteAsync(unidadeNegocioId, ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
    }

    private static async Task<IResult> Criar(
        Guid unidadeNegocioId, IdentityProviderRequest? request, ICriarIdentityProviderUseCase useCase, CancellationToken ct)
    {
        var input = new IdentityProviderInput(request?.Tipo ?? string.Empty, request?.DominiosAutorizados, request?.Parametros);
        var resultado = await useCase.ExecuteAsync(unidadeNegocioId, input, ct);
        return resultado.Sucesso
            ? Results.Created(
                $"{PerfisController.BaseRoute}/unidades-negocio/{unidadeNegocioId}/identity-providers/{resultado.Valor!.Id}",
                resultado.Valor)
            : Traduzir(resultado);
    }

    private static async Task<IResult> Atualizar(
        Guid unidadeNegocioId, Guid id, IdentityProviderRequest? request, IAtualizarIdentityProviderUseCase useCase, CancellationToken ct)
    {
        var input = new IdentityProviderInput(request?.Tipo ?? string.Empty, request?.DominiosAutorizados, request?.Parametros);
        var resultado = await useCase.ExecuteAsync(unidadeNegocioId, id, input, ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
    }

    private static async Task<IResult> AlterarStatus(
        Guid unidadeNegocioId, Guid id, IdentityProviderStatusRequest? request,
        IAlterarStatusIdentityProviderUseCase useCase, CancellationToken ct)
    {
        if (request is null) return Results.BadRequest(new { code = "requisicao_invalida", message = "Informe o status desejado." });

        var resultado = await useCase.ExecuteAsync(unidadeNegocioId, id, request.Ativo, ct);
        return resultado.Sucesso ? Results.Ok(resultado.Valor) : Traduzir(resultado);
    }

    private static IResult Traduzir<T>(RbacResultado<T> resultado) => resultado.Falha switch
    {
        RbacFalha.UnidadeNegocioNaoEncontrada => Results.NotFound(new { code = "unidade_negocio_nao_encontrada", message = resultado.Mensagem }),
        RbacFalha.IdentityProviderNaoEncontrado => Results.NotFound(new { code = "identity_provider_nao_encontrado", message = resultado.Mensagem }),
        RbacFalha.TipoObrigatorio => Results.BadRequest(new { code = "tipo_obrigatorio", message = resultado.Mensagem }),
        _ => Results.BadRequest(new { code = "requisicao_invalida", message = resultado.Mensagem }),
    };
}
