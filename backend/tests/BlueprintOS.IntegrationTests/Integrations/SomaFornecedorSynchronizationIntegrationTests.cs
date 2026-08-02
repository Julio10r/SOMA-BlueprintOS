using BlueprintOS.Infrastructure.Integrations.ERP.Soma;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlueprintOS.IntegrationTests.Integrations;

public sealed class SomaFornecedorSynchronizationIntegrationTests
{
    [Fact]
    public async Task Reader_Should_Connect_To_SomaDesenv_When_Vpn_And_Secrets_Are_Available()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("ErpConnection");
        if (string.IsNullOrWhiteSpace(connectionString) || connectionString.StartsWith("__SET_", StringComparison.Ordinal))
        {
            return;
        }

        var reader = new SomaFornecedorReader(configuration, NullLogger<SomaFornecedorReader>.Instance);
        var fornecedores = await reader.BuscarFornecedoresAsync(1);

        Assert.True(fornecedores.Count <= 1);
        Assert.All(fornecedores, fornecedor =>
        {
            Assert.Equal("SOMA_DESENV", fornecedor.ErpSistema);
            Assert.False(string.IsNullOrWhiteSpace(fornecedor.ErpFornecedorId));
            Assert.False(string.IsNullOrWhiteSpace(fornecedor.Dados.DocumentoFiscal));
        });
    }
}
