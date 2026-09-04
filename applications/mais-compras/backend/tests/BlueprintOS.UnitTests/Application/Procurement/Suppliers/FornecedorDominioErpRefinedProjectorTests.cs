using BlueprintOS.Application.Procurement.Suppliers;

namespace BlueprintOS.UnitTests.Application.Procurement.Suppliers;

public sealed class FornecedorDominioErpRefinedProjectorTests
{
    [Fact]
    public void Codigo_Novo_Vira_Insert()
    {
        var raw = new[] { new FornecedorDominioErpRefinedItem("TipoFornecedor", "IND", "IND", null) };

        var plano = FornecedorDominioErpRefinedProjector.Projetar(raw, new Dictionary<(string, string), FornecedorDominioErpExistente>());

        var decisao = Assert.Single(plano.Decisoes);
        Assert.Equal(FornecedorDominioErpRefinedAction.Insert, decisao.Action);
        Assert.Equal(1, plano.Inseridos);
    }

    [Fact]
    public void Codigo_Existente_Com_Mesma_Descricao_E_NoChange()
    {
        var raw = new[] { new FornecedorDominioErpRefinedItem("TipoFornecedor", "IND", "IND", null) };
        var existentes = new Dictionary<(string, string), FornecedorDominioErpExistente> { [("TipoFornecedor", "IND")] = new(Guid.NewGuid(), "IND") };

        var plano = FornecedorDominioErpRefinedProjector.Projetar(raw, existentes);

        Assert.Equal(FornecedorDominioErpRefinedAction.NoChange, Assert.Single(plano.Decisoes).Action);
    }

    [Fact]
    public void Codigo_Existente_Com_Descricao_Diferente_E_Update()
    {
        var raw = new[] { new FornecedorDominioErpRefinedItem("CondicaoPagamento", "030", "30 DIAS", null) };
        var existentes = new Dictionary<(string, string), FornecedorDominioErpExistente> { [("CondicaoPagamento", "030")] = new(Guid.NewGuid(), "TRINTA DIAS") };

        var plano = FornecedorDominioErpRefinedProjector.Projetar(raw, existentes);

        Assert.Equal(FornecedorDominioErpRefinedAction.Update, Assert.Single(plano.Decisoes).Action);
    }

    [Fact]
    public void Codigo_Erp_Vazio_E_Rejeitado_Nunca_Inventado()
    {
        // Achado real (COND_ENT_PGTOS tem 1 linha com CONDICAO_PGTO em branco): rejeitar, nunca criar com
        // código vazio nem quebrar a execução.
        var raw = new[] { new FornecedorDominioErpRefinedItem("CondicaoPagamento", "   ", "60/90/100 D.D.D", null) };

        var plano = FornecedorDominioErpRefinedProjector.Projetar(raw, new Dictionary<(string, string), FornecedorDominioErpExistente>());

        Assert.Empty(plano.Decisoes);
        var rejeicao = Assert.Single(plano.Rejeicoes);
        Assert.Equal("CODIGO_ERP_VAZIO", rejeicao.Code);
    }

    [Fact]
    public void Chave_Composta_De_Subtipo_Diferencia_Mesmo_Codigo_Sob_Tipos_Diferentes()
    {
        var raw = new[]
        {
            new FornecedorDominioErpRefinedItem("SubtipoFornecedor", "IND:01", "ADIANTAMENTO", null),
            new FornecedorDominioErpRefinedItem("SubtipoFornecedor", "PESSOAL:01", "ADIANTAMENTO", null),
        };

        var plano = FornecedorDominioErpRefinedProjector.Projetar(raw, new Dictionary<(string, string), FornecedorDominioErpExistente>());

        Assert.Equal(2, plano.Inseridos);
    }

    /// <summary>Onda 2 — auditoria RAW determinística (04/09/2026): embora este dataset seja FULL apenas
    /// (RAW sempre truncado — nunca acumula entre execuções), a MESMA leitura pode conter 2 linhas para a
    /// mesma chave (TipoDominio, CodigoErp) — dado sujo real na origem (FORNECEDOR_TIPOS/SUBTIPO/
    /// COND_ENT_PGTOS). Sem desempate, 2 Insert colidiriam com a unicidade da chave; 2 Update produziriam
    /// resultado não determinístico. Deve vencer a versão mais recente por UltimaAlteracao, independente da
    /// ordem de entrada.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Mesma_Chave_Duplicada_Resolve_Pela_Versao_Mais_Recente_Independente_Da_Ordem(bool inverterOrdem)
    {
        var antiga = new FornecedorDominioErpRefinedItem("TipoFornecedor", "IND", "INDUSTRIA (ANTIGO)", new DateTime(2026, 1, 1), Id: 10);
        var recente = new FornecedorDominioErpRefinedItem("TipoFornecedor", "IND", "INDUSTRIA", new DateTime(2026, 2, 1), Id: 11);
        var raw = inverterOrdem ? new[] { recente, antiga } : new[] { antiga, recente };

        var plano = FornecedorDominioErpRefinedProjector.Projetar(raw, new Dictionary<(string, string), FornecedorDominioErpExistente>());

        var decisao = Assert.Single(plano.Decisoes);
        Assert.Equal("INDUSTRIA", decisao.Descricao);
        Assert.Equal(1, plano.Inseridos);
    }

    /// <summary>Empate exato de UltimaAlteracao entre duas versões da mesma chave: desempate estável por
    /// maior Id.</summary>
    [Fact]
    public void Mesma_Chave_Duplicada_Com_Empate_De_UltimaAlteracao_Desempata_Por_Maior_Id()
    {
        var mesmaData = new DateTime(2026, 1, 1);
        var raw = new[]
        {
            new FornecedorDominioErpRefinedItem("TipoFornecedor", "IND", "A", mesmaData, Id: 5),
            new FornecedorDominioErpRefinedItem("TipoFornecedor", "IND", "B", mesmaData, Id: 9),
        };

        var plano = FornecedorDominioErpRefinedProjector.Projetar(raw, new Dictionary<(string, string), FornecedorDominioErpExistente>());

        Assert.Equal("B", Assert.Single(plano.Decisoes).Descricao); // Id=9 (maior) venceu
    }
}
