using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Procurement.Negotiations.Contracts;
using BlueprintOS.Application.Procurement.Negotiations.Models;
using BlueprintOS.Core.AI.Memory.Contracts;
using BlueprintOS.Core.AI.Negotiation.Contracts;
using BlueprintOS.Core.AI.Negotiation.Models;

namespace BlueprintOS.Application.Procurement.Negotiations;

/// <summary>Compõe o contexto existente e delega a decisão à estratégia de negociação.</summary>
public sealed class NegotiationRecommendationUseCase : INegotiationRecommendationUseCase
{
    private readonly ICurrentIdentity _currentIdentity;
    private readonly INegotiationMemory _negotiationMemory;
    private readonly INegotiationStrategy _negotiationStrategy;

    public NegotiationRecommendationUseCase(
        ICurrentIdentity currentIdentity,
        INegotiationMemory negotiationMemory,
        INegotiationStrategy negotiationStrategy)
    {
        _currentIdentity = currentIdentity;
        _negotiationMemory = negotiationMemory;
        _negotiationStrategy = negotiationStrategy;
    }

    public NegotiationRecommendationResult Execute(NegotiationRecommendationCommand command)
    {
        _ = _currentIdentity.GetRequired();
        var supplierHistory = _negotiationMemory.GetSupplierHistory(command.SupplierId);
        var context = new NegotiationContext
        {
            SupplierId = command.SupplierId,
            ProductId = command.ProductId,
            CurrentPrice = command.CurrentPrice,
            HistoricalBestPrice = _negotiationMemory.FindBestHistoricalPrice(command.ProductId),
            SupplierScore = _negotiationMemory.CalculateSupplierScore(command.SupplierId),
            LeadTime = command.LeadTimeDays,
            Sla = command.SlaScore,
            PurchaseValue = command.PurchaseValue,
            IsCriticalItem = command.IsCriticalItem,
            IsRecurringPurchase = command.IsRecurringPurchase,
            NumberOfSuppliers = command.NumberOfSuppliers,
            BudgetLimit = command.BudgetLimit,
            UrgencyLevel = command.UrgencyLevel,
            NegotiationCount = supplierHistory?.NegotiationCount ?? 0,
            PriceTrend = _negotiationMemory.GetPriceTrend(command.ProductId),
        };

        return new NegotiationRecommendationResult(
            command.RequestId,
            _negotiationStrategy.Evaluate(context),
            supplierHistory is not null);
    }
}
