namespace BlueprintOS.Application.Identity;

public enum ItemFiscalRefinedAction
{
    Insert,
    AtualizarDeErp,
    PreservarLocal,
    SemAlteracao,
}

/// <summary>Linha RAW já com <see cref="UltimaAlteracaoErp"/> convertida para UTC (America/Sao_Paulo local
/// time no Linx, sem DST) — a conversão acontece no chamador (Infrastructure), nunca aqui: este projetor é
/// puro e não conhece fuso horário.</summary>
public sealed record ItemFiscalRefinedItem(string CodigoErp, string Descricao, string? UnidadeErp, string? ContaContabilErp, bool InativoErp, DateTimeOffset? UltimaAlteracaoErp, int Id = 0);

public sealed record ItemFiscalExistente(Guid Id, string Descricao, string? UnidadeMedidaCodigoErp, string? ContaContabilCodigoErp, bool Ativo, DateTimeOffset? UltimaAlteracaoLocalEm);

public sealed record ItemFiscalRefinedDecision(
    string CodigoErp, ItemFiscalRefinedAction Action, Guid? ExistenteId,
    string Descricao, string? UnidadeErp, string? ContaContabilErp, bool Ativo, DateTimeOffset? UltimaAlteracaoErp);

public sealed record ItemFiscalRefinedOcorrencia(string Code, string Mensagem, string? OriginRecordKey);

public sealed record ItemFiscalRefinedPlan(IReadOnlyList<ItemFiscalRefinedDecision> Decisoes, IReadOnlyList<ItemFiscalRefinedOcorrencia> Rejeicoes)
{
    public int Inseridos => Decisoes.Count(d => d.Action == ItemFiscalRefinedAction.Insert);
    public int AtualizadosDeErp => Decisoes.Count(d => d.Action == ItemFiscalRefinedAction.AtualizarDeErp);
    public int PreservadosLocal => Decisoes.Count(d => d.Action == ItemFiscalRefinedAction.PreservarLocal);
    public int SemAlteracao => Decisoes.Count(d => d.Action == ItemFiscalRefinedAction.SemAlteracao);
}

