using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Api.Identity;

public static class DevelopmentHeaderAuthenticationDefaults
{
    public const string Scheme = "DevelopmentHeader";
}

/// <summary>Esquema de autenticação exclusivo de Development (O1.4.2.1, Etapa 3; hardening de origem em
/// O1.4.2.2 — Security Validation II, Achado E). Só é registrado em <c>Program.cs</c> quando
/// <c>IHostEnvironment.IsDevelopment()</c>; ainda assim mantém checagem interna do mesmo ambiente como
/// segunda barreira independente — não confia apenas no registro condicional.
///
/// Origem: exige <c>RemoteIpAddress</c> estritamente loopback (IPv4 127.0.0.1 ou IPv6 ::1) — a mesma
/// defesa já usada em <c>GET /dev/otp</c>. Nenhum header (<c>X-Forwarded-For</c>, <c>Forwarded</c>,
/// <c>Host</c>, <c>Origin</c>, <c>Referer</c>) é lido ou honrado como prova de origem; nenhum
/// <c>UseForwardedHeaders()</c> está registrado em <c>Program.cs</c>, então <c>RemoteIpAddress</c> é
/// sempre o peer TCP real, nunca algo que o cliente possa forjar por header.
///
/// Este mecanismo NÃO é suportado através de proxy reverso (Nginx/IIS/YARP), túnel (ngrok ou
/// equivalente), rede compartilhada ou acesso remoto — mesmo em Development. Se este processo algum dia
/// rodar atrás de um proxy no mesmo host, todo tráfego externo chegaria ao Kestrel como loopback e este
/// mecanismo deveria ser desabilitado, nunca adaptado para confiar em headers de proxy. Uma necessidade
/// futura de Development remoto exige uma decisão de segurança própria, não uma flexibilização desta
/// checagem. Na dúvida sobre a origem: nega (fail-closed).
///
/// Não substitui <see cref="DevelopmentRequestIdentity"/> (ADR-0011), que continua sendo chamada de
/// forma independente pelos casos de uso e mantém sua própria checagem interna de ambiente — duas
/// barreiras não conflitantes, nunca uma única fonte de verdade. Não aceita claims adicionais do
/// request além do identificador e do papel já suportados pelo mecanismo de Development existente.</summary>
public sealed class DevelopmentHeaderAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IHostEnvironment environment)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private const string UserIdHeader = "X-Development-User-Id";
    private const string RoleHeader = "X-Development-Role";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!environment.IsDevelopment())
        {
            return Task.FromResult(AuthenticateResult.Fail("DevelopmentHeader indisponível fora de Development."));
        }

        var remoteIp = Context.Connection.RemoteIpAddress;
        if (remoteIp is null || !IPAddress.IsLoopback(remoteIp))
        {
            // Nunca autentica origem não-local, mesmo em Development — sem fallback permissivo, sem
            // identidade parcial. O endpoint protegido termina em 401 via FallbackPolicy.
            return Task.FromResult(AuthenticateResult.Fail("DevelopmentHeader exige origem loopback."));
        }

        var userIdValue = Request.Headers[UserIdHeader].FirstOrDefault();
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var role = Request.Headers[RoleHeader].FirstOrDefault();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, string.IsNullOrWhiteSpace(role) ? "Buyer" : role),
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
