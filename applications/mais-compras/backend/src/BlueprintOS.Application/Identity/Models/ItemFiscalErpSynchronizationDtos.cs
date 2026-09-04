namespace BlueprintOS.Application.Identity.Models;

/// <summary><c>Limite</c> &lt;= 0 pagina até a fonte esgotar naturalmente (14.103 registros reais
/// confirmados em Produção, `docs/audits/B3-Bloco5A-PreValidacaoLinxProducao.md`) — sem teto artificial,
/// mesmo comportamento de <c>SincronizarFornecedoresErpDto</c>. <c>DryRun</c>: classifica todos os
/// registros sem persistir nada.</summary>
public sealed record SincronizarItensFiscaisErpDto(int Limite, string? CorrelationId, bool DryRun = false);

/// <summary>Decisão final produzida pelo algoritmo de Last Write Wins (Bloco 5A.7) para um Item Fiscal que
/// já existia localmente e cujo conteúdo divergia do Linx nesta execução. Compara
/// `CADASTRO_ITEM_FISCAL.DATA_PARA_TRANSFERENCIA` (Linx) com `ItemFiscal.UltimaAlteracaoLocalEm` (+Compras)
/// — nunca com o horário da própria sincronização.</summary>
public enum ItemFiscalErpDecisaoLww
{
    /// <summary>Caso B: Linx mais novo que o timestamp local relevante — Linx prevalece, +Compras atualizado.</summary>
    AtualizadoLinxMaisNovo,

    /// <summary>Caso C: timestamp local relevante mais novo que o Linx — +Compras preservado, nada aplicado.</summary>
    PreservadoLocalMaisNovo,

    /// <summary>Caso E: timestamps equivalentes, conteúdo divergente — ambiguidade resolvida por ADR-0024
    /// (Linx prevalece), registrada como ocorrência distinta de uma comparação real B/C.</summary>
    AtualizadoEmpateAdr0024,

    /// <summary>Caso F: timestamp local relevante e/ou timestamp Linx indisponível para comparação — nenhum
    /// timestamp é inventado; aplica-se a regra de autoridade homologada (ADR-0024, Linx prevalece) e a
    /// situação é registrada explicitamente (distinta de uma comparação real ou de um empate B/C/E).</summary>
    AtualizadoTimestampIndisponivelAdr0024,
}

/// <summary>Um Item Fiscal já existente localmente cujo conteúdo divergiu do Linx nesta execução e cuja
/// resolução foi decidida pelo algoritmo de Last Write Wins (Bloco 5A.7) — nunca por escolha manual.
/// Apenas diagnóstico/auditoria; <see cref="CamposDivergentes"/> lista os nomes dos campos cujo valor local
/// e do Linx diferiam no momento da decisão.</summary>
public sealed record ItemFiscalErpOcorrenciaLww(
    string CodigoItem,
    DateTimeOffset? DataTransferenciaLinx,
    DateTimeOffset? TimestampLocalRelevante,
    IReadOnlyList<string> CamposDivergentes,
    ItemFiscalErpDecisaoLww Decisao);

public sealed record SincronizacaoItensFiscaisErpResumo(
    string Status,
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    int Consultados,
    int Incluidos,
    int SemAlteracao,
    int AtualizadosLinxMaisNovo,
    int PreservadosLocalMaisNovo,
    int AtualizadosEmpateAdr0024,
    int AtualizadosTimestampIndisponivelAdr0024,
    int Erros,
    long DuracaoMs,
    string? CorrelationId,
    bool PossivelmenteTruncado,
    IReadOnlyList<ItemFiscalErpOcorrenciaLww> Ocorrencias);
