using BlueprintOS.Domain.Knowledge.Linx;

namespace BlueprintOS.UnitTests.Domain.Knowledge.Linx;

/// <summary>O1.13.5 — proveniência, versionamento e regras de nascimento/promoção do agregado central da
/// base de conhecimento dos Agents Especialistas Linx.</summary>
public sealed class LinxKnowledgeEntryTests
{
    private static readonly DateTimeOffset Agora = DateTimeOffset.Parse("2026-08-11T12:00:00Z");

    private static LinxKnowledgeEntry Criar(LinxConhecimentoProveniencia proveniencia = LinxConhecimentoProveniencia.Descoberto) =>
        LinxKnowledgeEntry.Criar(
            LinxEspecialista.LinxDatabaseSpecialist, LinxConhecimentoCategoria.SchemaTabelaColuna,
            "Estrutura de Fornecedor", "CADASTRO_CLI_FOR possui a coluna COD_CLIFOR.", proveniencia,
            "SomaFornecedorReader", "agent-database-specialist", null, ["fornecedor", "schema"], Agora);

    [Fact]
    public void Criar_Should_Start_As_Version_1_With_Itself_As_Root()
    {
        var entrada = Criar();

        Assert.Equal(1, entrada.Versao);
        Assert.Equal(entrada.Id, entrada.VersaoRaizId);
        Assert.Null(entrada.EntradaAnteriorId);
    }

    [Fact]
    public void Criar_Should_Reject_Aprovado_As_Initial_Provenance() =>
        Assert.Throws<InvalidOperationException>(() => Criar(LinxConhecimentoProveniencia.Aprovado));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_Should_Reject_Empty_Fonte(string fonte)
    {
        var ex = Assert.Throws<ArgumentException>(() => LinxKnowledgeEntry.Criar(
            LinxEspecialista.LinxErpSpecialist, LinxConhecimentoCategoria.RegraFuncional, "Assunto", "Conteúdo",
            LinxConhecimentoProveniencia.Descoberto, fonte, "ator", null, null, Agora));

        Assert.Contains("Fonte", ex.Message);
    }

    [Fact]
    public void EhGlobal_Should_Be_True_When_UnidadeNegocioId_Is_Null()
    {
        var global = Criar();
        Assert.True(global.EhGlobal());

        var buId = Guid.NewGuid();
        var especifica = LinxKnowledgeEntry.Criar(
            LinxEspecialista.LinxDatabaseSpecialist, LinxConhecimentoCategoria.Integracao, "Mapping BU", "conteúdo",
            LinxConhecimentoProveniencia.Descoberto, "fonte", "ator", buId, null, Agora);
        Assert.False(especifica.EhGlobal());
    }

    [Theory]
    [InlineData(LinxConhecimentoProveniencia.Descoberto, LinxConhecimentoProveniencia.Validado, true)]
    [InlineData(LinxConhecimentoProveniencia.Inferido, LinxConhecimentoProveniencia.Validado, true)]
    [InlineData(LinxConhecimentoProveniencia.Validado, LinxConhecimentoProveniencia.Aprovado, true)]
    [InlineData(LinxConhecimentoProveniencia.Descoberto, LinxConhecimentoProveniencia.Aprovado, false)]
    [InlineData(LinxConhecimentoProveniencia.Inferido, LinxConhecimentoProveniencia.Aprovado, false)]
    [InlineData(LinxConhecimentoProveniencia.Validado, LinxConhecimentoProveniencia.Descoberto, false)]
    [InlineData(LinxConhecimentoProveniencia.Aprovado, LinxConhecimentoProveniencia.Validado, false)]
    [InlineData(LinxConhecimentoProveniencia.Descoberto, LinxConhecimentoProveniencia.Inferido, false)]
    public void Promover_Should_Only_Allow_The_Documented_Transitions(
        LinxConhecimentoProveniencia de, LinxConhecimentoProveniencia para, bool valida)
    {
        var entrada = ComProveniencia(de);

        if (valida)
        {
            entrada.Promover(para, "revisor", Agora.AddHours(1));
            Assert.Equal(para, entrada.Proveniencia);
        }
        else
        {
            var antes = entrada.Proveniencia;
            Assert.Throws<InvalidOperationException>(() => entrada.Promover(para, "revisor", Agora.AddHours(1)));
            Assert.Equal(antes, entrada.Proveniencia);
        }
    }

