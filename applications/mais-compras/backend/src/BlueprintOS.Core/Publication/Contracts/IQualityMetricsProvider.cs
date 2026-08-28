using BlueprintOS.Core.Publication.Models;

namespace BlueprintOS.Core.Publication.Contracts;

/// <summary>
/// Define o contrato de coleta de indicadores reais de qualidade (build e testes) do repositório.
/// </summary>
public interface IQualityMetricsProvider
{
    /// <summary>
    /// Coleta os indicadores de qualidade atuais do repositório.
    /// </summary>
    Task<QualityMetrics> GetMetricsAsync(CancellationToken cancellationToken = default);
}
