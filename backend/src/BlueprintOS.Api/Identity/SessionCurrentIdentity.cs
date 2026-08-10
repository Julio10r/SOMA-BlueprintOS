using System.Security.Claims;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;

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
        return new RequestIdentity(userId, role);
    }
}
