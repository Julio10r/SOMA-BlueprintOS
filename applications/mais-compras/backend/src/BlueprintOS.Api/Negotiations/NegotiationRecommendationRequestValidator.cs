using BlueprintOS.Core.AI.Negotiation.Models;

namespace BlueprintOS.Api.Negotiations;

/// <summary>Valida somente o contrato HTTP antes de o caso de uso ser chamado.</summary>
public static class NegotiationRecommendationRequestValidator
{
    public static IReadOnlyDictionary<string, string[]> Validate(NegotiationRecommendationRequest request, out NegotiationUrgencyLevel urgencyLevel)
    {
        var errors = new Dictionary<string, string[]>();
        Add(errors, request.SupplierId == Guid.Empty, nameof(request.SupplierId), "SupplierId is required.");
        Add(errors, request.ProductId == Guid.Empty, nameof(request.ProductId), "ProductId is required.");
        Add(errors, request.CurrentPrice <= 0, nameof(request.CurrentPrice), "CurrentPrice must be greater than zero.");
        Add(errors, request.LeadTimeDays < 0, nameof(request.LeadTimeDays), "LeadTimeDays cannot be negative.");
        Add(errors, request.SlaScore is < 0 or > 100, nameof(request.SlaScore), "SlaScore must be between 0 and 100.");
        Add(errors, request.PurchaseValue <= 0, nameof(request.PurchaseValue), "PurchaseValue must be greater than zero.");
        Add(errors, request.NumberOfSuppliers < 1, nameof(request.NumberOfSuppliers), "NumberOfSuppliers must be at least one.");
        Add(errors, request.BudgetLimit is <= 0, nameof(request.BudgetLimit), "BudgetLimit must be greater than zero when provided.");

        if (!Enum.TryParse(request.Urgency, true, out urgencyLevel))
        {
            urgencyLevel = NegotiationUrgencyLevel.Normal;
            Add(errors, true, nameof(request.Urgency), "Urgency must be Low, Normal, High or Critical.");
        }

        return errors;
    }

    private static void Add(Dictionary<string, string[]> errors, bool condition, string key, string message)
    {
        if (condition)
        {
            errors[key] = [message];
        }
    }
}
