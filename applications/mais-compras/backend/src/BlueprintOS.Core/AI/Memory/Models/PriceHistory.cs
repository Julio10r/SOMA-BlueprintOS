namespace BlueprintOS.Core.AI.Memory.Models;

/// <summary>
/// Representa um registro pontual de preço praticado por um fornecedor para um produto.
/// </summary>
public class PriceHistory
{
    /// <summary>
    /// Identificador do produto negociado.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Identificador do fornecedor que praticou o preço.
    /// </summary>
    public Guid SupplierId { get; set; }

    /// <summary>
    /// Preço unitário praticado.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Data em que o preço foi negociado.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Moeda em que o preço foi praticado.
    /// </summary>
    public string Currency { get; set; } = "BRL";

    /// <summary>
    /// Valor de frete associado à negociação.
    /// </summary>
    public decimal Freight { get; set; }

    /// <summary>
    /// Valor de impostos associado à negociação.
    /// </summary>
    public decimal Taxes { get; set; }

    /// <summary>
    /// Prazo de entrega, em dias, associado à negociação.
    /// </summary>
    public int DeliveryDays { get; set; }
}
