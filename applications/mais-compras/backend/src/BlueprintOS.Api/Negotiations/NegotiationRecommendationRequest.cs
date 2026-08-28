namespace BlueprintOS.Api.Negotiations;

/// <summary>Contrato HTTP mínimo compatível com o contexto de negociação existente.</summary>
public sealed record NegotiationRecommendationRequest(
    Guid SupplierId,
    Guid ProductId,
    decimal CurrentPrice,
    int LeadTimeDays,
    double SlaScore,
    decimal PurchaseValue,
    bool IsCriticalItem,
    bool IsRecurringPurchase,
    int NumberOfSuppliers,
    decimal? BudgetLimit,
    string Urgency);
