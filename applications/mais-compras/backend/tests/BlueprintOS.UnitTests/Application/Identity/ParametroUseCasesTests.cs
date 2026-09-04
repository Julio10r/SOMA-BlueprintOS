using BlueprintOS.Application.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlueprintOS.UnitTests.Application.Identity;

/// <summary>O1.11 — Parâmetros gerais (globais ou por Unidade de Negócio). Cobre: unicidade de
/// (Chave, UnidadeNegocioId) incluindo o caso global (UnidadeNegocioId nulo), 404 de Unidade de Negócio
/// inexistente e exclusão física (decisão explícita da Work Order: Parâmetro não é dado mestre de ERP).</summary>
public sealed class ParametroUseCasesTests
{
    private static readonly Guid Bu = Guid.NewGuid();

    private sealed class FakeUnidadeNegocioRepository : IUnidadeNegocioRepository
    {
        public List<UnidadeNegocio> All { get; } = [];
        public Task<UnidadeNegocio?> ObterPorIdAsync(Guid id, CancellationToken ct) => Task.FromResult(All.SingleOrDefault(x => x.Id == id));
        public Task<UnidadeNegocio?> ObterPorSlugAsync(string slug, CancellationToken ct) => Task.FromResult(All.SingleOrDefault(x => x.Slug == slug));
        public Task<bool> PossuiAdministradorSeniorAtivoAsync(Guid unidadeNegocioId, CancellationToken ct) => Task.FromResult(false);
        public Task AdicionarAsync(UnidadeNegocio unidadeNegocio, CancellationToken ct) { All.Add(unidadeNegocio); return Task.CompletedTask; }
        public Task<IReadOnlyList<UnidadeNegocio>> ListarTodasAsync(CancellationToken ct) => Task.FromResult((IReadOnlyList<UnidadeNegocio>)All);
        public Task<bool> ExisteComSlugAsync(string slug, Guid? excluirId, CancellationToken ct) => Task.FromResult(false);
        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeParametroRepository : IParametroRepository
    {
        public List<Parametro> All { get; } = [];

        public Task<IReadOnlyList<Parametro>> ListarAsync(Guid? unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<Parametro>)All.Where(x => unidadeNegocioId == null || x.UnidadeNegocioId == unidadeNegocioId).ToArray());

        public Task<Parametro?> ObterPorIdAsync(Guid id, CancellationToken ct) => Task.FromResult(All.SingleOrDefault(x => x.Id == id));

        public Task<bool> ExisteComChaveAsync(string chave, Guid? unidadeNegocioId, Guid? excluirId, CancellationToken ct) =>
            Task.FromResult(All.Any(x => x.Chave == chave && x.UnidadeNegocioId == unidadeNegocioId && (excluirId == null || x.Id != excluirId)));

        public Task AdicionarAsync(Parametro parametro, CancellationToken ct) { All.Add(parametro); return Task.CompletedTask; }
        public Task RemoverAsync(Parametro parametro, CancellationToken ct) { All.Remove(parametro); return Task.CompletedTask; }
        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private static (FakeParametroRepository parametros, FakeUnidadeNegocioRepository unidades) Arrange()
    {
        var unidades = new FakeUnidadeNegocioRepository();
        unidades.All.Add(new UnidadeNegocio("SOMA", "soma"));
        return (new FakeParametroRepository(), unidades);
    }

    [Fact]
    public async Task Criar_Should_Allow_Same_Chave_Global_And_Por_Unidade()
    {
        var (parametros, unidades) = Arrange();
        var buId = unidades.All[0].Id;
        var useCase = new CriarParametroUseCase(parametros, unidades, TimeProvider.System, NullLogger<CriarParametroUseCase>.Instance);

        var global = await useCase.ExecuteAsync(new ParametroCriarInput("timeout.segundos", "30", "desc", null), CancellationToken.None);
        var porUnidade = await useCase.ExecuteAsync(new ParametroCriarInput("timeout.segundos", "60", "desc", buId), CancellationToken.None);

        Assert.True(global.Sucesso);
        Assert.True(porUnidade.Sucesso);
        Assert.Equal(2, parametros.All.Count);
    }

    [Fact]
    public async Task Criar_Should_Reject_Duplicate_Chave_Same_Scope()
    {
        var (parametros, unidades) = Arrange();
        var useCase = new CriarParametroUseCase(parametros, unidades, TimeProvider.System, NullLogger<CriarParametroUseCase>.Instance);

        await useCase.ExecuteAsync(new ParametroCriarInput("chave.global", "1", "", null), CancellationToken.None);
        var duplicado = await useCase.ExecuteAsync(new ParametroCriarInput("chave.global", "2", "", null), CancellationToken.None);

        Assert.False(duplicado.Sucesso);
        Assert.Equal(RbacFalha.ParametroDuplicado, duplicado.Falha);
    }

    [Fact]
    public async Task Criar_Should_Return_NotFound_When_UnidadeNegocio_Does_Not_Exist()
    {
        var (parametros, unidades) = Arrange();
        var useCase = new CriarParametroUseCase(parametros, unidades, TimeProvider.System, NullLogger<CriarParametroUseCase>.Instance);

        var resultado = await useCase.ExecuteAsync(new ParametroCriarInput("chave", "1", "", Guid.NewGuid()), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.UnidadeNegocioNaoEncontrada, resultado.Falha);
    }

    [Fact]
    public async Task Excluir_Should_Physically_Remove_Parametro()
    {
        var (parametros, unidades) = Arrange();
        var criar = new CriarParametroUseCase(parametros, unidades, TimeProvider.System, NullLogger<CriarParametroUseCase>.Instance);
        var excluir = new ExcluirParametroUseCase(parametros, NullLogger<ExcluirParametroUseCase>.Instance);

        var criado = await criar.ExecuteAsync(new ParametroCriarInput("chave", "1", "", null), CancellationToken.None);
        var resultado = await excluir.ExecuteAsync(criado.Valor!.Id, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Empty(parametros.All);
    }

    [Fact]
    public async Task Excluir_Should_Return_NotFound_For_Unknown_Id()
    {
        var (parametros, _) = Arrange();
        var excluir = new ExcluirParametroUseCase(parametros, NullLogger<ExcluirParametroUseCase>.Instance);

        var resultado = await excluir.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.ParametroNaoEncontrado, resultado.Falha);
    }
}
