using BlueprintOS.Core.AI.Memory.Contracts;
using BlueprintOS.Core.AI.Memory.Models;

namespace BlueprintOS.Core.AI.Memory;

/// <summary>
/// Implementação de <see cref="INegotiationMemory"/> responsável por registrar negociações,
/// consolidar o histórico de fornecedores e preços, e calcular o score de fornecedores com
/// base em pesos configuráveis.
/// </summary>
public sealed class NegotiationMemory : INegotiationMemory
{
    private readonly INegotiationMemoryStore _store;
    private readonly NegotiationScoreOptions _options;

    /// <summary>
    /// Inicializa a memória de negociações com o repositório de persistência e as opções
    /// de score a serem utilizados.
    /// </summary>
    /// <param name="store">Abstração de persistência utilizada para ler e gravar o histórico.</param>
    /// <param name="options">Pesos e limites utilizados no cálculo de score e tendência de preço.</param>
    public NegotiationMemory(INegotiationMemoryStore store, NegotiationScoreOptions options)
    {
        _store = store;
        _options = options;
    }

    /// <inheritdoc />
    public void RegisterNegotiation(NegotiationRecord negotiation)
    {
        ArgumentNullException.ThrowIfNull(negotiation);

        _store.AddPriceHistory(new PriceHistory
        {
            ProductId = negotiation.ProductId,
            SupplierId = negotiation.SupplierId,
            Price = negotiation.Price,
            Date = negotiation.Date,
            Currency = negotiation.Currency,
            Freight = negotiation.Freight,
            Taxes = negotiation.Taxes,
            DeliveryDays = negotiation.DeliveryDays,
        });

        var metrics = UpdateScoringMetrics(negotiation);
        UpdateSupplierHistory(negotiation, metrics);

        _store.LinkSupplierToProduct(negotiation.ProductId, negotiation.SupplierId);
    }

    /// <inheritdoc />
    public SupplierHistory? GetSupplierHistory(Guid supplierId) => _store.GetSupplierHistory(supplierId);

    /// <inheritdoc />
    public IReadOnlyCollection<PriceHistory> GetPriceHistory(Guid productId) => _store.GetPriceHistory(productId);

    /// <inheritdoc />
    public double CalculateSupplierScore(Guid supplierId)
    {
        var history = _store.GetSupplierHistory(supplierId);
        if (history is null)
        {
            return 0d;
        }

        var metrics = _store.GetOrCreateScoringMetrics(supplierId);
        return CalculateScore(history, metrics);
    }

    /// <inheritdoc />
    public SupplierHistory? FindBestSupplier(Guid productId)
    {
        var supplierIds = _store.GetSuppliersForProduct(productId);

        SupplierHistory? best = null;
        var bestScore = double.MinValue;

        foreach (var supplierId in supplierIds)
        {
            var history = _store.GetSupplierHistory(supplierId);
            if (history is null)
            {
                continue;
            }

            var score = CalculateSupplierScore(supplierId);
            if (score > bestScore)
            {
                bestScore = score;
                best = history;
            }
        }

        return best;
    }

    /// <inheritdoc />
    public decimal? FindBestHistoricalPrice(Guid productId)
    {
        var history = _store.GetPriceHistory(productId);
        return history.Count == 0 ? null : history.Min(p => p.Price);
    }

    /// <inheritdoc />
    public PriceTrend GetPriceTrend(Guid productId)
    {
        var history = _store.GetPriceHistory(productId)
            .OrderBy(p => p.Date)
            .ToList();

        if (history.Count < 2)
        {
            return PriceTrend.Stable;
        }

        var averagePrice = history.Average(p => p.Price);
        if (averagePrice == 0)
        {
            return PriceTrend.Stable;
        }

        var slope = CalculateLinearRegressionSlope(history);
        var relativeChangePerStep = (double)(slope / averagePrice);

        if (relativeChangePerStep > _options.PriceTrendTolerancePercentage)
        {
            return PriceTrend.Increasing;
        }

        if (relativeChangePerStep < -_options.PriceTrendTolerancePercentage)
        {
            return PriceTrend.Decreasing;
        }

        return PriceTrend.Stable;
    }

    private SupplierScoringMetrics UpdateScoringMetrics(NegotiationRecord negotiation)
    {
        var metrics = _store.GetOrCreateScoringMetrics(negotiation.SupplierId);
        metrics.SupplierId = negotiation.SupplierId;
        metrics.DeliveryCount++;

        if (negotiation.DeliveryDays <= negotiation.PromisedDeliveryDays)
        {
            metrics.OnTimeDeliveryCount++;
        }

        metrics.QualityScoreCount++;
        metrics.TotalQualityScore += negotiation.QualityScore;

        _store.SaveScoringMetrics(metrics);
        return metrics;
    }

