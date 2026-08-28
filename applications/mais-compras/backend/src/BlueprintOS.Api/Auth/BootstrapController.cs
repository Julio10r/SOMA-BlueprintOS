using System.Security.Claims;
using BlueprintOS.Api.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BlueprintOS.Api.Auth;

/// <summary>Endpoints REST da Fundação Backend do Bootstrap (O1.4.3.1; security-design-auth-o1.4.md §20;
/// Work Order O1.4.3, seções 7/15). Escopo estritamente desta etapa: consulta de estado, início do fluxo
/// (secret + e-mail pré-autorizado) e validação de OTP com criação da sessão de Bootstrap. NÃO inclui
/// <c>POST /bootstrap/concluir</c> (escopo de O1.4.3.2) — nenhuma criação de Unidade de Negócio, Usuário ou
/// vínculo de Administrador Sênior ocorre nesta etapa.
///
/// Anônimos, por exceção explícita, mesmo padrão de <see cref="AuthController"/> (security-design-auth-o1.4.md
/// §20.17): os três endpoints abaixo não podem exigir uma sessão que ainda não existe — protegidos por
/// secret + allowlist + OTP + rate limiting, nunca por <c>AuthorizationOptions.FallbackPolicy</c>.</summary>
public static class BootstrapController
{
    private const string GenericMessage = "Se as informações fornecidas forem válidas, um código foi enviado.";
    private const string GenericOtpInvalidMessage = "Código inválido ou expirado.";

    public static IEndpointRouteBuilder MapBootstrap(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/bootstrap").WithTags("Bootstrap");

        group.MapGet("/estado", GetEstado).AllowAnonymous();

        group.MapPost("/iniciar", Iniciar)
            .RequireRateLimiting(RateLimitingPolicies.BootstrapIniciar)
            .AddEndpointFilter<CsrfHeaderFilter>()
            .AllowAnonymous();

        group.MapPost("/otp/verificar", VerificarOtp)
            .RequireRateLimiting(RateLimitingPolicies.OtpVerify)
            .AddEndpointFilter<CsrfHeaderFilter>()
            .AllowAnonymous();

        // O1.4.3.2 — exige BootstrapSessao válida e Concluido==false (política BootstrapAuthenticated,
        // Work Order O1.4.3, seção 8.1/15); nunca FallbackPolicy nem AllowAnonymous.
        group.MapPost("/concluir", Concluir)
            .RequireRateLimiting(RateLimitingPolicies.BootstrapConcluir)
            .AddEndpointFilter<CsrfHeaderFilter>()
            .RequireAuthorization(BootstrapAuthorizationPolicies.BootstrapAuthenticated);

        return endpoints;
    }

    private static async Task<IResult> GetEstado(IConsultarBootstrapEstadoUseCase useCase, CancellationToken ct)
    {
        var resultado = await useCase.ExecuteAsync(ct);
        return Results.Ok(new { disponivel = resultado.Disponivel });
    }

    /// <summary>Resposta idêntica (200 + mensagem genérica) para secret inválido, e-mail não autorizado e
    /// sucesso — nunca diferenciada ao cliente (§20.6/§20.12). Somente quando o Bootstrap já foi concluído
    /// (ou a linha está ausente/fail-closed) a resposta é 404, indistinguível de rota inexistente — nunca
    /// 403, que confirmaria a existência do endpoint (§20.10).</summary>
    private static async Task<IResult> Iniciar(BootstrapIniciarRequest? request, IIniciarBootstrapUseCase useCase, CancellationToken ct)
    {
        var email = request?.Email;
        var secret = request?.Secret;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(secret))
        {
            // Mesma resposta genérica mesmo para payload incompleto — não deve haver caminho de resposta
            // que sinalize "faltou secret" versus "secret incorreto" ao cliente.
            return Results.Ok(new { message = GenericMessage });
        }

        var resultado = await useCase.ExecuteAsync(email, secret, ct);
        if (!resultado.BootstrapDisponivel)
        {
            return Results.NotFound();
        }

        return Results.Ok(new { message = GenericMessage });
    }

    private static async Task<IResult> VerificarOtp(
        BootstrapOtpVerificarRequest? request,
        IValidarOtpBootstrapUseCase useCase,
        HttpContext http,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request?.Email) || string.IsNullOrWhiteSpace(request?.Codigo))
        {
            return Results.BadRequest(new { code = "invalid_request", message = GenericOtpInvalidMessage });
        }

        var resultado = await useCase.ExecuteAsync(request.Email, request.Codigo, ct);
        if (!resultado.Sucesso || resultado.SessionRawToken is null)
        {
            return Results.BadRequest(new { code = "otp_invalido", message = resultado.MotivoGenerico ?? GenericOtpInvalidMessage });
        }

        var maxAge = BlueprintOS.Domain.Identity.BootstrapSessao.Validade;
        http.Response.Cookies.Append(BootstrapCookie.Name, resultado.SessionRawToken, BootstrapCookie.BuildOptions(maxAge));

        return Results.NoContent();
    }

    /// <summary>Conclusão transacional (O1.4.3.2; Work Order O1.4.3, seção 13). O e-mail do Administrador
    /// Sênior nunca vem do payload — apenas o identificador da própria <c>BootstrapSessao</c>, já
    /// autenticada pela política <c>BootstrapAuthenticated</c>, é usado para obter o e-mail validado por
    /// OTP dentro do caso de uso (seção 13, passo 3).</summary>
    private static async Task<IResult> Concluir(
        BootstrapConcluirRequest? request,
        [FromServices] IConcluirBootstrapUseCase useCase,
        ClaimsPrincipal user,
        HttpContext http,
        CancellationToken ct)
    {
        var sessionIdClaim = user.FindFirst(BootstrapSessionAuthenticationHandler.BootstrapSessionClaimType)?.Value;
        if (!Guid.TryParse(sessionIdClaim, out var bootstrapSessaoId))
        {
            return Results.Unauthorized();
        }

        var unidadeNegocio = new UnidadeNegocioBootstrapPayload(
            request?.UnidadeNegocio?.Id, request?.UnidadeNegocio?.Nome, request?.UnidadeNegocio?.Slug);
        var administrador = new AdministradorSeniorBootstrapPayload(request?.Administrador?.Nome);

        var resultado = await useCase.ExecuteAsync(bootstrapSessaoId, unidadeNegocio, administrador, ct);
        if (!resultado.Sucesso)
        {
            return Results.BadRequest(new { code = "bootstrap_nao_concluido", message = resultado.MotivoGenerico });
        }

        // Uso único: a sessão de Bootstrap não sobrevive à conclusão bem-sucedida — nenhum login automático
        // do Administrador recém-criado (Work Order O1.4.3, seção 15); ele usa o fluxo normal de OTP depois.
        http.Response.Cookies.Delete(BootstrapCookie.Name, BootstrapCookie.BuildDeleteOptions());

        return Results.Ok(new
        {
            usuario = new { id = resultado.UsuarioId, email = resultado.Email, nome = resultado.Nome },
            unidadeNegocioId = resultado.UnidadeNegocioId,
        });
    }
}
