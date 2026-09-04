namespace BlueprintOS.Application.Identity;

public enum ItemFiscalReferenciaFornecedorRefinedAction
{
    Insert,
    Update,
    NoChange,
}

public sealed record ItemFiscalReferenciaFornecedorRefinedItem(string CodigoItem, string CodigoItemFornecedor, string? ErpFornecedorId, int FornecedoresResolvidos);

public sealed record ItemFiscalReferenciaFornecedorExistente(Guid Id, string CodigoItemFornecedor);

public sealed record ItemFiscalReferenciaFornecedorRefinedDecision(
    Guid ItemFiscalId, Guid FornecedorId, string CodigoItemFornecedor, ItemFiscalReferenciaFornecedorRefinedAction Action, Guid? ExistenteId);

public sealed record ItemFiscalReferenciaFornecedorConflito(string Code, string Mensagem, string? OriginRecordKey);

public sealed record ItemFiscalReferenciaFornecedorRefinedPlan(IReadOnlyList<ItemFiscalReferenciaFornecedorRefinedDecision> Decisoes, IReadOnlyList<ItemFiscalReferenciaFornecedorConflito> Conflitos);

/// <summary>
/// B3 — Bloco 5A (preparação de certificação final): projetor PURO que reproduz, em lote, a resolução já
/// homologada em <c>SincronizarItemFiscalReferenciasFornecedorErpUseCase</c> — a identidade do Fornecedor
/// (NOME_CLIFOR -&gt; CLIFOR -&gt; COD_FORNECEDOR) já vem resolvida no RAW (ver
/// <c>RawLinxItemFiscalReferenciaFornecedorRegistro</c>); aqui só resta: (1) confirmar que a resolução foi
/// EXATAMENTE 1 (nunca 0 nem &gt;1 — nunca escolhe arbitrariamente em ambiguidade, nunca usa CNPJ como
/// fallback), (2) resolver o Item Fiscal e o vínculo Linx (QUALQUER vínculo conhecido, nunca só o Principal —
/// decisão do PO/GAP PLATINUM já homologada) localmente, sem nunca inventar quando um dos dois ainda não
/// existe localmente, (3) impedir que o mesmo código de item no fornecedor seja associado a mais de um Item
/// Fiscal (unicidade já homologada, Bloco 4).
/// </summary>
public static class ItemFiscalReferenciaFornecedorRefinedProjector
{
    public static ItemFiscalReferenciaFornecedorRefinedPlan Projetar(
        IReadOnlyList<ItemFiscalReferenciaFornecedorRefinedItem> raw,
        IReadOnlyDictionary<string, Guid> itensFiscaisPorCodigo,
        IReadOnlyDictionary<string, Guid> fornecedorIdPorCodigoErpVinculo,
        IReadOnlyDictionary<(Guid ItemFiscalId, Guid FornecedorId), ItemFiscalReferenciaFornecedorExistente> existentes,
        IReadOnlyDictionary<(Guid FornecedorId, string CodigoItemFornecedor), Guid> itemFiscalIdPorCodigoNoFornecedor)
    {
        var conflitos = new List<ItemFiscalReferenciaFornecedorConflito>();

        // Passo 1: resolve cada linha independentemente (checagens já homologadas — resolução de Fornecedor
        // por nome, Item Fiscal local, vínculo Linx local, unicidade contra o domínio pré-existente). Ainda
        // não decide Insert/Update/NoChange aqui — só reúne as linhas que passaram em tudo, para então
        // detectar colisão ENTRE ELAS (passo 2) antes de decidir (passo 3).
        var resolvidas = new List<(string CodigoItem, Guid ItemFiscalId, Guid FornecedorId, string CodigoNoFornecedor)>();

        foreach (var item in raw)
        {
            // Mesmo achado de padding já visto em Fornecedor/Centro de Custo/FornecedorDominioErp: CODIGO_ITEM
            // aqui precisa ser aparado antes de comparar contra o dicionário local (ItemFiscal.Codigo já é
            // persistido aparado) — sem isso, a comparação por igualdade ordinal nunca bate.
            var codigoItem = item.CodigoItem.Trim();

            if (item.FornecedoresResolvidos != 1 || string.IsNullOrWhiteSpace(item.ErpFornecedorId))
            {
                conflitos.Add(new ItemFiscalReferenciaFornecedorConflito(
                    "NOME_FORNECEDOR_NAO_RESOLVIDO_OU_AMBIGUO",
                    $"Referência do item '{codigoItem}' não resolveu para exatamente 1 Fornecedor por nome ({item.FornecedoresResolvidos} correspondência(s)) — nunca escolhido arbitrariamente.",
                    codigoItem));
                continue;
            }

            if (!itensFiscaisPorCodigo.TryGetValue(codigoItem, out var itemFiscalId))
            {
                conflitos.Add(new ItemFiscalReferenciaFornecedorConflito(
                    "ITEM_FISCAL_AINDA_NAO_SINCRONIZADO_LOCALMENTE",
                    $"Item Fiscal '{codigoItem}' existe no Linx mas ainda não tem Item Fiscal local correspondente.",
                    codigoItem));
                continue;
            }

            if (!fornecedorIdPorCodigoErpVinculo.TryGetValue(item.ErpFornecedorId, out var fornecedorId))
            {
                conflitos.Add(new ItemFiscalReferenciaFornecedorConflito(
                    "FORNECEDOR_AINDA_NAO_SINCRONIZADO_LOCALMENTE",
                    $"Fornecedor ERP '{item.ErpFornecedorId}' (item '{codigoItem}') resolvido no Linx mas sem vínculo local correspondente ainda.",
                    item.ErpFornecedorId));
                continue;
            }

            var codigoNoFornecedor = item.CodigoItemFornecedor.Trim();

            if (itemFiscalIdPorCodigoNoFornecedor.TryGetValue((fornecedorId, codigoNoFornecedor), out var itemFiscalIdDoCodigo) && itemFiscalIdDoCodigo != itemFiscalId)
            {
                conflitos.Add(new ItemFiscalReferenciaFornecedorConflito(
                    "CODIGO_ITEM_FORNECEDOR_JA_ASSOCIADO_A_OUTRO_ITEM",
                    $"Código '{codigoNoFornecedor}' já está associado a outro Item Fiscal para este Fornecedor — unicidade (FornecedorId, CodigoItemFornecedor) preservada, nenhuma decisão tomada.",
                    codigoNoFornecedor));
                continue;
            }

            resolvidas.Add((codigoItem, itemFiscalId, fornecedorId, codigoNoFornecedor));
        }

        // Passo 2: achado real (Onda 2, auditoria RAW determinística, 04/09/2026) — este dataset é FULL
        // apenas (RAW sempre truncado antes de recarregar — nunca acumula linhas de execuções anteriores
        // como o Fornecedor sob Incremental), então o padrão "linha antiga vs. recém-anexada" não se aplica
        // aqui. Ainda assim, a MESMA snapshot pode conter 2+ linhas que resolvem para a mesma chave lógica —
        // e esta tabela não tem timestamp confiável (ADR-0024) para decidir qual "vence". Nunca inventa
        // critério: qualquer chave (ItemFiscalId, FornecedorId) ou (FornecedorId, CodigoItemFornecedor) com
        // mais de uma linha resolvida neste lote vira conflito — NENHUMA das linhas envolvidas gera decisão,
        // nunca "a primeira processada" (isso ainda seria arbitrário/dependente da ordem de enumeração).
        var chavesItemFiscalFornecedorDuplicadas = resolvidas
            .GroupBy(r => (r.ItemFiscalId, r.FornecedorId))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();

        var chavesCodigoNoFornecedorDuplicadas = resolvidas
            .GroupBy(r => (r.FornecedorId, r.CodigoNoFornecedor))
            .Where(g => g.Select(r => r.ItemFiscalId).Distinct().Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();

        var decisoes = new List<ItemFiscalReferenciaFornecedorRefinedDecision>();

        foreach (var r in resolvidas)
        {
            if (chavesItemFiscalFornecedorDuplicadas.Contains((r.ItemFiscalId, r.FornecedorId)))
            {
                conflitos.Add(new ItemFiscalReferenciaFornecedorConflito(
                    "ITEM_FISCAL_FORNECEDOR_DUPLICADO_NA_MESMA_LEITURA",
                    $"Mais de uma linha do Linx nesta leitura resolve para o mesmo Item Fiscal + Fornecedor (item '{r.CodigoItem}') — sem timestamp confiável nesta tabela (ADR-0024) para decidir qual prevalece. Nenhuma decisão foi tomada automaticamente.",
                    r.CodigoItem));
                continue;
            }

            if (chavesCodigoNoFornecedorDuplicadas.Contains((r.FornecedorId, r.CodigoNoFornecedor)))
            {
                conflitos.Add(new ItemFiscalReferenciaFornecedorConflito(
                    "CODIGO_ITEM_FORNECEDOR_DUPLICADO_NA_MESMA_LEITURA",
                    $"Código '{r.CodigoNoFornecedor}' aparece mais de uma vez nesta leitura para o mesmo Fornecedor, associado a Itens Fiscais diferentes — sem timestamp confiável nesta tabela (ADR-0024) para decidir qual prevalece. Nenhuma decisão foi tomada automaticamente.",
                    r.CodigoNoFornecedor));
                continue;
            }

            // Passo 3: única linha resolvida para esta chave — decide normalmente (regra já homologada).
            if (existentes.TryGetValue((r.ItemFiscalId, r.FornecedorId), out var existente))
            {
                var acao = existente.CodigoItemFornecedor == r.CodigoNoFornecedor
                    ? ItemFiscalReferenciaFornecedorRefinedAction.NoChange
                    : ItemFiscalReferenciaFornecedorRefinedAction.Update; // ADR-0024: sem timestamp confiável nesta tabela, Linx prevalece
                decisoes.Add(new ItemFiscalReferenciaFornecedorRefinedDecision(r.ItemFiscalId, r.FornecedorId, r.CodigoNoFornecedor, acao, existente.Id));
            }
            else
            {
                decisoes.Add(new ItemFiscalReferenciaFornecedorRefinedDecision(r.ItemFiscalId, r.FornecedorId, r.CodigoNoFornecedor, ItemFiscalReferenciaFornecedorRefinedAction.Insert, null));
            }
        }

        return new ItemFiscalReferenciaFornecedorRefinedPlan(decisoes, conflitos);
    }
}
