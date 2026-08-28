using System.Security.Cryptography;
using System.Text;

namespace BlueprintOS.Application.Identity.Security;

/// <summary>Identificador determinístico e não reversível do e-mail, exclusivamente para correlacionar
/// eventos de auditoria (O1.4.2.1, Achado G) — nunca o e-mail em claro, nunca o OTP/sessão/segredo.
/// Determinístico (sem salt) de propósito: o mesmo e-mail deve produzir o mesmo identificador em
/// diferentes linhas de log para permitir investigação, sem exigir acesso ao banco.</summary>
public static class EmailAuditHasher
{
    public static string Hash(string emailNormalizado)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(emailNormalizado));
        return Convert.ToBase64String(bytes)[..12];
    }
}
