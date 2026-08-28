using BlueprintOS.Application.Identity.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Identity;

/// <summary>Fail-closed incondicional fora de Development (Work Order O1.4.3, seção 9 — "Refinamento
/// necessário": a validação de startup não tem acesso trivial ao banco/<c>BootstrapEstado.Concluido</c> no
/// momento do boot; valida-se apenas a presença do secret sempre que o ambiente não for Development, o que
/// é mais simples e mais seguro do que tentar consultar o banco durante <c>ValidateOnStart()</c> — aceitando
/// que, em ambientes onde o Bootstrap já foi concluído há muito tempo, o secret configurado continua
/// tecnicamente exigido no startup, mas nunca é lido em runtime, security-design-auth-o1.4.md §20.10). Mesmo
/// padrão já usado por <see cref="CorporateOtpEmailSenderOptionsValidator"/>.</summary>
public sealed class BootstrapSecretOptionsValidator(IHostEnvironment environment) : IValidateOptions<BootstrapSecretOptions>
{
    public ValidateOptionsResult Validate(string? name, BootstrapSecretOptions options)
    {
        if (environment.IsDevelopment())
        {
            // Em Development, o secret é opcional apenas se o Bootstrap já tiver sido concluído — esta
            // validação de startup não tem acesso ao banco (BootstrapEstado.Concluido); portanto, aceita a
            // ausência aqui, e o endpoint /bootstrap/iniciar responde a mesma rejeição genérica usada para
            // secret incorreto, nunca aceitando um secret vazio como "sempre válido" (Work Order O1.4.3,
            // seção 9 — a rejeição em runtime é garantida por IniciarBootstrapUseCase.SecretEhValido, que
            // trata secret configurado vazio como sempre inválido).
            return ValidateOptionsResult.Success;
        }

        return string.IsNullOrWhiteSpace(options.Secret)
            ? ValidateOptionsResult.Fail(
                "Bootstrap:Secret não configurado. Fora de Development, um Bootstrap Secret válido é " +
                "obrigatório antes da aplicação aceitar tráfego (security-design-auth-o1.4.md §20.4; " +
                "Work Order O1.4.3, seção 9).")
            : ValidateOptionsResult.Success;
    }
}
