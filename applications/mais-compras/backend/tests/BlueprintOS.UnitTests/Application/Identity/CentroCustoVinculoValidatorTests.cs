using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Administration;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;

namespace BlueprintOS.UnitTests.Application.Identity;

/// <summary>DEB-15/M2 (Gate Final pós-O1.14) — cobre a correção da ancoragem de <see cref="CentroCustoMetadado"/>
/// sem transação compartilhada com o vínculo Usuário×Centro de Custo (O1.7). Antes desta correção,
/// <see cref="CentroCustoVinculoValidator"/> chamava <c>SalvarAlteracoesAsync</c> imediatamente após ancorar um
/// metadado novo — em uma transação separada da que persiste o Usuário/vínculo em
/// <c>CriarUsuarioUseCase</c>/<c>AtualizarUsuarioUseCase</c>. Se a segunda chamada falhasse (ex.: corrida no
/// índice único de e-mail), o metadado já estaria commitado, órfão. A correção remove a chamada intermediária:
/// o metadado passa a ser apenas rastreado (<c>AdicionarAsync</c>) no mesmo <c>DbContext</c> compartilhado,
/// persistido junto com o restante na única chamada final do caso de uso chamador.</summary>
public sealed class CentroCustoVinculoValidatorTests
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

    /// <summary>Espião: registra quantas vezes <c>SalvarAlteracoesAsync</c> foi chamado, para provar que o
    /// validador nunca persiste por conta própria (DEB-15/M2) — apenas rastreia via <c>AdicionarAsync</c>.</summary>
    private sealed class SpyCentroCustoMetadadoRepository : ICentroCustoMetadadoRepository
    {
        public List<CentroCustoMetadado> Registros { get; } = [];
        public int ChamadasSalvarAlteracoes { get; private set; }

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

        public Task SalvarAlteracoesAsync(CancellationToken ct)
        {
            ChamadasSalvarAlteracoes++;
            return Task.CompletedTask;
        }
    }

    private static (CentroCustoVinculoValidator Validator, FakeCentroCustoErpReader Erp, SpyCentroCustoMetadadoRepository Metadados) Arrange()
    {
        var erp = new FakeCentroCustoErpReader();
        var metadados = new SpyCentroCustoMetadadoRepository();
        var validator = new CentroCustoVinculoValidator(erp, metadados, TimeProvider.System);
        return (validator, erp, metadados);
    }

    [Fact]
    public async Task ValidarEAncorarAsync_Should_Track_New_Metadado_Without_Calling_SalvarAlteracoesAsync()
    {
        var (validator, erp, metadados) = Arrange();
        erp.CentrosCusto.Add(new CentroCustoErpDto("CC-100", "Centro de Custo 100", DateTimeOffset.UtcNow));

        var resultado = await validator.ValidarEAncorarAsync(["CC-100"], Bu, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        // A ancoragem foi rastreada no DbContext...
        Assert.Single(metadados.Registros, x => x.CodigoErp == "CC-100" && x.UnidadeNegocioId == Bu);
        // ...mas NUNCA persistida por conta própria: fica a cargo do SalvarAlteracoesAsync único do caso de
        // uso chamador (CriarUsuarioUseCase/AtualizarUsuarioUseCase), na mesma transação do Usuário e do
        // vínculo Usuário×Centro de Custo. Antes da correção do DEB-15/M2, este contador seria 1.
        Assert.Equal(0, metadados.ChamadasSalvarAlteracoes);
    }

    [Fact]
    public async Task ValidarEAncorarAsync_Should_Not_Call_SalvarAlteracoesAsync_When_Codigo_Already_Anchored()
    {
        var (validator, erp, metadados) = Arrange();
        metadados.Registros.Add(new CentroCustoMetadado("CC-200", Bu, DateTimeOffset.UtcNow));

        var resultado = await validator.ValidarEAncorarAsync(["CC-200"], Bu, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal(0, metadados.ChamadasSalvarAlteracoes);
    }

    /// <summary>Onda 2 (Multi-BU/Multi-ERP, 03/09/2026, decisão do Product Owner): um código ERP já ancorado
    /// em OUTRA Unidade de Negócio deixa de ser rejeitado — são contextos independentes. A Unidade de
    /// Negócio da sessão passa a validar contra o ERP real e ancorar seu PRÓPRIO metadado, sem tocar nem
    /// depender do metadado já existente em outra BU (substitui o teste anterior desta classe que esperava
    /// rejeição por "pertence a outra Unidade de Negócio").</summary>
    [Fact]
    public async Task ValidarEAncorarAsync_Should_Anchor_Independently_When_Codigo_Already_Anchored_By_Other_UnidadeNegocio()
    {
        var (validator, erp, metadados) = Arrange();
        metadados.Registros.Add(new CentroCustoMetadado("CC-300", OutraBu, DateTimeOffset.UtcNow));
        erp.CentrosCusto.Add(new CentroCustoErpDto("CC-300", "Centro de Custo 300", DateTimeOffset.UtcNow));

        var resultado = await validator.ValidarEAncorarAsync(["CC-300"], Bu, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Single(metadados.Registros, x => x.CodigoErp == "CC-300" && x.UnidadeNegocioId == Bu);
        Assert.Single(metadados.Registros, x => x.CodigoErp == "CC-300" && x.UnidadeNegocioId == OutraBu);
        Assert.Equal(0, metadados.ChamadasSalvarAlteracoes);
    }

    [Fact]
    public async Task ValidarEAncorarAsync_Should_Reject_Codigo_Nao_Existente_No_Erp_Without_Saving()
    {
        var (validator, erp, metadados) = Arrange();

        var resultado = await validator.ValidarEAncorarAsync(["CC-INEXISTENTE"], Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(0, metadados.ChamadasSalvarAlteracoes);
        Assert.Empty(metadados.Registros);
    }
}
