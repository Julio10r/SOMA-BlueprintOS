using BlueprintOS.Application.Identity;

namespace BlueprintOS.UnitTests.Application.Identity;

public sealed class ItemFiscalReferenciaFornecedorRefinedProjectorTests
{
    private static readonly Dictionary<(Guid, Guid), ItemFiscalReferenciaFornecedorExistente> SemExistentes = new();
    private static readonly Dictionary<(Guid, string), Guid> SemCodigosUsados = new();

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Resolucao_Diferente_De_Um_Vira_Conflito_Nunca_Escolhido_Arbitrariamente(int fornecedoresResolvidos)
    {
        var raw = new[] { new ItemFiscalReferenciaFornecedorRefinedItem("COD-1", "REF-1", fornecedoresResolvidos == 0 ? null : "000001", fornecedoresResolvidos) };

        var plano = ItemFiscalReferenciaFornecedorRefinedProjector.Projetar(raw, new Dictionary<string, Guid>(), new Dictionary<string, Guid>(), SemExistentes, SemCodigosUsados);

        Assert.Empty(plano.Decisoes);
        Assert.Equal("NOME_FORNECEDOR_NAO_RESOLVIDO_OU_AMBIGUO", Assert.Single(plano.Conflitos).Code);
    }

    [Fact]
    public void Item_Fiscal_Nao_Sincronizado_Localmente_Vira_Conflito()
    {
        var raw = new[] { new ItemFiscalReferenciaFornecedorRefinedItem("COD-1", "REF-1", "000001", 1) };

        var plano = ItemFiscalReferenciaFornecedorRefinedProjector.Projetar(raw, new Dictionary<string, Guid>(), new Dictionary<string, Guid> { ["000001"] = Guid.NewGuid() }, SemExistentes, SemCodigosUsados);

        Assert.Equal("ITEM_FISCAL_AINDA_NAO_SINCRONIZADO_LOCALMENTE", Assert.Single(plano.Conflitos).Code);
    }

    [Fact]
    public void Fornecedor_Sem_Vinculo_Local_Vira_Conflito_Nunca_Usa_Cnpj_Fallback()
    {
        var itemFiscalId = Guid.NewGuid();
        var raw = new[] { new ItemFiscalReferenciaFornecedorRefinedItem("COD-1", "REF-1", "000001", 1) };

        var plano = ItemFiscalReferenciaFornecedorRefinedProjector.Projetar(
            raw, new Dictionary<string, Guid> { ["COD-1"] = itemFiscalId }, new Dictionary<string, Guid>(), SemExistentes, SemCodigosUsados);

        Assert.Equal("FORNECEDOR_AINDA_NAO_SINCRONIZADO_LOCALMENTE", Assert.Single(plano.Conflitos).Code);
    }

    [Fact]
    public void Resolucao_Completa_Sem_Existente_Vira_Insert()
    {
        var itemFiscalId = Guid.NewGuid();
        var fornecedorId = Guid.NewGuid();
        var raw = new[] { new ItemFiscalReferenciaFornecedorRefinedItem("COD-1", "REF-1", "000001", 1) };

        var plano = ItemFiscalReferenciaFornecedorRefinedProjector.Projetar(
            raw, new Dictionary<string, Guid> { ["COD-1"] = itemFiscalId }, new Dictionary<string, Guid> { ["000001"] = fornecedorId }, SemExistentes, SemCodigosUsados);

        var decisao = Assert.Single(plano.Decisoes);
        Assert.Equal(ItemFiscalReferenciaFornecedorRefinedAction.Insert, decisao.Action);
        Assert.Equal(itemFiscalId, decisao.ItemFiscalId);
        Assert.Equal(fornecedorId, decisao.FornecedorId);
    }

    [Fact]
    public void Existente_Com_Mesmo_Codigo_E_NoChange()
    {
        var itemFiscalId = Guid.NewGuid();
        var fornecedorId = Guid.NewGuid();
        var existenteId = Guid.NewGuid();
        var raw = new[] { new ItemFiscalReferenciaFornecedorRefinedItem("COD-1", "REF-1", "000001", 1) };
        var existentes = new Dictionary<(Guid, Guid), ItemFiscalReferenciaFornecedorExistente> { [(itemFiscalId, fornecedorId)] = new(existenteId, "REF-1") };

        var plano = ItemFiscalReferenciaFornecedorRefinedProjector.Projetar(
            raw, new Dictionary<string, Guid> { ["COD-1"] = itemFiscalId }, new Dictionary<string, Guid> { ["000001"] = fornecedorId }, existentes, SemCodigosUsados);

        Assert.Equal(ItemFiscalReferenciaFornecedorRefinedAction.NoChange, Assert.Single(plano.Decisoes).Action);
    }

    [Fact]
    public void Existente_Com_Codigo_Diferente_E_Update_Adr0024()
    {
        var itemFiscalId = Guid.NewGuid();
        var fornecedorId = Guid.NewGuid();
        var raw = new[] { new ItemFiscalReferenciaFornecedorRefinedItem("COD-1", "REF-NOVO", "000001", 1) };
        var existentes = new Dictionary<(Guid, Guid), ItemFiscalReferenciaFornecedorExistente> { [(itemFiscalId, fornecedorId)] = new(Guid.NewGuid(), "REF-ANTIGO") };

        var plano = ItemFiscalReferenciaFornecedorRefinedProjector.Projetar(
            raw, new Dictionary<string, Guid> { ["COD-1"] = itemFiscalId }, new Dictionary<string, Guid> { ["000001"] = fornecedorId }, existentes, SemCodigosUsados);

        Assert.Equal(ItemFiscalReferenciaFornecedorRefinedAction.Update, Assert.Single(plano.Decisoes).Action);
    }

