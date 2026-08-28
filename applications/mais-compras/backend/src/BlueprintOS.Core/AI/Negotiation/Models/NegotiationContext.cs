using BlueprintOS.Core.AI.Memory.Models;

namespace BlueprintOS.Core.AI.Negotiation.Models;

/// <summary>
/// Representa o contexto de uma compra a ser negociada, reunindo os dados do produto,
/// do fornecedor e do histórico existente utilizados pelo agente comprador sênior para
/// escolher a estratégia de negociação mais adequada. Novas propriedades podem ser
/// adicionadas livremente sem impactar os consumidores existentes.
/// </summary>
public sealed class NegotiationContext
{
    /// <summary>
    /// Identificador do fornecedor com quem a negociação será conduzida.
    /// </summary>
    public required Guid SupplierId { get; init; }

    /// <summary>
    /// Identificador do produto objeto da negociação.
    /// </summary>
    public required Guid ProductId { get; init; }

    /// <summary>
    /// Preço unitário atualmente praticado pelo fornecedor.
    /// </summary>
    public required decimal CurrentPrice { get; init; }

    /// <summary>
    /// Melhor preço unitário já registrado no histórico do produto, quando existente.
    /// </summary>
    public decimal? HistoricalBestPrice { get; init; }

    /// <summary>
    /// Score do fornecedor (0 a 100), calculado a partir do histórico de negociações.
    /// </summary>
    public required double SupplierScore { get; init; }

    /// <summary>
    /// Prazo de entrega praticado pelo fornecedor, em dias.
    /// </summary>
    public required int LeadTime { get; init; }

    /// <summary>
    /// Pontuação de SLA (0 a 100) praticada pelo fornecedor.
    /// </summary>
    public required double Sla { get; init; }

    /// <summary>
    /// Valor total estimado da compra.
    /// </summary>
    public required decimal PurchaseValue { get; init; }

    /// <summary>
    /// Indica se o item é crítico para a operação, exigindo maior cautela na negociação.
    /// </summary>
    public bool IsCriticalItem { get; init; }

    /// <summary>
    /// Indica se a compra faz parte de um relacionamento de fornecimento recorrente.
    /// </summary>
    public bool IsRecurringPurchase { get; init; }

    /// <summary>
    /// Quantidade de fornecedores alternativos disponíveis no mercado para o produto.
    /// </summary>
    public int NumberOfSuppliers { get; init; } = 1;

    /// <summary>
    /// Limite de orçamento disponível para a compra, quando aplicável.
    /// </summary>
    public decimal? BudgetLimit { get; init; }

    /// <summary>
    /// Grau de urgência da necessidade de compra.
    /// </summary>
    public NegotiationUrgencyLevel UrgencyLevel { get; init; } = NegotiationUrgencyLevel.Normal;

    /// <summary>
    /// Quantidade de negociações anteriores já realizadas com o fornecedor. Utilizada
    /// para identificar fornecedores novos, sem histórico consolidado.
    /// </summary>
    public int NegotiationCount { get; init; }

    /// <summary>
    /// Tendência observada no histórico de preços do produto, utilizada no cálculo
    /// do preço alvo.
    /// </summary>
    public PriceTrend PriceTrend { get; init; } = PriceTrend.Stable;
}
