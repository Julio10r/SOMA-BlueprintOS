namespace BlueprintOS.Api.Auth;

/// <summary>Defesa em profundidade contra CSRF além de `SameSite=Strict` (security-design-auth-o1.4.md,
/// §3.5) — exige um header customizado que apenas JavaScript same-origin consegue anexar; um POST de
/// formulário cross-site simples não pode incluí-lo.</summary>
public sealed class CsrfHeaderFilter : IEndpointFilter
{
    public const string HeaderName = "X-MaisCompras-Csrf";

    /// <summary>Métodos seguros (RFC 9110): não alteram estado, portanto não são alvo de CSRF. Exigir o
    /// header neles não acrescentaria segurança e impediria aplicar o filtro no nível de um
    /// <c>MapGroup</c> — que é o que garante que um endpoint novo nasça protegido em vez de depender de
    /// alguém lembrar de anexar o filtro (O1.5). Nenhum endpoint pré-existente muda de comportamento: o
    /// filtro só estava anexado a rotas POST.</summary>
    private static bool EhMetodoSeguro(string metodo) =>
        HttpMethods.IsGet(metodo) || HttpMethods.IsHead(metodo) || HttpMethods.IsOptions(metodo) || HttpMethods.IsTrace(metodo);

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (EhMetodoSeguro(context.HttpContext.Request.Method))
        {
            return next(context);
        }

        if (!context.HttpContext.Request.Headers.ContainsKey(HeaderName))
        {
            return ValueTask.FromResult<object?>(Results.StatusCode(StatusCodes.Status403Forbidden));
        }

        return next(context);
    }
}