    [Fact]
    public void Codigo_Item_Com_Espaco_A_Direita_E_Reconhecido_Contra_Dicionario_Aparado()
    {
        // Regressão real: CODIGO_ITEM em ITEM_FISCAL_REF_FORNECEDOR chega com espaço à direita do Linx;
        // ItemFiscal.Codigo já é persistido aparado — sem Trim() aqui, a resolução falha silenciosamente.
        var itemFiscalId = Guid.NewGuid();
        var fornecedorId = Guid.NewGuid();
        var raw = new[] { new ItemFiscalReferenciaFornecedorRefinedItem("COD-1   ", "REF-1", "000001", 1) };

        var plano = ItemFiscalReferenciaFornecedorRefinedProjector.Projetar(
            raw, new Dictionary<string, Guid> { ["COD-1"] = itemFiscalId }, new Dictionary<string, Guid> { ["000001"] = fornecedorId }, SemExistentes, SemCodigosUsados);

        Assert.Empty(plano.Conflitos);
        Assert.Equal(ItemFiscalReferenciaFornecedorRefinedAction.Insert, Assert.Single(plano.Decisoes).Action);
    }

    [Fact]
    public void Codigo_Ja_Associado_A_Outro_Item_No_Mesmo_Fornecedor_Vira_Conflito()
    {
        var itemFiscalId = Guid.NewGuid();
        var outroItemFiscalId = Guid.NewGuid();
        var fornecedorId = Guid.NewGuid();
        var raw = new[] { new ItemFiscalReferenciaFornecedorRefinedItem("COD-1", "REF-1", "000001", 1) };
        var codigosUsados = new Dictionary<(Guid, string), Guid> { [(fornecedorId, "REF-1")] = outroItemFiscalId };

        var plano = ItemFiscalReferenciaFornecedorRefinedProjector.Projetar(
            raw, new Dictionary<string, Guid> { ["COD-1"] = itemFiscalId }, new Dictionary<string, Guid> { ["000001"] = fornecedorId }, SemExistentes, codigosUsados);

        Assert.Equal("CODIGO_ITEM_FORNECEDOR_JA_ASSOCIADO_A_OUTRO_ITEM", Assert.Single(plano.Conflitos).Code);
    }

    /// <summary>Onda 2 — auditoria RAW determinística (04/09/2026): este dataset é FULL apenas (RAW sempre
    /// truncado — não acumula entre execuções) mas NÃO tem timestamp confiável (ADR-0024). Duas linhas da
    /// MESMA leitura resolvendo para o mesmo (ItemFiscalId, FornecedorId) não podem ter uma "vencedora"
    /// inventada — vira conflito explícito, nenhuma decisão tomada para nenhuma das duas, independente da
    /// ordem de entrada.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Mesmo_ItemFiscal_E_Fornecedor_Duplicado_Na_Mesma_Leitura_Vira_Conflito_Sem_Decisao(bool inverterOrdem)
    {
        var itemFiscalId = Guid.NewGuid();
        var fornecedorId = Guid.NewGuid();
        var linha1 = new ItemFiscalReferenciaFornecedorRefinedItem("COD-1", "REF-A", "000001", 1);
        var linha2 = new ItemFiscalReferenciaFornecedorRefinedItem("COD-1", "REF-B", "000001", 1);
        var raw = inverterOrdem ? new[] { linha2, linha1 } : new[] { linha1, linha2 };

        var plano = ItemFiscalReferenciaFornecedorRefinedProjector.Projetar(
            raw, new Dictionary<string, Guid> { ["COD-1"] = itemFiscalId }, new Dictionary<string, Guid> { ["000001"] = fornecedorId }, SemExistentes, SemCodigosUsados);

        Assert.Empty(plano.Decisoes);
        Assert.Equal(2, plano.Conflitos.Count);
        Assert.All(plano.Conflitos, c => Assert.Equal("ITEM_FISCAL_FORNECEDOR_DUPLICADO_NA_MESMA_LEITURA", c.Code));
    }

    /// <summary>Mesmo código no fornecedor apontando para 2 Itens Fiscais diferentes DENTRO da mesma
    /// leitura (nenhum dos dois ainda existe no domínio) — sem timestamp para desempatar, vira conflito,
    /// nunca escolhe arbitrariamente qual Item Fiscal fica com o código.</summary>
    [Fact]
    public void Mesmo_Codigo_No_Fornecedor_Para_Dois_Itens_Fiscais_Na_Mesma_Leitura_Vira_Conflito()
    {
        var itemFiscalId1 = Guid.NewGuid();
        var itemFiscalId2 = Guid.NewGuid();
        var fornecedorId = Guid.NewGuid();
        var raw = new[]
        {
            new ItemFiscalReferenciaFornecedorRefinedItem("COD-1", "REF-1", "000001", 1),
            new ItemFiscalReferenciaFornecedorRefinedItem("COD-2", "REF-1", "000001", 1),
        };
        var itensFiscaisPorCodigo = new Dictionary<string, Guid> { ["COD-1"] = itemFiscalId1, ["COD-2"] = itemFiscalId2 };

        var plano = ItemFiscalReferenciaFornecedorRefinedProjector.Projetar(
            raw, itensFiscaisPorCodigo, new Dictionary<string, Guid> { ["000001"] = fornecedorId }, SemExistentes, SemCodigosUsados);

        // Nenhuma das duas linhas gera decisão — nunca "a primeira processada", que ainda seria arbitrário.
        Assert.Empty(plano.Decisoes);
        Assert.Equal(2, plano.Conflitos.Count);
        Assert.All(plano.Conflitos, c => Assert.Equal("CODIGO_ITEM_FORNECEDOR_DUPLICADO_NA_MESMA_LEITURA", c.Code));
    }
}
