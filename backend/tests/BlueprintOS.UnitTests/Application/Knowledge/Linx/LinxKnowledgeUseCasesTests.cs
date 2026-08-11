using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Knowledge.Linx;
using BlueprintOS.Application.Knowledge.Linx.Contracts;
using BlueprintOS.Application.Knowledge.Linx.Models;
using BlueprintOS.Domain.Knowledge.Linx;

namespace BlueprintOS.UnitTests.Application.Knowledge.Linx;

public sealed class LinxKnowledgeUseCasesTests
{
    private static readonly Guid Bu = Guid.NewGuid();
    private static readonly Guid OutraBu = Guid.NewGuid();
    private static readonly FakeClock Clock = new(DateTimeOffset.Parse("2026-08-11T12:00:00Z"));

    private sealed class FakeClock(DateTimeOffset inicial) : TimeProvider
    {
        public DateTimeOffset Agora { get; set; } = inicial;
        public override DateTimeOffset GetUtcNow() => Agora;
    }

    private sealed class FakeLinxKnowledgeRepository : ILinxKnowledgeRepository
    {
        public List<LinxKnowledgeEntry> All { get; } = [];

        public Task AdicionarAsync(LinxKnowledgeEntry entrada, CancellationToken ct)
        {
            All.Add(entrada);
            return Task.CompletedTask;
        }

        public Task<LinxKnowledgeEntry?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(All.SingleOrDefault(x => x.Id == id));

        public Task<LinxKnowledgeEntry?> ObterUltimaVersaoAsync(Guid versaoRaizId, CancellationToken ct) =>
            Task.FromResult(All.Where(x => x.VersaoRaizId == versaoRaizId).OrderByDescending(x => x.Versao).FirstOrDefault());

        public Task<IReadOnlyList<LinxKnowledgeEntry>> ObterHistoricoAsync(Guid versaoRaizId, CancellationToken ct) =>
            Task.FromResult((IReadOnlyList<LinxKnowledgeEntry>)All.Where(x => x.VersaoRaizId == versaoRaizId).OrderBy(x => x.Versao).ToArray());

