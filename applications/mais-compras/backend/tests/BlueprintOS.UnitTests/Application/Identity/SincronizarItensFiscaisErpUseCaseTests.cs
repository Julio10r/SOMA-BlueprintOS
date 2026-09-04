using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Identity;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;
using BlueprintOS.Infrastructure.Integrations.ERP.Soma;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlueprintOS.UnitTests.Application.Identity;

/// <summary>B3 — Bloco 5A/5A.7: cobre a criação de Itens Fiscais vindos do Linx (caso A) e os seis casos do
/// algoritmo de Last Write Wins homologado pelo Product Owner quando o registro já existe localmente e o
/// conteúdo diverge — nunca uma escolha manual. Compara sempre `DATA_PARA_TRANSFERENCIA` (Linx) com
/// `ItemFiscal.UltimaAlteracaoLocalEm` (+Compras), nunca com o horário da própria sincronização.</summary>
public sealed class SincronizarItensFiscaisErpUseCaseTests
{
    private static readonly DateTimeOffset T1 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T2 = new(2026, 6, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Execute_Should_Insert_New_Item_From_Erp_Preserving_Cadastral_Status()
    {
        await using var context = NewContext();
        var reader = new FakeReader(new ItemFiscalErpDto("COD-1", "Descricao Linx", "UN", "1.1.01", Inativo: false, DateTimeOffset.UtcNow));

        var result = await Create(context, reader).ExecuteAsync(new SincronizarItensFiscaisErpDto(100, "corr-1"));

        var stored = await context.ItensFiscais.SingleAsync();
        Assert.Equal("Concluida", result.Status);
        Assert.Equal(1, result.Consultados);
        Assert.Equal(1, result.Incluidos);
        Assert.Equal(0, result.Erros);
        Assert.Empty(result.Ocorrencias);
        Assert.Equal("COD-1", stored.Codigo);
        Assert.Equal("UN", stored.UnidadeMedidaCodigoErp);
        Assert.Equal("1.1.01", stored.ContaContabilCodigoErp);
        Assert.True(stored.Ativo);
        Assert.Equal(OrigemInformacaoItemFiscal.Linx, stored.OrigemInformacao);
        // Caso A: item nunca teve edição local — nenhum timestamp local é inventado.
        Assert.Null(stored.UltimaAlteracaoLocalEm);
    }

    [Fact]
    public async Task Execute_Should_Insert_New_Item_Without_Inventing_ContaContabil_Or_Unidade()
    {
        await using var context = NewContext();
        // Real na pré-validação: 144 itens ativos reais no Linx sem Conta Contábil; 2 sem Unidade.
        var reader = new FakeReader(new ItemFiscalErpDto("COD-SEM-CONTA", "Descricao Linx", UnidadeErp: null, ContaContabilErp: null, Inativo: false, DateTimeOffset.UtcNow));

        var result = await Create(context, reader).ExecuteAsync(new SincronizarItensFiscaisErpDto(100, null));

        var stored = await context.ItensFiscais.SingleAsync();
        Assert.Equal(1, result.Incluidos);
        Assert.Null(stored.ContaContabilCodigoErp);
        Assert.Null(stored.UnidadeMedidaCodigoErp);
        Assert.True(stored.Ativo);
    }

    [Fact]
    public async Task Execute_Should_Reflect_Real_Cadastral_Inactivation_On_New_Item()
    {
        await using var context = NewContext();
        var reader = new FakeReader(new ItemFiscalErpDto("COD-INATIVO", "Descricao Linx", "UN", "1.1.01", Inativo: true, DateTimeOffset.UtcNow));

        await Create(context, reader).ExecuteAsync(new SincronizarItensFiscaisErpDto(100, null));

        var stored = await context.ItensFiscais.SingleAsync();
        Assert.False(stored.Ativo);
    }

    [Fact]
    public async Task Execute_Should_Classify_As_SemAlteracao_When_Identical()
    {
        await using var context = NewContext();
        var unidadeNegocioId = Guid.NewGuid();
        var existente = ItemFiscal.CriarDeErp("COD-1", "Descricao Linx", "UN", "1.1.01", ativo: true, unidadeNegocioId, T1, DateTimeOffset.UtcNow);
        await new ItemFiscalRepository(context).AdicionarAsync(existente, default);
        await context.SaveChangesAsync();

        // Caso D: conteúdo idêntico — mesmo com um timestamp do Linx diferente do incorporado, não há nada a aplicar.
        var reader = new FakeReader(new ItemFiscalErpDto("COD-1", "Descricao Linx", "UN", "1.1.01", Inativo: false, T2));
        var result = await Create(context, reader).ExecuteAsync(new SincronizarItensFiscaisErpDto(100, null));

        Assert.Equal(1, result.SemAlteracao);
        Assert.Empty(result.Ocorrencias);
    }

    [Fact]
    public async Task Execute_Should_Apply_Linx_When_Linx_Timestamp_Is_Newer_Than_Local_Case_B()
    {
        await using var context = NewContext();
        var unidadeNegocioId = Guid.NewGuid();
        // Item de origem local (+Compras), nunca editado depois da criação — UltimaAlteracaoLocalEm = T1.
        var existente = new ItemFiscal("COD-1", "Descricao Local", "CX", "2.2.02", unidadeNegocioId, T1);
        await new ItemFiscalRepository(context).AdicionarAsync(existente, default);
        await context.SaveChangesAsync();

        var reader = new FakeReader(new ItemFiscalErpDto("COD-1", "Descricao Linx Nova", "UN", "1.1.01", Inativo: false, T2));
        var result = await Create(context, reader).ExecuteAsync(new SincronizarItensFiscaisErpDto(100, null));

        var stored = await context.ItensFiscais.SingleAsync();
        Assert.Equal(1, result.AtualizadosLinxMaisNovo);
        Assert.Equal(0, result.PreservadosLocalMaisNovo);
        Assert.Single(result.Ocorrencias);
        Assert.Equal(ItemFiscalErpDecisaoLww.AtualizadoLinxMaisNovo, result.Ocorrencias[0].Decisao);
        Assert.Equal(T2, result.Ocorrencias[0].DataTransferenciaLinx);
        Assert.Equal(T1, result.Ocorrencias[0].TimestampLocalRelevante);
        Assert.Contains(nameof(ItemFiscal.Descricao), result.Ocorrencias[0].CamposDivergentes);
        Assert.Equal("Descricao Linx Nova", stored.Descricao);
        Assert.Equal("UN", stored.UnidadeMedidaCodigoErp);
        Assert.Equal("1.1.01", stored.ContaContabilCodigoErp);
        Assert.Equal(OrigemInformacaoItemFiscal.Linx, stored.OrigemInformacao);
        Assert.Equal(T2, stored.UltimaAlteracaoErp);
        // O timestamp local relevante não é tocado pela aplicação do Linx.
        Assert.Equal(T1, stored.UltimaAlteracaoLocalEm);
    }

    [Fact]
    public async Task Execute_Should_Preserve_Local_When_Local_Timestamp_Is_Newer_Case_C()
    {
        await using var context = NewContext();
        var unidadeNegocioId = Guid.NewGuid();
        var existente = new ItemFiscal("COD-1", "Descricao Local Original", "CX", "2.2.02", unidadeNegocioId, T1);
        existente.Atualizar("Descricao Local Editada", "CX", "2.2.02", T2);
        await new ItemFiscalRepository(context).AdicionarAsync(existente, default);
        await context.SaveChangesAsync();

        // Linx mais antigo que a última edição local (T1 < T2) — +Compras deve prevalecer.
        var reader = new FakeReader(new ItemFiscalErpDto("COD-1", "Descricao Linx Antiga", "UN", "1.1.01", Inativo: false, T1));
        var result = await Create(context, reader).ExecuteAsync(new SincronizarItensFiscaisErpDto(100, null));

        var stored = await context.ItensFiscais.SingleAsync();
        Assert.Equal(0, result.AtualizadosLinxMaisNovo);
        Assert.Equal(1, result.PreservadosLocalMaisNovo);
        Assert.Single(result.Ocorrencias);
        Assert.Equal(ItemFiscalErpDecisaoLww.PreservadoLocalMaisNovo, result.Ocorrencias[0].Decisao);
        Assert.Equal("Descricao Local Editada", stored.Descricao);
        Assert.Equal("CX", stored.UnidadeMedidaCodigoErp);
        Assert.Equal("2.2.02", stored.ContaContabilCodigoErp);
        Assert.Equal(OrigemInformacaoItemFiscal.MaisCompras, stored.OrigemInformacao);
        Assert.Null(stored.UltimaAlteracaoErp);
    }

    [Fact]
    public async Task Execute_Should_Apply_Linx_On_Tie_With_Divergent_Content_Case_E_Adr0024()
    {
        await using var context = NewContext();
        var unidadeNegocioId = Guid.NewGuid();
        // Timestamp local relevante igual ao do Linx desta rodada — empate, não comparação real.
        var existente = new ItemFiscal("COD-1", "Descricao Local", "CX", "2.2.02", unidadeNegocioId, T1);
        await new ItemFiscalRepository(context).AdicionarAsync(existente, default);
        await context.SaveChangesAsync();

        var reader = new FakeReader(new ItemFiscalErpDto("COD-1", "Descricao Linx", "UN", "1.1.01", Inativo: false, T1));
        var result = await Create(context, reader).ExecuteAsync(new SincronizarItensFiscaisErpDto(100, null));

        var stored = await context.ItensFiscais.SingleAsync();
        Assert.Equal(1, result.AtualizadosEmpateAdr0024);
        Assert.Single(result.Ocorrencias);
        Assert.Equal(ItemFiscalErpDecisaoLww.AtualizadoEmpateAdr0024, result.Ocorrencias[0].Decisao);
        Assert.Equal(T1, result.Ocorrencias[0].DataTransferenciaLinx);
        Assert.Equal(T1, result.Ocorrencias[0].TimestampLocalRelevante);
        // ADR-0024: em ambiguidade, Linx prevalece.
        Assert.Equal("Descricao Linx", stored.Descricao);
        Assert.Equal(OrigemInformacaoItemFiscal.Linx, stored.OrigemInformacao);
    }

    [Fact]
    public async Task Execute_Should_Apply_Linx_When_Local_Timestamp_Is_Unavailable_Case_F_Adr0024()
    {
        await using var context = NewContext();
        var unidadeNegocioId = Guid.NewGuid();
        // Criado direto do Linx e nunca editado localmente: UltimaAlteracaoLocalEm é null (ausência real, não inventada).
        var existente = ItemFiscal.CriarDeErp("COD-1", "Descricao Linx Antiga", "UN", "1.1.01", ativo: true, unidadeNegocioId, T1, DateTimeOffset.UtcNow);
        await new ItemFiscalRepository(context).AdicionarAsync(existente, default);
        await context.SaveChangesAsync();
        Assert.Null(existente.UltimaAlteracaoLocalEm);

        var reader = new FakeReader(new ItemFiscalErpDto("COD-1", "Descricao Linx Nova", "UN", "1.1.01", Inativo: false, T2));
        var result = await Create(context, reader).ExecuteAsync(new SincronizarItensFiscaisErpDto(100, null));

        var stored = await context.ItensFiscais.SingleAsync();
        Assert.Equal(1, result.AtualizadosTimestampIndisponivelAdr0024);
        Assert.Single(result.Ocorrencias);
        Assert.Equal(ItemFiscalErpDecisaoLww.AtualizadoTimestampIndisponivelAdr0024, result.Ocorrencias[0].Decisao);
        Assert.Null(result.Ocorrencias[0].TimestampLocalRelevante);
        Assert.Equal("Descricao Linx Nova", stored.Descricao);
    }

    [Fact]
    public async Task Execute_Should_Apply_Linx_When_Linx_Timestamp_Is_Unavailable_Case_F_Adr0024()
    {
        await using var context = NewContext();
        var unidadeNegocioId = Guid.NewGuid();
        var existente = new ItemFiscal("COD-1", "Descricao Local", "CX", "2.2.02", unidadeNegocioId, T1);
        await new ItemFiscalRepository(context).AdicionarAsync(existente, default);
        await context.SaveChangesAsync();

        // DATA_PARA_TRANSFERENCIA nula no Linx — não há timestamp confiável para comparar.
        var reader = new FakeReader(new ItemFiscalErpDto("COD-1", "Descricao Linx", "UN", "1.1.01", Inativo: false, UltimaAlteracaoEm: null));
        var result = await Create(context, reader).ExecuteAsync(new SincronizarItensFiscaisErpDto(100, null));

        var stored = await context.ItensFiscais.SingleAsync();
        Assert.Equal(1, result.AtualizadosTimestampIndisponivelAdr0024);
        Assert.Null(result.Ocorrencias[0].DataTransferenciaLinx);
        Assert.Equal("Descricao Linx", stored.Descricao);
        Assert.Null(stored.UltimaAlteracaoErp);
    }

    [Fact]
    public async Task Execute_DryRun_Should_Classify_Without_Persisting_Anything()
    {
        await using var context = NewContext();
        var reader = new FakeReader(new ItemFiscalErpDto("COD-1", "Descricao Linx", "UN", "1.1.01", Inativo: false, DateTimeOffset.UtcNow));

        var result = await Create(context, reader).ExecuteAsync(new SincronizarItensFiscaisErpDto(100, null, DryRun: true));

        Assert.Equal("DryRunConcluido", result.Status);
        Assert.Equal(1, result.Incluidos);
        Assert.Equal(0, await context.ItensFiscais.CountAsync());
    }

    [Fact]
    public async Task Execute_DryRun_Should_Classify_Lww_Decision_Without_Persisting_Anything()
    {
        await using var context = NewContext();
        var unidadeNegocioId = Guid.NewGuid();
        var existente = new ItemFiscal("COD-1", "Descricao Local", "CX", "2.2.02", unidadeNegocioId, T1);
        await new ItemFiscalRepository(context).AdicionarAsync(existente, default);
        await context.SaveChangesAsync();

        var reader = new FakeReader(new ItemFiscalErpDto("COD-1", "Descricao Linx Nova", "UN", "1.1.01", Inativo: false, T2));
        var result = await Create(context, reader).ExecuteAsync(new SincronizarItensFiscaisErpDto(100, null, DryRun: true));

        Assert.Equal(1, result.AtualizadosLinxMaisNovo);
        var stored = await context.ItensFiscais.SingleAsync();
        Assert.Equal("Descricao Local", stored.Descricao);
    }

    private static SincronizarItensFiscaisErpUseCase Create(BlueprintOSDbContext context, FakeReader reader) =>
        new(reader, new ItemFiscalRepository(context), new FakeIdentity(), NullLogger<SincronizarItensFiscaisErpUseCase>.Instance);

    private static BlueprintOSDbContext NewContext() =>
        new(new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private sealed class FakeIdentity : ICurrentIdentity
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public Guid UnidadeNegocioId { get; } = Guid.NewGuid();
        public RequestIdentity GetRequired() => new(UserId, "Buyer", UnidadeNegocioId);
    }

    private sealed class FakeReader(params ItemFiscalErpDto[] itens) : IItemFiscalErpReader
    {
        public Task<IReadOnlyList<ItemFiscalErpDto>> BuscarItensFiscaisAsync(int skip, int take, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ItemFiscalErpDto>>(itens.Skip(skip).Take(take).ToList());
    }
}
