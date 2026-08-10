using System.Security.Cryptography;
using System.Text;

namespace BlueprintOS.Application.Identity.Security;

/// <summary>Hash+salt do OTP (nunca texto claro) e verificação em tempo constante — mitiga timing attacks
/// na comparação (security-design-auth-o1.4.md, §2.3/§3.1).</summary>
public static class OtpHasher
{
    public static (string Hash, string Salt) Hash(string codigo)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        var salt = Convert.ToBase64String(saltBytes);
        var hash = ComputeHash(codigo, saltBytes);
        return (hash, salt);
    }

    public static bool Verify(string codigo, string hash, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var computed = ComputeHash(codigo, saltBytes);
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(computed),
            Convert.FromBase64String(hash));
    }

    private static string ComputeHash(string codigo, byte[] saltBytes)
    {
        var input = saltBytes.Concat(Encoding.UTF8.GetBytes(codigo)).ToArray();
        var hashBytes = SHA256.HashData(input);
        return Convert.ToBase64String(hashBytes);
    }
}
