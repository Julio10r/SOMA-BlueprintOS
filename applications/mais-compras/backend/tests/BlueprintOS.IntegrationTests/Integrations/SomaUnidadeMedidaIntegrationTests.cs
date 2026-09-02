using BlueprintOS.Infrastructure.Integrations.ERP.Soma;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlueprintOS.IntegrationTests.Integrations;

/// <summary>B3 — Bloco 2: leitura real de Unidades de Medida do SOMA_DESENV. Mesmo padrão de early-return
/// de <c>SomaContaContabilIntegrationTests</c>/<c>SomaFilialCentroCustoIntegrationTests</c>: sem
/// `ConnectionStrings:ErpConnection` configurada (VPN corporativa indisponível neste ambiente), o teste
/// retorna sem falhar o build/test.</summary>
public sealed class SomaUnidadeMedidaIntegrationTests
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
    public async Task UnidadeMedidaReader_Should_Connect_To_SomaDesenv_When_Vpn_And_Secrets_Are_Available()
    {
        var configuration = BuildConfiguration();
        if (ErpIndisponivel(configuration)) return;

        var reader = new SomaUnidadeMedidaReader(configuration, NullLogger<SomaUnidadeMedidaReader>.Instance);
        var unidades = await reader.BuscarUnidadesAsync(0, 1);

        Assert.True(unidades.Count <= 1);
        Assert.All(unidades, unidade => Assert.False(string.IsNullOrWhiteSpace(unidade.CodigoErp)));
    }
}
