namespace BlueprintOS.Api.Middleware;

/// <summary>Headers de segurança obrigatórios conforme security-design-auth-o1.4.md §3.4/§8 — nenhum
/// destes existia antes de O1.4.2.</summary>
public static class SecurityHeadersMiddleware
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["X-Frame-Options"] = "DENY";
            headers["Content-Security-Policy"] = "default-src 'self'; frame-ancestors 'none'";

            if (context.Request.Path.StartsWithSegments("/auth") || context.Request.Path.StartsWithSegments("/dev"))
            {
                headers["Cache-Control"] = "no-store";
            }

            await next();
        });
    }
}
