using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace BlueprintOS.Api.Auth;

/// <summary>Rate limiting por IP em nível de middleware (security-design-auth-o1.4.md, §3.3) — resistente
/// a bypass por chamada direta ao endpoint, pois roda antes do handler, não como lógica de aplicação.</summary>
public static class RateLimitingPolicies
{
    public const string OtpRequest = "otp-request";
    public const string OtpVerify = "otp-verify";

    /// <summary>Política própria de <c>POST /bootstrap/iniciar</c> (security-design-auth-o1.4.md §20.16;
    /// Work Order O1.4.3, seção 15/16) — mesmo limite agressivo por IP de <see cref="OtpRequest"/> (3/15min),
    /// nunca superior, pois este endpoint protege o Bootstrap Secret (o alvo mais valioso de brute force do
    /// sistema). O limite complementar por e-mail candidato normalizado é aplicado dentro do próprio caso de
    /// uso (<c>IniciarBootstrapUseCase</c>, reaproveitando <c>OtpRequestThrottle</c>) — este limite por IP é
    /// uma camada independente, em nível de middleware, resistente a chamada direta ao endpoint.</summary>
    public const string BootstrapIniciar = "bootstrap-iniciar";

    /// <summary>Política de <c>POST /bootstrap/concluir</c> (O1.4.3.2; Work Order O1.4.3, seção 15) — limite
    /// adicional por IP para conter tentativas de força bruta sobre validação de payload, complementar à
    /// exigência de <c>BootstrapSessao</c> válida (uso único) já imposta pela política de autorização.</summary>
    public const string BootstrapConcluir = "bootstrap-concluir";

    public static void Configure(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddPolicy(OtpRequest, httpContext => RateLimitPartition.GetFixedWindowLimiter(
            PartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(15),
                PermitLimit = 3,
                QueueLimit = 0,
            }));

        options.AddPolicy(OtpVerify, httpContext => RateLimitPartition.GetFixedWindowLimiter(
            PartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(15),
                PermitLimit = 10,
                QueueLimit = 0,
            }));

        options.AddPolicy(BootstrapIniciar, httpContext => RateLimitPartition.GetFixedWindowLimiter(
            PartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(15),
                PermitLimit = 3,
                QueueLimit = 0,
            }));

        options.AddPolicy(BootstrapConcluir, httpContext => RateLimitPartition.GetFixedWindowLimiter(
            PartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(15),
                PermitLimit = 3,
                QueueLimit = 0,
            }));
    }

    private static string PartitionKey(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
