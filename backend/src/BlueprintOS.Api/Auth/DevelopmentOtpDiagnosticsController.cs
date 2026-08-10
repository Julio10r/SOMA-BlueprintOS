using System.Net;
using BlueprintOS.Api.Identity;
using Microsoft.Extensions.Hosting;

namespace BlueprintOS.Api.Auth;

/// <summary>Mecanismo de diagnóstico exclusivo de Development, isolado do fluxo normal de autenticação
/// (security-design-auth-o1.4.md, §17.5). Só é mapeado quando <c>IHostEnvironment.IsDevelopment()</c> —
/// nunca existe em Staging/Production — mas não confia apenas nisso (O1.4.2.1, Etapa 4): o handler
/// verifica <c>IHostEnvironment.IsDevelopment()</c> novamente, de forma independente, como segunda
/// barreira caso este mapeamento seja movido/reaproveitado incorretamente no futuro.
///
/// Confia exclusivamente em <c>HttpContext.Connection.RemoteIpAddress</c> — nenhum forwarded header
/// (<c>X-Forwarded-For</c>, <c>Forwarded</c>, <c>Host</c>) é lido ou honrado, e
/// <c>UseForwardedHeaders()</c> não está registrado em <c>Program.cs</c>; portanto o valor observado
/// aqui é sempre o peer TCP real, nunca algo que um cliente possa forjar por header.
///
/// Isto NÃO é suportado através de proxy reverso, túnel, ngrok, acesso remoto ou rede compartilhada —
/// mesmo em Development. Se este processo algum dia rodar atrás de um proxy no mesmo host (comum em
/// topologias de produção, e este projeto já tentou expor o backend via túnel ngrok no passado —
/// PROJECT_STATE.md), todo tráfego passaria a chegar como loopback e este mecanismo deveria ser
/// desabilitado, não adaptado para confiar em headers de proxy. Na dúvida sobre a origem: nega
/// (fail-closed) — é exatamente o que o código abaixo já faz.</summary>
public static class DevelopmentOtpDiagnosticsController
{
    public static IEndpointRouteBuilder MapDevelopmentOtpDiagnostics(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/dev/otp", GetLastOtp).AllowAnonymous();
        return endpoints;
    }

    internal static IResult GetLastOtp(string? email, HttpContext http, DevelopmentOtpInspectionStore store, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return Results.NotFound();
        }

        var remoteIp = http.Connection.RemoteIpAddress;
        if (remoteIp is null || !IPAddress.IsLoopback(remoteIp))
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return Results.BadRequest(new { code = "invalid_request", message = "email é obrigatório." });
        }

        return store.TryTakeOnce(email, out var codigo)
            ? Results.Ok(new { codigo })
            : Results.NotFound();
    }
}
