using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;

namespace BlueprintOS.UnitTests.Infrastructure.Persistence.Repositories;

/// <summary>Regressão B2.9: <see cref="ResolvedorBusinessUnit"/> deriva a chave de ErpIntegration:BusinessUnits
/// a partir de <c>UnidadeNegocio.Slug.ToUpperInvariant()</c> — não do nome de exibição da BU. A única
/// Unidade de Negócio real hoje (Grupo Soma) tem slug "grupo-soma", nunca "SOMA": sem a chave "GRUPO-SOMA"
/// em appsettings.json, todo cadastro/atualização feito por um usuário real (não a identidade de
/// desenvolvimento) falha ao garantir o fornecedor no ERP e fica preso em StatusSincronizacao=Pendente —
/// bug real encontrado em validação E2E (13/08/2026), corrigido adicionando a chave ausente.</summary>
public sealed class GarantirFornecedorErpAdapterResolverTests
{
    [Theory]
    [InlineData("DEFAULT")]
    [InlineData("SOMA")]
    [InlineData("GRUPO-SOMA")]
    public void Resolver_Should_Resolve_Erp_For_Every_BusinessUnit_Key_Configured_In_AppSettings(string businessUnit)
    {
        var configuration = LoadApiAppSettings();
        var adapter = new FakeAdapter("SOMA_DESENV");
        var resolver = new GarantirFornecedorErpAdapterResolver([adapter], configuration);

        var resolved = resolver.Resolver(businessUnit);

        Assert.Same(adapter, resolved);
    }

    [Fact]
    public void Resolver_Should_Fail_Closed_For_A_BusinessUnit_Slug_Not_Configured()
    {
        var configuration = LoadApiAppSettings();
        var resolver = new GarantirFornecedorErpAdapterResolver([new FakeAdapter("SOMA_DESENV")], configuration);

        var ex = Assert.Throws<ErpFornecedorEscritaException>(() => resolver.Resolver("BU-INEXISTENTE"));
        Assert.Equal(ErpFornecedorErro.Validacao, ex.Tipo);
    }

    private static IConfiguration LoadApiAppSettings()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "BlueprintOS.sln")))
            directory = directory.Parent;
        Assert.NotNull(directory);

        var appSettingsPath = Path.Combine(directory!.FullName, "src", "BlueprintOS.Api", "appsettings.json");
        Assert.True(File.Exists(appSettingsPath), $"appsettings.json real não encontrado em {appSettingsPath}.");

        return new ConfigurationBuilder().AddJsonFile(appSettingsPath, optional: false).Build();
    }

    private sealed class FakeAdapter(string erpSistema) : IGarantirFornecedorErpAdapter
    {
        public string ErpSistema => erpSistema;
        public Task<GarantirFornecedorErpResultado> GarantirAsync(GarantirFornecedorErpRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Fake usado apenas para resolução de adapter, não para execução.");
    }
}
