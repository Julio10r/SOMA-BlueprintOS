using System.Security.Cryptography;

namespace BlueprintOS.Application.Identity.Security;

/// <summary>Identificador opaco de sessão de alta entropia (≥128 bits, CSPRNG) — o valor bruto vai ao
/// cookie do cliente; apenas o hash é armazenado no servidor (security-design-auth-o1.4.md, §1.2).</summary>
public static class OpaqueSessionToken
{
    public static string GenerateRawToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string Hash(string rawToken) =>
        Convert.ToBase64String(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken)));
}
