namespace BlueprintOS.Core.AI.Memory.Models;

/// <summary>
/// Dados de uma negociação concluída com um fornecedor, utilizados para atualizar
/// a memória de negociações do agente comprador.
/// </summary>
/// <param name="ProductId">Identificador do produto negociado.</param>
/// <param name="SupplierId">Identificador do fornecedor.</param>
/// <param name="SupplierName">Nome do fornecedor.</param>
/// <param name="Price">Preço unitário final negociado.</param>
/// <param name="ListPrice">Preço de tabela do fornecedor antes da negociação, utilizado para calcular o desconto obtido.</param>
/// <param name="Freight">Valor de frete negociado.</param>
/// <param name="Taxes">Valor de impostos aplicável.</param>
/// <param name="DeliveryDays">Prazo de entrega efetivamente cumprido, em dias.</param>
/// <param name="PromisedDeliveryDays">Prazo de entrega prometido pelo fornecedor, em dias.</param>
/// <param name="QuantityOrdered">Quantidade solicitada ao fornecedor.</param>
/// <param name="QuantityDelivered">Quantidade efetivamente entregue pelo fornecedor.</param>
/// <param name="SlaScore">Pontuação de SLA (0 a 100) observada nesta negociação.</param>
/// <param name="QualityScore">Pontuação de qualidade (0 a 100) observada nesta negociação.</param>
/// <param name="Date">Data em que a negociação foi concluída.</param>
/// <param name="Currency">Moeda utilizada na negociação.</param>
/// <param name="Observations">Observações livres sobre a negociação.</param>
public sealed record NegotiationRecord(
    Guid ProductId,
    Guid SupplierId,
    string SupplierName,
    decimal Price,
    decimal ListPrice,
    decimal Freight,
    decimal Taxes,
    int DeliveryDays,
    int PromisedDeliveryDays,
    decimal QuantityOrdered,
    decimal QuantityDelivered,
    double SlaScore,
    double QualityScore,
    DateTime Date,
    string Currency = "BRL",
    string? Observations = null);
