namespace BlueprintOS.Core.AI.Models;

/// <summary>
/// Representa o consumo de tokens de uma execução de IA.
/// </summary>
/// <param name="PromptTokens">Quantidade de tokens utilizados na entrada (prompt).</param>
/// <param name="CompletionTokens">Quantidade de tokens gerados na resposta.</param>
public sealed record TokenUsage(int PromptTokens, int CompletionTokens)
{
    /// <summary>
    /// Total de tokens consumidos, somando entrada e saída.
    /// </summary>
    public int TotalTokens => PromptTokens + CompletionTokens;
}
