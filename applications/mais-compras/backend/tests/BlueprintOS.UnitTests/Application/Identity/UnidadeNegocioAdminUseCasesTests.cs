using BlueprintOS.Application.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using Microsoft.Extensions.Logging.Abstractions;

namespace BlueprintOS.UnitTests.Application.Identity;

/// <summary>O1.11 — CRUD real de Unidades de Negócio. Cobre: criação com validação de nome/slug,
/// unicidade de slug, imutabilidade de slug (não há input de edição de slug), inativação preserva o
/// registro (nunca exclusão física) e "não encontrada" para Id inexistente.</summary>
public sealed class UnidadeNegocioAdminUseCasesTests
{
    private sealed class FakeUnidadeNegocioRepository : IUnidadeNegocioRepository
    {
        public List<UnidadeNegocio> All { get; } = [];

        public Task<UnidadeNegocio?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(All.SingleOrDefault(x => x.Id == id));

        public Task<bool> PossuiAdministradorSeniorAtivoAsync(Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(false);

        public Task AdicionarAsync(UnidadeNegocio unidadeNegocio, CancellationToken ct)
        {
            All.Add(unidadeNegocio);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<UnidadeNegocio>> ListarTodasAsync(CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<UnidadeNegocio>)All.OrderBy(x => x.Nome).ToArray());

        public Task<bool> ExisteComSlugAsync(string slug, Guid? excluirId, CancellationToken ct) =>
            Task.FromResult(All.Any(x => x.Slug == slug && (excluirId == null || x.Id != excluirId)));

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private static FakeUnidadeNegocioRepository Arrange() => new();

    private static CriarUnidadeNegocioUseCase Criar(FakeUnidadeNegocioRepository r) =>
        new(
            r,
            new CatalogoInicialPerfisDeNegocioUseCase(
                new FakePerfilRepository(), new FakePermissaoRepository(), TimeProvider.System,
                NullLogger<CatalogoInicialPerfisDeNegocioUseCase>.Instance),
            NullLogger<CriarUnidadeNegocioUseCase>.Instance);

    private static RenomearUnidadeNegocioUseCase Renomear(FakeUnidadeNegocioRepository r) =>
        new(r, NullLogger<RenomearUnidadeNegocioUseCase>.Instance);

    private static AlterarStatusUnidadeNegocioUseCase Status(FakeUnidadeNegocioRepository r) =>
        new(r, NullLogger<AlterarStatusUnidadeNegocioUseCase>.Instance);

    [Fact]
    public async Task Criar_Should_Persist_UnidadeNegocio()
    {
        var repo = Arrange();
        var resultado = await Criar(repo).ExecuteAsync(new UnidadeNegocioCriarInput("SOMA Distribuidora", "soma-distribuidora"), CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal("soma-distribuidora", resultado.Valor!.Slug);
        Assert.True(resultado.Valor.Ativa);
        Assert.Single(repo.All);
    }

    [Fact]
    public async Task Criar_Should_Reject_Duplicate_Slug()
    {
        var repo = Arrange();
        await Criar(repo).ExecuteAsync(new UnidadeNegocioCriarInput("SOMA", "soma"), CancellationToken.None);

        var resultado = await Criar(repo).ExecuteAsync(new UnidadeNegocioCriarInput("SOMA 2", "soma"), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.SlugDuplicado, resultado.Falha);
    }

    [Theory]
    [InlineData("Soma Distribuidora")]
    [InlineData("soma_distribuidora")]
    [InlineData("-soma")]
    [InlineData("soma-")]
    [InlineData("soma--distribuidora")]
    public async Task Criar_Should_Reject_Invalid_Slug(string slugInvalido)
    {
        var repo = Arrange();
        var resultado = await Criar(repo).ExecuteAsync(new UnidadeNegocioCriarInput("Nome", slugInvalido), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.SlugInvalido, resultado.Falha);
    }

    [Fact]
    public async Task Renomear_Should_Not_Change_Slug()
    {
        var repo = Arrange();
        var criado = await Criar(repo).ExecuteAsync(new UnidadeNegocioCriarInput("SOMA", "soma"), CancellationToken.None);

        var renomeado = await Renomear(repo).ExecuteAsync(criado.Valor!.Id, new UnidadeNegocioRenomearInput("SOMA Grupo"), CancellationToken.None);

        Assert.True(renomeado.Sucesso);
        Assert.Equal("SOMA Grupo", renomeado.Valor!.Nome);
        Assert.Equal("soma", renomeado.Valor.Slug);
    }

    [Fact]
    public async Task Renomear_Should_Return_NotFound_For_Unknown_Id()
    {
        var repo = Arrange();
        var resultado = await Renomear(repo).ExecuteAsync(Guid.NewGuid(), new UnidadeNegocioRenomearInput("X"), CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.UnidadeNegocioNaoEncontrada, resultado.Falha);
    }

    [Fact]
    public async Task Inativar_Should_Preserve_Record()
    {
        var repo = Arrange();
        var criado = await Criar(repo).ExecuteAsync(new UnidadeNegocioCriarInput("SOMA", "soma"), CancellationToken.None);

        var inativado = await Status(repo).ExecuteAsync(criado.Valor!.Id, ativa: false, CancellationToken.None);

        Assert.True(inativado.Sucesso);
        Assert.False(inativado.Valor!.Ativa);
        Assert.Single(repo.All);
        Assert.Equal(criado.Valor.Id, repo.All[0].Id);
    }
}
