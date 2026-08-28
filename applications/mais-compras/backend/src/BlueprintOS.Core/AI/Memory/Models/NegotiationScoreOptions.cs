namespace BlueprintOS.Core.AI.Memory.Models;

/// <summary>
/// Parâmetros e limites utilizados no cálculo do score de fornecedores e na
/// classificação de tendência de preços, centralizados para facilitar ajustes futuros.
/// </summary>
public sealed class NegotiationScoreOptions
{
    /// <summary>
    /// Seção do appsettings onde esta configuração é lida.
    /// </summary>
    public const string SectionName = "NegotiationScore";

    /// <summary>
    /// Pesos aplicados a cada componente do score de fornecedor.
    /// </summary>
    public NegotiationScoreWeights Weights { get; set; } = new();

    /// <summary>
    /// Prazo de entrega, em dias, considerado o limite aceitável para a pontuação máxima
    /// do componente de lead time.
    /// </summary>
    public double MaxAcceptableLeadTimeDays { get; set; } = 30;

    /// <summary>
    /// Quantidade de negociações a partir da qual o componente de histórico atinge a
    /// pontuação máxima.
    /// </summary>
    public int MaxRelevantNegotiationCount { get; set; } = 10;

    /// <summary>
    /// Volume total comprado a partir do qual o componente de volume entregue atinge a
    /// pontuação máxima.
    /// </summary>
    public decimal ReferenceVolumeForFullScore { get; set; } = 1000m;

    /// <summary>
    /// Tolerância percentual de oscilação de preço, por ponto do histórico, abaixo da qual
    /// a tendência é considerada estável.
    /// </summary>
    public double PriceTrendTolerancePercentage { get; set; } = 0.01;
}
