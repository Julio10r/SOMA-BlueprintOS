using System.Security.Claims;
using BlueprintOS.Api.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Api.Auth;

/// <summary>Endpoints REST do fluxo de Login Passwordless OTP (O1.4.2). Nenhum destes endpoints
/// devolve o código OTP, o identificador de sessão em claro no corpo, ou qualquer dado sensível —
/// o identificador de sessão só é transmitido via cookie HttpOnly (security-design-auth-o1.4.md, §17).
///
/// Anônimos, por exceção explícita (O1.4.2.1, Etapa 3 — o padrão do restante da aplicação é exigir
/// autenticação, via <c>AuthorizationOptions.FallbackPolicy</c> em <c>Program.cs</c>):
/// - <c>POST /auth/otp/request</c> e <c>POST /auth/otp/verify</c>: são o próprio mecanismo de login —
///   não podem exigir uma sessão que ainda não existe.
/// - <c>POST /auth/logout</c>: idempotente e seguro mesmo sem sessão (encerrar algo que já não existe
///   não deve ser tratado como erro) — evita a complexidade de tratar 401 no fluxo de logout do cliente
///   sem ganho de segurança real, já que o handler já verifica a presença/validade do cookie.
/// <c>GET /auth/me</c> NÃO é anônimo — depende da sessão por definição; a ausência de sessão já produz
/// 401 automaticamente via a policy global, sem precisar de lógica própria neste arquivo.</summary>
public static class AuthController
{
    private const string GenericOtpRequestMessage = "Se o e-mail informado for válido, um código foi enviado.";

    public static IEndpointRouteBuilder MapAuth(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth").WithTags("Autenticação");

        group.MapPost("/otp/request", RequestOtp)
            .RequireRateLimiting(RateLimitingPolicies.OtpRequest)
            .AddEndpointFilter<CsrfHeaderFilter>()
            .AllowAnonymous();
        group.MapPost("/otp/verify", VerifyOtp)
            .RequireRateLimiting(RateLimitingPolicies.OtpVerify)
            .AddEndpointFilter<CsrfHeaderFilter>()
            .AllowAnonymous();
        group.MapPost("/logout", Logout)
            .AddEndpointFilter<CsrfHeaderFilter>()
            .AllowAnonymous();
        group.MapGet("/me", Me);

        return endpoints;
    }

    private static async Task<IResult> RequestOtp(OtpRequestRequest? request, ISolicitarOtpUseCase useCase, CancellationToken ct)
    {
        var email = request?.Email;
        if (string.IsNullOrWhiteSpace(email))
        {
            // Mesma resposta genérica — nem a ausência de e-mail no payload deve diferenciar o
            // comportamento observável de "e-mail inválido" (§2.1).
            return Results.Ok(new { message = GenericOtpRequestMessage });
        }

        await useCase.ExecuteAsync(email, ct);
        return Results.Ok(new { message = GenericOtpRequestMessage });
    }

    private static async Task<IResult> VerifyOtp(
        OtpVerifyRequest? request,
        IValidarOtpUseCase useCase,
        IOptions<AuthSessionOptions> sessionOptions,
        HttpContext http,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request?.Email) || string.IsNullOrWhiteSpace(request?.Codigo))
        {
            return Results.BadRequest(new { code = "invalid_request", message = "Código inválido ou expirado." });
        }

        var resultado = await useCase.ExecuteAsync(request.Email, request.Codigo, ct);
        if (!resultado.Sucesso || resultado.SessionRawToken is null)
        {
            return Results.BadRequest(new { code = "otp_invalido", message = resultado.MotivoGenerico });
        }

        var maxAge = TimeSpan.FromHours(sessionOptions.Value.AbsoluteExpirationHours);
        http.Response.Cookies.Append(AuthCookie.Name, resultado.SessionRawToken, AuthCookie.BuildOptions(maxAge));

        return Results.Ok(new
        {
            usuario = new { id = resultado.UsuarioId, email = resultado.Email, nome = resultado.Nome, unidadeNegocioId = resultado.UnidadeNegocioId },
        });
    }

    private static async Task<IResult> Logout(ILogoutUseCase useCase, HttpContext http, CancellationToken ct)
    {
        var rawToken = http.Request.Cookies[AuthCookie.Name];
        if (!string.IsNullOrWhiteSpace(rawToken))
        {
            await useCase.ExecuteAsync(rawToken, ct);
        }

        http.Response.Cookies.Delete(AuthCookie.Name, AuthCookie.BuildDeleteOptions());
        return Results.NoContent();
    }

    /// <summary>Protegido pela fallback policy global — se chegar aqui, `HttpContext.User` já está
    /// autenticado. Lê apenas as claims já resolvidas pelo authentication handler, sem I/O adicional
    /// (O1.4.2.1: elimina a segunda consulta de sessão que existia antes desta etapa).</summary>
    private static IResult Me(ClaimsPrincipal user)
    {
        var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(idClaim, out var usuarioId))
        {
            return Results.Unauthorized();
        }

        var email = user.FindFirst(ClaimTypes.Email)?.Value;
        var nome = user.FindFirst(ClaimTypes.Name)?.Value;
        var unidadeNegocioIdClaim = user.FindFirst("unidade_negocio_id")?.Value;
        if (email is null || nome is null || !Guid.TryParse(unidadeNegocioIdClaim, out var unidadeNegocioId))
        {
            // Esquema de Development não carrega estas claims — sem dados de negócio suficientes
            // para responder /me de forma útil fora do fluxo real de sessão.
            return Results.Unauthorized();
        }

        // O1.5 — as permissões efetivas acompanham a identidade para que o frontend possa REFLETIR o
        // acesso (esconder menu/ação). Isto é exclusivamente UX: o backend continua sendo a única
        // barreira, e cada endpoint protegido revalida a permissão por policy independentemente do que o
        // cliente faça com esta lista.
        var permissoes = user.FindAll(Authorization.RbacClaims.Permissao).Select(x => x.Value).ToArray();

        // Gate Final da Onda 1 — exclusivamente informativo para o frontend refletir a UI (ex.: seletor
        // de Unidade de Negócio para o Administrador Sênior). O backend nunca confia neste valor de
        // volta: cada endpoint administrativo revalida o escopo a partir da própria sessão.
        var escopoAdministrativo = user.FindFirst(Authorization.RbacClaims.EscopoAdministrativo)?.Value
            ?? Domain.Identity.EscopoAdministrativo.Negocio.ToString();

        return Results.Ok(new
        {
            usuario = new { id = usuarioId, email, nome, unidadeNegocioId, permissoes, escopoAdministrativo },
        });
    }
}
