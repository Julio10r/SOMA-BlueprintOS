using BlueprintOS.Core.AI.Memory.Contracts;
using BlueprintOS.Core.AI.Memory.Models;
using BlueprintOS.Core.AI.Negotiation.Contracts;
using BlueprintOS.Core.AI.Negotiation.Models;

namespace BlueprintOS.Api.Negotiations;

/// <summary>
/// Expõe o primeiro fluxo consultivo de negociação do +COMPRAS.
/// </summary>
public static class NegotiationEndpoints
{
    public static IEndpointRouteBuilder MapNegotiationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/negotiations")
            .WithTags("Negotiations");

        group.MapPost("/history", RegisterHistory);
        group.MapGet("/suppliers/{supplierId:guid}", GetSupplierHistory);
        group.MapPost("/recommendations", GetRecommendation);

        return endpoints;
    }

    private static IResult RegisterHistory(
        RegisterNegotiationHistoryRequest request,
        INegotiationMemory negotiationMemory,
        ILogger<Program> logger)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var record = new NegotiationRecord(
            request.ProductId,
            request.SupplierId,
            request.SupplierName.Trim(),
            request.Price,
            request.ListPrice,
            request.Freight,
            request.Taxes,
            request.DeliveryDays,
            request.PromisedDeliveryDays,
            request.QuantityOrdered,
            request.QuantityDelivered,
            request.SlaScore,
            request.QualityScore,
            request.CompletedAt.UtcDateTime,
            request.Currency.Trim(),
            request.Observations?.Trim());

        negotiationMemory.RegisterNegotiation(record);
        var history = negotiationMemory.GetSupplierHistory(request.SupplierId)!;

        logger.LogInformation(
            "Negotiation history registered for supplier {SupplierId} and product {ProductId}",
            request.SupplierId,
            request.ProductId);

        return Results.Created(
            $"/api/v1/negotiations/suppliers/{request.SupplierId}",
            SupplierHistoryResponse.From(history));
    }

    private static IResult GetSupplierHistory(Guid supplierId, INegotiationMemory negotiationMemory)
    {
        var history = negotiationMemory.GetSupplierHistory(supplierId);
        return history is null
            ? Results.NotFound()
            : Results.Ok(SupplierHistoryResponse.From(history));
    }

    private static IResult GetRecommendation(
        NegotiationRecommendationRequest request,
        INegotiationMemory negotiationMemory,
        INegotiationStrategy negotiationStrategy,
        ILogger<Program> logger)
    {
        var errors = Validate(request, out var urgency);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var supplierHistory = negotiationMemory.GetSupplierHistory(request.SupplierId);
        var context = new NegotiationContext
        {
            SupplierId = request.SupplierId,
            ProductId = request.ProductId,
            CurrentPrice = request.CurrentPrice,
            HistoricalBestPrice = negotiationMemory.FindBestHistoricalPrice(request.ProductId),
            SupplierScore = negotiationMemory.CalculateSupplierScore(request.SupplierId),
            LeadTime = request.LeadTimeDays,
            Sla = request.SlaScore,
            PurchaseValue = request.PurchaseValue,
            IsCriticalItem = request.IsCriticalItem,
            IsRecurringPurchase = request.IsRecurringPurchase,
            NumberOfSuppliers = request.NumberOfSuppliers,
            BudgetLimit = request.BudgetLimit,
            UrgencyLevel = urgency,
            NegotiationCount = supplierHistory?.NegotiationCount ?? 0,
            PriceTrend = negotiationMemory.GetPriceTrend(request.ProductId),
        };

        var recommendation = negotiationStrategy.Evaluate(context);

        logger.LogInformation(
            "Negotiation recommendation generated for supplier {SupplierId} and product {ProductId}",
            request.SupplierId,
            request.ProductId);

        return Results.Ok(NegotiationRecommendationResponse.From(recommendation, supplierHistory is not null));
    }

    private static Dictionary<string, string[]> Validate(RegisterNegotiationHistoryRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        AddErrorIf(errors, request.ProductId == Guid.Empty, nameof(request.ProductId), "ProductId is required.");
        AddErrorIf(errors, request.SupplierId == Guid.Empty, nameof(request.SupplierId), "SupplierId is required.");
        AddErrorIf(errors, string.IsNullOrWhiteSpace(request.SupplierName), nameof(request.SupplierName), "SupplierName is required.");
        AddErrorIf(errors, request.Price <= 0, nameof(request.Price), "Price must be greater than zero.");
        AddErrorIf(errors, request.ListPrice < 0, nameof(request.ListPrice), "ListPrice cannot be negative.");
        AddErrorIf(errors, request.Freight < 0, nameof(request.Freight), "Freight cannot be negative.");
        AddErrorIf(errors, request.Taxes < 0, nameof(request.Taxes), "Taxes cannot be negative.");
        AddErrorIf(errors, request.DeliveryDays < 0, nameof(request.DeliveryDays), "DeliveryDays cannot be negative.");
        AddErrorIf(errors, request.PromisedDeliveryDays < 0, nameof(request.PromisedDeliveryDays), "PromisedDeliveryDays cannot be negative.");
        AddErrorIf(errors, request.QuantityOrdered < 0, nameof(request.QuantityOrdered), "QuantityOrdered cannot be negative.");
        AddErrorIf(errors, request.QuantityDelivered < 0, nameof(request.QuantityDelivered), "QuantityDelivered cannot be negative.");
        AddErrorIf(errors, !IsScoreValid(request.SlaScore), nameof(request.SlaScore), "SlaScore must be between 0 and 100.");
        AddErrorIf(errors, !IsScoreValid(request.QualityScore), nameof(request.QualityScore), "QualityScore must be between 0 and 100.");
        AddErrorIf(errors, request.CompletedAt == default, nameof(request.CompletedAt), "CompletedAt is required.");
        AddErrorIf(errors, string.IsNullOrWhiteSpace(request.Currency), nameof(request.Currency), "Currency is required.");
        return errors;
    }

    private static Dictionary<string, string[]> Validate(
        NegotiationRecommendationRequest request,
        out NegotiationUrgencyLevel urgency)
    {
        var errors = new Dictionary<string, string[]>();
        AddErrorIf(errors, request.ProductId == Guid.Empty, nameof(request.ProductId), "ProductId is required.");
        AddErrorIf(errors, request.SupplierId == Guid.Empty, nameof(request.SupplierId), "SupplierId is required.");
        AddErrorIf(errors, request.CurrentPrice <= 0, nameof(request.CurrentPrice), "CurrentPrice must be greater than zero.");
        AddErrorIf(errors, request.LeadTimeDays < 0, nameof(request.LeadTimeDays), "LeadTimeDays cannot be negative.");
        AddErrorIf(errors, !IsScoreValid(request.SlaScore), nameof(request.SlaScore), "SlaScore must be between 0 and 100.");
        AddErrorIf(errors, request.PurchaseValue <= 0, nameof(request.PurchaseValue), "PurchaseValue must be greater than zero.");
        AddErrorIf(errors, request.NumberOfSuppliers < 1, nameof(request.NumberOfSuppliers), "NumberOfSuppliers must be at least one.");
        AddErrorIf(errors, request.BudgetLimit is <= 0, nameof(request.BudgetLimit), "BudgetLimit must be greater than zero when provided.");

        if (!Enum.TryParse(request.Urgency, ignoreCase: true, out urgency))
        {
            urgency = NegotiationUrgencyLevel.Normal;
            AddErrorIf(errors, true, nameof(request.Urgency), "Urgency must be Low, Normal, High or Critical.");
        }

        return errors;
    }

    private static void AddErrorIf(Dictionary<string, string[]> errors, bool condition, string key, string error)
    {
        if (condition)
        {
            errors[key] = [error];
        }
    }

    private static bool IsScoreValid(double score) => score is >= 0 and <= 100;
}

public sealed record RegisterNegotiationHistoryRequest(
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
    DateTimeOffset CompletedAt,
    string Currency = "BRL",
    string? Observations = null);

public sealed record SupplierHistoryResponse(
    Guid SupplierId,
    string SupplierName,
    int NegotiationCount,
    double CurrentScore,
    decimal LastPrice,
    decimal BestPrice,
    decimal WorstPrice,
    DateTime? LastPurchaseDate)
{
    public static SupplierHistoryResponse From(SupplierHistory history) => new(
        history.SupplierId,
        history.SupplierName,
        history.NegotiationCount,
        history.CurrentScore,
        history.LastPrice,
        history.BestPrice,
        history.WorstPrice,
        history.LastPurchaseDate);
}


