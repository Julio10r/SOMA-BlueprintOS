namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Responsabilidade exclusiva: envio do OTP de autenticação. O domínio/aplicação não conhece
/// SMTP, Microsoft Graph, Office 365 ou Entra ID — apenas este contrato (security-design-auth-o1.4.md, §17.2).</summary>
public interface IOtpEmailSender
{
    Task<OtpEmailSendResult> SendAsync(string email, string codigo, CancellationToken ct);
}

public sealed record OtpEmailSendResult(bool Success, string? FailureReason);
