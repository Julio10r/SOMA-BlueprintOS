namespace BlueprintOS.Application.Identity.Models;

/// <summary>Allowlist de identidades pré-autorizadas para o Bootstrap (security-design-auth-o1.4.md §20.5;
/// Work Order O1.4.3, seção 10). Vive exclusivamente em configuração/secret manager — nunca em
/// <c>appsettings.json</c> versionado com e-mails reais. Lista ausente/vazia nunca significa "sem
/// restrição": significa que o Bootstrap fica efetivamente indisponível (nenhum e-mail passa a checagem),
/// não um erro de startup.</summary>
public sealed class BootstrapAllowedCandidatesOptions
{
    /// <summary>Chave de configuração exata (Work Order O1.4.3, seção 10): <c>Bootstrap:AllowedCandidateEmails</c>
    /// — um array de strings diretamente nesta chave (não um objeto aninhado), vinculado explicitamente à
    /// propriedade <see cref="Emails"/> na composição raiz (não pelo binder padrão de seção, que exigiria
    /// nomes de propriedade e de chave idênticos).</summary>
    public const string ConfigurationKey = "Bootstrap:AllowedCandidateEmails";

    public string[] Emails { get; set; } = Array.Empty<string>();

    /// <summary>Normalização idêntica à já usada para <c>OtpRequestThrottle</c>/<c>Usuario.Email</c>
    /// (trim + <c>ToLowerInvariant()</c>) — aplicada à configuração na leitura, nunca alterando o array
    /// original em <see cref="Emails"/>.</summary>
    public IReadOnlySet<string> ObterEmailsNormalizados() =>
        Emails
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim().ToLowerInvariant())
            .ToHashSet();

    /// <summary>Fail-closed explícito para e-mail candidato (já normalizado pelo chamador): lista
    /// ausente/vazia nunca autoriza nenhum e-mail — nunca "lista vazia = qualquer um passa" (Work Order
    /// O1.4.3, seção 10).</summary>
    public bool Autoriza(string emailCandidatoNormalizado) =>
        ObterEmailsNormalizados().Contains(emailCandidatoNormalizado);
}
