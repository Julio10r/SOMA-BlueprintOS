using System.Security.Claims;
using System.Text.Encodings.Web;
using BlueprintOS.Api.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Api.Auth;

public static class BootstrapSessionAuthenticationDefaults
{
    public const string Scheme = "BootstrapSession";
}

/// <summary>Esquema de autenticação da sessão de Bootstrap (security-design-auth-o1.4.md §20.7; Work Order
/// O1.4.3, seção 8.1) — registrado em TODOS os ambientes (Bootstrap não é exclusivo de Development, ao
/// contrário de <see cref="DevelopmentHeaderAuthenticationDefaults"/>), como um esquema adicional, NUNCA
/// substituindo <see cref="SessionCookieAuthenticationDefaults"/>/<see cref="DevelopmentHeaderAuthenticationDefaults"/>
/// nem alterando o esquema default do host.
///
/// Lê exclusivamente o cookie próprio (<see cref="BootstrapCookie.Name"/>, nunca <c>AuthCookie.Name</c>),
/// resolve a <c>BootstrapSessao</c> pelo hash do identificador (mesma primitiva de
/// <see cref="OpaqueSessionToken"/> já usada pela sessão normal) e publica apenas duas claims mínimas — uma
/// claim de "sessão Bootstrap válida" e o e-mail candidato já validado por OTP — NUNCA <c>ClaimTypes.Role</c>
/// nem qualquer claim de papel/perfil (Work Order O1.4.3, seção 8: "nunca uma claim de papel/perfil"). A
/// checagem adicional de <c>BootstrapEstado.Concluido == false</c> NÃO vive aqui — vive na política de
/// autorização <c>BootstrapAuthenticated</c> (<see cref="BootstrapNaoConcluidoRequirement"/>), avaliada a
/// cada requisição via acesso a repositório, conforme decisão da Work Order (seção 8.1).</summary>
public sealed class BootstrapSessionAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IBootstrapSessaoRepository bootstrapSessoes,
    TimeProvider clock)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>Claim exclusiva de escopo de Bootstrap — nunca reaproveitada por nenhum outro esquema, nunca
    /// um papel/perfil.</summary>
    public const string BootstrapSessionClaimType = "bootstrap_session_id";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var rawToken = Request.Cookies[BootstrapCookie.Name];
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return AuthenticateResult.NoResult();
        }

        var hash = OpaqueSessionToken.Hash(rawToken);
        var sessao = await bootstrapSessoes.ObterPorIdentificadorHashAsync(hash, Context.RequestAborted);
        if (sessao is null || !sessao.EstaValidaEm(clock.GetUtcNow()))
        {
            return AuthenticateResult.Fail("Sessão de Bootstrap inválida, expirada, já usada ou revogada.");
        }

        var claims = new[]
        {
            new Claim(BootstrapSessionClaimType, sessao.Id.ToString()),
            new Claim(ClaimTypes.Email, sessao.EmailCandidato),
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
