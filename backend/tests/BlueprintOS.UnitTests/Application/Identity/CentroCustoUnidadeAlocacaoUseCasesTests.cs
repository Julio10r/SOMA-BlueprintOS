using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Administration;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;

namespace BlueprintOS.UnitTests.Application.Identity;

/// <summary>O1.9 — vínculo N:N Centro de Custo × Unidade de Alocação (ADR-0020 item 6, D4/ADR-0021). Cobre:
/// criação do metadado local sob demanda ao vincular pela primeira vez, substituição idempotente do
/// conjunto de vínculos, regra de Unidade de Alocação padrão (no máximo uma por Centro de Custo, deve
/// estar entre as vinculadas), isolamento cross-BU e rejeição de código ERP/Unidade de Alocação
/// inexistentes.</summary>
public sealed class CentroCustoUnidadeAlocacaoUseCasesTests
{
    private static readonly Guid Bu = Guid.NewGuid();
    private static readonly Guid OutraBu = Guid.NewGuid();

    private sealed class FakeCentroCustoErpReader : ICentroCustoErpReader
    {
        public List<CentroCustoErpDto> CentrosCusto { get; } = [];

        public Task<IReadOnlyList<CentroCustoErpDto>> BuscarCentrosCustoAsync(int skip, int take, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<CentroCustoErpDto>)CentrosCusto.Skip(skip).Take(take).ToArray());