    [Fact]
    public void Promover_To_Aprovado_Should_Never_Be_Reachable_Without_Passing_Through_Validado()
    {
        var descoberto = Criar(LinxConhecimentoProveniencia.Descoberto);
        Assert.Throws<InvalidOperationException>(() => descoberto.Promover(LinxConhecimentoProveniencia.Aprovado, "x", Agora));

        var validado = Criar(LinxConhecimentoProveniencia.Descoberto);
        validado.Promover(LinxConhecimentoProveniencia.Validado, "revisor", Agora.AddHours(1));
        validado.Promover(LinxConhecimentoProveniencia.Aprovado, "aprovador", Agora.AddHours(2));
        Assert.Equal(LinxConhecimentoProveniencia.Aprovado, validado.Proveniencia);
    }

    /// <summary>Helper de teste: como <see cref="LinxKnowledgeEntry.Criar"/> nunca aceita <c>Aprovado</c>
    /// como proveniência inicial, uma entrada nesse estado só existe percorrendo a máquina de estados real
    /// (Descoberto → Validado → Aprovado) — nunca fabricada diretamente.</summary>
    private static LinxKnowledgeEntry ComProveniencia(LinxConhecimentoProveniencia proveniencia)
    {
        if (proveniencia != LinxConhecimentoProveniencia.Aprovado) return Criar(proveniencia);

        var entrada = Criar(LinxConhecimentoProveniencia.Descoberto);
        entrada.Promover(LinxConhecimentoProveniencia.Validado, "revisor", Agora.AddMinutes(1));
        entrada.Promover(LinxConhecimentoProveniencia.Aprovado, "aprovador", Agora.AddMinutes(2));
        return entrada;
    }

    [Fact]
    public void Aprovado_Should_Be_Terminal_And_Reject_Any_Further_Promotion()
    {
        var entrada = Criar(LinxConhecimentoProveniencia.Descoberto);
        entrada.Promover(LinxConhecimentoProveniencia.Validado, "revisor", Agora.AddHours(1));
        entrada.Promover(LinxConhecimentoProveniencia.Aprovado, "aprovador", Agora.AddHours(2));

        Assert.Throws<InvalidOperationException>(() => entrada.Promover(LinxConhecimentoProveniencia.Validado, "x", Agora.AddHours(3)));
        Assert.Throws<InvalidOperationException>(() => entrada.Promover(LinxConhecimentoProveniencia.Descoberto, "x", Agora.AddHours(3)));
    }

    [Fact]
    public void NovaVersao_Should_Never_Mutate_The_Original_Entry()
    {
        var v1 = Criar(LinxConhecimentoProveniencia.Descoberto);
        v1.Promover(LinxConhecimentoProveniencia.Validado, "revisor", Agora.AddHours(1));
        var conteudoOriginal = v1.Conteudo;
        var provenienciaOriginal = v1.Proveniencia;

        var v2 = v1.NovaVersao("Conteúdo refinado — nova coluna descoberta.", LinxConhecimentoProveniencia.Inferido, "nova investigação", "agent", null, Agora.AddDays(1));

        Assert.Equal(conteudoOriginal, v1.Conteudo);
        Assert.Equal(provenienciaOriginal, v1.Proveniencia);
        Assert.NotEqual(v1.Id, v2.Id);
        Assert.Equal(v1.Id, v2.EntradaAnteriorId);
        Assert.Equal(v1.VersaoRaizId, v2.VersaoRaizId);
        Assert.Equal(v1.Versao + 1, v2.Versao);
    }

    [Fact]
    public void NovaVersao_Should_Never_Inherit_Validado_Or_Aprovado_Provenance_Automatically()
    {
        var v1 = Criar(LinxConhecimentoProveniencia.Descoberto);
        v1.Promover(LinxConhecimentoProveniencia.Validado, "revisor", Agora.AddHours(1));

        Assert.Throws<InvalidOperationException>(() =>
            v1.NovaVersao("conteúdo novo", LinxConhecimentoProveniencia.Validado, "fonte", "agent", null, Agora.AddDays(1)));
        Assert.Throws<InvalidOperationException>(() =>
            v1.NovaVersao("conteúdo novo", LinxConhecimentoProveniencia.Aprovado, "fonte", "agent", null, Agora.AddDays(1)));

        var v2 = v1.NovaVersao("conteúdo novo", LinxConhecimentoProveniencia.Inferido, "fonte", "agent", null, Agora.AddDays(1));
        Assert.Equal(LinxConhecimentoProveniencia.Inferido, v2.Proveniencia);
    }
}
