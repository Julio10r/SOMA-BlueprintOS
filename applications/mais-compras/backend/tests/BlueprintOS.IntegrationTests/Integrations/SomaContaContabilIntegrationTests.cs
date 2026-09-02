using BlueprintOS.Infrastructure.Integrations.ERP.Soma;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlueprintOS.IntegrationTests.Integrations;

/// <summary>B3 — Bloco 1: leitura real de Contas Contábeis do SOMA_DESENV. Mesmo padrão de early-return de
/// <c>SomaFilialCentroCustoIntegrationTests</c> (O1.7): sem `ConnectionStrings:ErpConnection` configurada
/// (VPN corporativa indisponível neste ambiente), o teste retorna sem falhar o build/test.</summary>
public sealed class SomaContaContabilIntegrationTests
{
    private static IConfiguration BuildConfiguration() => new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

    private static bool ErpIndisponivel(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ErpConnection");
        return string.IsNullOrWhiteSpace(connectionString) || connectionString.StartsWith("__SET_", StringComparison.Ordinal);
    }

    [Fact]
    public async Task ContaContabilReader_Should_Connect_To_SomaDesenv_When_Vpn_And_Secrets_Are_Available()
    {
        var configuration = BuildConfiguration();
        if (ErpIndisponivel(configuration)) return;

        var reader = new SomaContaContabilReader(configuration, NullLogger<SomaContaContabilReader>.Instance);
        var contas = await reader.BuscarContasContabeisAsync(0, 1);

        Assert.True(contas.Count <= 1);
        Assert.All(contas, conta => Assert.False(string.IsNullOrWhiteSpace(conta.CodigoErp)));
    }
}