        public Task<IReadOnlyList<LinxKnowledgeEntry>> BuscarUltimasVersoesAsync(LinxKnowledgeFiltro filtro, CancellationToken ct)
        {
            var ultimas = All.GroupBy(x => x.VersaoRaizId).Select(g => g.OrderByDescending(x => x.Versao).First()).AsEnumerable();

            if (filtro.Especialista is { } especialista) ultimas = ultimas.Where(x => x.Especialista == especialista);
            if (filtro.UnidadeNegocioId is { } bu) ultimas = ultimas.Where(x => x.UnidadeNegocioId == null || x.UnidadeNegocioId == bu);
            if (!string.IsNullOrWhiteSpace(filtro.Texto))
                ultimas = ultimas.Where(x => x.Conteudo.Contains(filtro.Texto, StringComparison.OrdinalIgnoreCase) || x.Assunto.Contains(filtro.Texto, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult((IReadOnlyList<LinxKnowledgeEntry>)ultimas.ToArray());
        }

        public Task AtualizarProvenienciaAsync(LinxKnowledgeEntry entrada, CancellationToken ct) => Task.CompletedTask;
    }

    private static RegistrarConhecimentoInput NovoInput(
        LinxConhecimentoProveniencia proveniencia = LinxConhecimentoProveniencia.Descoberto,
        Guid? versaoRaizId = null, Guid? unidadeNegocioId = null, string conteudo = "COD_CLIFOR identifica o fornecedor.") =>
        new(LinxEspecialista.LinxDatabaseSpecialist, LinxConhecimentoCategoria.SchemaTabelaColuna,
            "Estrutura de Fornecedor", conteudo, proveniencia, "SomaFornecedorReader", unidadeNegocioId, ["fornecedor"], versaoRaizId);

    [Fact]
    public async Task Registrar_Should_Persist_A_New_Root_Entry()
    {
        var repo = new FakeLinxKnowledgeRepository();
        var useCase = new RegistrarConhecimentoUseCase(repo, Clock);

        var resultado = await useCase.ExecuteAsync(NovoInput(), "agent-database-specialist", CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal(1, resultado.Valor!.Versao);
        Assert.Single(repo.All);
    }

    [Fact]
    public async Task Registrar_Should_Reject_Aprovado_As_Initial_Provenance()
    {
        var repo = new FakeLinxKnowledgeRepository();
        var useCase = new RegistrarConhecimentoUseCase(repo, Clock);

        var resultado = await useCase.ExecuteAsync(NovoInput(LinxConhecimentoProveniencia.Aprovado), "agent", CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.TransicaoProvenienciaInvalida, resultado.Falha);
        Assert.Empty(repo.All);
    }

    [Theory]
    [InlineData(nameof(RegistrarConhecimentoInput.Assunto), RbacFalha.AssuntoObrigatorio)]
    [InlineData(nameof(RegistrarConhecimentoInput.Conteudo), RbacFalha.ConteudoObrigatorio)]
    [InlineData(nameof(RegistrarConhecimentoInput.Fonte), RbacFalha.FonteObrigatoria)]
    public async Task Registrar_Should_Reject_Missing_Required_Fields(string campo, RbacFalha falhaEsperada)
    {
        var repo = new FakeLinxKnowledgeRepository();
        var useCase = new RegistrarConhecimentoUseCase(repo, Clock);
        var input = campo switch
        {
            nameof(RegistrarConhecimentoInput.Assunto) => NovoInput() with { Assunto = " " },
            nameof(RegistrarConhecimentoInput.Conteudo) => NovoInput() with { Conteudo = " " },
            _ => NovoInput() with { Fonte = " " },
        };

        var resultado = await useCase.ExecuteAsync(input, "agent", CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(falhaEsperada, resultado.Falha);
    }

    [Fact]
    public async Task Registrar_New_Version_Should_Not_Overwrite_Previous_Row()
    {
        var repo = new FakeLinxKnowledgeRepository();
        var useCase = new RegistrarConhecimentoUseCase(repo, Clock);
        var v1 = await useCase.ExecuteAsync(NovoInput(), "agent", CancellationToken.None);

        var v2 = await useCase.ExecuteAsync(
            NovoInput(LinxConhecimentoProveniencia.Inferido, v1.Valor!.VersaoRaizId, conteudo: "Refinamento: também identifica o tipo de pessoa."),
            "agent", CancellationToken.None);

        Assert.True(v2.Sucesso);
        Assert.Equal(2, repo.All.Count);
        Assert.Equal(2, v2.Valor!.Versao);
        Assert.Equal(v1.Valor.Id, v2.Valor.EntradaAnteriorId);
        // A versão 1 continua no histórico, inalterada.
        Assert.Contains(repo.All, e => e.Id == v1.Valor.Id && e.Conteudo == v1.Valor.Conteudo);
    }

    /// <summary>Work Order, seção 12: uma nova descoberta que contradiz uma versão já Validada/Aprovada
    /// nunca substitui silenciosamente — o conflito é registrado como falha explícita.</summary>
    [Fact]
    public async Task Registrar_Should_Detect_Conflict_Against_A_Validated_Version_Instead_Of_Silently_Overwriting()
    {
        var repo = new FakeLinxKnowledgeRepository();
        var registrar = new RegistrarConhecimentoUseCase(repo, Clock);
        var promover = new PromoverConhecimentoUseCase(repo, Clock);

        var v1 = await registrar.ExecuteAsync(NovoInput(), "agent", CancellationToken.None);
        await promover.ExecuteAsync(v1.Valor!.Id, LinxConhecimentoProveniencia.Validado, "revisor-humano", CancellationToken.None);

        var conflitante = await registrar.ExecuteAsync(
            NovoInput(LinxConhecimentoProveniencia.Inferido, v1.Valor.VersaoRaizId, conteudo: "CONTEÚDO TOTALMENTE DIFERENTE E CONTRADITÓRIO."),
            "outro-agent", CancellationToken.None);

        Assert.False(conflitante.Sucesso);
        Assert.Equal(RbacFalha.ConflitoDeConhecimentoDetectado, conflitante.Falha);
        Assert.Equal(1, repo.All.Count(e => e.VersaoRaizId == v1.Valor.VersaoRaizId));
    }

    [Fact]
    public async Task Registrar_Should_Allow_A_New_Version_When_Content_Matches_The_Validated_One()
    {
        var repo = new FakeLinxKnowledgeRepository();
        var registrar = new RegistrarConhecimentoUseCase(repo, Clock);
        var promover = new PromoverConhecimentoUseCase(repo, Clock);

        var v1 = await registrar.ExecuteAsync(NovoInput(), "agent", CancellationToken.None);
        await promover.ExecuteAsync(v1.Valor!.Id, LinxConhecimentoProveniencia.Validado, "revisor-humano", CancellationToken.None);

        var v2 = await registrar.ExecuteAsync(
            NovoInput(LinxConhecimentoProveniencia.Descoberto, v1.Valor.VersaoRaizId, conteudo: v1.Valor.Conteudo),
            "agent", CancellationToken.None);

        Assert.True(v2.Sucesso);
    }

    [Fact]
    public async Task Promover_Should_Return_NotFound_For_Unknown_Id()
    {
        var repo = new FakeLinxKnowledgeRepository();
        var useCase = new PromoverConhecimentoUseCase(repo, Clock);

        var resultado = await useCase.ExecuteAsync(Guid.NewGuid(), LinxConhecimentoProveniencia.Validado, "revisor", CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.ConhecimentoLinxNaoEncontrado, resultado.Falha);
    }

    [Fact]
    public async Task Promover_Should_Reject_Invalid_Transition_Via_Domain_Exception_Translated_To_Failure()
    {
        var repo = new FakeLinxKnowledgeRepository();
        var registrar = new RegistrarConhecimentoUseCase(repo, Clock);
        var promover = new PromoverConhecimentoUseCase(repo, Clock);
        var v1 = await registrar.ExecuteAsync(NovoInput(), "agent", CancellationToken.None);

        var resultado = await promover.ExecuteAsync(v1.Valor!.Id, LinxConhecimentoProveniencia.Aprovado, "quem-quer-pular-etapa", CancellationToken.None);

        Assert.False(resultado.Sucesso);
        Assert.Equal(RbacFalha.TransicaoProvenienciaInvalida, resultado.Falha);
    }

    [Fact]
    public async Task Buscar_Should_Scope_By_UnidadeNegocio_Never_Leaking_Across_BUs()
    {
        var repo = new FakeLinxKnowledgeRepository();
        var registrar = new RegistrarConhecimentoUseCase(repo, Clock);
        await registrar.ExecuteAsync(NovoInput(unidadeNegocioId: Bu, conteudo: "Config específica da BU 1"), "agent", CancellationToken.None);
        await registrar.ExecuteAsync(NovoInput(unidadeNegocioId: OutraBu, conteudo: "Config específica da BU 2"), "agent", CancellationToken.None);
        await registrar.ExecuteAsync(NovoInput(unidadeNegocioId: null, conteudo: "Conceito global do Linx"), "agent", CancellationToken.None);

        var buscar = new BuscarConhecimentoUseCase(repo);
        var resultadosBu1 = await buscar.ExecuteAsync(new LinxKnowledgeFiltro(UnidadeNegocioId: Bu), CancellationToken.None);

        Assert.Equal(2, resultadosBu1.Count); // a global + a específica da Bu, nunca a da OutraBu
        Assert.DoesNotContain(resultadosBu1, x => x.Conteudo.Contains("BU 2"));
    }

    [Fact]
    public async Task ObterHistorico_Should_Return_All_Versions_In_Order()
    {
        var repo = new FakeLinxKnowledgeRepository();
        var registrar = new RegistrarConhecimentoUseCase(repo, Clock);
        var v1 = await registrar.ExecuteAsync(NovoInput(), "agent", CancellationToken.None);
        var v2 = await registrar.ExecuteAsync(
            NovoInput(LinxConhecimentoProveniencia.Inferido, v1.Valor!.VersaoRaizId, conteudo: "versão 2"), "agent", CancellationToken.None);

        var historico = new ObterHistoricoConhecimentoUseCase(repo);
        var resultado = await historico.ExecuteAsync(v1.Valor.VersaoRaizId, CancellationToken.None);

        Assert.True(resultado.Sucesso);
        Assert.Equal(2, resultado.Valor!.Count);
        Assert.Equal(1, resultado.Valor[0].Versao);
        Assert.Equal(2, resultado.Valor[1].Versao);
        Assert.Equal(v2.Valor!.Id, resultado.Valor[1].Id);
    }
}
