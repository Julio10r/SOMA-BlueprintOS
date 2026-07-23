namespace BlueprintOS.Core.AI.Models;

/// <summary>
/// Representa a resposta produzida por um modelo de IA para uma requisição.
/// </summary>
/// <param name="Message">Mensagem gerada pelo modelo.</param>
/// <param name="Usage">Consumo de tokens da execução.</param>
/// <param name="Metrics">Métricas de execução coletadas durante a chamada.</param>
/// <param name="FinishReason">Motivo pelo qual a geração foi encerrada (ex.: "stop", "length", "tool_calls").</param>
public sealed record AIResponse(
    ChatMessage Message,
    TokenUsage Usage,
    AIExecutionMetrics Metrics,
    string? FinishReason = null);
