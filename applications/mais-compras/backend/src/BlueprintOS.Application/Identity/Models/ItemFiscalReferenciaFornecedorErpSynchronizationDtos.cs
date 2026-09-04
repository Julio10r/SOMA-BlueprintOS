namespace BlueprintOS.Application.Identity.Models;

public sealed record SincronizarItemFiscalReferenciasFornecedorErpDto(int Limite, string? CorrelationId, bool DryRun = false);

/// <summary>Motivo padronizado de conflito — nunca uma associação é criada/alterada sem resolução
/// inequívoca de Item Fiscal, Fornecedor e nome (B3 — Bloco 5A, decisão do Product Owner).</summary>
public enum ItemFiscalReferenciaFornecedorErpConflitoMotivo
{
    NomeFornecedorNaoResolvidoOuAmbiguo,
    ItemFiscalAindaNaoSincronizadoLocalmente,
    FornecedorAindaNaoSincronizadoLocalmente,
    CodigoItemFornecedorJaAssociadoAOutroItem,
}

public sealed record ItemFiscalReferenciaFornecedorErpConflito(
    string CodigoItem, string CodigoItemFornecedor, string? FornecedorLinxNome,
    ItemFiscalReferenciaFornecedorErpConflitoMotivo Motivo);

public sealed record SincronizacaoItemFiscalReferenciasFornecedorErpResumo(
    string Status,
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    int Consultados,
    int Incluidos,
    int Atualizados,
    int SemAlteracao,
    int Erros,
    long DuracaoMs,
    string? CorrelationId,
    bool PossivelmenteTruncado,
    IReadOnlyList<ItemFiscalReferenciaFornecedorErpConflito> Conflitos);
