using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Procurement.Suppliers;
using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.UnitTests.Application.Procurement.Suppliers;

/// <summary>B3 — Bloco 5A.9 (§5/§15): troca explícita de Principal pelo comprador — nunca automática.</summary>
public sealed class FornecedorLinxVinculoUseCasesTests
{
    [Fact]
    public async Task Definir_Principal_Deve_Trocar_Principal_Entre_Vinculos_Ativos()
    {
        await using var context = NewContext();
        var identity = new FakeIdentity();
        var fornecedor = new Fornecedor(Guid.NewGuid(), "Fornecedor", DocumentoFiscal.Create("12345678000195"), "PJ", null, null, null,
            null, null, null, null, "Ativo", null, DateTimeOffset.UtcNow, identity.UnidadeNegocioId);
        await new FornecedorRepository(context).AdicionarAsync(fornecedor);
        var vinculoRepo = new FornecedorLinxVinculoRepository(context);
        var v1 = new FornecedorLinxVinculo(fornecedor.Id, identity.UnidadeNegocioId, "SOMA_DESENV", "001", "FORNECEDOR", false, false, DateTimeOffset.UtcNow, principal: true, agora: DateTimeOffset.UtcNow);
        var v2 = new FornecedorLinxVinculo(fornecedor.Id, identity.UnidadeNegocioId, "SOMA_DESENV", "002", "FORNECEDOR", false, false, DateTimeOffset.UtcNow, principal: false, agora: DateTimeOffset.UtcNow);
        await vinculoRepo.AdicionarAsync(v1);
        await vinculoRepo.AdicionarAsync(v2);
        await vinculoRepo.SalvarAlteracoesAsync();

        var useCase = new DefinirFornecedorLinxVinculoPrincipalUseCase(new FornecedorRepository(context), vinculoRepo);
        var sucesso = await useCase.ExecuteAsync(fornecedor.Id, v2.Id);

        Assert.True(sucesso);
        Assert.False((await context.FornecedorLinxVinculos.SingleAsync(v => v.Id == v1.Id)).Principal);
        Assert.True((await context.FornecedorLinxVinculos.SingleAsync(v => v.Id == v2.Id)).Principal);
        Assert.Equal("002", (await context.Fornecedores.SingleAsync()).ErpFornecedorId);
    }

    [Fact]
    public async Task Definir_Principal_Deve_Rejeitar_Vinculo_Inativo()
    {
        await using var context = NewContext();
        var identity = new FakeIdentity();
        var fornecedor = new Fornecedor(Guid.NewGuid(), "Fornecedor", DocumentoFiscal.Create("12345678000195"), "PJ", null, null, null,
            null, null, null, null, "Ativo", null, DateTimeOffset.UtcNow, identity.UnidadeNegocioId);
        await new FornecedorRepository(context).AdicionarAsync(fornecedor);
        var vinculoRepo = new FornecedorLinxVinculoRepository(context);
        var inativo = new FornecedorLinxVinculo(fornecedor.Id, identity.UnidadeNegocioId, "SOMA_DESENV", "001", "FORNECEDOR", true, false, DateTimeOffset.UtcNow, principal: false, agora: DateTimeOffset.UtcNow);
        await vinculoRepo.AdicionarAsync(inativo);
        await vinculoRepo.SalvarAlteracoesAsync();

        var useCase = new DefinirFornecedorLinxVinculoPrincipalUseCase(new FornecedorRepository(context), vinculoRepo);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(fornecedor.Id, inativo.Id));
    }

    [Fact]
    public async Task Listar_Vinculos_Deve_Retornar_Null_Quando_Fornecedor_Nao_Encontrado()
    {
        await using var context = NewContext();
        var identity = new FakeIdentity();
        var useCase = new ListarFornecedorLinxVinculosUseCase(new FornecedorRepository(context), new FornecedorLinxVinculoRepository(context));

        Assert.Null(await useCase.ExecuteAsync(Guid.NewGuid()));
    }

    private static BlueprintOSDbContext NewContext() =>
        new(new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class FakeIdentity : ICurrentIdentity
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public Guid UnidadeNegocioId { get; } = Guid.NewGuid();
        public RequestIdentity GetRequired() => new(UserId, "Buyer", UnidadeNegocioId);
    }
}
