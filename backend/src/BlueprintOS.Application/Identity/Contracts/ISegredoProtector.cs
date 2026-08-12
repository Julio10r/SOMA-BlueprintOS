namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Abstração de cifragem de segredos (O1.11: parâmetros de <c>IdentityProvider</c> e
/// <c>ConfiguracaoErp</c>) — mantém a camada de Aplicação livre de dependência direta de
/// <c>Microsoft.AspNetCore.DataProtection</c>. A implementação real (Infraestrutura) usa
/// <c>IDataProtectionProvider</c> com um propósito nomeado por finalidade. Nunca logar nem devolver o
/// valor em claro depois de protegido.
///
/// Nunca injetado diretamente — apenas via as especializações por domínio abaixo
/// (<see cref="IIdentityProviderSegredoProtector"/>/<see cref="IConfiguracaoErpSegredoProtector"/>).
/// Cada uma é resolvida por um propósito de <c>DataProtection</c> distinto (DEB-16, Gate Final
/// pós-O1.14): antes desta correção, um único <c>ISegredoProtector</c>/propósito era compartilhado entre
/// os dois domínios — dados de um poderiam, em teoria, ser descifrados por código que só deveria ter
/// acesso ao outro.</summary>
public interface ISegredoProtector
{
    string Proteger(string valorEmClaro);

    string Desproteger(string valorProtegido);
}

/// <summary>Especialização de <see cref="ISegredoProtector"/> exclusiva dos parâmetros protegidos de
/// <c>IdentityProvider</c> (ex.: client secret de OIDC/SAML) — propósito de <c>DataProtection</c> próprio,
/// nunca compartilhado com <see cref="IConfiguracaoErpSegredoProtector"/> (DEB-16).</summary>
public interface IIdentityProviderSegredoProtector : ISegredoProtector;

/// <summary>Especialização de <see cref="ISegredoProtector"/> exclusiva dos parâmetros de conexão
/// protegidos de <c>ConfiguracaoErp</c> (ex.: credenciais de conexão com o ERP) — propósito de
/// <c>DataProtection</c> próprio, nunca compartilhado com <see cref="IIdentityProviderSegredoProtector"/>
/// (DEB-16).</summary>
public interface IConfiguracaoErpSegredoProtector : ISegredoProtector;
