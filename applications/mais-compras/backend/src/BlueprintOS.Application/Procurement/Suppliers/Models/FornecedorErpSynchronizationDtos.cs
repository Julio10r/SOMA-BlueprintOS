namespace BlueprintOS.Application.Procurement.Suppliers.Models;

/// <summary><c>DryRun</c>: quando true, a execução percorre e classifica normalmente todas as páginas
/// da fonte, mas não grava nenhuma alteração em Fornecedores nem persiste o agregado de sincronização
/// como uma execução real (ver <c>SincronizacaoFornecedor.ConcluirDryRun</c>).</summary>
public sealed record SincronizarFornecedoresErpDto(string BusinessUnit, int Limite, string? CorrelationId, bool DryRun = false);

/// <summary>
/// <c>TotalInativados</c> conta, apenas em memória (não é uma coluna persistida em
/// <c>SincronizacaoFornecedor</c>), quantos UPDATEs desta execução mudaram o Status de Ativo para
/// Inativo. <c>PossivelmenteTruncado</c> é true quando a execução foi limitada por
/// <see cref="SincronizarFornecedoresErpDto.Limite"/> (um teto explícito informado pelo chamador) e
/// ainda havia mais registros disponíveis na fonte além do que foi processado.
/// </summary>
public sealed record SincronizacaoFornecedoresErpResumo(
    Guid ExecucaoId,
    string Status,
    DateTimeOffset Inicio,
    DateTimeOffset Fim,
    int Consultados,
    int Incluidos,
    int Atualizados,
    int SemAlteracao,
    int Erros,
    long DuracaoMs,
    string BusinessUnit,
    string ErpSistema,
    string CorrelationId,
    DateTimeOffset ExecutadaEm,
    int TotalInativados = 0,
    bool PossivelmenteTruncado = false,
    IReadOnlyList<string>? OcorrenciasVinculos = null);
