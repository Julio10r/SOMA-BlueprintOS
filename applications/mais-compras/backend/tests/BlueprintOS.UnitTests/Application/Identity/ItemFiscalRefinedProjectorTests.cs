using BlueprintOS.Application.Identity;

namespace BlueprintOS.UnitTests.Application.Identity;

/// <summary>
/// B3 — Bloco 5A (preparação de certificação final): prova, como computação pura (sem banco), que o
/// processamento em lote reproduz exatamente as mesmas regras já homologadas de
/// <c>SincronizarItensFiscaisErpUseCase.DecidirLww</c> (casos A-F) e <c>ItemFiscal.CriarDeErp</c>/
/// <c>AtualizarDeErp</c> (nunca inventa/rejeita Unidade/Conta Contábil ausentes).
/// </summary>
public sealed class ItemFiscalRefinedProjectorTests
{
    private static readonly DateTimeOffset T1 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T2 = new(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CasoA_Codigo_Novo_Vira_Insert_Mesmo_Sem_Unidade_Ou_Conta()
    {
        var raw = new[] { new ItemFiscalRefinedItem("COD-1", "Item Novo", null, null, InativoErp: false, T1) };

        var plano = ItemFiscalRefinedProjector.Projetar(raw, new Dictionary<string, ItemFiscalExistente>());

        var decisao = Assert.Single(plano.Decisoes);
        Assert.Equal(ItemFiscalRefinedAction.Insert, decisao.Action);
        Assert.Null(decisao.UnidadeErp);
        Assert.Null(decisao.ContaContabilErp);
        Assert.True(decisao.Ativo);
    }

    [Fact]
    public void CasoD_Conteudo_Identico_E_SemAlteracao_Mesmo_Com_Timestamps_Diferentes()
    {
        var existente = new ItemFiscalExistente(Guid.NewGuid(), "Item X", "UN", "1.1.01", true, T1);
        var raw = new[] { new ItemFiscalRefinedItem("COD-1", "Item X", "UN", "1.1.01", InativoErp: false, T2) };

        var plano = ItemFiscalRefinedProjector.Projetar(raw, new Dictionary<string, ItemFiscalExistente> { ["COD-1"] = existente });

        Assert.Equal(ItemFiscalRefinedAction.SemAlteracao, Assert.Single(plano.Decisoes).Action);
    }

    [Fact]
    public void CasoB_Linx_Mais_Novo_Atualiza()
    {
        var existente = new ItemFiscalExistente(Guid.NewGuid(), "Antigo", "UN", "1.1.01", true, T1);
        var raw = new[] { new ItemFiscalRefinedItem("COD-1", "Novo", "UN", "1.1.01", InativoErp: false, T2) };

        var plano = ItemFiscalRefinedProjector.Projetar(raw, new Dictionary<string, ItemFiscalExistente> { ["COD-1"] = existente });

        Assert.Equal(ItemFiscalRefinedAction.AtualizarDeErp, Assert.Single(plano.Decisoes).Action);
    }

    [Fact]
    public void CasoC_Local_Mais_Novo_Preserva_Sem_Escrever()
    {
        var existente = new ItemFiscalExistente(Guid.NewGuid(), "Local Mais Novo", "UN", "1.1.01", true, T2);
        var raw = new[] { new ItemFiscalRefinedItem("COD-1", "Linx Antigo", "UN", "1.1.01", InativoErp: false, T1) };

        var plano = ItemFiscalRefinedProjector.Projetar(raw, new Dictionary<string, ItemFiscalExistente> { ["COD-1"] = existente });

        Assert.Equal(ItemFiscalRefinedAction.PreservarLocal, Assert.Single(plano.Decisoes).Action);
    }

    [Fact]
    public void CasoE_Empate_Linx_Prevalece_Adr0024()
    {
        var existente = new ItemFiscalExistente(Guid.NewGuid(), "Antigo", "UN", "1.1.01", true, T1);
        var raw = new[] { new ItemFiscalRefinedItem("COD-1", "Novo", "UN", "1.1.01", InativoErp: false, T1) };

        var plano = ItemFiscalRefinedProjector.Projetar(raw, new Dictionary<string, ItemFiscalExistente> { ["COD-1"] = existente });

        Assert.Equal(ItemFiscalRefinedAction.AtualizarDeErp, Assert.Single(plano.Decisoes).Action);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void CasoF_Timestamp_Indisponivel_De_Qualquer_Lado_Linx_Prevalece_Adr0024(bool localNulo, bool linxNulo)
    {
        var existente = new ItemFiscalExistente(Guid.NewGuid(), "Antigo", "UN", "1.1.01", true, localNulo ? null : T1);
        var raw = new[] { new ItemFiscalRefinedItem("COD-1", "Novo", "UN", "1.1.01", InativoErp: false, linxNulo ? null : T2) };

        var plano = ItemFiscalRefinedProjector.Projetar(raw, new Dictionary<string, ItemFiscalExistente> { ["COD-1"] = existente });

        Assert.Equal(ItemFiscalRefinedAction.AtualizarDeErp, Assert.Single(plano.Decisoes).Action);
    }

    [Fact]
    public void Codigo_Vazio_E_Rejeitado_Nunca_Inventado()
    {
        var raw = new[] { new ItemFiscalRefinedItem("   ", "Descricao", "UN", "1.1.01", InativoErp: false, T1) };

        var plano = ItemFiscalRefinedProjector.Projetar(raw, new Dictionary<string, ItemFiscalExistente>());

        Assert.Empty(plano.Decisoes);
        Assert.Equal("CODIGO_ERP_VAZIO", Assert.Single(plano.Rejeicoes).Code);
    }

    [Fact]
    public void Inativo_No_Linx_Reflete_Como_Ativo_Falso()
    {
        var raw = new[] { new ItemFiscalRefinedItem("COD-1", "Item", null, null, InativoErp: true, T1) };

        var plano = ItemFiscalRefinedProjector.Projetar(raw, new Dictionary<string, ItemFiscalExistente>());

        Assert.False(Assert.Single(plano.Decisoes).Ativo);
    }

    /// <summary>Onda 2 — auditoria RAW determinística (04/09/2026): sob Incremental, RAW é append-only — o
    /// mesmo CodigoErp pode aparecer 2x (linha antiga + recém-anexada). Sem desempate explícito, o Insert
    /// duplicado colidiria com o índice único de Codigo e o Update produziria resultado não determinístico.
    /// Deve vencer a versão mais recente por UltimaAlteracaoErp, independente da ordem de entrada.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Mesmo_Codigo_Duplicado_Resolve_Pela_Versao_Mais_Recente_Independente_Da_Ordem(bool inverterOrdem)
    {
        var antiga = new ItemFiscalRefinedItem("COD-1", "Descricao Antiga", "UN-A", "1.1.01", InativoErp: false, T1, Id: 10);
        var recente = new ItemFiscalRefinedItem("COD-1", "Descricao Recente", "UN-B", "1.1.02", InativoErp: true, T2, Id: 11);
        var raw = inverterOrdem ? new[] { recente, antiga } : new[] { antiga, recente };

        var plano = ItemFiscalRefinedProjector.Projetar(raw, new Dictionary<string, ItemFiscalExistente>());

        var decisao = Assert.Single(plano.Decisoes);
        Assert.Equal("Descricao Recente", decisao.Descricao);
        Assert.False(decisao.Ativo);
    }

    /// <summary>Empate exato de UltimaAlteracaoErp entre duas versões do mesmo código: desempate estável por
    /// maior Id (RAW só cresce sob Incremental — Id mais alto é sempre a linha fisicamente mais recente).</summary>
    [Fact]
    public void Mesmo_Codigo_Duplicado_Com_Empate_De_UltimaAlteracao_Desempata_Por_Maior_Id()
    {
        var raw = new[]
        {
            new ItemFiscalRefinedItem("COD-1", "A", "UN-A", "1.1.01", InativoErp: false, T1, Id: 5),
            new ItemFiscalRefinedItem("COD-1", "B", "UN-B", "1.1.02", InativoErp: true, T1, Id: 9),
        };

        var plano = ItemFiscalRefinedProjector.Projetar(raw, new Dictionary<string, ItemFiscalExistente>());

        var decisao = Assert.Single(plano.Decisoes);
        Assert.Equal("B", decisao.Descricao); // Id=9 (maior) venceu
    }
}
