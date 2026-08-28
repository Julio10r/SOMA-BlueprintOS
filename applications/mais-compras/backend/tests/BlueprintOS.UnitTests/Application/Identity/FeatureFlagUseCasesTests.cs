using BlueprintOS.Application.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlueprintOS.UnitTests.Application.Identity;

/// <summary>O1.11 — Feature Flags e o vínculo N:N por Unidade de Negócio. Cobre: catálogo nasce vazio,
/// unicidade de nome, ativação/desativação por Unidade de Negócio (N:N) e 404 de Unidade de Negócio
/// inexistente.</summary>
public sealed class FeatureFlagUseCasesTests
{
    private sealed class FakeUnidadeNegocioRepository : IUnidadeNegocioRepository
    {
        public List<UnidadeNegocio> All { get; } = [];
        public Task<UnidadeNegocio?> ObterPorIdAsync(Guid id, CancellationToken ct) => Task.FromResult(All.SingleOrDefault(x => x.Id == id));
        public Task<bool> PossuiAdministradorSeniorAtivoAsync(Guid unidadeNegocioId, CancellationToken ct) => Task.FromResult(false);
        public Task AdicionarAsync(UnidadeNegocio unidadeNegocio, CancellationToken ct) { All.Add(unidadeNegocio); return Task.CompletedTask; }
        public Task<IReadOnlyList<UnidadeNegocio>> ListarTodasAsync(CancellationToken ct) => Task.FromResult((IReadOnlyList<UnidadeNegocio>)All);
        public Task<bool> ExisteComSlugAsync(string slug, Guid? excluirId, CancellationToken ct) => Task.FromResult(false);
        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeFeatureFlagRepository : IFeatureFlagRepository
    {
        public List<FeatureFlag> Flags { get; } = [];
        public List<FeatureFlagUnidadeNegocio> Status { get; } = [];

        public Task<IReadOnlyList<FeatureFlag>> ListarAsync(CancellationToken ct) => Task.FromResult((IReadOnlyList<FeatureFlag>)Flags);
        public Task<FeatureFlag?> ObterPorIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Flags.SingleOrDefault(x => x.Id == id));
        public Task<bool> ExisteComNomeAsync(string nome, CancellationToken ct) => Task.FromResult(Flags.Any(x => x.Nome == nome));

        public Task<IReadOnlyList<FeatureFlagUnidadeNegocio>> ListarStatusPorFlagAsync(Guid featureFlagId, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<FeatureFlagUnidadeNegocio>)Status.Where(x => x.FeatureFlagId == featureFlagId).ToArray());

        public Task<FeatureFlagUnidadeNegocio?> ObterStatusAsync(Guid featureFlagId, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(Status.SingleOrDefault(x => x.FeatureFlagId == featureFlagId && x.UnidadeNegocioId == unidadeNegocioId));

        public Task AdicionarAsync(FeatureFlag featureFlag, CancellationToken ct) { Flags.Add(featureFlag); return Task.CompletedTask; }
        public Task AdicionarStatusAsync(FeatureFlagUnidadeNegocio status, CancellationToken ct) { Status.Add(status); return Task.CompletedTask; }
        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    [Fact]
    public async Task Listar_Should_Be_Empty_By_Default()
    {
        var flags = new FakeFeatureFlagRepository();
        var unidades = new FakeUnidadeNegocioRepository();
        var useCase = new ListarFeatureFlagsUseCase(flags, new FeatureFlagProjector(unidades, flags));

        var resultado = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Empty(resultado);
    }

    [Fact]
    public async Task Criar_Should_Reject_Duplicate_Nome()
    {
        var flags = new FakeFeatureFlagRepository();
        var unidades = new FakeUnidadeNegocioRepository();
        var useCase = new CriarFeatureFlagUseCase(flags, new FeatureFlagProjector(unidades, flags), TimeProvider.System, NullLogger<CriarFeatureFlagUseCase>.Instance);

        await useCase.ExecuteAsync(new FeatureFlagCriarInput("nova-negociacao-ia", "desc"), CancellationToken.None);
        var duplicado = await useCase.ExecuteAsync(new FeatureFlagCriarInput("nova-negociacao-ia", "outra desc"), CancellationToken.None);

        Assert.False(duplicado.Sucesso);
        Assert.Equal(RbacFalha.FeatureFlagDuplicada, duplicado.Falha);
    }

    [Fact]
    public async Task AlterarStatus_Should_Activate_Flag_For_Specific_UnidadeNegocio()
    {
        var flags = new FakeFeatureFlagRepository();
        var unidades = new FakeUnidadeNegocioRepository();
        var bu = new UnidadeNegocio("SOMA", "soma");
        unidades.All.Add(bu);

        var projector = new FeatureFlagProjector(unidades, flags);
        var criar = new CriarFeatureFlagUseCase(flags, projector, TimeProvider.System, NullLogger<CriarFeatureFlagUseCase>.Instance);
        var alterar = new AlterarStatusFeatureFlagUnidadeUseCase(flags, unidades, projector, TimeProvider.System, NullLogger<AlterarStatusFeatureFlagUnidadeUseCase>.Instance);

        var flag = await criar.ExecuteAsync(new FeatureFlagCriarInput("nova-negociacao-ia", "desc"), CancellationToken.None);
        var resultado = await alterar.ExecuteAsync(flag.Valor!.Id, bu.Id, ativa: true, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        var status = Assert.Single(resultado.Valor!.Status);
        Assert.Equal(bu.Id, status.UnidadeNegocioId);
        Assert.True(status.Ativa);
    }

    [Fact]
    public async Task AlterarStatus_Should_Return_NotFound_When_UnidadeNegocio_Does_Not_Exist()
    {
        var flags = new FakeFeatureFlagRepository();
        var unidades = new FakeUnidadeNegocioRepository();
        var projector = new FeatureFlagProjector(unidades, flags);
        var criar = new CriarFeatureFlagUseCase(flags, projector, TimeProvider.System, NullLogger<CriarFeatureFlagUseCase>.Instance);
        var alterar = new AlterarStatusFeatureFlagUnidadeUseCase(flags, unidades, projector, TimeProvider.System, NullLogger<AlterarStatusFeatureFlagUnidadeUseCase>.Instance);

        var flag = await criar.ExecuteAsync(new FeatureFlagCriarInput("flag-x", "desc"), CancellationToken.None);
        var resultado = await alterar.ExecuteAsync(flag.Valor!.Id, Guid.NewGuid(), ativa: true, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.UnidadeNegocioNaoEncontrada, resultado.Falha);
    }

    [Fact]
    public async Task AlterarStatus_Should_Return_NotFound_For_Unknown_Flag()
    {
        var flags = new FakeFeatureFlagRepository();
        var unidades = new FakeUnidadeNegocioRepository();
        var bu = new UnidadeNegocio("SOMA", "soma");
        unidades.All.Add(bu);
        var projector = new FeatureFlagProjector(unidades, flags);
        var alterar = new AlterarStatusFeatureFlagUnidadeUseCase(flags, unidades, projector, TimeProvider.System, NullLogger<AlterarStatusFeatureFlagUnidadeUseCase>.Instance);

        var resultado = await alterar.ExecuteAsync(Guid.NewGuid(), bu.Id, ativa: true, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.FeatureFlagNaoEncontrada, resultado.Falha);
    }
}
