using System.Security.Claims;
using System.Text.Encodings.Web;
using BlueprintOS.Api.Identity;
using BlueprintOS.Application.Identity.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Api.Auth;

public static class SessionCookieAuthenticationDefaults
{
    public const string Scheme = "SessionCookie";
}

/// <summary>Esquema de autenticação da sessão de cookie fora de Development (O1.4.2.1, Etapa 3). Faz a
/// única resolução assíncrona da sessão por requisição — via <see cref="IObterIdentidadeAtualUseCase"/>,
/// que já revalida usuário Ativo e sessão ativa (§2.5) — e publica o resultado em <c>HttpContext.User</c>.
/// <see cref="SessionCurrentIdentity"/> apenas lê essas claims, sem I/O adicional, eliminando a dívida
/// síncrono/assíncrono anterior. Este é o mecanismo que a <c>AuthorizationOptions.FallbackPolicy</c> usa
/// para decidir 401 antes de qualquer endpoint/caso de uso ser executado — a primeira barreira.</summary>
public sealed class SessionCookieAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IObterIdentidadeAtualUseCase obterIdentidadeAtual)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var rawToken = Request.Cookies[AuthCookie.Name];
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return AuthenticateResult.NoResult();
        }

        var identidade = await obterIdentidadeAtual.ExecuteAsync(rawToken, Context.RequestAborted);
        if (identidade is null)
        {
            return AuthenticateResult.Fail("Sessão inválida, expirada ou revogada.");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, identidade.UsuarioId.ToString()),
            new Claim(ClaimTypes.Email, identidade.Email),
            new Claim(ClaimTypes.Name, identidade.Nome),
            new Claim("unidade_negocio_id", identidade.UnidadeNegocioId.ToString()),
            new Claim(ClaimTypes.Role, "Buyer"),
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
