using System.Security.Claims;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Api.Identity;

/// <summary>Adaptador de produção de <see cref="ICurrentIdentity"/> fora de Development. A partir de
/// O1.4.2.1, lê exclusivamente <c>HttpContext.User</c> — já autenticado pelo
/// <see cref="BlueprintOS.Api.Auth.SessionCookieAuthenticationHandler"/> como primeira barreira (a
/// <c>AuthorizationOptions.FallbackPolicy</c> já garantiu que a requisição só chega até aqui se
/// autenticada). Isto elimina a dívida técnica anterior de <c>GetAwaiter().GetResult()</c> sobre um caso
/// de uso assíncrono: nenhuma chamada de I/O acontece mais aqui — a única resolução de sessão por
/// requisição já ocorreu, de forma corretamente assíncrona, no authentication handler.</summary>
public sealed class SessionCurrentIdentity(IHttpContextAccessor httpContextAccessor) : ICurrentIdentity
{
    public RequestIdentity GetRequired()
    {
        var user = httpContextAccessor.HttpContext?.User;
        var idClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (user?.Identity?.IsAuthenticated != true || !Guid.TryParse(idClaim, out var userId))
        {
            throw new IdentityUnavailableException("Nenhuma sessão autenticada válida encontrada.", false);
        }

        var role = user.FindFirst(ClaimTypes.Role)?.Value ?? "Buyer";

        // O1.5 — Unidade de Negócio e permissões efetivas vêm exclusivamente das claims já publicadas
        // pelo authentication handler a partir do banco. Continua sem I/O aqui. O esquema exclusivo de
        // Development (X-Development-User-Id) não emite estas claims, então `UnidadeNegocioId` fica nulo e
        // os casos de uso administrativos falham fechado — nunca assumem uma Unidade de Negócio.
        Guid? unidadeNegocioId = Guid.TryParse(user.FindFirst("unidade_negocio_id")?.Value, out var bu) ? bu : null;
        var permissoes = user.FindAll(Authorization.RbacClaims.Permissao).Select(x => x.Value).ToArray();

        // Fail-closed: qualquer valor ausente/inesperado na claim (esquemas que não a emitem, como
        // Development) resolve para EscopoAdministrativo.Negocio — nunca assume Produto por omissão.
        var escopoAdministrativo = Enum.TryParse<EscopoAdministrativo>(
            user.FindFirst(Authorization.RbacClaims.EscopoAdministrativo)?.Value, out var escopo)
            ? escopo
            : EscopoAdministrativo.Negocio;

        return new RequestIdentity(userId, role, unidadeNegocioId, permissoes, escopoAdministrativo);
    }
}
