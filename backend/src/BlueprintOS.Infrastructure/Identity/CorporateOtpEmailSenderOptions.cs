using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Identity;

/// <summary>Configuração do provider corporativo de OTP (Microsoft Graph/SMTP corporativo), a ser
/// preenchida pela Infra antes de Homologação (Authentication Infra Readiness Gate,
/// security-design-auth-o1.4.md §17.7). Nenhum valor real é definido por esta sprint.</summary>
public sealed class CorporateOtpEmailSenderOptions
{
    public const string SectionName = "Identity:Otp:Corporate";

    public string? Provider { get; set; }
}

/// <summary>Fora de Development, a ausência de um provider corporativo válido deve falhar o startup
/// (fail-closed) — nunca "log critical + continuar executando" (security-design-auth-o1.4.md §17.4).</summary>
public sealed class CorporateOtpEmailSenderOptionsValidator : IValidateOptions<CorporateOtpEmailSenderOptions>
{
    public ValidateOptionsResult Validate(string? name, CorporateOtpEmailSenderOptions options) =>
        string.IsNullOrWhiteSpace(options.Provider)
            ? ValidateOptionsResult.Fail(
                "Identity:Otp:Corporate:Provider não configurado. Fora de Development, um provider " +
                "corporativo de OTP válido é obrigatório antes da aplicação aceitar tráfego " +
                "(Authentication Infra Readiness Gate, security-design-auth-o1.4.md §17.7).")
            : ValidateOptionsResult.Success;
}
