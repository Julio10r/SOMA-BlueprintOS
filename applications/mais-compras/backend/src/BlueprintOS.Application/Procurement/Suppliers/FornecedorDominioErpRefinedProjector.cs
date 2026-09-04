namespace BlueprintOS.Application.Procurement.Suppliers;

public enum FornecedorDominioErpRefinedAction
{
    Insert,
    Update,
    NoChange,
}

public sealed record FornecedorDominioErpRefinedItem(string TipoDominio, string CodigoErp, string Descricao, DateTime? UltimaAlteracao, int Id = 0);

public sealed record FornecedorDominioErpExistente(Guid Id, string Descricao);

public sealed record FornecedorDominioErpDecision(string TipoDominio, string CodigoErp, string Descricao, FornecedorDominioErpRefinedAction Action, Guid? ExistenteId);

public sealed record FornecedorDominioErpRejeicao(string TipoDominio, string Code, string Mensagem, string? OriginRecordKey);

public sealed record FornecedorDominioErpRefinedPlan(IReadOnlyList<FornecedorDominioErpDecision> Decisoes, IReadOnlyList<FornecedorDominioErpRejeicao> Rejeicoes)
{
    public int Inseridos => Decisoes.Count(d => d.Action == FornecedorDominioErpRefinedAction.Insert);
    public int Atualizados => Decisoes.Count(d => d.Action == FornecedorDominioErpRefinedAction.Update);
    public int SemAlteracao => Decisoes.Count(d => d.Action == FornecedorDominioErpRefinedAction.NoChange);
}

/// <summary>
/// B3 — Bloco 5A (preparação de certificação final): projetor PURO para os 3 catálogos que alimentam
/// <c>FornecedorDominioErp</c> (Tipo, Subtipo, Condição de Pagamento de Fornecedor), descobertos via FK real
/// de FORNECEDORES. Chave de correlação é (TipoDominio, CodigoErp) — nunca inventa/gera um Id novo para um
/// código já existente, apenas decide Insert/Update/NoChange. Nunca remove um registro ausente do RAW desta
/// execução (mesma decisão já aplicada a Fornecedor/vínculo — dado que sai do RAW pode ter sido só um
/// problema transitório de leitura, nunca inferimos exclusão por ausência).
/// </summary>
public static class FornecedorDominioErpRefinedProjector
{
    public static FornecedorDominioErpRefinedPlan Projetar(
        IReadOnlyList<FornecedorDominioErpRefinedItem> raw,
        IReadOnlyDictionary<(string TipoDominio, string CodigoErp), FornecedorDominioErpExistente> existentes)
    {
        var decisoes = new List<FornecedorDominioErpDecision>();
        var rejeicoes = new List<FornecedorDominioErpRejeicao>();

        // Achado real (Onda 2, auditoria RAW determinística, 04/09/2026): este dataset é FULL apenas
        // (`EstrategiaNormal: DatasetLoadKind.Full`, "volume pequeno, sem necessidade de incremental" —
        // RAW sempre truncado antes de recarregar, nunca acumula linhas de execuções anteriores), mas a
        // MESMA leitura pode conter 2 linhas RAW para a mesma chave (TipoDominio, CodigoErp) — os 3
        // catálogos de origem (FORNECEDOR_TIPOS/FORNECEDOR_SUBTIPO/COND_ENT_PGTOS) podem ter dado sujo
        // duplicado como qualquer outra tabela Linx real. Sem deduplicação, o `foreach` abaixo processava
        // cada linha independentemente: 2 Insert para a mesma chave colidem com a unicidade
        // (TipoDominio, CodigoErp); 2 Update tinham a última processada "vencendo" por ordem não garantida
        // do banco. Desempate por maior `UltimaAlteracao`, depois maior `Id` — mesmo princípio já
        // homologado em Fornecedor/Item Fiscal, adaptado à chave composta real deste dataset.
        var rawVencedorPorChave = raw
            .GroupBy(item => (item.TipoDominio, CodigoErp: item.CodigoErp.Trim()))
            .Select(g => g
                .OrderByDescending(item => item.UltimaAlteracao ?? DateTime.MinValue)
                .ThenByDescending(item => item.Id)
                .First())
            .ToList();

        foreach (var item in rawVencedorPorChave)
        {
            var codigo = item.CodigoErp.Trim();
            var descricao = (item.Descricao ?? string.Empty).Trim();

            // Achado real (COND_ENT_PGTOS tem 1 linha com CONDICAO_PGTO em branco): um código vazio nunca é
            // inventado nem cadastrado como está — é uma rejeição registrada, mesma decisão já aplicada a
            // CNPJ/CPF inválido de Fornecedor. A descrição (quando existe) serve de identificador seguro,
            // já que o próprio código — a chave natural — está ausente.
            if (string.IsNullOrEmpty(codigo))
            {
                rejeicoes.Add(new FornecedorDominioErpRejeicao(
                    item.TipoDominio, "CODIGO_ERP_VAZIO",
                    $"Registro do catálogo Linx '{item.TipoDominio}' tem código vazio/em branco — rejeitado, nunca inventado. Descrição bruta: '{item.Descricao}'.",
                    string.IsNullOrWhiteSpace(descricao) ? null : descricao));
                continue;
            }

            var chave = (item.TipoDominio, codigo);

            if (existentes.TryGetValue(chave, out var existente))
            {
                var acao = existente.Descricao == descricao ? FornecedorDominioErpRefinedAction.NoChange : FornecedorDominioErpRefinedAction.Update;
                decisoes.Add(new FornecedorDominioErpDecision(item.TipoDominio, codigo, descricao, acao, existente.Id));
            }
            else
            {
                decisoes.Add(new FornecedorDominioErpDecision(item.TipoDominio, codigo, descricao, FornecedorDominioErpRefinedAction.Insert, null));
            }
        }

        return new FornecedorDominioErpRefinedPlan(decisoes, rejeicoes);
    }
}
