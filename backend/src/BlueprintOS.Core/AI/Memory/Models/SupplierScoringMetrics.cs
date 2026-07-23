namespace BlueprintOS.Core.AI.Memory.Models;

/// <summary>
/// Métricas agregadas de pontualidade e qualidade de um fornecedor, utilizadas no
/// cálculo do score que não fazem parte do resumo exposto em <see cref="SupplierHistory"/>.
/// </summary>
public sealed class SupplierScoringMetrics
{
    /// <summary>
    /// Identificador do fornecedor ao qual as métricas pertencem.
    /// </summary>
    public Guid SupplierId { get; set; }

    /// <summary>
    /// Quantidade de entregas realizadas dentro do prazo prometido.
    /// </summary>
    public int OnTimeDeliveryCount { get; set; }

    /// <summary>
    /// Quantidade total de entregas consideradas no cálculo de pontualidade.
    /// </summary>
    public int DeliveryCount { get; set; }

    /// <summary>
    /// Soma das pontuações de qualidade recebidas nas negociações registradas.
    /// </summary>
    public double TotalQualityScore { get; set; }

    /// <summary>
    /// Quantidade de pontuações de qualidade consideradas na média.
    /// </summary>
    public int QualityScoreCount { get; set; }

    /// <summary>
    /// Taxa de entregas realizadas dentro do prazo (0 a 1). Sem histórico, assume-se o máximo.
    /// </summary>
    public double OnTimeDeliveryRate => DeliveryCount == 0 ? 1d : (double)OnTimeDeliveryCount / DeliveryCount;

    /// <summary>
    /// Pontuação média de qualidade (0 a 100). Sem histórico, assume-se o máximo.
    /// </summary>
    public double AverageQualityScore => QualityScoreCount == 0 ? 100d : TotalQualityScore / QualityScoreCount;
}
