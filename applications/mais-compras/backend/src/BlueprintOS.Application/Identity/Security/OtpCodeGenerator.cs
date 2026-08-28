using System.Security.Cryptography;

namespace BlueprintOS.Application.Identity.Security;

/// <summary>Gera códigos OTP de 6 dígitos numéricos usando CSPRNG (security-design-auth-o1.4.md, §3.1).</summary>
public static class OtpCodeGenerator
{
    public static string Generate() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
}
