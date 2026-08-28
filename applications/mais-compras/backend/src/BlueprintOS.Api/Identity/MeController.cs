using BlueprintOS.Api.Auth;
using BlueprintOS.Application.Identity.Contracts;

namespace BlueprintOS.Api.Identity;

/// <summary>O1.11 — Seleção de Unidade de Negócio. `GET /me/unidades-negocio` não exige nenhuma
/// permissão especial (apenas sessão válida — enforcement automático via
/// <c>AuthorizationOptions.FallbackPolicy</c> em <c>Program.cs</c>, mesmo padrão de <c>GET /auth/me</c>):
/// qualquer usuário autenticado pode consultar as próprias Unidades de Negócio. Sistema hoje
/// single-BU-por-usuário — devolve sempre uma única UN (a da sessão); o frontend decide sozinho não
/// mostrar a tela de seleção quando há apenas uma. Nenhuma mudança de sessão/claims/cookies (O1.4.x
/// permanece intocado).</summary>
public static class MeController
{
    public static IEndpointRouteBuilder MapMe(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/me").WithTags("Minha Conta");

        group.MapGet("/unidades-negocio", ListarMinhasUnidadesNegocio);

        return endpoints;
    }

    private static async Task<IResult> ListarMinhasUnidadesNegocio(
        ICurrentIdentity identity, IListarMinhasUnidadesNegocioUseCase useCase, CancellationToken ct)
    {
        var atual = identity.GetRequired();
        if (atual.UnidadeNegocioId is null || atual.UnidadeNegocioId == Guid.Empty)
        {
            return Results.Ok(Array.Empty<object>());
        }

        var unidades = await useCase.ExecuteAsync(atual.UnidadeNegocioId.Value, ct);
        return Results.Ok(unidades);
    }
}
