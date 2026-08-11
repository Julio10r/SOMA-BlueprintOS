using BlueprintOS.Application.Identity;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.UnitTests.Application.Identity;

/// <summary>O1.8 — casos de uso da Gestão de Unidades de Alocação. Cobre: criação/edição, unicidade de
/// nome por Unidade de Negócio, isolamento cross-BU, ativação/inativação. Sem vínculo com Centro de Custo
/// (escopo da O1.9) e sem integração ERP (ADR-0020, item 4).</summary>
public sealed class UnidadeAlocacaoUseCasesTests
{
    private static readonly Guid Bu = Guid.NewGuid();
    private static readonly Guid OutraBu = Guid.NewGuid();

    private sealed class FakeUnidadeAlocacaoRepository : IUnidadeAlocacaoRepository
    {
        public List<UnidadeAlocacao> All { get; } = [];

        public Task<IReadOnlyList<UnidadeAlocacao>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<UnidadeAlocacao>)All
                .Where(x => x.UnidadeNegocioId == unidadeNegocioId)
                .OrderBy(x => x.Nome)
                .ToArray());

        public Task<UnidadeAlocacao?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct) =>
            Task.FromResult(All.SingleOrDefault(x => x.Id == id && x.UnidadeNegocioId == unidadeNegocioId));

        public Task<bool> ExisteComNomeAsync(string nome, Guid unidadeNegocioId, Guid? excluirId, CancellationToken ct) =>
            Task.FromResult(All.Any(x =>
                x.UnidadeNegocioId == unidadeNegocioId
                && x.Nome.Equals(nome, StringComparison.OrdinalIgnoreCase)
                && (excluirId is null || x.Id != excluirId.Value)));

        public Task AdicionarAsync(UnidadeAlocacao unidadeAlocacao, CancellationToken ct)
        {
            All.Add(unidadeAlocacao);
            return Task.CompletedTask;
        }

        public Task SalvarAlteracoesAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private static FakeUnidadeAlocacaoRepository Arrange() => new();

    private static CriarUnidadeAlocacaoUseCase Criar(FakeUnidadeAlocacaoRepository r) => new(r, TimeProvider.System);
    private static AtualizarUnidadeAlocacaoUseCase Atualizar(FakeUnidadeAlocacaoRepository r) => new(r, TimeProvider.System);
    private static AlterarStatusUnidadeAlocacaoUseCase AlterarStatus(FakeUnidadeAlocacaoRepository r) => new(r, TimeProvider.System);
    private static ListarUnidadesAlocacaoUseCase Listar(FakeUnidadeAlocacaoRepository r) => new(r);
    private static ObterUnidadeAlocacaoUseCase Obter(FakeUnidadeAlocacaoRepository r) => new(r);

    [Fact]
    public async Task Criar_Should_Persist_UnidadeAlocacao()
    {
        var r = Arrange();

        var resultado = await Criar(r).ExecuteAsync(new UnidadeAlocacaoInput("Farm", "Marca Farm"), Bu, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal("Farm", resultado.Valor!.Nome);
        Assert.Equal("Marca Farm", resultado.Valor.Descricao);
        Assert.True(resultado.Valor.Ativo);
        Assert.Equal(Bu, resultado.Valor.UnidadeNegocioId);
        Assert.Single(r.All);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Criar_Should_Reject_Empty_Nome(string nome)
    {
        var r = Arrange();

        var resultado = await Criar(r).ExecuteAsync(new UnidadeAlocacaoInput(nome, "desc"), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.NomeObrigatorio, resultado.Falha);
        Assert.Empty(r.All);
    }

    [Fact]
    public async Task Criar_Should_Reject_Duplicated_Nome_In_Same_UnidadeNegocio()
    {
        var r = Arrange();
        await Criar(r).ExecuteAsync(new UnidadeAlocacaoInput("Animale", "x"), Bu, CancellationToken.None);

        var resultado = await Criar(r).ExecuteAsync(new UnidadeAlocacaoInput("ANIMALE", "y"), Bu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.NomeDuplicado, resultado.Falha);
        Assert.Single(r.All);
    }

    /// <summary>O mesmo nome pode existir em Unidades de Negócio diferentes — a unicidade é escopada por
    /// UnidadeNegocioId, nunca global.</summary>
    [Fact]
    public async Task Criar_Should_Allow_Same_Nome_In_Different_UnidadeNegocio()
    {
        var r = Arrange();
        await Criar(r).ExecuteAsync(new UnidadeAlocacaoInput("Projetos Especiais", "x"), Bu, CancellationToken.None);

        var resultado = await Criar(r).ExecuteAsync(new UnidadeAlocacaoInput("Projetos Especiais", "y"), OutraBu, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal(2, r.All.Count);
    }

    [Fact]
    public async Task Atualizar_Should_Change_Nome_And_Descricao()
    {
        var r = Arrange();
        var criado = await Criar(r).ExecuteAsync(new UnidadeAlocacaoInput("Fabula", "desc original"), Bu, CancellationToken.None);

        var resultado = await Atualizar(r).ExecuteAsync(
            criado.Valor!.Id, new UnidadeAlocacaoInput("Fabula Editado", "desc nova"), Bu, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal("Fabula Editado", resultado.Valor!.Nome);
        Assert.Equal("desc nova", resultado.Valor.Descricao);
    }

    [Fact]
    public async Task Atualizar_Should_Reject_Duplicated_Nome_Excluding_Itself()
    {
        var r = Arrange();
        await Criar(r).ExecuteAsync(new UnidadeAlocacaoInput("Nome A", "x"), Bu, CancellationToken.None);
        var b = await Criar(r).ExecuteAsync(new UnidadeAlocacaoInput("Nome B", "y"), Bu, CancellationToken.None);

        // Atualizar "Nome B" mantendo o mesmo nome não deve ser rejeitado como duplicado de si mesmo.
        var semAlteracao = await Atualizar(r).ExecuteAsync(b.Valor!.Id, new UnidadeAlocacaoInput("Nome B", "y2"), Bu, CancellationToken.None);
        Assert.True(semAlteracao.Sucesso);

        var resultado = await Atualizar(r).ExecuteAsync(b.Valor.Id, new UnidadeAlocacaoInput("Nome A", "z"), Bu, CancellationToken.None);
        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.NomeDuplicado, resultado.Falha);
    }

    /// <summary>Isolamento cross-BU: um Id válido de outra Unidade de Negócio nunca é encontrado — tratado
    /// como "não encontrada", nunca revelando a existência em outra BU.</summary>
    [Fact]
    public async Task Atualizar_Should_Return_NotFound_For_UnidadeAlocacao_From_Another_UnidadeNegocio()
    {
        var r = Arrange();
        var criado = await Criar(r).ExecuteAsync(new UnidadeAlocacaoInput("SOMA Corporativo", "x"), Bu, CancellationToken.None);

        var resultado = await Atualizar(r).ExecuteAsync(
            criado.Valor!.Id, new UnidadeAlocacaoInput("Hack", "y"), OutraBu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.UnidadeAlocacaoNaoEncontrada, resultado.Falha);
    }

    [Fact]
    public async Task AlterarStatus_Should_Activate_And_Inactivate()
    {
        var r = Arrange();
        var criado = await Criar(r).ExecuteAsync(new UnidadeAlocacaoInput("Projetos Especiais", "x"), Bu, CancellationToken.None);

        var inativado = await AlterarStatus(r).ExecuteAsync(criado.Valor!.Id, false, Bu, CancellationToken.None);
        Assert.True(inativado.Sucesso);
        Assert.False(inativado.Valor!.Ativo);

        var reativado = await AlterarStatus(r).ExecuteAsync(criado.Valor.Id, true, Bu, CancellationToken.None);
        Assert.True(reativado.Sucesso);
        Assert.True(reativado.Valor!.Ativo);
    }

    [Fact]
    public async Task AlterarStatus_Should_Return_NotFound_For_Id_From_Another_UnidadeNegocio()
    {
        var r = Arrange();
        var criado = await Criar(r).ExecuteAsync(new UnidadeAlocacaoInput("SOMA Corporativo", "x"), Bu, CancellationToken.None);

        var resultado = await AlterarStatus(r).ExecuteAsync(criado.Valor!.Id, false, OutraBu, CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.UnidadeAlocacaoNaoEncontrada, resultado.Falha);
    }

    [Fact]
    public async Task Listar_Should_Scope_By_UnidadeNegocio()
    {
        var r = Arrange();
        await Criar(r).ExecuteAsync(new UnidadeAlocacaoInput("Da Bu", "x"), Bu, CancellationToken.None);
        await Criar(r).ExecuteAsync(new UnidadeAlocacaoInput("Da Outra Bu", "y"), OutraBu, CancellationToken.None);

        var resultado = await Listar(r).ExecuteAsync(Bu, CancellationToken.None);

        Assert.Single(resultado);
        Assert.Equal("Da Bu", resultado[0].Nome);
    }

    [Fact]
    public async Task Obter_Should_Return_Null_For_Id_From_Another_UnidadeNegocio()
    {
        var r = Arrange();
        var criado = await Criar(r).ExecuteAsync(new UnidadeAlocacaoInput("SOMA Corporativo", "x"), Bu, CancellationToken.None);

        var resultado = await Obter(r).ExecuteAsync(criado.Valor!.Id, OutraBu, CancellationToken.None);

        Assert.Null(resultado);
    }
}
