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

        Assert.Equal("Sincronizado", first.Status); Assert.Equal("Sincronizado", second.Status); Assert.Equal(1, adapter.CreateCount); Assert.Equal(0, adapter.UpdateCount);
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

    [Fact]
    public async Task Import_Should_Apply_ERP_When_ERP_Timestamp_Is_Newer()
    {
        await using var context = NewContext(); var user = new FakeIdentity(); var local = new Fornecedor(Guid.NewGuid(), "Local", Cnpj.Create("12345678000195"), null, null, null, null, "São Paulo", "SP", "BR", "Ativo", null, user.UserId, DateTimeOffset.UtcNow.AddMinutes(-5), "BU-A", "SOMA_DESENV", "ERP-1");
        await new FornecedorRepository(context).AdicionarAsync(local);
        var adapter = new FakeAdapter { Current = new("ERP-1", "ERP Atualizado", "12345678000195", "São Paulo", "SP", "BR", true, DateTimeOffset.UtcNow.AddMinutes(5)) };
        var result = await Create(context, user, adapter).ExecuteAsync(new("BU-A", "SOMA_DESENV", "ERP-1", null, DirecaoSincronizacao.ErpParaMaisCompras, "newer"));
        Assert.Equal("Sincronizado", result.Status); Assert.Equal("ERP Atualizado", (await context.Fornecedores.SingleAsync()).Nome);
    }

    [Fact]
    public async Task Export_Inactivation_Should_Be_Idempotent_And_Audited()
    {
        await using var context = NewContext(); var user = new FakeIdentity(); var local = new Fornecedor(Guid.NewGuid(), "Teste", Cnpj.Create("12345678000195"), null, null, null, null, null, "SP", "BR", "Ativo", null, user.UserId, DateTimeOffset.UtcNow, "BU-A", "SOMA_DESENV", "ERP-1");
        await new FornecedorRepository(context).AdicionarAsync(local); var adapter = new FakeAdapter { Current = new("ERP-1", "Teste", "12345678000195", null, "SP", "BR") };
        var useCase = Create(context, user, adapter); var dto = new SincronizarFornecedorDto("BU-A", "SOMA_DESENV", null, local.Id, DirecaoSincronizacao.MaisComprasParaErp, "inactive", OperacaoFornecedor.Inativar);
        var first = await useCase.ExecuteAsync(dto); var second = await useCase.ExecuteAsync(dto);
        Assert.Equal("Sincronizado", first.Status); Assert.Equal("Inativo", (await context.Fornecedores.SingleAsync()).Status); Assert.Equal(2, await context.FornecedoresSincronizacoes.CountAsync());
    }

    [Fact]
    public async Task Equal_Timestamp_With_Different_Data_Should_Preserve_MaisCompras()
    {
        await using var context = NewContext(); var user = new FakeIdentity(); var timestamp = DateTimeOffset.UtcNow; var local = new Fornecedor(Guid.NewGuid(), "Local", Cnpj.Create("12345678000195"), null, null, null, null, null, "SP", "BR", "Ativo", null, user.UserId, timestamp, "BU-A", "SOMA_DESENV", "ERP-1");
        await new FornecedorRepository(context).AdicionarAsync(local); var adapter = new FakeAdapter { Current = new("ERP-1", "ERP", "12345678000195", null, "SP", "BR", true, timestamp) };
        await Create(context, user, adapter).ExecuteAsync(new("BU-A", "SOMA_DESENV", "ERP-1", null, DirecaoSincronizacao.ErpParaMaisCompras, "tie"));
        Assert.Equal("Local", (await context.Fornecedores.SingleAsync()).Nome); Assert.Equal(0, adapter.UpdateCount);
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
        public int CreateCount { get; private set; } public int UpdateCount { get; private set; } public int InactivateCount { get; private set; }
        public Task<ErpFornecedorDto?> ObterAsync(string id, CancellationToken ct = default) { ct.ThrowIfCancellationRequested(); if (Error is not null) throw Error; return Task.FromResult(Current?.Id == id ? Current : null); }
        public Task<ErpFornecedorDto> CriarAsync(ErpFornecedorParaEscrita f, CancellationToken ct = default) { ct.ThrowIfCancellationRequested(); CreateCount++; Current = new("ERP-NEW", f.Nome, f.Cnpj, f.Cidade, f.Estado, f.Pais); return Task.FromResult(Current); }
        public Task<ErpFornecedorDto> AtualizarAsync(ErpFornecedorParaEscrita f, CancellationToken ct = default) { ct.ThrowIfCancellationRequested(); UpdateCount++; Current = new(f.Id, f.Nome, f.Cnpj, f.Cidade, f.Estado, f.Pais); return Task.FromResult(Current); }
        public Task<ErpFornecedorDto> InativarAsync(string id, CancellationToken ct = default) { ct.ThrowIfCancellationRequested(); InactivateCount++; Current = Current is null ? new(id, "Inativo", "00000000000000", null, null, null, false) : Current with { Id = id, Ativo = false, UltimaAlteracaoEm = DateTimeOffset.UtcNow }; return Task.FromResult(Current); }
    }
}
