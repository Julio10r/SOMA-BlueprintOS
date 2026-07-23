namespace BlueprintOS.Core.AI.Models;

/// <summary>
/// Métricas de execução coletadas durante uma chamada a um modelo de IA.
/// </summary>
/// <param name="Provider">Nome do provedor que executou a requisição.</param>
/// <param name="ModelId">Identificador do modelo utilizado.</param>
/// <param name="Duration">Tempo total de execução da requisição.</param>
/// <param name="Usage">Consumo de tokens da execução.</param>
public sealed record AIExecutionMetrics(
    string Provider,
    string ModelId,
    TimeSpan Duration,
    TokenUsage Usage);
