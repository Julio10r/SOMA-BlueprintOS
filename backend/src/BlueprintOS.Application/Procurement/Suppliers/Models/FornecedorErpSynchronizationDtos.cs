namespace BlueprintOS.Application.Procurement.Suppliers.Models;

public sealed record SincronizarFornecedoresErpDto(string BusinessUnit, int Limite, string? CorrelationId);

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
    DateTimeOffset ExecutadaEm);
