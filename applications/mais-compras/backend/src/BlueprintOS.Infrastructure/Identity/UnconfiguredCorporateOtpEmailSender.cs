using BlueprintOS.Application.Identity.Contracts;

namespace BlueprintOS.Infrastructure.Identity;

/// <summary>Placeholder registrado fora de Development enquanto o provider corporativo real (Microsoft
/// Graph/SMTP corporativo) não é implementado. Nunca deveria ser efetivamente invocado, porque o
/// <c>ValidateOnStart()</c> de <see cref="CorporateOtpEmailSenderOptions"/> já impede a aplicação de subir
/// sem configuração válida — esta exceção é uma segunda camada de fail-closed, não a primeira.</summary>
public sealed class UnconfiguredCorporateOtpEmailSender : IOtpEmailSender
{
    public Task<OtpEmailSendResult> SendAsync(string email, string codigo, CancellationToken ct) =>
        throw new InvalidOperationException(
            "Nenhum provider corporativo de OTP está implementado/configurado. Startup deveria ter falhado antes deste ponto.");
}
