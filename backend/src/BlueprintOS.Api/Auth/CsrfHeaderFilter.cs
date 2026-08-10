namespace BlueprintOS.Api.Auth;

/// <summary>Defesa em profundidade contra CSRF além de `SameSite=Strict` (security-design-auth-o1.4.md,
/// §3.5) — exige um header customizado que apenas JavaScript same-origin consegue anexar; um POST de
/// formulário cross-site simples não pode incluí-lo.</summary>
public sealed class CsrfHeaderFilter : IEndpointFilter
{
    public const string HeaderName = "X-MaisCompras-Csrf";

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!context.HttpContext.Request.Headers.ContainsKey(HeaderName))
        {
            return ValueTask.FromResult<object?>(Results.StatusCode(StatusCodes.Status403Forbidden));
        }

        return next(context);
    }
}
