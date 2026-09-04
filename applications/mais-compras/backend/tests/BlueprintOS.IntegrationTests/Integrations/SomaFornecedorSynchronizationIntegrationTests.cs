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
        var fornecedores = await reader.BuscarFornecedoresAsync(0, 1);

        Assert.True(fornecedores.Count <= 1);
        Assert.All(fornecedores, fornecedor =>
        {
            Assert.Equal("SOMA_DESENV", fornecedor.ErpSistema);
            Assert.False(string.IsNullOrWhiteSpace(fornecedor.ErpFornecedorId));
            Assert.False(string.IsNullOrWhiteSpace(fornecedor.Dados.DocumentoFiscal));
        });
    }

    /// <summary>B3 — Bloco 5A.7: o teste acima usa uma amostra de 1 registro e não teria como detectar um
    /// JOIN quebrado entre <c>FORNECEDORES</c> e <c>CADASTRO_CLI_FOR</c> (`SomaFornecedorReader.TableShape.FromClause`)
    /// — um LEFT JOIN cujo ON está incorreto/removido produz <c>NomeFantasia</c> nulo silenciosamente, sem
    /// lançar exceção, e o teste anterior passaria do mesmo jeito. Usa uma amostra real maior (50 registros)
    /// e exige que a maioria resolva <c>NomeFantasia</c> (`CADASTRO_CLI_FOR.NOME_CLIFOR`) e sempre resolva
    /// <c>RazaoSocial</c> (`COALESCE(CADASTRO_CLI_FOR.RAZAO_SOCIAL, FORNECEDORES.FORNECEDOR)`) — a
    /// correspondência real entre as duas tabelas é praticamente universal (investigação B3-Bloco5A, 0
    /// duplicidade em 27.754 fornecedores reais); uma taxa de resolução próxima de zero é o sintoma direto
    /// de um JOIN quebrado, não de dado ausente no Linx.</summary>
    [Fact]
    public async Task Reader_Should_Resolve_Join_Between_Fornecedores_And_CadastroCliFor()
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
        var fornecedores = await reader.BuscarFornecedoresAsync(0, 50);

        Assert.NotEmpty(fornecedores);
        Assert.All(fornecedores, fornecedor =>
        {
            Assert.Equal("SOMA_DESENV", fornecedor.ErpSistema);
            Assert.False(string.IsNullOrWhiteSpace(fornecedor.ErpFornecedorId));
            Assert.False(string.IsNullOrWhiteSpace(fornecedor.Dados.RazaoSocial));
        });

        var comNomeFantasiaResolvido = fornecedores.Count(f => !string.IsNullOrWhiteSpace(f.Dados.NomeFantasia));
        var percentualResolvido = (double)comNomeFantasiaResolvido / fornecedores.Count;

        Assert.True(percentualResolvido >= 0.8,
            $"JOIN FORNECEDORES-CADASTRO_CLI_FOR aparenta quebrado: apenas {comNomeFantasiaResolvido}/{fornecedores.Count} " +
            $"({percentualResolvido:P0}) registros resolveram NomeFantasia (NOME_CLIFOR). Esperado >= 80% com base na " +
            "correspondência real investigada (B3-Bloco5A).");
    }
}
