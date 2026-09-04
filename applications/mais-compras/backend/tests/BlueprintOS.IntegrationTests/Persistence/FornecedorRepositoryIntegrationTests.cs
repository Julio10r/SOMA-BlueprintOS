using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.IntegrationTests.Persistence;

public sealed class FornecedorRepositoryIntegrationTests
{
    private static readonly Guid GrupoSoma = Guid.NewGuid();
    private static readonly Guid Reserva = Guid.NewGuid();

    [Fact]
    public async Task Repository_Should_Persist_And_Search_Corporate_Supplier()
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new BlueprintOSDbContext(options);
        var repository = new FornecedorRepository(context);
        var supplier = new Fornecedor(Guid.NewGuid(), "Alpha Suprimentos", Cnpj.Create("12345678000195"), null, null, null, null, null, null, null, "Ativo", 80, DateTimeOffset.UtcNow, GrupoSoma);
        await repository.AdicionarAsync(supplier);
        Assert.True(await repository.ExisteAsync("12345678000195", GrupoSoma));
        Assert.Single(await repository.PesquisarAsync("Alpha", GrupoSoma));
        Assert.Single(await repository.ListarAsync(GrupoSoma));

        // DR-18 (Design Review Pos-Onda 1): "excluir" Fornecedor e semantica de inativacao, nunca
        // remocao fisica da linha — nem +Compras nem ERP executam DELETE fisico como operacao funcional.
        supplier.AlterarStatus(false, DateTimeOffset.UtcNow, "MaisCompras");
        await repository.AtualizarAsync(supplier);
        var listados = await repository.ListarAsync(GrupoSoma);
        Assert.Single(listados);
        Assert.Equal("Inativo", listados[0].Status);
    }

    [Fact]
    public async Task PesquisarPaginadoAsync_Should_Return_Total_And_Page_Without_Materializing_Before_Paging()
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new BlueprintOSDbContext(options);
        var repository = new FornecedorRepository(context);
        var cnpjsValidos = new[] { "12345678000195", "11444777000161" };
        for (var i = 0; i < 25; i++)
        {
            await repository.AdicionarAsync(new Fornecedor(Guid.NewGuid(), $"Fornecedor {i:00}",
                Cnpj.Create(cnpjsValidos[i % cnpjsValidos.Length]), null, null, null, null, null, null, null, "Ativo", null, DateTimeOffset.UtcNow, GrupoSoma));
        }

        var pagina1 = await repository.PesquisarPaginadoAsync(null, FornecedorStatusFiltro.Todos, FornecedorOrdenacaoCampo.RazaoSocial, false, 1, 10, GrupoSoma);
        Assert.Equal(25, pagina1.TotalCount);
        Assert.Equal(10, pagina1.Items.Count);
        Assert.Equal(1, pagina1.Page);
        Assert.Equal("Fornecedor 00", pagina1.Items[0].RazaoSocial);

        var pagina3 = await repository.PesquisarPaginadoAsync(null, FornecedorStatusFiltro.Todos, FornecedorOrdenacaoCampo.RazaoSocial, false, 3, 10, GrupoSoma);
        Assert.Equal(5, pagina3.Items.Count);
        Assert.Equal(25, pagina3.TotalCount);
    }

    [Fact]
    public async Task PesquisarPaginadoAsync_Should_Filter_By_Status_Ativo_Inativo_And_Todos()
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new BlueprintOSDbContext(options);
        var repository = new FornecedorRepository(context);
        var ativo = new Fornecedor(Guid.NewGuid(), "Ativa Ltda", Cnpj.Create("12345678000195"), null, null, null, null, null, null, null, "Ativo", null, DateTimeOffset.UtcNow, GrupoSoma);
        var inativo = new Fornecedor(Guid.NewGuid(), "Inativa Ltda", Cnpj.Create("11444777000161"), null, null, null, null, null, null, null, "Ativo", null, DateTimeOffset.UtcNow, GrupoSoma);
        inativo.AlterarStatus(false, DateTimeOffset.UtcNow, "MaisCompras");
        await repository.AdicionarAsync(ativo);
        await repository.AdicionarAsync(inativo);

        var somenteAtivos = await repository.PesquisarPaginadoAsync(null, FornecedorStatusFiltro.Ativo, FornecedorOrdenacaoCampo.RazaoSocial, false, 1, 20, GrupoSoma);
        Assert.Single(somenteAtivos.Items);
        Assert.Equal("Ativa Ltda", somenteAtivos.Items[0].RazaoSocial);

        var somenteInativos = await repository.PesquisarPaginadoAsync(null, FornecedorStatusFiltro.Inativo, FornecedorOrdenacaoCampo.RazaoSocial, false, 1, 20, GrupoSoma);
        Assert.Single(somenteInativos.Items);
        Assert.Equal("Inativa Ltda", somenteInativos.Items[0].RazaoSocial);

        var todos = await repository.PesquisarPaginadoAsync(null, FornecedorStatusFiltro.Todos, FornecedorOrdenacaoCampo.RazaoSocial, false, 1, 20, GrupoSoma);
        Assert.Equal(2, todos.TotalCount);
    }

    [Fact]
    public async Task PesquisarPaginadoAsync_Should_Match_Partial_Name_And_Return_Zero_When_No_Match()
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new BlueprintOSDbContext(options);
        var repository = new FornecedorRepository(context);
        await repository.AdicionarAsync(new Fornecedor(Guid.NewGuid(), "Alpha Suprimentos", Cnpj.Create("12345678000195"), null, null, null, null, null, null, null, "Ativo", null, DateTimeOffset.UtcNow, GrupoSoma));

        var encontrados = await repository.PesquisarPaginadoAsync("Alpha", FornecedorStatusFiltro.Todos, FornecedorOrdenacaoCampo.RazaoSocial, false, 1, 20, GrupoSoma);
        Assert.Single(encontrados.Items);

        var semResultado = await repository.PesquisarPaginadoAsync("Inexistente", FornecedorStatusFiltro.Todos, FornecedorOrdenacaoCampo.RazaoSocial, false, 1, 20, GrupoSoma);
        Assert.Empty(semResultado.Items);
        Assert.Equal(0, semResultado.TotalCount);
    }

    /// <summary>B3 — Bloco 5A.9 (correção do resíduo arquitetural TemporaryUserId, decisão do Product
    /// Owner): Fornecedor é corporativo — a pesquisa nunca mais recebe (nem precisa de) um "dono" como
    /// filtro. Dois Fornecedores criados de forma independente (nenhum vínculo com um "usuário
    /// proprietário") aparecem ambos na mesma consulta, comprovando que não há isolamento nem duplicação
    /// por usuário.</summary>
    [Fact]
    public async Task PesquisarPaginadoAsync_Should_Return_All_Corporate_Suppliers_Regardless_Of_Who_Created_Them()
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new BlueprintOSDbContext(options);
        var repository = new FornecedorRepository(context);
        await repository.AdicionarAsync(new Fornecedor(Guid.NewGuid(), "Alpha Suprimentos", Cnpj.Create("12345678000195"), null, null, null, null, null, null, null, "Ativo", null, DateTimeOffset.UtcNow, GrupoSoma));
        await repository.AdicionarAsync(new Fornecedor(Guid.NewGuid(), "Beta Comercio", Cnpj.Create("11444777000161"), null, null, null, null, null, null, null, "Ativo", null, DateTimeOffset.UtcNow, GrupoSoma));

        var resultado = await repository.PesquisarPaginadoAsync(null, FornecedorStatusFiltro.Todos, FornecedorOrdenacaoCampo.RazaoSocial, false, 1, 20, GrupoSoma);

        Assert.Equal(2, resultado.TotalCount);
        Assert.Equal(2, resultado.Items.Select(x => x.Cnpj_Cpf).Distinct().Count()); // nenhuma duplicação
    }

    [Fact]
    public async Task Supplier_Should_Link_To_Erp_Domain_Records()
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new BlueprintOSDbContext(options);
        var syncedAt = DateTimeOffset.UtcNow;
        var domain = new FornecedorDominioErp(Guid.NewGuid(), "TipoFornecedor", "IND", "Industrial", "BU-A", "SOMA_DESENV", "Ativo", syncedAt);
        var supplier = new Fornecedor(Guid.NewGuid(), "Alpha Suprimentos", DocumentoFiscal.Create("12345678909"), "PF", null, null, null, null, null, "SP", "BR", "Ativo", null, syncedAt, GrupoSoma);
        supplier.VincularDominios(null, domain.Id, null, syncedAt);

        await context.FornecedoresDominiosErp.AddAsync(domain);
        await context.Fornecedores.AddAsync(supplier);
        await context.SaveChangesAsync();

        var stored = await context.Fornecedores.SingleAsync();
        Assert.Equal(domain.Id, stored.TipoFornecedorDominioId);
        Assert.Equal("IND", (await context.FornecedoresDominiosErp.SingleAsync()).CodigoERP);
    }

    // ---- Onda 2 (Multi-BU/Multi-ERP, 03/09/2026) — identidade funcional (UnidadeNegocioId, Cnpj_Cpf) ----

    [Fact]
    public async Task Same_Cnpj_Should_Be_Allowed_As_Independent_Suppliers_In_Different_BusinessUnits()
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new BlueprintOSDbContext(options);
        var repository = new FornecedorRepository(context);
        const string cnpj = "12345678000195";

        var daGrupoSoma = new Fornecedor(Guid.NewGuid(), "Fornecedor Grupo Soma", Cnpj.Create(cnpj), null, null, null, null, null, null, null, "Ativo", null, DateTimeOffset.UtcNow, GrupoSoma);
        var daReserva = new Fornecedor(Guid.NewGuid(), "Fornecedor Reserva", Cnpj.Create(cnpj), null, null, null, null, null, null, null, "Ativo", null, DateTimeOffset.UtcNow, Reserva);

        await repository.AdicionarAsync(daGrupoSoma);
        await repository.AdicionarAsync(daReserva);

        Assert.NotEqual(daGrupoSoma.Id, daReserva.Id);
        Assert.Equal(daGrupoSoma.Id, (await repository.ObterPorCnpjAsync(cnpj, GrupoSoma))!.Id);
        Assert.Equal(daReserva.Id, (await repository.ObterPorCnpjAsync(cnpj, Reserva))!.Id);
    }

    /// <summary>O provider InMemory usado nestes testes não força índices únicos em tempo de execução
    /// (só o SQL Server real o faz — ver <see cref="FornecedorRepository.AdicionarAsync"/>, que traduz a
    /// violação real para <see cref="DuplicateRecordException"/>). A garantia testável aqui, sem SQL Server
    /// real, é dupla: (1) o índice composto `(UnidadeNegocioId, Cnpj_Cpf)` está de fato configurado no
    /// modelo EF — não voltou a ser `Cnpj_Cpf` sozinho; (2) a pré-checagem de aplicação
    /// (<see cref="IFornecedorRepository.ExisteAsync"/>, usada por <c>CadastrarFornecedorUseCase</c> antes
    /// de qualquer INSERT) já rejeita corretamente o mesmo CNPJ dentro da mesma BU. A conversão real de uma
    /// violação de índice único em <see cref="DuplicateRecordException"/> está coberta contra SQL Server
    /// real na Work Order B2.9 (corrida real provocada e revertida com sucesso).</summary>
    [Fact]
    public async Task Same_Cnpj_Should_Be_Rejected_As_Duplicate_Within_The_Same_BusinessUnit()
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new BlueprintOSDbContext(options);
        var repository = new FornecedorRepository(context);
        const string cnpj = "12345678000195";

        await repository.AdicionarAsync(new Fornecedor(Guid.NewGuid(), "Primeiro Cadastro", Cnpj.Create(cnpj), null, null, null, null, null, null, null, "Ativo", null, DateTimeOffset.UtcNow, GrupoSoma));

        Assert.True(await repository.ExisteAsync(cnpj, GrupoSoma));

        var indice = context.Model.FindEntityType(typeof(Fornecedor))!.GetIndexes()
            .Single(i => i.IsUnique && i.Properties.Any(p => p.Name == nameof(Fornecedor.Cnpj_Cpf)));
        Assert.Equal(
            new[] { nameof(Fornecedor.UnidadeNegocioId), nameof(Fornecedor.Cnpj_Cpf) },
            indice.Properties.Select(p => p.Name));
    }

    [Fact]
    public async Task Query_In_One_BusinessUnit_Should_Never_Return_Supplier_From_Another()
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new BlueprintOSDbContext(options);
        var repository = new FornecedorRepository(context);

        await repository.AdicionarAsync(new Fornecedor(Guid.NewGuid(), "Somente Grupo Soma", Cnpj.Create("12345678000195"), null, null, null, null, null, null, null, "Ativo", null, DateTimeOffset.UtcNow, GrupoSoma));
        await repository.AdicionarAsync(new Fornecedor(Guid.NewGuid(), "Somente Reserva", Cnpj.Create("11444777000161"), null, null, null, null, null, null, null, "Ativo", null, DateTimeOffset.UtcNow, Reserva));

        var listaGrupoSoma = await repository.ListarAsync(GrupoSoma);
        var listaReserva = await repository.ListarAsync(Reserva);
        var paginaGrupoSoma = await repository.PesquisarPaginadoAsync(null, FornecedorStatusFiltro.Todos, FornecedorOrdenacaoCampo.RazaoSocial, false, 1, 20, GrupoSoma);

        Assert.Single(listaGrupoSoma);
        Assert.Equal("Somente Grupo Soma", listaGrupoSoma[0].RazaoSocial);
        Assert.Single(listaReserva);
        Assert.Equal("Somente Reserva", listaReserva[0].RazaoSocial);
        Assert.Single(paginaGrupoSoma.Items);
        Assert.DoesNotContain(paginaGrupoSoma.Items, x => x.RazaoSocial == "Somente Reserva");
        Assert.Null(await repository.ObterPorCnpjAsync("11444777000161", GrupoSoma));
    }

    [Fact]
    public async Task Inativar_Supplier_In_One_BusinessUnit_Should_Never_Affect_The_Same_Cnpj_In_Another()
    {
        var options = new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new BlueprintOSDbContext(options);
        var repository = new FornecedorRepository(context);
        const string cnpj = "12345678000195";

        var daGrupoSoma = new Fornecedor(Guid.NewGuid(), "Fornecedor Grupo Soma", Cnpj.Create(cnpj), null, null, null, null, null, null, null, "Ativo", null, DateTimeOffset.UtcNow, GrupoSoma);
        var daReserva = new Fornecedor(Guid.NewGuid(), "Fornecedor Reserva", Cnpj.Create(cnpj), null, null, null, null, null, null, null, "Ativo", null, DateTimeOffset.UtcNow, Reserva);
        await repository.AdicionarAsync(daGrupoSoma);
        await repository.AdicionarAsync(daReserva);

        daGrupoSoma.AlterarStatus(false, DateTimeOffset.UtcNow, "MaisCompras");
        await repository.AtualizarAsync(daGrupoSoma);

        Assert.Equal("Inativo", (await repository.ObterPorCnpjAsync(cnpj, GrupoSoma))!.Status);
        Assert.Equal("Ativo", (await repository.ObterPorCnpjAsync(cnpj, Reserva))!.Status);
    }
}
