namespace BlueprintOS.Core.AI.Memory.Models;

/// <summary>
/// Representa a tendência observada no histórico de preços de um produto.
/// </summary>
public enum PriceTrend
{
    /// <summary>
    /// Os preços vêm subindo de forma consistente ao longo do tempo.
    /// </summary>
    Increasing,

    /// <summary>
    /// Os preços permanecem estáveis, dentro da tolerância de oscilação.
    /// </summary>
    Stable,

    /// <summary>
    /// Os preços vêm caindo de forma consistente ao longo do tempo.
    /// </summary>
    Decreasing,
}
