namespace BlueprintOS.Api.Identity;

/// <summary>Cookie próprio da sessão de Bootstrap — nome distinto de <see cref="AuthCookie.Name"/> (Work
/// Order O1.4.3, seção 8: "nunca AuthCookie.Name"), mesmas flags de segurança
/// (HttpOnly/Secure/SameSite=Strict).</summary>
public static class BootstrapCookie
{
    public const string Name = "mc_bootstrap_sid";

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
