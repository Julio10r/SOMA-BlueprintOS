namespace BlueprintOS.Core.AI.Negotiation.Models;

/// <summary>
/// Opções de configuração da engine de estratégia de negociação, incluindo os
/// limiares utilizados pelas regras e os fatores utilizados nos cálculos de preço
/// alvo e probabilidade de sucesso.
/// </summary>
public sealed class NegotiationStrategyOptions
{
    /// <summary>
    /// Nome da seção de configuração correspondente no <c>appsettings.json</c>.
    /// </summary>
    public const string SectionName = "NegotiationStrategy";

    /// <summary>
    /// Taxa de inflação configurável aplicada ao melhor preço histórico ao calcular
    /// o preço alvo, refletindo o reajuste natural de custos ao longo do tempo.
    /// </summary>
    public double InflationRatePercentage { get; set; } = 0.02;

    /// <summary>
    /// Percentual de ajuste aplicado ao preço alvo em função da tendência de preço
    /// observada no histórico do produto.
    /// </summary>
    public double TrendAdjustmentPercentage { get; set; } = 0.03;

    /// <summary>
    /// Percentual de influência do score do fornecedor sobre o preço alvo: fornecedores
    /// com score mais alto têm menos margem para redução de preço.
    /// </summary>
    public double ScoreInfluencePercentage { get; set; } = 0.05;

    /// <summary>
    /// Prazo de entrega máximo aceitável, em dias, utilizado como referência no
    /// cálculo da probabilidade de sucesso.
    /// </summary>
    public double MaxAcceptableLeadTimeDays { get; set; } = 30;

    /// <summary>
    /// Quantidade de negociações anteriores a partir da qual o histórico com o
    /// fornecedor é considerado plenamente consolidado.
    /// </summary>
    public int MaxRelevantNegotiationCountForHistory { get; set; } = 10;

    /// <summary>
    /// Score mínimo do fornecedor (0 a 100) a partir do qual ele é considerado de
    /// alto desempenho, elegível para a estratégia <see cref="NegotiationStrategyType.Partnership"/>.
    /// </summary>
    public double HighSupplierScoreThreshold { get; set; } = 80;

    /// <summary>
    /// Percentual mínimo de desvio acima do melhor preço histórico a partir do qual
    /// o fornecedor é considerado caro.
    /// </summary>
    public double ExpensivePriceDeviationThreshold { get; set; } = 0.10;

    /// <summary>
    /// Percentual mínimo de desvio acima do melhor preço histórico a partir do qual
    /// se justifica uma postura agressiva de negociação.
    /// </summary>
    public double AggressivePriceDeviationThreshold { get; set; } = 0.20;

    /// <summary>
    /// Quantidade mínima de fornecedores concorrentes disponíveis no mercado para que
    /// a concorrência seja considerada elevada.
    /// </summary>
    public int MinCompetitorsForCompetitive { get; set; } = 3;

    /// <summary>
    /// Fatores multiplicadores de preço alvo aplicados por estratégia de negociação.
    /// </summary>
    public NegotiationStrategyPriceFactors PriceFactors { get; set; } = new();

    /// <summary>
    /// Probabilidades base de sucesso aplicadas por estratégia de negociação.
    /// </summary>
    public NegotiationStrategySuccessBaselines SuccessBaselines { get; set; } = new();

    /// <summary>
    /// Pesos utilizados no cálculo ponderado da probabilidade de sucesso.
    /// </summary>
    public NegotiationSuccessProbabilityWeights ProbabilityWeights { get; set; } = new();
}
