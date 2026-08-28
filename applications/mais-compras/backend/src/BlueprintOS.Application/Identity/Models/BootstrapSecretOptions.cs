namespace BlueprintOS.Application.Identity.Models;

/// <summary>Bootstrap Secret (security-design-auth-o1.4.md §20.4; Work Order O1.4.3, seção 9). Vive
/// exclusivamente em configuração/secret manager (User Secrets em Development, secret manager corporativo em
/// Homologação/Produção) — nunca persistido em banco, nunca hasheado (não há necessidade: o único lugar onde
/// o valor "em claro" existe é a configuração, já protegida pelo secret manager). A validação fail-closed
/// (<c>IValidateOptions&lt;BootstrapSecretOptions&gt;</c>) vive em <c>Infrastructure/Identity</c> — mesmo
/// local de <c>CorporateOtpEmailSenderOptionsValidator</c> — por depender de <c>IHostEnvironment</c>, não
/// referenciado por este projeto (Application).</summary>
public sealed class BootstrapSecretOptions
{
    public const string SectionName = "Bootstrap";

    public string? Secret { get; set; }
}
