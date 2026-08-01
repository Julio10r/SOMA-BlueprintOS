using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Procurement.Suppliers;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BlueprintOS.UnitTests.Application.Procurement.Suppliers;

public sealed class SincronizarFornecedorUseCaseTests
{
    [Fact]
    public async Task Import_Should_Create_And_Reexecution_Should_Update_Without_Duplicate()
    {
        await using var context = NewContext(); var user = new FakeIdentity();
        var adapter = new FakeAdapter { Current = new("ERP-1", "  Fornecedor ERP  ", "12345678000195", "São Paulo", "SP", "BR") };
        var useCase = Create(context, user, adapter);
        var dto = new SincronizarFornecedorDto("BU-A", "SOMA_DESENV", "ERP-1", null, DirecaoSincronizacao.ErpParaMaisCompras, "corr-1");

        var first = await useCase.ExecuteAsync(dto); var second = await useCase.ExecuteAsync(dto);

        Assert.Equal("Sincronizado", first.Status); Assert.Equal(first.FornecedorId, second.FornecedorId);
        Assert.Single(await context.Fornecedores.ToListAsync()); Assert.Equal("ERP", (await context.Fornecedores.SingleAsync()).OrigemInformacao);
        Assert.Equal(2, await context.FornecedoresSincronizacoes.CountAsync());
    }

    [Fact]
    public async Task Export_Should_Create_Then_Update_Using_External_Key()
    {
        await using var context = NewContext(); var user = new FakeIdentity(); var adapter = new FakeAdapter();
        var local = new Fornecedor(Guid.NewGuid(), "Teste B21", Cnpj.Create("98765432000110"), "Própria", "teste@example.invalid", null, null, "São Paulo", "SP", "BR", "Ativo", null, user.UserId, DateTimeOffset.UtcNow);
        await new FornecedorRepository(context).AdicionarAsync(local);
        var useCase = Create(context, user, adapter);

        var first = await useCase.ExecuteAsync(new("BU-A", "SOMA_DESENV", null, local.Id, DirecaoSincronizacao.MaisComprasParaErp, null));
        var second = await useCase.ExecuteAsync(new("BU-A", "SOMA_DESENV", null, local.Id, DirecaoSincronizacao.MaisComprasParaErp, null));

        Assert.Equal("Sincronizado", first.Status); Assert.Equal("Sincronizado", second.Status); Assert.Equal(1, adapter.CreateCount); Assert.Equal(1, adapter.UpdateCount);
        Assert.Equal("ERP-NEW", (await context.Fornecedores.SingleAsync()).ErpFornecedorId);
    }

    [Fact]
    public async Task Failure_Should_Be_Sanitized_And_Not_Expose_Exception_Details()
    {
        await using var context = NewContext(); var adapter = new FakeAdapter { Error = new InvalidOperationException("Password=super-secret; Server=private") };
        var result = await Create(context, new FakeIdentity(), adapter).ExecuteAsync(new("BU-A", "SOMA_DESENV", "ERP-1", null, DirecaoSincronizacao.ErpParaMaisCompras, null));
        Assert.Equal("Falhou", result.Status); Assert.Equal("Falha ao comunicar com o ERP.", result.Mensagem); Assert.DoesNotContain("super-secret", result.Mensagem);
    }

    [Fact]
    public async Task Cancellation_Should_Be_Propagated()
    {
        await using var context = NewContext(); using var cts = new CancellationTokenSource(); cts.Cancel();
        var adapter = new FakeAdapter();
        await Assert.ThrowsAsync<OperationCanceledException>(() => Create(context, new FakeIdentity(), adapter).ExecuteAsync(
            new("BU-A", "SOMA_DESENV", "ERP-1", null, DirecaoSincronizacao.ErpParaMaisCompras, null), cts.Token));
    }

    [Fact]
    public void Resolver_Should_Select_Adapter_By_Configured_Business_Unit()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ErpIntegration:BusinessUnits:BU-A:ErpSistema"] = "SOMA_DESENV" }).Build();
        var selected = new ErpFornecedorAdapterResolver([new FakeAdapter()], config).Resolver("BU-A", "SOMA_DESENV");
        Assert.Equal("SOMA_DESENV", selected.ErpSistema);
        Assert.Throws<InvalidOperationException>(() => new ErpFornecedorAdapterResolver([new FakeAdapter()], config).Resolver("BU-B", "OUTRO"));
        Assert.Throws<InvalidOperationException>(() => new ErpFornecedorAdapterResolver([new FakeAdapter()], config).Resolver("BU-B", "SOMA_DESENV"));
    }

    private static SincronizarFornecedorUseCase Create(BlueprintOSDbContext context, FakeIdentity identity, FakeAdapter adapter) =>
        new(new FornecedorRepository(context), new FornecedorSincronizacaoRepository(context), new FakeResolver(adapter), identity);
    private static BlueprintOSDbContext NewContext() => new(new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class FakeIdentity : ICurrentIdentity
    { public Guid UserId { get; } = Guid.NewGuid(); public RequestIdentity GetRequired() => new(UserId, "Buyer"); }
    private sealed class FakeResolver(FakeAdapter adapter) : IErpFornecedorAdapterResolver { public IErpFornecedorAdapter Resolver(string _, string __) => adapter; }
    private sealed class FakeAdapter : IErpFornecedorAdapter
    {
        public string ErpSistema => "SOMA_DESENV"; public ErpFornecedorDto? Current { get; set; } public Exception? Error { get; set; }
        public int CreateCount { get; private set; } public int UpdateCount { get; private set; }
        public Task<ErpFornecedorDto?> ObterAsync(string id, CancellationToken ct = default) { ct.ThrowIfCancellationRequested(); if (Error is not null) throw Error; return Task.FromResult(Current?.Id == id ? Current : null); }
        public Task<ErpFornecedorDto> CriarAsync(ErpFornecedorParaEscrita f, CancellationToken ct = default) { ct.ThrowIfCancellationRequested(); CreateCount++; Current = new("ERP-NEW", f.Nome, f.Cnpj, f.Cidade, f.Estado, f.Pais); return Task.FromResult(Current); }
        public Task<ErpFornecedorDto> AtualizarAsync(ErpFornecedorParaEscrita f, CancellationToken ct = default) { ct.ThrowIfCancellationRequested(); UpdateCount++; Current = new(f.Id, f.Nome, f.Cnpj, f.Cidade, f.Estado, f.Pais); return Task.FromResult(Current); }
    }
}