    private void UpdateSupplierHistory(NegotiationRecord negotiation, SupplierScoringMetrics metrics)
    {
        var history = _store.GetSupplierHistory(negotiation.SupplierId) ?? new SupplierHistory
        {
            SupplierId = negotiation.SupplierId,
            SupplierName = negotiation.SupplierName,
        };

        var previousCount = history.NegotiationCount;
        var isNewSupplier = previousCount == 0;

        history.SupplierName = negotiation.SupplierName;
        history.NegotiationCount = previousCount + 1;
        history.AverageLeadTime = RunningAverage(history.AverageLeadTime, previousCount, negotiation.DeliveryDays);
        history.SlaScore = RunningAverage(history.SlaScore, previousCount, negotiation.SlaScore);

        var discount = negotiation.ListPrice > 0
            ? (negotiation.ListPrice - negotiation.Price) / negotiation.ListPrice
            : 0m;
        history.AverageDiscount = RunningAverage(history.AverageDiscount, previousCount, discount);

        history.LastPurchaseDate = negotiation.Date;
        history.LastPrice = negotiation.Price;
        history.BestPrice = isNewSupplier ? negotiation.Price : Math.Min(history.BestPrice, negotiation.Price);
        history.WorstPrice = isNewSupplier ? negotiation.Price : Math.Max(history.WorstPrice, negotiation.Price);
        history.TotalPurchased += negotiation.QuantityDelivered;

        if (!string.IsNullOrWhiteSpace(negotiation.Observations))
        {
            history.Observations = negotiation.Observations;
        }

        history.CurrentScore = CalculateScore(history, metrics);

        _store.SaveSupplierHistory(history);
    }

    private double CalculateScore(SupplierHistory history, SupplierScoringMetrics metrics)
    {
        var weights = _options.Weights;
        var totalWeight = weights.Total;
        if (totalWeight <= 0)
        {
            return 0d;
        }

        var priceScore = CalculatePriceScore(history);
        var leadTimeScore = CalculateLeadTimeScore(history.AverageLeadTime);
        var slaScore = Math.Clamp(history.SlaScore, 0, 100);
        var deliveredQuantityScore = CalculateDeliveredQuantityScore(history.TotalPurchased);
        var historyScore = CalculateHistoryScore(history.NegotiationCount);
        var delaysScore = metrics.OnTimeDeliveryRate * 100;
        var qualityScore = Math.Clamp(metrics.AverageQualityScore, 0, 100);

        var weightedSum =
            weights.Price * priceScore +
            weights.LeadTime * leadTimeScore +
            weights.Sla * slaScore +
            weights.DeliveredQuantity * deliveredQuantityScore +
            weights.NegotiationHistory * historyScore +
            weights.Delays * delaysScore +
            weights.Quality * qualityScore;

        return Math.Clamp(weightedSum / totalWeight, 0, 100);
    }

    private static double CalculatePriceScore(SupplierHistory history)
    {
        if (history.WorstPrice <= history.BestPrice)
        {
            return 100;
        }

        var range = history.WorstPrice - history.BestPrice;
        var positionFromWorst = history.WorstPrice - history.LastPrice;

        return Math.Clamp((double)(positionFromWorst / range) * 100, 0, 100);
    }

    private double CalculateLeadTimeScore(double averageLeadTime)
    {
        if (_options.MaxAcceptableLeadTimeDays <= 0)
        {
            return 0;
        }

        var score = 100 - averageLeadTime / _options.MaxAcceptableLeadTimeDays * 100;
        return Math.Clamp(score, 0, 100);
    }

    private double CalculateDeliveredQuantityScore(decimal totalPurchased)
    {
        if (_options.ReferenceVolumeForFullScore <= 0)
        {
            return 0;
        }

        var score = (double)(totalPurchased / _options.ReferenceVolumeForFullScore) * 100;
        return Math.Clamp(score, 0, 100);
    }

    private double CalculateHistoryScore(int negotiationCount)
    {
        if (_options.MaxRelevantNegotiationCount <= 0)
        {
            return 0;
        }

        var score = (double)negotiationCount / _options.MaxRelevantNegotiationCount * 100;
        return Math.Clamp(score, 0, 100);
    }

    private static double RunningAverage(double currentAverage, int previousCount, double newValue)
        => (currentAverage * previousCount + newValue) / (previousCount + 1);

    private static decimal RunningAverage(decimal currentAverage, int previousCount, decimal newValue)
        => (currentAverage * previousCount + newValue) / (previousCount + 1);

    private static decimal CalculateLinearRegressionSlope(IReadOnlyList<PriceHistory> chronologicalHistory)
    {
        var n = chronologicalHistory.Count;
        decimal sumX = 0, sumY = 0, sumXy = 0, sumXx = 0;

        for (var i = 0; i < n; i++)
        {
            var price = chronologicalHistory[i].Price;

            sumX += i;
            sumY += price;
            sumXy += i * price;
            sumXx += (decimal)(i * i);
        }

        var denominator = n * sumXx - sumX * sumX;
        if (denominator == 0)
        {
            return 0;
        }

        return (n * sumXy - sumX * sumY) / denominator;
    }
}
