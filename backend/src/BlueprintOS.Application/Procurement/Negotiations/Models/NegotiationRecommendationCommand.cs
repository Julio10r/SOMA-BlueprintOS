using BlueprintOS.Core.AI.Negotiation.Models;

namespace BlueprintOS.Application.Procurement.Negotiations.Models;

/// <summary>Dados de entrada compatíveis com o contexto de negociação existente.</summary>
public sealed record NegotiationRecommendationCommand(
    string RequestId,
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
    NegotiationUrgencyLevel UrgencyLevel);
