using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.IntegrationTests.Persistence;

public sealed class FornecedorRepositoryIntegrationTests
{
    [Fact]
    public async Task Repository_Should_Persist_Search_And_Isolate_By_Temporary_User()
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new BlueprintOSDbContext(options);
        var repository = new FornecedorRepository(context); var user = Guid.NewGuid();
        var supplier = new Fornecedor(Guid.NewGuid(), "Alpha Suprimentos", Cnpj.Create("12345678000195"), null, null, null, null, null, null, null, "Ativo", 80, user, DateTimeOffset.UtcNow);
        await repository.AdicionarAsync(supplier);
        Assert.True(await repository.ExisteAsync("12345678000195"));
        Assert.Single(await repository.PesquisarAsync("Alpha", user));
        Assert.Empty(await repository.ListarAsync(Guid.NewGuid()));

        // DR-18 (Design Review Pos-Onda 1): "excluir" Fornecedor e semantica de inativacao, nunca
        // remocao fisica da linha — nem +Compras nem ERP executam DELETE fisico como operacao funcional.
        supplier.AlterarStatus(false, DateTimeOffset.UtcNow, "MaisCompras");
        await repository.AtualizarAsync(supplier);
        var listados = await repository.ListarAsync(user);
        Assert.Single(listados);
        Assert.Equal("Inativo", listados[0].Status);
    }

    [Fact]
    public async Task PesquisarPaginadoAsync_Should_Return_Total_And_Page_Without_Materializing_Before_Paging()
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new BlueprintOSDbContext(options);
        var repository = new FornecedorRepository(context); var user = Guid.NewGuid();
        var cnpjsValidos = new[] { "12345678000195", "11444777000161" };
        for (var i = 0; i < 25; i++)
        {
            await repository.AdicionarAsync(new Fornecedor(Guid.NewGuid(), $"Fornecedor {i:00}",
                Cnpj.Create(cnpjsValidos[i % cnpjsValidos.Length]), null, null, null, null, null, null, null, "Ativo", null, user, DateTimeOffset.UtcNow));
        }

        var pagina1 = await repository.PesquisarPaginadoAsync(user, null, FornecedorStatusFiltro.Todos, FornecedorOrdenacaoCampo.RazaoSocial, false, 1, 10);
        Assert.Equal(25, pagina1.TotalCount);
        Assert.Equal(10, pagina1.Items.Count);
        Assert.Equal(1, pagina1.Page);
        Assert.Equal("Fornecedor 00", pagina1.Items[0].RazaoSocial);

        var pagina3 = await repository.PesquisarPaginadoAsync(user, null, FornecedorStatusFiltro.Todos, FornecedorOrdenacaoCampo.RazaoSocial, false, 3, 10);
        Assert.Equal(5, pagina3.Items.Count);
        Assert.Equal(25, pagina3.TotalCount);
    }

    [Fact]
    public async Task PesquisarPaginadoAsync_Should_Filter_By_Status_Ativo_Inativo_And_Todos()
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new BlueprintOSDbContext(options);
        var repository = new FornecedorRepository(context); var user = Guid.NewGuid();
        var ativo = new Fornecedor(Guid.NewGuid(), "Ativa Ltda", Cnpj.Create("12345678000195"), null, null, null, null, null, null, null, "Ativo", null, user, DateTimeOffset.UtcNow);
        var inativo = new Fornecedor(Guid.NewGuid(), "Inativa Ltda", Cnpj.Create("11444777000161"), null, null, null, null, null, null, null, "Ativo", null, user, DateTimeOffset.UtcNow);
        inativo.AlterarStatus(false, DateTimeOffset.UtcNow, "MaisCompras");
        await repository.AdicionarAsync(ativo);
        await repository.AdicionarAsync(inativo);

        var somenteAtivos = await repository.PesquisarPaginadoAsync(user, null, FornecedorStatusFiltro.Ativo, FornecedorOrdenacaoCampo.RazaoSocial, false, 1, 20);
        Assert.Single(somenteAtivos.Items);
        Assert.Equal("Ativa Ltda", somenteAtivos.Items[0].RazaoSocial);

        var somenteInativos = await repository.PesquisarPaginadoAsync(user, null, FornecedorStatusFiltro.Inativo, FornecedorOrdenacaoCampo.RazaoSocial, false, 1, 20);
        Assert.Single(somenteInativos.Items);
        Assert.Equal("Inativa Ltda", somenteInativos.Items[0].RazaoSocial);

        var todos = await repository.PesquisarPaginadoAsync(user, null, FornecedorStatusFiltro.Todos, FornecedorOrdenacaoCampo.RazaoSocial, false, 1, 20);
        Assert.Equal(2, todos.TotalCount);
    }

    [Fact]
    public async Task PesquisarPaginadoAsync_Should_Match_Partial_Name_And_Return_Zero_When_No_Match()
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new BlueprintOSDbContext(options);
        var repository = new FornecedorRepository(context); var user = Guid.NewGuid();
        await repository.AdicionarAsync(new Fornecedor(Guid.NewGuid(), "Alpha Suprimentos", Cnpj.Create("12345678000195"), null, null, null, null, null, null, null, "Ativo", null, user, DateTimeOffset.UtcNow));

        var encontrados = await repository.PesquisarPaginadoAsync(user, "Alpha", FornecedorStatusFiltro.Todos, FornecedorOrdenacaoCampo.RazaoSocial, false, 1, 20);
        Assert.Single(encontrados.Items);

        var semResultado = await repository.PesquisarPaginadoAsync(user, "Inexistente", FornecedorStatusFiltro.Todos, FornecedorOrdenacaoCampo.RazaoSocial, false, 1, 20);
        Assert.Empty(semResultado.Items);
        Assert.Equal(0, semResultado.TotalCount);
    }

    [Fact]
    public async Task PesquisarPaginadoAsync_Should_Not_Return_Rows_From_Another_User()
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new BlueprintOSDbContext(options);
        var repository = new FornecedorRepository(context); var user = Guid.NewGuid();
        await repository.AdicionarAsync(new Fornecedor(Guid.NewGuid(), "Alpha Suprimentos", Cnpj.Create("12345678000195"), null, null, null, null, null, null, null, "Ativo", null, Guid.NewGuid(), DateTimeOffset.UtcNow));

        var resultado = await repository.PesquisarPaginadoAsync(user, null, FornecedorStatusFiltro.Todos, FornecedorOrdenacaoCampo.RazaoSocial, false, 1, 20);
        Assert.Empty(resultado.Items);
        Assert.Equal(0, resultado.TotalCount);
    }

    [Fact]
    public async Task Supplier_Should_Link_To_Erp_Domain_Records()
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new BlueprintOSDbContext(options);
        var syncedAt = DateTimeOffset.UtcNow;
        var domain = new FornecedorDominioErp(Guid.NewGuid(), "TipoFornecedor", "IND", "Industrial", "BU-A", "SOMA_DESENV", "Ativo", syncedAt);
        var supplier = new Fornecedor(Guid.NewGuid(), "Alpha Suprimentos", DocumentoFiscal.Create("12345678909"), "PF", null, null, null, null, null, "SP", "BR", "Ativo", null, Guid.NewGuid(), syncedAt);
        supplier.VincularDominios(null, domain.Id, null, syncedAt);

        await context.FornecedoresDominiosErp.AddAsync(domain);
        await context.Fornecedores.AddAsync(supplier);
        await context.SaveChangesAsync();

        var stored = await context.Fornecedores.SingleAsync();
        Assert.Equal(domain.Id, stored.TipoFornecedorDominioId);
        Assert.Equal("IND", (await context.FornecedoresDominiosErp.SingleAsync()).CodigoERP);
    }
}
