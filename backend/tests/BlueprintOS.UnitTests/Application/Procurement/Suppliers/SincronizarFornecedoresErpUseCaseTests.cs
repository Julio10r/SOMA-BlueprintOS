using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;
using BlueprintOS.Infrastructure.Integrations.ERP.Soma;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.UnitTests.Application.Procurement.Suppliers;

public sealed class SincronizarFornecedoresErpUseCaseTests
{
    [Fact]
    public async Task Execute_Should_Map_Erp_Data_To_Domain_And_Insert_New_Supplier()
    {
        await using var context = NewContext();
        var identity = new FakeIdentity();
        var reader = new FakeReader(new FornecedorErpIntegracaoDto("ERP-10", "SOMA_DESENV", Canonical("Fornecedor ERP", "Fantasia ERP", "12345678000195", "hash-1"), DateTimeOffset.UtcNow));

        var result = await Create(context, identity, reader).ExecuteAsync(new("BU-A", 100, "corr-erp"));

        var stored = await context.Fornecedores.SingleAsync();
        Assert.Equal(1, result.Consultados);
        Assert.Equal(1, result.Incluidos);
        Assert.Equal("Fornecedor ERP", stored.RazaoSocial);
        Assert.Equal("Fantasia ERP", stored.NomeFantasia);
        Assert.Equal("ERP", stored.OrigemInformacao);
        Assert.Equal("SOMA_DESENV", stored.ErpSistema);
        Assert.Equal("ERP-10", stored.ErpFornecedorId);
    }

    [Fact]
    public async Task Execute_Should_Update_Existing_Supplier_And_Preserve_NomeFantasia_Only_For_Erp_Source()
    {
        await using var context = NewContext();
        var identity = new FakeIdentity();
        var existing = new Fornecedor(Guid.NewGuid(), "Fornecedor Antigo", DocumentoFiscal.Create("12345678000195"), null, null, null, null,
            null, "Rio de Janeiro", "RJ", "BR", "Ativo", null, identity.UserId, DateTimeOffset.UtcNow.AddDays(-1));
        existing.AplicarContratoCanonico(Canonical("Fornecedor Antigo", "Fantasia Original ERP", "12345678000195", "old"), "ERP", DateTimeOffset.UtcNow.AddDays(-1));
        await new FornecedorRepository(context).AdicionarAsync(existing);

        var reader = new FakeReader(new FornecedorErpIntegracaoDto("ERP-10", "SOMA_DESENV", Canonical("Fornecedor Atualizado", "Fantasia Nova ERP", "12345678000195", "hash-2"), DateTimeOffset.UtcNow));
        var result = await Create(context, identity, reader).ExecuteAsync(new("BU-A", 100, null));

        var stored = await context.Fornecedores.SingleAsync();
        Assert.Equal(1, result.Atualizados);
        Assert.Equal("Fornecedor Atualizado", stored.RazaoSocial);
        Assert.Equal("Fantasia Nova ERP", stored.NomeFantasia);

        stored.AplicarContratoCanonico(Canonical("Alteracao MaisCompras", "Fantasia Manual", "12345678000195", "manual"), "MaisCompras", DateTimeOffset.UtcNow);
        Assert.Equal("Fantasia Nova ERP", stored.NomeFantasia);
    }

    [Fact]
    public async Task Execute_Should_Count_Unchanged_When_Hash_Matches()
    {
        await using var context = NewContext();
        var identity = new FakeIdentity();
        var dados = Canonical("Fornecedor ERP", "Fantasia ERP", "12345678000195", "hash-1");
        var existing = new Fornecedor(Guid.NewGuid(), dados.RazaoSocial, DocumentoFiscal.Create(dados.DocumentoFiscal), dados.TipoPessoa, null, null,
            null, null, dados.Cidade, dados.Uf, dados.Pais, "Ativo", null, identity.UserId, DateTimeOffset.UtcNow, "BU-A", "SOMA_DESENV", "ERP-10");
        existing.AplicarContratoCanonico(dados, "ERP", DateTimeOffset.UtcNow);
        await new FornecedorRepository(context).AdicionarAsync(existing);

        var result = await Create(context, identity, new FakeReader(new FornecedorErpIntegracaoDto("ERP-10", "SOMA_DESENV", dados, DateTimeOffset.UtcNow)))
            .ExecuteAsync(new("BU-A", 100, null));

        Assert.Equal(1, result.SemAlteracao);
        Assert.Equal(0, result.Atualizados);
    }

    private static SincronizarFornecedoresErpUseCase Create(BlueprintOSDbContext context, FakeIdentity identity, FakeReader reader) =>
        new(reader, new FornecedorRepository(context), identity);

    private static BlueprintOSDbContext NewContext() =>
        new(new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static FornecedorCanonico Canonical(string razaoSocial, string nomeFantasia, string documento, string hash) =>
        new(razaoSocial, nomeFantasia, documento, "PJ", "BR", null, null, "01001000", "Rua ERP", "100", null, "Centro",
            "Sao Paulo", "SP", null, "11", "999999999", "erp@example.invalid", "fiscal@example.invalid", null, null, null,
            null, "001", "Fornecedor", null, null, "Normal", false, null, true, false, true, false, false, false, true,
            DateTimeOffset.UtcNow, hash);

    private sealed class FakeIdentity : ICurrentIdentity
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public RequestIdentity GetRequired() => new(UserId, "Buyer");
    }

    private sealed class FakeReader(params FornecedorErpIntegracaoDto[] fornecedores) : IFornecedorErpReader
    {
        public Task<IReadOnlyList<FornecedorErpIntegracaoDto>> BuscarFornecedoresAsync(int limite, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FornecedorErpIntegracaoDto>>(fornecedores.Take(limite).ToList());
    }
}
