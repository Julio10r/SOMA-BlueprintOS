using BlueprintOS.Infrastructure.Integrations.ERP.Soma;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlueprintOS.IntegrationTests.Integrations;

/// <summary>O1.7 — leitura real de Filiais/Centros de Custo do SOMA_DESENV. Mesmo padrão de early-return de
/// <c>SomaFornecedorSynchronizationIntegrationTests</c> (B2.1.2): sem `ConnectionStrings:ErpConnection`
/// configurada (VPN corporativa indisponível neste ambiente), o teste retorna sem falhar o build/test.</summary>
public sealed class SomaFilialCentroCustoIntegrationTests
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
    public async Task FilialReader_Should_Connect_To_SomaDesenv_When_Vpn_And_Secrets_Are_Available()
    {
        var configuration = BuildConfiguration();
        if (ErpIndisponivel(configuration)) return;

        var reader = new SomaFilialReader(configuration, NullLogger<SomaFilialReader>.Instance);
        var filiais = await reader.BuscarFiliaisAsync(0, 1);

        Assert.True(filiais.Count <= 1);
        Assert.All(filiais, filial => Assert.False(string.IsNullOrWhiteSpace(filial.CodigoCliFor)));
    }

    [Fact]
    public async Task CentroCustoReader_Should_Connect_To_SomaDesenv_When_Vpn_And_Secrets_Are_Available()
    {
        var configuration = BuildConfiguration();
        if (ErpIndisponivel(configuration)) return;

        var reader = new SomaCentroCustoReader(configuration, NullLogger<SomaCentroCustoReader>.Instance);
        var centrosCusto = await reader.BuscarCentrosCustoAsync(0, 1);

        Assert.True(centrosCusto.Count <= 1);
        Assert.All(centrosCusto, centro => Assert.False(string.IsNullOrWhiteSpace(centro.CodigoErp)));
    }
}
