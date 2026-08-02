namespace BlueprintOS.Application.Procurement.Suppliers.Models;

public sealed record SincronizarFornecedoresErpDto(string BusinessUnit, int Limite, string? CorrelationId);

public sealed record SincronizacaoFornecedoresErpResumo(
    int Consultados,
    int Incluidos,
    int Atualizados,
    int SemAlteracao,
    string BusinessUnit,
    string ErpSistema,
    string CorrelationId,
    DateTimeOffset ExecutadaEm);
