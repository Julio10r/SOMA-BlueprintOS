using BlueprintOS.Application.Identity.Contracts;
using Microsoft.AspNetCore.DataProtection;

namespace BlueprintOS.Infrastructure.Identity;

/// <summary>Implementação real de <see cref="ISegredoProtector"/> (O1.11) usando
/// <c>Microsoft.AspNetCore.DataProtection</c> com um propósito nomeado exclusivo por domínio, recebido no
/// construtor — nunca reaproveitando o mesmo propósito para dois domínios diferentes (DEB-16, Gate Final
/// pós-O1.14; ver as duas especializações concretas abaixo, cada uma com seu próprio propósito).
///
/// Isolamento de chave por propósito é uma garantia do próprio <c>IDataProtectionProvider</c>
/// (<c>CreateProtector</c>): dois propósitos distintos derivam sub-chaves distintas do mesmo par de chaves
/// mestras, de modo que o texto cifrado por um propósito não é decifrável por um protector criado com
/// outro propósito — ainda que ambos compartilhem a mesma infraestrutura de <c>DataProtection</c>
/// (mesmo repositório de chaves).</summary>
public abstract class DataProtectionSegredoProtector : ISegredoProtector
{
    private readonly IDataProtector _protector;

    protected DataProtectionSegredoProtector(IDataProtectionProvider provider, string proposito)
    {
        _protector = provider.CreateProtector(proposito);
    }

    public string Proteger(string valorEmClaro) => _protector.Protect(valorEmClaro);

    public string Desproteger(string valorProtegido) => _protector.Unprotect(valorProtegido);
}

/// <summary>Propósito exclusivo dos parâmetros protegidos de <c>IdentityProvider</c> — nunca compartilhado
/// com <see cref="ConfiguracaoErpSegredoProtector"/> (DEB-16).</summary>
public sealed class IdentityProviderSegredoProtector(IDataProtectionProvider provider)
    : DataProtectionSegredoProtector(provider, Proposito), IIdentityProviderSegredoProtector
{
    private const string Proposito = "BlueprintOS.IdentityProvider.Parametros.v1";
}

/// <summary>Propósito exclusivo dos parâmetros de conexão protegidos de <c>ConfiguracaoErp</c> — nunca
/// compartilhado com <see cref="IdentityProviderSegredoProtector"/> (DEB-16).</summary>
public sealed class ConfiguracaoErpSegredoProtector(IDataProtectionProvider provider)
    : DataProtectionSegredoProtector(provider, Proposito), IConfiguracaoErpSegredoProtector
{
    private const string Proposito = "BlueprintOS.ConfiguracaoErp.ParametrosConexao.v1";
}