/// <summary>
/// B3 — Bloco 5A (preparação de certificação final): projetor PURO e determinístico que reproduz, em lote,
/// exatamente as regras já homologadas de <c>ItemFiscal.CriarDeErp</c>/<c>AtualizarDeErp</c> e
/// <c>SincronizarItensFiscaisErpUseCase.DecidirLww</c> — nunca reinventa a regra, apenas a aplica em lote:
/// <list type="bullet">
/// <item>Código Linx sem Item Fiscal local -&gt; Insert (caso A). Unidade/Conta Contábil ausentes/nulas
/// nunca bloqueiam nem são inventadas — passam como estão (144 itens ativos sem Conta Contábil, 2 sem
/// Unidade, comprovados por Discovery real).</item>
/// <item>Conteúdo idêntico ao já existente (Descrição, Conta, Unidade, Ativo) -&gt; SemAlteracao (caso D),
/// mesmo que os timestamps difiram — nunca escreve por escrever.</item>
/// <item>Conteúdo divergente: LWW por <see cref="ItemFiscalExistente.UltimaAlteracaoLocalEm"/> vs
/// <see cref="ItemFiscalRefinedItem.UltimaAlteracaoErp"/> — Linx mais novo, ambos nulos, ou empate
/// (ADR-0024) -&gt; AtualizarDeErp (casos B/E/F, Linx prevalece); local mais novo -&gt; PreservarLocal (caso
/// C, nenhuma escrita).</item>
/// <item>Código Linx vazio/em branco (achado real, mesma classe do bug de FornecedorDominioErp) -&gt; rejeitado,
/// nunca inventado.</item>
/// </list>
/// </summary>
public static class ItemFiscalRefinedProjector
{
    public static ItemFiscalRefinedPlan Projetar(IReadOnlyList<ItemFiscalRefinedItem> raw, IReadOnlyDictionary<string, ItemFiscalExistente> existentesPorCodigo)
    {
        var decisoes = new List<ItemFiscalRefinedDecision>();
        var rejeicoes = new List<ItemFiscalRefinedOcorrencia>();

        // Achado real (Onda 2, auditoria RAW determinística, 04/09/2026): sob Incremental, RAW é append-only
        // (nunca trunca — só Full trunca), então o mesmo CodigoErp pode aparecer mais de uma vez (a linha
        // antiga e a recém-anexada). Sem esta deduplicação, este `foreach` processava CADA linha
        // independentemente, produzindo 2 decisões para o mesmo código — no caminho Insert isso colide com o
        // índice único de `Codigo` (falha o lote inteiro); no caminho AtualizarDeErp, a última processada
        // "vencia" por mera ordem de enumeração do banco (sem ORDER BY, não determinística). Desempate por
        // maior `UltimaAlteracaoErp`, depois maior `Id` (RAW só cresce sob Incremental, então Id mais alto é
        // sempre a linha mais recente) — mesmo princípio já homologado em FornecedorRefinedProjector, adaptado
        // à chave e aos timestamps reais deste dataset (aqui não há a ambiguidade de formatação pré-trim que
        // existe em Cadastro de Apoio, então não há uma segunda categoria a preservar).
        var rawVencedorPorCodigo = raw
            .GroupBy(item => item.CodigoErp.Trim(), StringComparer.Ordinal)
            .Select(g => g
                .OrderByDescending(item => item.UltimaAlteracaoErp ?? DateTimeOffset.MinValue)
                .ThenByDescending(item => item.Id)
                .First())
            .ToList();

        foreach (var item in rawVencedorPorCodigo)
        {
            var codigo = item.CodigoErp.Trim();
            if (string.IsNullOrEmpty(codigo))
            {
                rejeicoes.Add(new ItemFiscalRefinedOcorrencia(
                    "CODIGO_ERP_VAZIO", $"Item Fiscal com código vazio/em branco no Linx — rejeitado, nunca inventado. Descrição bruta: '{item.Descricao}'.",
                    string.IsNullOrWhiteSpace(item.Descricao) ? null : item.Descricao.Trim()));
                continue;
            }

            var unidade = NormalizarErp(item.UnidadeErp);
            var conta = NormalizarErp(item.ContaContabilErp);
            var descricao = item.Descricao.Trim();
            var ativo = !item.InativoErp;

            if (!existentesPorCodigo.TryGetValue(codigo, out var existente))
            {
                decisoes.Add(new ItemFiscalRefinedDecision(codigo, ItemFiscalRefinedAction.Insert, null, descricao, unidade, conta, ativo, item.UltimaAlteracaoErp));
                continue;
            }

            var divergente = existente.Descricao != descricao
                || existente.UnidadeMedidaCodigoErp != unidade
                || existente.ContaContabilCodigoErp != conta
                || existente.Ativo != ativo;

            if (!divergente)
            {
                decisoes.Add(new ItemFiscalRefinedDecision(codigo, ItemFiscalRefinedAction.SemAlteracao, existente.Id, descricao, unidade, conta, ativo, item.UltimaAlteracaoErp));
                continue;
            }

            var acao = DecidirLww(existente.UltimaAlteracaoLocalEm, item.UltimaAlteracaoErp);
            decisoes.Add(new ItemFiscalRefinedDecision(codigo, acao, existente.Id, descricao, unidade, conta, ativo, item.UltimaAlteracaoErp));
        }

        return new ItemFiscalRefinedPlan(decisoes, rejeicoes);
    }

    /// <summary>Mesma tabela-verdade de <c>SincronizarItensFiscaisErpUseCase.DecidirLww</c>: ambos presentes
    /// e Linx mais novo, ou qualquer um ausente (ADR-0024), ou empate (ADR-0024) -&gt; Linx prevalece; local
    /// mais novo -&gt; preservado, nenhuma escrita.</summary>
    private static ItemFiscalRefinedAction DecidirLww(DateTimeOffset? timestampLocal, DateTimeOffset? timestampLinx)
    {
        if (timestampLocal is null || timestampLinx is null) return ItemFiscalRefinedAction.AtualizarDeErp;
        if (timestampLinx.Value > timestampLocal.Value) return ItemFiscalRefinedAction.AtualizarDeErp;
        if (timestampLocal.Value > timestampLinx.Value) return ItemFiscalRefinedAction.PreservarLocal;
        return ItemFiscalRefinedAction.AtualizarDeErp; // empate -> ADR-0024, Linx prevalece
    }

    private static string? NormalizarErp(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
