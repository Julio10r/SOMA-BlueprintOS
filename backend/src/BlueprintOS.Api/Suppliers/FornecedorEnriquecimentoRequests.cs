using BlueprintOS.Application.Procurement.Suppliers.Models;

namespace BlueprintOS.Api.Suppliers;

public sealed record FornecedorEnriquecimentoRequest(
    ConsultaCnpjResultado Consulta,
    Guid? ConsultaId,
    string BusinessUnit,
    string? ErpSistema,
    string? CorrelationId)
{
    public AnalisarEnriquecimentoFornecedorDto ToDto() => new(Consulta, ConsultaId, BusinessUnit, ErpSistema, CorrelationId);
}

public sealed record FornecedorEnriquecimentoDecisaoRequest(
    ConsultaCnpjResultado Consulta,
    Guid? ConsultaId,
    IReadOnlyList<string>? Campos,
    string BusinessUnit,
    string? ErpSistema,
    string? CorrelationId)
{
    public DecidirEnriquecimentoFornecedorDto ToDto() => new(Consulta, ConsultaId, Campos ?? [], BusinessUnit, ErpSistema, CorrelationId);
}
