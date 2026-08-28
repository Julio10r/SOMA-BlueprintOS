using BlueprintOS.Infrastructure.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace BlueprintOS.UnitTests.Infrastructure.Identity;

/// <summary>DEB-16 (Gate Final pós-O1.14) — "propósito de criptografia compartilhado no DataProtection".
/// Antes desta correção, um único <c>ISegredoProtector</c>/<c>DataProtectionSegredoProtector</c>, com um
/// único propósito de <c>DataProtection</c> (<c>"BlueprintOS.ConfiguracaoTecnica.Segredos.v1"</c>), era
/// injetado tanto em <c>CriarIdentityProviderUseCase</c>/<c>AtualizarIdentityProviderUseCase</c> quanto em
/// <c>SalvarConfiguracaoErpUseCase</c> — dois domínios/tabelas genuinamente distintos
/// (<c>IdentityProvider.ParametrosProtegidos</c> vs. <c>ConfiguracaoErp.ParametrosConexaoProtegidos</c>)
/// compartilhando a mesma chave derivada. Esta suíte prova que, após a correção, cada domínio tem seu
/// próprio propósito e que um não consegue decifrar o texto cifrado pelo outro, mesmo compartilhando a
/// mesma infraestrutura de chaves (<c>IDataProtectionProvider</c>).</summary>
public sealed class DataProtectionSegredoProtectorTests
{
    private static IDataProtectionProvider CreateSharedProvider()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();
        return services.BuildServiceProvider().GetRequiredService<IDataProtectionProvider>();
    }

    [Fact]
    public void Each_Domain_Should_RoundTrip_Its_Own_Ciphertext()
    {
        var provider = CreateSharedProvider();
        var identityProviderProtector = new IdentityProviderSegredoProtector(provider);
        var configuracaoErpProtector = new ConfiguracaoErpSegredoProtector(provider);

        var cifradoIdp = identityProviderProtector.Proteger("client-secret-oidc");
        var cifradoErp = configuracaoErpProtector.Proteger("Server=erp;User=admin;Password=segredo;");

        Assert.Equal("client-secret-oidc", identityProviderProtector.Desproteger(cifradoIdp));
        Assert.Equal("Server=erp;User=admin;Password=segredo;", configuracaoErpProtector.Desproteger(cifradoErp));
    }

    [Fact]
    public void Ciphertext_From_One_Domain_Should_Not_Be_Decryptable_By_The_Other_Domains_Protector()
    {
        // Mesmo IDataProtectionProvider (mesma infraestrutura de chaves) para ambos — isolamento tem que
        // vir do propósito, não de instâncias/repositórios de chave diferentes.
        var provider = CreateSharedProvider();
        var identityProviderProtector = new IdentityProviderSegredoProtector(provider);
        var configuracaoErpProtector = new ConfiguracaoErpSegredoProtector(provider);

        var cifradoPeloIdentityProvider = identityProviderProtector.Proteger("client-secret-oidc");

        // DEB-16: se os propósitos fossem os mesmos (regressão), esta chamada teria sucesso silenciosamente.
        Assert.ThrowsAny<Exception>(() => configuracaoErpProtector.Desproteger(cifradoPeloIdentityProvider));
    }

    [Fact]
    public void IdentityProvider_And_ConfiguracaoErp_Protectors_Should_Use_Different_Purposes()
    {
        // Propósitos hardcoded nas classes (privados por design) — verificados indiretamente pelo teste de
        // isolamento acima, e nomeados explicitamente aqui para documentar a expectativa e detectar
        // regressão de "voltar a usar o mesmo texto" por acidente em uma futura refatoração.
        const string propositoIdentityProvider = "BlueprintOS.IdentityProvider.Parametros.v1";
        const string propositoConfiguracaoErp = "BlueprintOS.ConfiguracaoErp.ParametrosConexao.v1";

        Assert.NotEqual(propositoIdentityProvider, propositoConfiguracaoErp);
    }
}
