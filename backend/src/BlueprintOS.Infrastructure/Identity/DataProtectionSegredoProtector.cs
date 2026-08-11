using BlueprintOS.Application.Identity.Contracts;
using Microsoft.AspNetCore.DataProtection;

namespace BlueprintOS.Infrastructure.Identity;

/// <summary>Implementação real de <see cref="ISegredoProtector"/> (O1.11) usando
/// <c>Microsoft.AspNetCore.DataProtection</c> com um propósito nomeado exclusivo desta finalidade —
/// segue a mesma família de propósito de outras cifragens do +Compras, isolando a chave por uso
/// (nunca reaproveitando o protetor de outro subsistema).</summary>
public sealed class DataProtectionSegredoProtector : ISegredoProtector
{
    private const string Proposito = "BlueprintOS.ConfiguracaoTecnica.Segredos.v1";

    private readonly IDataProtector _protector;

    public DataProtectionSegredoProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(Proposito);
    }

    public string Proteger(string valorEmClaro) => _protector.Protect(valorEmClaro);

    public string Desproteger(string valorProtegido) => _protector.Unprotect(valorProtegido);
}
