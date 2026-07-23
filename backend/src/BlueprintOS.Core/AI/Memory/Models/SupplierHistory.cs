namespace BlueprintOS.Core.AI.Memory.Models;

/// <summary>
/// Consolida o histórico de negociações realizadas com um fornecedor específico.
/// </summary>
public class SupplierHistory
{
    /// <summary>
    /// Identificador único do fornecedor.
    /// </summary>
    public Guid SupplierId { get; set; }

    /// <summary>
    /// Nome do fornecedor.
    /// </summary>
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>
    /// Quantidade total de negociações já registradas com o fornecedor.
    /// </summary>
    public int NegotiationCount { get; set; }

    /// <summary>
    /// Prazo médio de entrega, em dias, observado nas negociações registradas.
    /// </summary>
    public double AverageLeadTime { get; set; }

    /// <summary>
    /// Pontuação média de SLA (0 a 100) observada nas negociações registradas.
    /// </summary>
    public double SlaScore { get; set; }

    /// <summary>
    /// Percentual médio de desconto obtido em relação ao preço de tabela.
    /// </summary>
    public decimal AverageDiscount { get; set; }

    /// <summary>
    /// Score atual do fornecedor (0 a 100), calculado a partir do histórico consolidado.
    /// </summary>
    public double CurrentScore { get; set; }

    /// <summary>
    /// Data da última compra realizada com o fornecedor.
    /// </summary>
    public DateTime? LastPurchaseDate { get; set; }

    /// <summary>
    /// Último preço negociado com o fornecedor.
    /// </summary>
    public decimal LastPrice { get; set; }

    /// <summary>
    /// Melhor preço já negociado com o fornecedor.
    /// </summary>
    public decimal BestPrice { get; set; }

    /// <summary>
    /// Pior preço já negociado com o fornecedor.
    /// </summary>
    public decimal WorstPrice { get; set; }

    /// <summary>
    /// Volume total já comprado do fornecedor.
    /// </summary>
    public decimal TotalPurchased { get; set; }

    /// <summary>
    /// Observações livres registradas sobre o fornecedor.
    /// </summary>
    public string? Observations { get; set; }
}
