namespace BlueprintOS.Api.Identity;

/// <summary>Nome e flags do cookie de sessão — sem informação interna no nome, sem conteúdo sensível
/// no valor (apenas o identificador opaco). HttpOnly/Secure/SameSite=Strict conforme
/// security-design-auth-o1.4.md §3.2/§17.</summary>
public static class AuthCookie
{
    public const string Name = "mc_sid";

    public static CookieOptions BuildOptions(TimeSpan maxAge) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
        MaxAge = maxAge,
    };

    public static CookieOptions BuildDeleteOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/",
    };
}