        public Task<CentroCustoErpDto?> BuscarPorCodigoAsync(string codigoErp, CancellationToken ct) =>
            Task.FromResult(CentrosCusto.FirstOrDefault(x => x.CodigoErp == codigoErp));
    }

    private sealed class FakeCentroCustoMetadadoRepository : ICentroCustoMetadadoRepository
    {
        public List<CentroCustoMetadado> Registros { get; } = [];

        public Task<IReadOnlyDictionary<string, CentroCustoMetadado>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult((IReadOnlyDictionary<string, CentroCustoMetadado>)Registros
                .Where(x => x.UnidadeNegocioId == unidadeNegocioId)
                .ToDictionary(x => x.CodigoErp, StringComparer.OrdinalIgnoreCase));

        public Task<CentroCustoMetadado?> ObterPorCodigoErpAsync(string codigoErp, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(Registros.SingleOrDefault(x => x.CodigoErp == codigoErp && x.UnidadeNegocioId == unidadeNegocioId));

        public Task<CentroCustoMetadado?> ObterPorCodigoErpGlobalAsync(string codigoErp, CancellationToken ct) =>
            Task.FromResult(Registros.FirstOrDefault(x => x.CodigoErp == codigoErp));

        public Task AdicionarAsync(CentroCustoMetadado metadado, CancellationToken ct)
        {
            Registros.Add(metadado);
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    /// <summary>Reflete no fake a mesma técnica de "zera padrão antes de reaplicar" da implementação real
    /// (evita violar a invariante de padrão único durante a substituição).</summary>
    private sealed class FakeCentroCustoUnidadeAlocacaoRepository : ICentroCustoUnidadeAlocacaoRepository
    {
        public List<CentroCustoUnidadeAlocacao> Registros { get; } = [];

        public Task<IReadOnlyList<CentroCustoUnidadeAlocacao>> ListarPorCentroCustoMetadadoAsync(Guid centroCustoMetadadoId, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<CentroCustoUnidadeAlocacao>)Registros
                .Where(x => x.CentroCustoMetadadoId == centroCustoMetadadoId).ToArray());

        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<CentroCustoUnidadeAlocacao>>> ListarPorCentrosCustoMetadadoAsync(
            IReadOnlyCollection<Guid> centroCustoMetadadoIds, CancellationToken ct) =>
            Task.FromResult((IReadOnlyDictionary<Guid, IReadOnlyList<CentroCustoUnidadeAlocacao>>)Registros
                .Where(x => centroCustoMetadadoIds.Contains(x.CentroCustoMetadadoId))
                .GroupBy(x => x.CentroCustoMetadadoId)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<CentroCustoUnidadeAlocacao>)g.ToArray()));

        public Task SubstituirVinculosAsync(
            Guid centroCustoMetadadoId, IReadOnlyList<(Guid UnidadeAlocacaoId, bool Padrao)> vinculos, CancellationToken ct)
        {
            Registros.RemoveAll(x => x.CentroCustoMetadadoId == centroCustoMetadadoId);
            var agora = DateTimeOffset.UtcNow;
            foreach (var (unidadeAlocacaoId, padrao) in vinculos)
            {
                Registros.Add(new CentroCustoUnidadeAlocacao(centroCustoMetadadoId, unidadeAlocacaoId, padrao, agora));
            }
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeUnidadeAlocacaoRepository : IUnidadeAlocacaoRepository
    {
        public List<UnidadeAlocacao> Registros { get; } = [];

        public Task<IReadOnlyList<UnidadeAlocacao>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<UnidadeAlocacao>)Registros.Where(x => x.UnidadeNegocioId == unidadeNegocioId).ToArray());

        public Task<UnidadeAlocacao?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(Registros.SingleOrDefault(x => x.Id == id && x.UnidadeNegocioId == unidadeNegocioId));

        public Task<IReadOnlyList<UnidadeAlocacao>> ObterPorIdsEUnidadeNegocioAsync(IReadOnlyCollection<Guid> ids, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<UnidadeAlocacao>)Registros.Where(x => ids.Contains(x.Id) && x.UnidadeNegocioId == unidadeNegocioId).ToArray());

        public Task<bool> ExisteComNomeAsync(string nome, Guid unidadeNegocioId, Guid? excluirId, CancellationToken ct) =>
            Task.FromResult(Registros.Any(x => x.UnidadeNegocioId == unidadeNegocioId && x.Nome.Equals(nome, StringComparison.OrdinalIgnoreCase) && (excluirId is null || x.Id != excluirId.Value)));

        public Task AdicionarAsync(UnidadeAlocacao unidadeAlocacao, CancellationToken ct)
        {
            Registros.Add(unidadeAlocacao);
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed record Cenario(
        FakeCentroCustoErpReader Erp, FakeCentroCustoMetadadoRepository Metadados,
        FakeCentroCustoUnidadeAlocacaoRepository Vinculos, FakeUnidadeAlocacaoRepository UnidadesAlocacao);

    private static Cenario Arrange() => new(new(), new(), new(), new());

    private static UnidadeAlocacao CriarUnidadeAlocacao(Cenario c, Guid bu, string nome)
    {
        var unidade = new UnidadeAlocacao(nome, "descrição", bu);
        c.UnidadesAlocacao.Registros.Add(unidade);
        return unidade;
    }

    private static ListarVinculosUnidadeAlocacaoUseCase Listar(Cenario c) => new(c.Erp, c.Metadados, c.Vinculos, c.UnidadesAlocacao);
    private static SubstituirVinculosUnidadeAlocacaoUseCase Substituir(Cenario c) => new(c.Erp, c.Metadados, c.Vinculos, c.UnidadesAlocacao, TimeProvider.System);

    [Fact]
    public async Task Listar_Should_Return_Empty_When_No_Local_Metadata_Exists_Yet()
    {
        var c = Arrange();
        c.Erp.CentrosCusto.Add(new CentroCustoErpDto("CC-001", "Compras Corporativo", null));

        var resultado = await Listar(c).ExecuteAsync("CC-001", Bu, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Empty(resultado.Valor!);
        Assert.Empty(c.Metadados.Registros);
    }

    [Fact]
    public async Task Listar_Should_Reject_Unknown_Erp_Code()
    {
        var c = Arrange();

        var resultado = await Listar(c).ExecuteAsync("CC-999", Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(ErpMetadadoFalha.CodigoErpNaoEncontrado, resultado.Falha);
    }

    [Fact]
    public async Task Substituir_Should_Create_Local_Metadata_On_First_Link()
    {
        var c = Arrange();
        c.Erp.CentrosCusto.Add(new CentroCustoErpDto("CC-001", "Compras Corporativo", null));
        var farm = CriarUnidadeAlocacao(c, Bu, "Farm");

        var resultado = await Substituir(c).ExecuteAsync(
            "CC-001", new SubstituirVinculosUnidadeAlocacaoInput([farm.Id], farm.Id), Bu, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Single(c.Metadados.Registros);
        var vinculo = Assert.Single(resultado.Valor!);
        Assert.Equal("Farm", vinculo.Nome);
        Assert.True(vinculo.Padrao);
    }

    [Fact]
    public async Task Substituir_Should_Reject_Unknown_Erp_Code()
    {
        var c = Arrange();

        var resultado = await Substituir(c).ExecuteAsync(
            "CC-999", new SubstituirVinculosUnidadeAlocacaoInput([], null), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(ErpMetadadoFalha.CodigoErpNaoEncontrado, resultado.Falha);
    }

    [Fact]
    public async Task Substituir_Should_Reject_UnidadeAlocacao_From_Another_UnidadeNegocio()
    {
        var c = Arrange();
        c.Erp.CentrosCusto.Add(new CentroCustoErpDto("CC-001", "Compras Corporativo", null));
        var deOutraBu = CriarUnidadeAlocacao(c, OutraBu, "De outra BU");

        var resultado = await Substituir(c).ExecuteAsync(
            "CC-001", new SubstituirVinculosUnidadeAlocacaoInput([deOutraBu.Id], null), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(ErpMetadadoFalha.UnidadeAlocacaoInvalida, resultado.Falha);
        Assert.Empty(c.Vinculos.Registros);
    }

    [Fact]
    public async Task Substituir_Should_Reject_Padrao_Outside_Vinculo()
    {
        var c = Arrange();
        c.Erp.CentrosCusto.Add(new CentroCustoErpDto("CC-001", "Compras Corporativo", null));
        var farm = CriarUnidadeAlocacao(c, Bu, "Farm");
        var animale = CriarUnidadeAlocacao(c, Bu, "Animale");

        var resultado = await Substituir(c).ExecuteAsync(
            "CC-001", new SubstituirVinculosUnidadeAlocacaoInput([farm.Id], animale.Id), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(ErpMetadadoFalha.PadraoForaDoVinculo, resultado.Falha);
    }

    [Fact]
    public async Task Substituir_Should_Replace_Vinculos_Idempotently_And_Allow_Only_One_Padrao()
    {
        var c = Arrange();
        c.Erp.CentrosCusto.Add(new CentroCustoErpDto("CC-001", "Compras Corporativo", null));
        var farm = CriarUnidadeAlocacao(c, Bu, "Farm");
        var animale = CriarUnidadeAlocacao(c, Bu, "Animale");
        var fabula = CriarUnidadeAlocacao(c, Bu, "Fabula");

        var primeira = await Substituir(c).ExecuteAsync(
            "CC-001", new SubstituirVinculosUnidadeAlocacaoInput([farm.Id, animale.Id], farm.Id), Bu, CancellationToken.None);
        Assert.True(primeira.Sucesso);
        Assert.Equal(2, primeira.Valor!.Count);
        Assert.Equal(1, primeira.Valor.Count(v => v.Padrao));

        // Segunda chamada troca o conjunto e o padrão — deve substituir integralmente, sem duplicar.
        var segunda = await Substituir(c).ExecuteAsync(
            "CC-001", new SubstituirVinculosUnidadeAlocacaoInput([animale.Id, fabula.Id], fabula.Id), Bu, CancellationToken.None);

        Assert.True(segunda.Sucesso);
        Assert.Equal(2, segunda.Valor!.Count);
        Assert.DoesNotContain(segunda.Valor, v => v.Nome == "Farm");
        Assert.Equal(1, segunda.Valor.Count(v => v.Padrao));
        Assert.True(segunda.Valor.Single(v => v.Nome == "Fabula").Padrao);
        Assert.Single(c.Metadados.Registros);
    }

    [Fact]
    public async Task Substituir_Should_Allow_Clearing_All_Vinculos()
    {
        var c = Arrange();
        c.Erp.CentrosCusto.Add(new CentroCustoErpDto("CC-001", "Compras Corporativo", null));
        var farm = CriarUnidadeAlocacao(c, Bu, "Farm");
        await Substituir(c).ExecuteAsync("CC-001", new SubstituirVinculosUnidadeAlocacaoInput([farm.Id], farm.Id), Bu, CancellationToken.None);

        var resultado = await Substituir(c).ExecuteAsync(
            "CC-001", new SubstituirVinculosUnidadeAlocacaoInput([], null), Bu, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Empty(resultado.Valor!);
    }

    [Fact]
    public async Task Substituir_Should_Reject_When_CentroCusto_Already_Anchored_By_Another_UnidadeNegocio()
    {
        var c = Arrange();
        c.Erp.CentrosCusto.Add(new CentroCustoErpDto("CC-001", "Compras Corporativo", null));
        c.Metadados.Registros.Add(new CentroCustoMetadado("CC-001", OutraBu, DateTimeOffset.UtcNow));

        var resultado = await Substituir(c).ExecuteAsync(
            "CC-001", new SubstituirVinculosUnidadeAlocacaoInput([], null), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(ErpMetadadoFalha.AncoradoPorOutraUnidadeDeNegocio, resultado.Falha);
    }

    [Fact]
    public async Task Listar_Should_Reflect_Vinculos_Created_By_Substituir()
    {
        var c = Arrange();
        c.Erp.CentrosCusto.Add(new CentroCustoErpDto("CC-001", "Compras Corporativo", null));
        var farm = CriarUnidadeAlocacao(c, Bu, "Farm");
        await Substituir(c).ExecuteAsync("CC-001", new SubstituirVinculosUnidadeAlocacaoInput([farm.Id], farm.Id), Bu, CancellationToken.None);

        var resultado = await Listar(c).ExecuteAsync("CC-001", Bu, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        var vinculo = Assert.Single(resultado.Valor!);
        Assert.Equal("Farm", vinculo.Nome);
        Assert.True(vinculo.Ativo);
        Assert.True(vinculo.Padrao);
    }

    /// <summary>Isolamento cross-BU na leitura: mesmo que exista metadado local para o código, o vínculo
    /// nunca é visível fora da Unidade de Negócio dele (mesmo cuidado de ObterPorCodigoErpAsync).</summary>
    [Fact]
    public async Task Listar_Should_Not_Leak_Vinculos_From_Another_UnidadeNegocio()
    {
        var c = Arrange();
        c.Erp.CentrosCusto.Add(new CentroCustoErpDto("CC-001", "Compras Corporativo", null));
        var farmDaOutraBu = CriarUnidadeAlocacao(c, OutraBu, "Farm");
        await Substituir(c).ExecuteAsync("CC-001", new SubstituirVinculosUnidadeAlocacaoInput([farmDaOutraBu.Id], farmDaOutraBu.Id), OutraBu, CancellationToken.None);

        var resultado = await Listar(c).ExecuteAsync("CC-001", Bu, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Empty(resultado.Valor!);
    }

    /// <summary>Confirma que a listagem real de Centro de Custo (`CentrosCustoController`/`ListarCentrosCustoUseCase`)
    /// reflete o vínculo N:N: `UnidadeAlocacaoPadraoNome`/`QuantidadeUnidadesAlocacaoVinculadas` deixam de
    /// ser sempre indefinido/zero (dívida documentada no relatório da O1.7) assim que há vínculos reais.</summary>
    [Fact]
    public async Task ListarCentrosCusto_Should_Reflect_Real_Vinculo_Summary()
    {
        var c = Arrange();
        c.Erp.CentrosCusto.Add(new CentroCustoErpDto("CC-001", "Compras Corporativo", null));
        var farm = CriarUnidadeAlocacao(c, Bu, "Farm");
        var animale = CriarUnidadeAlocacao(c, Bu, "Animale");
        await Substituir(c).ExecuteAsync(
            "CC-001", new SubstituirVinculosUnidadeAlocacaoInput([farm.Id, animale.Id], animale.Id), Bu, CancellationToken.None);

        var listarCentrosCusto = new ListarCentrosCustoUseCase(c.Erp, c.Metadados, c.Vinculos, c.UnidadesAlocacao);
        var resultado = await listarCentrosCusto.ExecuteAsync(Bu, CancellationToken.None);

        var item = Assert.Single(resultado);
        Assert.Equal("Animale", item.UnidadeAlocacaoPadraoNome);
        Assert.Equal(2, item.QuantidadeUnidadesAlocacaoVinculadas);
    }
}
