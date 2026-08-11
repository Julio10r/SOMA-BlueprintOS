namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Abstração de cifragem de segredos (O1.11: parâmetros de <c>IdentityProvider</c> e
/// <c>ConfiguracaoErp</c>) — mantém a camada de Aplicação livre de dependência direta de
/// <c>Microsoft.AspNetCore.DataProtection</c>. A implementação real (Infraestrutura) usa
/// <c>IDataProtectionProvider</c> com um propósito nomeado por finalidade. Nunca logar nem devolver o
/// valor em claro depois de protegido.</summary>
public interface ISegredoProtector
{
    string Proteger(string valorEmClaro);

    string Desproteger(string valorProtegido);
}
