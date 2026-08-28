using System.Collections.Concurrent;
using BlueprintOS.Core.AI.Memory.Contracts;
using BlueprintOS.Core.AI.Memory.Models;

namespace BlueprintOS.Infrastructure.Memory;

/// <summary>
/// Implementação de <see cref="INegotiationMemoryStore"/> que mantém o histórico de
/// negociações em memória de processo. Serve como implementação padrão enquanto uma
/// persistência durável (ex.: banco de dados) não é necessária, sem acoplar o restante
/// da aplicação a este mecanismo de armazenamento.
/// </summary>
public sealed class InMemoryNegotiationMemoryStore : INegotiationMemoryStore
{
    private readonly ConcurrentDictionary<Guid, SupplierHistory> _supplierHistories = new();
    private readonly ConcurrentDictionary<Guid, SupplierScoringMetrics> _scoringMetrics = new();
    private readonly ConcurrentDictionary<Guid, List<PriceHistory>> _priceHistoriesByProduct = new();
    private readonly ConcurrentDictionary<Guid, HashSet<Guid>> _suppliersByProduct = new();
    private readonly object _syncRoot = new();

    /// <inheritdoc />
    public SupplierHistory? GetSupplierHistory(Guid supplierId)
        => _supplierHistories.TryGetValue(supplierId, out var history) ? Clone(history) : null;

    /// <inheritdoc />
    public void SaveSupplierHistory(SupplierHistory history)
        => _supplierHistories[history.SupplierId] = Clone(history);

    /// <inheritdoc />
    public SupplierScoringMetrics GetOrCreateScoringMetrics(Guid supplierId)
        => _scoringMetrics.TryGetValue(supplierId, out var metrics)
            ? metrics
            : new SupplierScoringMetrics { SupplierId = supplierId };

    /// <inheritdoc />
    public void SaveScoringMetrics(SupplierScoringMetrics metrics)
        => _scoringMetrics[metrics.SupplierId] = metrics;

    /// <inheritdoc />
    public void AddPriceHistory(PriceHistory priceHistory)
    {
        var list = _priceHistoriesByProduct.GetOrAdd(priceHistory.ProductId, static _ => new List<PriceHistory>());

        lock (_syncRoot)
        {
            list.Add(priceHistory);
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<PriceHistory> GetPriceHistory(Guid productId)
    {
        if (!_priceHistoriesByProduct.TryGetValue(productId, out var list))
        {
            return Array.Empty<PriceHistory>();
        }

        lock (_syncRoot)
        {
            return list.ToList();
        }
    }

    /// <inheritdoc />
    public void LinkSupplierToProduct(Guid productId, Guid supplierId)
    {
        var suppliers = _suppliersByProduct.GetOrAdd(productId, static _ => new HashSet<Guid>());

        lock (_syncRoot)
        {
            suppliers.Add(supplierId);
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<Guid> GetSuppliersForProduct(Guid productId)
    {
        if (!_suppliersByProduct.TryGetValue(productId, out var suppliers))
        {
            return Array.Empty<Guid>();
        }

        lock (_syncRoot)
        {
            return suppliers.ToList();
        }
    }

    private static SupplierHistory Clone(SupplierHistory source) => new()
    {
        SupplierId = source.SupplierId,
        SupplierName = source.SupplierName,
        NegotiationCount = source.NegotiationCount,
        AverageLeadTime = source.AverageLeadTime,
        SlaScore = source.SlaScore,
        AverageDiscount = source.AverageDiscount,
        CurrentScore = source.CurrentScore,
        LastPurchaseDate = source.LastPurchaseDate,
        LastPrice = source.LastPrice,
        BestPrice = source.BestPrice,
        WorstPrice = source.WorstPrice,
        TotalPurchased = source.TotalPurchased,
        Observations = source.Observations,
    };
}
