using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Administration;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;

namespace BlueprintOS.UnitTests.Application.Identity;

/// <summary>B3 — Bloco 2: Unidade de Medida, cadastro de apoio importado do Linx (Discovery homologado).
/// Cobre: listagem combinando ERP + metadados locais ("ativo por padrão sem metadado local"), criação do
/// metadado na primeira edição, atualização do existente, e rejeição de código ERP inexistente.</summary>
public sealed class UnidadeMedidaUseCasesTests
{
    private static readonly Guid Bu = Guid.NewGuid();

    private sealed class FakeUnidadeMedidaErpReader : IUnidadeMedidaErpReader
    {
        public List<UnidadeMedidaErpDto> Unidades { get; } = [];

        public Task<IReadOnlyList<UnidadeMedidaErpDto>> BuscarUnidadesAsync(int skip, int take, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<UnidadeMedidaErpDto>)Unidades.Skip(skip).Take(take).ToArray());

        public Task<UnidadeMedidaErpDto?> BuscarPorCodigoAsync(string codigoErp, CancellationToken ct) =>
            Task.FromResult(Unidades.FirstOrDefault(x => x.CodigoErp == codigoErp));
    }

    private sealed class FakeUnidadeMedidaMetadadoRepository : IUnidadeMedidaMetadadoRepository
    {
        public List<UnidadeMedidaMetadado> Registros { get; } = [];

        public Task<IReadOnlyDictionary<string, UnidadeMedidaMetadado>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult((IReadOnlyDictionary<string, UnidadeMedidaMetadado>)Registros
                .Where(x => x.UnidadeNegocioId == unidadeNegocioId)
                .ToDictionary(x => x.CodigoErp, StringComparer.OrdinalIgnoreCase));

        public Task<UnidadeMedidaMetadado?> ObterPorCodigoErpAsync(string codigoErp, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(Registros.SingleOrDefault(x => x.CodigoErp == codigoErp && x.UnidadeNegocioId == unidadeNegocioId));

        public Task AdicionarAsync(UnidadeMedidaMetadado metadado, CancellationToken ct)
        {
            Registros.Add(metadado);
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    [Fact]
    public async Task ListarUnidadesMedida_Should_Combine_Erp_With_Local_Metadata_And_Default_To_Active()
    {
        var reader = new FakeUnidadeMedidaErpReader();
        reader.Unidades.Add(new UnidadeMedidaErpDto("UN", "Unidade", DateTimeOffset.UtcNow));
        reader.Unidades.Add(new UnidadeMedidaErpDto("KG", "Quilograma", DateTimeOffset.UtcNow));
        var metadados = new FakeUnidadeMedidaMetadadoRepository();
        metadados.Registros.Add(new UnidadeMedidaMetadado("KG", Bu, DateTimeOffset.UtcNow, "Peso", false));

        var useCase = new ListarUnidadesMedidaUseCase(reader, metadados);
        var resultado = await useCase.ExecuteAsync(Bu, CancellationToken.None);

        Assert.Equal(2, resultado.Count);
        var semMetadado = resultado.Single(x => x.CodigoErp == "UN");
        Assert.True(semMetadado.AtivoNoMaisCompras);
        Assert.False(semMetadado.TemMetadadoLocal);

        var comMetadado = resultado.Single(x => x.CodigoErp == "KG");
        Assert.False(comMetadado.AtivoNoMaisCompras);
        Assert.Equal("Peso", comMetadado.DescricaoMaisCompras);
        Assert.True(comMetadado.TemMetadadoLocal);
    }

    [Fact]
    public async Task ListarUnidadesMedida_Should_Exclude_Records_With_Blank_Codigo()
    {
        // DECISAO DO PRODUCT OWNER (homologacao Bloco 2, 2026-09-02): ocorrencia real no Linx com codigo
        // nulo/vazio/so espacos ("VAZIO AUXILIAR") nao deve ser disponibilizada para uso funcional no
        // +Compras. Cobre os tres casos: string vazia, so espacos, e um codigo valido controle.
        var reader = new FakeUnidadeMedidaErpReader();
        reader.Unidades.Add(new UnidadeMedidaErpDto(string.Empty, "VAZIO AUXILIAR", null));
        reader.Unidades.Add(new UnidadeMedidaErpDto("   ", "SO ESPACOS", null));
        reader.Unidades.Add(new UnidadeMedidaErpDto("UN", "UNIDADE", null));
        var metadados = new FakeUnidadeMedidaMetadadoRepository();

        var useCase = new ListarUnidadesMedidaUseCase(reader, metadados);
        var resultado = await useCase.ExecuteAsync(Bu, CancellationToken.None);

        var item = Assert.Single(resultado);
        Assert.Equal("UN", item.CodigoErp);
    }

    [Fact]
    public async Task ListarUnidadesMedida_Should_Tolerate_Empty_Description_From_Erp()
    {
        // Comprovado por schema discovery dedicado: DESC_UNIDADE é nullable em UNIDADES — nem toda unidade
        // do Linx tem descrição preenchida.
        var reader = new FakeUnidadeMedidaErpReader();
        reader.Unidades.Add(new UnidadeMedidaErpDto("PC", string.Empty, null));
        var metadados = new FakeUnidadeMedidaMetadadoRepository();

        var useCase = new ListarUnidadesMedidaUseCase(reader, metadados);
        var resultado = await useCase.ExecuteAsync(Bu, CancellationToken.None);

        var item = Assert.Single(resultado);
        Assert.Equal(string.Empty, item.DescricaoErp);
        Assert.True(item.AtivoNoMaisCompras);
    }

    [Fact]
    public async Task AtualizarMetadadoUnidadeMedida_Should_Create_On_First_Edit_And_Update_After()
    {
        var reader = new FakeUnidadeMedidaErpReader();
        reader.Unidades.Add(new UnidadeMedidaErpDto("UN", "Unidade", null));
        var metadados = new FakeUnidadeMedidaMetadadoRepository();
        var useCase = new AtualizarMetadadoUnidadeMedidaUseCase(reader, metadados, TimeProvider.System);

        var primeira = await useCase.ExecuteAsync("UN", new UnidadeMedidaMetadadoInput("Unidade avulsa", false), Bu, CancellationToken.None);
        Assert.True(primeira.Sucesso);
        Assert.Single(metadados.Registros);
        Assert.Equal("Unidade avulsa", primeira.Valor!.DescricaoMaisCompras);
        Assert.False(primeira.Valor.AtivoNoMaisCompras);

        var segunda = await useCase.ExecuteAsync("UN", new UnidadeMedidaMetadadoInput("Outra descrição", true), Bu, CancellationToken.None);
        Assert.True(segunda.Sucesso);
        Assert.Single(metadados.Registros);
        Assert.Equal("Outra descrição", segunda.Valor!.DescricaoMaisCompras);
        Assert.True(segunda.Valor.AtivoNoMaisCompras);
    }

    [Fact]
    public async Task AtualizarMetadadoUnidadeMedida_Should_Reject_Unknown_Erp_Code()
    {
        var useCase = new AtualizarMetadadoUnidadeMedidaUseCase(new FakeUnidadeMedidaErpReader(), new FakeUnidadeMedidaMetadadoRepository(), TimeProvider.System);

        var resultado = await useCase.ExecuteAsync("ZZ", new UnidadeMedidaMetadadoInput(null, true), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(ErpMetadadoFalha.CodigoErpNaoEncontrado, resultado.Falha);
    }

    [Fact]
    public async Task AtualizarMetadadoUnidadeMedida_Should_Reject_Blank_Codigo_Even_If_Present_In_Erp()
    {
        // Defesa em profundidade: mesmo que a linha de codigo vazio exista fisicamente no Linx, tentar
        // criar/editar um metadado local para ela deve falhar como "nao encontrado" — nunca disponibilizar
        // para uso funcional (mesma decisao do Product Owner da listagem).
        var reader = new FakeUnidadeMedidaErpReader();
        reader.Unidades.Add(new UnidadeMedidaErpDto(string.Empty, "VAZIO AUXILIAR", null));
        var useCase = new AtualizarMetadadoUnidadeMedidaUseCase(reader, new FakeUnidadeMedidaMetadadoRepository(), TimeProvider.System);

        var resultado = await useCase.ExecuteAsync("   ", new UnidadeMedidaMetadadoInput("Tentativa", true), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(ErpMetadadoFalha.CodigoErpNaoEncontrado, resultado.Falha);
    }
}
