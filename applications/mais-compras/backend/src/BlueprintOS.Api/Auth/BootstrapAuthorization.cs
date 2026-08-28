using BlueprintOS.Application.Identity.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Api.Auth;

public static class BootstrapAuthorizationPolicies
{
    public const string BootstrapAuthenticated = "BootstrapAuthenticated";
}

/// <summary>Checagem adicional exigida pela política <see cref="BootstrapAuthorizationPolicies.BootstrapAuthenticated"/>
/// (security-design-auth-o1.4.md §20.7; Work Order O1.4.3, seção 8.1): mesmo com uma <c>BootstrapSessao</c>
/// tecnicamente válida (não expirada/usada/revogada — já garantido pelo <see cref="BootstrapSessionAuthenticationHandler"/>),
/// a autorização é negada no instante em que <c>BootstrapEstado.Concluido == true</c>. Implementado como
/// <see cref="IAuthorizationRequirement"/> customizado (não apenas presença de claim) porque exige acesso a
/// repositório — avaliado a cada requisição, nunca cacheado entre requisições.</summary>
public sealed class BootstrapNaoConcluidoRequirement : IAuthorizationRequirement
{
}

public sealed class BootstrapNaoConcluidoAuthorizationHandler(
    IBootstrapEstadoRepository estados,
    ILogger<BootstrapNaoConcluidoAuthorizationHandler> logger)
    : AuthorizationHandler<BootstrapNaoConcluidoRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BootstrapNaoConcluidoRequirement requirement)
    {
        var estado = await estados.ObterAsync(CancellationToken.None);
        if (estado is null || estado.Concluido)
        {
            // Fail-closed: linha ausente é tratada com a mesma severidade de "concluído" (Work Order
            // O1.4.3, seção 12) — nunca autoriza por omissão.
            logger.LogInformation("Autorização de sessão de Bootstrap negada — Bootstrap indisponível/concluído.");
            return;
        }

        context.Succeed(requirement);
    }
}
