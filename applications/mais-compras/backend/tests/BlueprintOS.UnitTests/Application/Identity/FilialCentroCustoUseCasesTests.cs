using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Administration;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;

namespace BlueprintOS.UnitTests.Application.Identity;

/// <summary>O1.7 — Filiais e Centros de Custo integrados ao ERP. Cobre: listagem combinando ERP + metadados
/// locais (com o padrão "ativo por padrão sem metadado local"), criação do metadado na primeira edição,
/// atualização do existente, e rejeição de código ERP inexistente — nunca cria/edita/exclui o dado ERP.</summary>
public sealed class FilialCentroCustoUseCasesTests
{
    private static readonly Guid Bu = Guid.NewGuid();
    private static readonly Guid OutraBu = Guid.NewGuid();

    private sealed class FakeFilialErpReader : IFilialErpReader
    {
        public List<FilialErpDto> Filiais { get; } = [];

        public Task<IReadOnlyList<FilialErpDto>> BuscarFiliaisAsync(int skip, int take, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<FilialErpDto>)Filiais.Skip(skip).Take(take).ToArray());

        public Task<FilialErpDto?> BuscarPorCodigoAsync(string codigoCliFor, CancellationToken ct) =>
            Task.FromResult(Filiais.FirstOrDefault(x => x.CodigoCliFor == codigoCliFor));
    }

    private sealed class FakeFilialMetadadoRepository : IFilialMetadadoRepository
    {
        public List<FilialMetadado> Registros { get; } = [];

        public Task<IReadOnlyDictionary<string, FilialMetadado>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult((IReadOnlyDictionary<string, FilialMetadado>)Registros
                .Where(x => x.UnidadeNegocioId == unidadeNegocioId)
                .ToDictionary(x => x.CodigoErp, StringComparer.OrdinalIgnoreCase));

        public Task<FilialMetadado?> ObterPorCodigoErpAsync(string codigoErp, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(Registros.SingleOrDefault(x => x.CodigoErp == codigoErp && x.UnidadeNegocioId == unidadeNegocioId));

        public Task AdicionarAsync(FilialMetadado metadado, CancellationToken ct)
        {
            Registros.Add(metadado);
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

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

        public Task<CentroCustoMetadado?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(Registros.SingleOrDefault(x => x.Id == id && x.UnidadeNegocioId == unidadeNegocioId));

        public Task AdicionarAsync(CentroCustoMetadado metadado, CancellationToken ct)
        {
            Registros.Add(metadado);
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    /// <summary>Fake vazio — os testes de O1.7 nesta classe não exercitam o vínculo N:N com Unidade de
    /// Alocação (O1.9), então nunca há vínculos a listar.</summary>
    private sealed class FakeCentroCustoUnidadeAlocacaoRepositoryVazio : ICentroCustoUnidadeAlocacaoRepository
    {
        public Task<IReadOnlyList<CentroCustoUnidadeAlocacao>> ListarPorCentroCustoMetadadoAsync(Guid centroCustoMetadadoId, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<CentroCustoUnidadeAlocacao>)Array.Empty<CentroCustoUnidadeAlocacao>());

        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<CentroCustoUnidadeAlocacao>>> ListarPorCentrosCustoMetadadoAsync(
            IReadOnlyCollection<Guid> centroCustoMetadadoIds, CancellationToken ct) =>
            Task.FromResult((IReadOnlyDictionary<Guid, IReadOnlyList<CentroCustoUnidadeAlocacao>>)new Dictionary<Guid, IReadOnlyList<CentroCustoUnidadeAlocacao>>());

        public Task SubstituirVinculosAsync(Guid centroCustoMetadadoId, IReadOnlyList<(Guid UnidadeAlocacaoId, bool Padrao)> vinculos, CancellationToken ct) =>
            Task.CompletedTask;

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    /// <summary>Fake vazio — os testes de O1.7 nesta classe não cadastram Unidades de Alocação.</summary>
    private sealed class FakeUnidadeAlocacaoRepositoryVazio : IUnidadeAlocacaoRepository
    {
        public Task<IReadOnlyList<UnidadeAlocacao>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<UnidadeAlocacao>)Array.Empty<UnidadeAlocacao>());

        public Task<UnidadeAlocacao?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult<UnidadeAlocacao?>(null);

        public Task<IReadOnlyList<UnidadeAlocacao>> ObterPorIdsEUnidadeNegocioAsync(IReadOnlyCollection<Guid> ids, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<UnidadeAlocacao>)Array.Empty<UnidadeAlocacao>());

        public Task<bool> ExisteComNomeAsync(string nome, Guid unidadeNegocioId, Guid? excluirId, CancellationToken ct) =>
            Task.FromResult(false);

        public Task AdicionarAsync(UnidadeAlocacao unidadeAlocacao, CancellationToken ct) => Task.CompletedTask;

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    [Fact]
    public async Task ListarFiliais_Should_Combine_Erp_With_Local_Metadata_And_Default_To_Active()
    {
        var reader = new FakeFilialErpReader();
        reader.Filiais.Add(new FilialErpDto("0101", "SOMA MATRIZ", null, DateTimeOffset.UtcNow));
        reader.Filiais.Add(new FilialErpDto("0102", "ANIMALE JARDINS", null, DateTimeOffset.UtcNow));
        var metadados = new FakeFilialMetadadoRepository();
        metadados.Registros.Add(new FilialMetadado("0102", Bu, DateTimeOffset.UtcNow, "Loja conceito", false));

        var useCase = new ListarFiliaisUseCase(reader, metadados);
        var resultado = await useCase.ExecuteAsync(Bu, CancellationToken.None);

        Assert.Equal(2, resultado.Count);
        var semMetadado = resultado.Single(x => x.CodigoCliFor == "0101");
        Assert.True(semMetadado.AtivoNoMaisCompras);
        Assert.False(semMetadado.TemMetadadoLocal);

        var comMetadado = resultado.Single(x => x.CodigoCliFor == "0102");
        Assert.False(comMetadado.AtivoNoMaisCompras);
        Assert.Equal("Loja conceito", comMetadado.DescricaoMaisCompras);
        Assert.True(comMetadado.TemMetadadoLocal);
    }

    [Fact]
    public async Task AtualizarMetadadoFilial_Should_Create_On_First_Edit_And_Update_After()
    {
        var reader = new FakeFilialErpReader();
        reader.Filiais.Add(new FilialErpDto("0101", "SOMA MATRIZ", null, null));
        var metadados = new FakeFilialMetadadoRepository();
        var useCase = new AtualizarMetadadoFilialUseCase(reader, metadados, TimeProvider.System);

        var primeira = await useCase.ExecuteAsync("0101", new FilialMetadadoInput("Nova descrição", false), Bu, CancellationToken.None);
        Assert.True(primeira.Sucesso);
        Assert.Single(metadados.Registros);
        Assert.Equal("Nova descrição", primeira.Valor!.DescricaoMaisCompras);
        Assert.False(primeira.Valor.AtivoNoMaisCompras);

        var segunda = await useCase.ExecuteAsync("0101", new FilialMetadadoInput("Outra descrição", true), Bu, CancellationToken.None);
        Assert.True(segunda.Sucesso);
        Assert.Single(metadados.Registros);
        Assert.Equal("Outra descrição", segunda.Valor!.DescricaoMaisCompras);
        Assert.True(segunda.Valor.AtivoNoMaisCompras);
    }

    [Fact]
    public async Task AtualizarMetadadoFilial_Should_Reject_Unknown_Erp_Code()
    {
        var useCase = new AtualizarMetadadoFilialUseCase(new FakeFilialErpReader(), new FakeFilialMetadadoRepository(), TimeProvider.System);

        var resultado = await useCase.ExecuteAsync("0999", new FilialMetadadoInput(null, true), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(ErpMetadadoFalha.CodigoErpNaoEncontrado, resultado.Falha);
    }

    [Fact]
    public async Task ListarCentrosCusto_Should_Combine_Erp_With_Local_Metadata_And_Default_To_Active()
    {
        var reader = new FakeCentroCustoErpReader();
        reader.CentrosCusto.Add(new CentroCustoErpDto("CC-001", "Compras Corporativo", DateTimeOffset.UtcNow));
        var metadados = new FakeCentroCustoMetadadoRepository();

        var useCase = new ListarCentrosCustoUseCase(reader, metadados, new FakeCentroCustoUnidadeAlocacaoRepositoryVazio(), new FakeUnidadeAlocacaoRepositoryVazio());
        var resultado = await useCase.ExecuteAsync(Bu, CancellationToken.None);

        var item = Assert.Single(resultado);
        Assert.Equal("CC-001", item.CodigoErp);
        Assert.True(item.AtivoNoMaisCompras);
        Assert.False(item.TemMetadadoLocal);
    }

    [Fact]
    public async Task AtualizarMetadadoCentroCusto_Should_Reject_Unknown_Erp_Code()
    {
        var useCase = new AtualizarMetadadoCentroCustoUseCase(new FakeCentroCustoErpReader(), new FakeCentroCustoMetadadoRepository(), new FakeCentroCustoUnidadeAlocacaoRepositoryVazio(), new FakeUnidadeAlocacaoRepositoryVazio(), TimeProvider.System);

        var resultado = await useCase.ExecuteAsync("CC-999", new CentroCustoMetadadoInput(null, true), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(ErpMetadadoFalha.CodigoErpNaoEncontrado, resultado.Falha);
    }

    /// <summary>Onda 2 (Multi-BU/Multi-ERP, 03/09/2026, decisão do Product Owner): um código já ancorado em
    /// OUTRA Unidade de Negócio deixa de ser rejeitado — contextos independentes, cada BU ancora seu
    /// próprio metadado local para o mesmo código ERP (substitui o teste anterior desta classe que esperava
    /// rejeição).</summary>
    [Fact]
    public async Task AtualizarMetadadoCentroCusto_Should_Anchor_Independently_When_Anchored_By_Another_Business_Unit()
    {
        var outraUnidade = Guid.NewGuid();
        var reader = new FakeCentroCustoErpReader();
        reader.CentrosCusto.Add(new CentroCustoErpDto("CC-001", "Compras Corporativo", null));
        var metadados = new FakeCentroCustoMetadadoRepository();
        metadados.Registros.Add(new CentroCustoMetadado("CC-001", outraUnidade, DateTimeOffset.UtcNow));
        var useCase = new AtualizarMetadadoCentroCustoUseCase(reader, metadados, new FakeCentroCustoUnidadeAlocacaoRepositoryVazio(), new FakeUnidadeAlocacaoRepositoryVazio(), TimeProvider.System);

        var resultado = await useCase.ExecuteAsync("CC-001", new CentroCustoMetadadoInput("Contexto Bu", true), Bu, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Single(metadados.Registros, x => x.CodigoErp == "CC-001" && x.UnidadeNegocioId == Bu);
        Assert.Single(metadados.Registros, x => x.CodigoErp == "CC-001" && x.UnidadeNegocioId == outraUnidade);
    }

    // ---- O1.6-L2 — validador real (CentroCustoVinculoValidator) ----

    [Fact]
    public async Task CentroCustoVinculoValidator_Should_Accept_Existing_Erp_Code_And_Anchor_Metadata()
    {
        var reader = new FakeCentroCustoErpReader();
        reader.CentrosCusto.Add(new CentroCustoErpDto("CC-001", "Compras Corporativo", null));
        var metadados = new FakeCentroCustoMetadadoRepository();
        var validator = new CentroCustoVinculoValidator(reader, metadados, TimeProvider.System);

        var resultado = await validator.ValidarEAncorarAsync(["CC-001"], Bu, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal(["CC-001"], resultado.Valor);
        Assert.Single(metadados.Registros);
        Assert.Equal(Bu, metadados.Registros[0].UnidadeNegocioId);
    }

    [Fact]
    public async Task CentroCustoVinculoValidator_Should_Reject_Code_Not_Found_In_Erp()
    {
        var validator = new CentroCustoVinculoValidator(new FakeCentroCustoErpReader(), new FakeCentroCustoMetadadoRepository(), TimeProvider.System);

        var resultado = await validator.ValidarEAncorarAsync(["CC-INEXISTENTE"], Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.CentroCustoInvalido, resultado.Falha);
    }

    /// <summary>Onda 2 (Multi-BU/Multi-ERP, 03/09/2026, decisão do Product Owner): um código já ancorado em
    /// OUTRA Unidade de Negócio deixa de ser rejeitado — contextos independentes. A BU da sessão valida
    /// contra o ERP real e ancora seu próprio metadado, sem depender do metadado de outra BU (substitui o
    /// teste anterior desta classe que esperava rejeição).</summary>
    [Fact]
    public async Task CentroCustoVinculoValidator_Should_Anchor_Independently_When_Code_Already_Anchored_To_Another_UnidadeNegocio()
    {
        var reader = new FakeCentroCustoErpReader();
        reader.CentrosCusto.Add(new CentroCustoErpDto("CC-001", "Compras Corporativo", null));
        var metadados = new FakeCentroCustoMetadadoRepository();
        metadados.Registros.Add(new CentroCustoMetadado("CC-001", OutraBu, DateTimeOffset.UtcNow));
        var validator = new CentroCustoVinculoValidator(reader, metadados, TimeProvider.System);

        var resultado = await validator.ValidarEAncorarAsync(["CC-001"], Bu, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Single(metadados.Registros, x => x.CodigoErp == "CC-001" && x.UnidadeNegocioId == Bu);
        Assert.Single(metadados.Registros, x => x.CodigoErp == "CC-001" && x.UnidadeNegocioId == OutraBu);
    }
}
