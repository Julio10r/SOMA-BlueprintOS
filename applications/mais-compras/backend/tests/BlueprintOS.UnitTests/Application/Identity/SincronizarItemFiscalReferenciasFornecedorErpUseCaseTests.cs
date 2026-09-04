using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Infrastructure.Identity;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;
using BlueprintOS.Infrastructure.Integrations.ERP.Soma;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlueprintOS.UnitTests.Application.Identity;

/// <summary>B3 — Bloco 5A: cobre a diferença deliberada em relação ao Item Fiscal — aqui `ADR-0024` já
/// resolve a divergência (Linx prevalece, sem timestamp confiável), então uma referência existente
/// diferente É atualizada; resolução de nome ambígua/zero, Item Fiscal ou Fornecedor ainda não
/// sincronizados, e colisão de `(Fornecedor, CodigoItemFornecedor)` continuam sempre como conflito, nunca
/// associação automática.</summary>
public sealed class SincronizarItemFiscalReferenciasFornecedorErpUseCaseTests
{
    [Fact]
    public async Task Execute_Should_Create_Reference_When_Resolution_Is_Unambiguous()
    {
        await using var context = NewContext();
        var itemFiscal = new ItemFiscal("COD-1", "Item", "UN", "1.1.01", Guid.NewGuid(), DateTimeOffset.UtcNow);
        await new ItemFiscalRepository(context).AdicionarAsync(itemFiscal, default);
        var fornecedor = await NovoFornecedorComVinculoAsync(context, "003316");
        await context.SaveChangesAsync();

        var reader = new FakeReader(new ItemFiscalReferenciaFornecedorErpDto("COD-1", "COD-FORN-1", "003316", FornecedoresResolvidos: 1));
        var result = await Create(context, reader).ExecuteAsync(new SincronizarItemFiscalReferenciasFornecedorErpDto(100, null));

        var referencia = await context.ItensFiscaisReferenciasFornecedor.SingleAsync();
        Assert.Equal(1, result.Incluidos);
        Assert.Empty(result.Conflitos);
        Assert.Equal(itemFiscal.Id, referencia.ItemFiscalId);
        Assert.Equal(fornecedor.Id, referencia.FornecedorId);
        Assert.Equal("COD-FORN-1", referencia.CodigoItemFornecedor);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task Execute_Should_Report_Conflict_When_Name_Resolution_Is_Not_Exactly_One(int fornecedoresResolvidos)
    {
        await using var context = NewContext();
        var itemFiscal = new ItemFiscal("COD-1", "Item", "UN", "1.1.01", Guid.NewGuid(), DateTimeOffset.UtcNow);
        await new ItemFiscalRepository(context).AdicionarAsync(itemFiscal, default);
        await context.SaveChangesAsync();

        var reader = new FakeReader(new ItemFiscalReferenciaFornecedorErpDto("COD-1", "COD-FORN-1", fornecedoresResolvidos == 0 ? null : "003316", fornecedoresResolvidos));
        var result = await Create(context, reader).ExecuteAsync(new SincronizarItemFiscalReferenciasFornecedorErpDto(100, null));

        Assert.Equal(0, result.Incluidos);
        Assert.Single(result.Conflitos);
        Assert.Equal(ItemFiscalReferenciaFornecedorErpConflitoMotivo.NomeFornecedorNaoResolvidoOuAmbiguo, result.Conflitos[0].Motivo);
        Assert.Empty(await context.ItensFiscaisReferenciasFornecedor.ToListAsync());
    }

    [Fact]
    public async Task Execute_Should_Report_Conflict_When_ItemFiscal_Not_Yet_Synced_Locally()
    {
        await using var context = NewContext();
        var fornecedor = await NovoFornecedorComVinculoAsync(context, "003316");
        await context.SaveChangesAsync();

        var reader = new FakeReader(new ItemFiscalReferenciaFornecedorErpDto("COD-INEXISTENTE", "COD-FORN-1", "003316", 1));
        var result = await Create(context, reader).ExecuteAsync(new SincronizarItemFiscalReferenciasFornecedorErpDto(100, null));

        Assert.Single(result.Conflitos);
        Assert.Equal(ItemFiscalReferenciaFornecedorErpConflitoMotivo.ItemFiscalAindaNaoSincronizadoLocalmente, result.Conflitos[0].Motivo);
    }

    [Fact]
    public async Task Execute_Should_Report_Conflict_When_Fornecedor_Not_Yet_Synced_Locally()
    {
        await using var context = NewContext();
        var itemFiscal = new ItemFiscal("COD-1", "Item", "UN", "1.1.01", Guid.NewGuid(), DateTimeOffset.UtcNow);
        await new ItemFiscalRepository(context).AdicionarAsync(itemFiscal, default);
        await context.SaveChangesAsync();

        var reader = new FakeReader(new ItemFiscalReferenciaFornecedorErpDto("COD-1", "COD-FORN-1", "003316", 1));
        var result = await Create(context, reader).ExecuteAsync(new SincronizarItemFiscalReferenciasFornecedorErpDto(100, null));

        Assert.Single(result.Conflitos);
        Assert.Equal(ItemFiscalReferenciaFornecedorErpConflitoMotivo.FornecedorAindaNaoSincronizadoLocalmente, result.Conflitos[0].Motivo);
    }

    [Fact]
    public async Task Execute_Should_Update_Existing_Reference_When_Linx_Diverges_ADR0024()
    {
        await using var context = NewContext();
        var itemFiscal = new ItemFiscal("COD-1", "Item", "UN", "1.1.01", Guid.NewGuid(), DateTimeOffset.UtcNow);
        await new ItemFiscalRepository(context).AdicionarAsync(itemFiscal, default);
        var fornecedor = await NovoFornecedorComVinculoAsync(context, "003316");
        await context.SaveChangesAsync();

        var referenciaRepo = new ItemFiscalReferenciaFornecedorRepository(context);
        var existente = new ItemFiscalReferenciaFornecedor(itemFiscal.Id, fornecedor.Id, "CODIGO-ANTIGO", DateTimeOffset.UtcNow);
        await referenciaRepo.AdicionarAsync(existente, default);
        await context.SaveChangesAsync();

        var reader = new FakeReader(new ItemFiscalReferenciaFornecedorErpDto("COD-1", "CODIGO-NOVO-LINX", "003316", 1));
        var result = await Create(context, reader).ExecuteAsync(new SincronizarItemFiscalReferenciasFornecedorErpDto(100, null));

        var referencia = await context.ItensFiscaisReferenciasFornecedor.SingleAsync();
        Assert.Equal(1, result.Atualizados);
        Assert.Empty(result.Conflitos);
        Assert.Equal("CODIGO-NOVO-LINX", referencia.CodigoItemFornecedor);
    }

    [Fact]
    public async Task Execute_Should_Classify_As_SemAlteracao_When_Reference_Already_Matches()
    {
        await using var context = NewContext();
        var itemFiscal = new ItemFiscal("COD-1", "Item", "UN", "1.1.01", Guid.NewGuid(), DateTimeOffset.UtcNow);
        await new ItemFiscalRepository(context).AdicionarAsync(itemFiscal, default);
        var fornecedor = await NovoFornecedorComVinculoAsync(context, "003316");
        await context.SaveChangesAsync();

        var referenciaRepo = new ItemFiscalReferenciaFornecedorRepository(context);
        await referenciaRepo.AdicionarAsync(new ItemFiscalReferenciaFornecedor(itemFiscal.Id, fornecedor.Id, "MESMO-CODIGO", DateTimeOffset.UtcNow), default);
        await context.SaveChangesAsync();

        var reader = new FakeReader(new ItemFiscalReferenciaFornecedorErpDto("COD-1", "MESMO-CODIGO", "003316", 1));
        var result = await Create(context, reader).ExecuteAsync(new SincronizarItemFiscalReferenciasFornecedorErpDto(100, null));

        Assert.Equal(1, result.SemAlteracao);
        Assert.Equal(0, result.Atualizados);
        Assert.Empty(result.Conflitos);
    }

    [Fact]
    public async Task Execute_Should_Report_Conflict_When_CodigoItemFornecedor_Already_Used_By_Another_ItemFiscal()
    {
        await using var context = NewContext();
        var unidadeNegocioId = Guid.NewGuid();
        var itemFiscalA = new ItemFiscal("COD-A", "Item A", "UN", "1.1.01", unidadeNegocioId, DateTimeOffset.UtcNow);
        var itemFiscalB = new ItemFiscal("COD-B", "Item B", "UN", "1.1.01", unidadeNegocioId, DateTimeOffset.UtcNow);
        var itensRepo = new ItemFiscalRepository(context);
        await itensRepo.AdicionarAsync(itemFiscalA, default);
        await itensRepo.AdicionarAsync(itemFiscalB, default);
        var fornecedor = await NovoFornecedorComVinculoAsync(context, "003316");
        await context.SaveChangesAsync();

        var referenciaRepo = new ItemFiscalReferenciaFornecedorRepository(context);
        await referenciaRepo.AdicionarAsync(new ItemFiscalReferenciaFornecedor(itemFiscalA.Id, fornecedor.Id, "CODIGO-COMPARTILHADO", DateTimeOffset.UtcNow), default);
        await context.SaveChangesAsync();

        // Linx tenta associar o MESMO fornecedor+codigo a um Item Fiscal DIFERENTE (itemFiscalB).
        var reader = new FakeReader(new ItemFiscalReferenciaFornecedorErpDto("COD-B", "CODIGO-COMPARTILHADO", "003316", 1));
        var result = await Create(context, reader).ExecuteAsync(new SincronizarItemFiscalReferenciasFornecedorErpDto(100, null));

        Assert.Single(result.Conflitos);
        Assert.Equal(ItemFiscalReferenciaFornecedorErpConflitoMotivo.CodigoItemFornecedorJaAssociadoAOutroItem, result.Conflitos[0].Motivo);
        Assert.Single(await context.ItensFiscaisReferenciasFornecedor.ToListAsync());
    }

    [Fact]
    public async Task Execute_DryRun_Should_Classify_Without_Persisting_Anything()
    {
        await using var context = NewContext();
        var itemFiscal = new ItemFiscal("COD-1", "Item", "UN", "1.1.01", Guid.NewGuid(), DateTimeOffset.UtcNow);
        await new ItemFiscalRepository(context).AdicionarAsync(itemFiscal, default);
        var fornecedor = await NovoFornecedorComVinculoAsync(context, "003316");
        await context.SaveChangesAsync();

        var reader = new FakeReader(new ItemFiscalReferenciaFornecedorErpDto("COD-1", "COD-FORN-1", "003316", 1));
        var result = await Create(context, reader).ExecuteAsync(new SincronizarItemFiscalReferenciasFornecedorErpDto(100, null, DryRun: true));

        Assert.Equal("DryRunConcluido", result.Status);
        Assert.Equal(1, result.Incluidos);
        Assert.Empty(await context.ItensFiscaisReferenciasFornecedor.ToListAsync());
    }

    private static readonly Guid UnidadeNegocioTeste = Guid.NewGuid();

    private sealed class FakeIdentity : ICurrentIdentity
    {
        public RequestIdentity GetRequired() => new(Guid.NewGuid(), "Buyer", UnidadeNegocioTeste);
    }

    private static SincronizarItemFiscalReferenciasFornecedorErpUseCase Create(BlueprintOSDbContext context, FakeReader reader) =>
        new(reader, new ItemFiscalRepository(context), new FornecedorLinxVinculoRepository(context), new ItemFiscalReferenciaFornecedorRepository(context),
            new FakeIdentity(), NullLogger<SincronizarItemFiscalReferenciasFornecedorErpUseCase>.Instance);

    private static BlueprintOSDbContext NewContext() =>
        new(new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    /// <summary>B3 — Bloco 5A.9: a resolução de referências passou a ser por vínculo Linx conhecido
    /// (`ErpSistema` + `CodigoErp`), não mais por `Fornecedor.ErpFornecedorId` — cria os dois juntos.</summary>
    private static async Task<Fornecedor> NovoFornecedorComVinculoAsync(BlueprintOSDbContext context, string erpFornecedorId)
    {
        var fornecedor = new Fornecedor(Guid.NewGuid(), "Fornecedor Teste", DocumentoFiscal.Create("12345678000195"), "PJ", null, null, null,
            null, null, null, null, "Ativo", null, DateTimeOffset.UtcNow, UnidadeNegocioTeste,
            businessUnit: null, erpSistema: "SOMA_DESENV", erpFornecedorId: erpFornecedorId);
        await new FornecedorRepository(context).AdicionarAsync(fornecedor);
        var vinculo = new FornecedorLinxVinculo(fornecedor.Id, UnidadeNegocioTeste, "SOMA_DESENV", erpFornecedorId, "FORNECEDOR TESTE",
            inativoFornecedores: false, inativoCadastroCliFor: false, DateTimeOffset.UtcNow, principal: true, agora: DateTimeOffset.UtcNow);
        await new FornecedorLinxVinculoRepository(context).AdicionarAsync(vinculo);
        await context.SaveChangesAsync();
        return fornecedor;
    }

    private sealed class FakeReader(params ItemFiscalReferenciaFornecedorErpDto[] referencias) : IItemFiscalReferenciaFornecedorErpReader
    {
        public Task<IReadOnlyList<ItemFiscalReferenciaFornecedorErpDto>> BuscarReferenciasAsync(int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ItemFiscalReferenciaFornecedorErpDto>>(referencias.Skip(skip).Take(take).ToList());
    }
}
