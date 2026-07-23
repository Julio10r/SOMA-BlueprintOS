namespace BlueprintOS.Core.Documentation.Contracts.Executive;

/// <summary>
/// Define o contrato do gerador de indicadores-chave de desempenho (KPIs) do projeto.
/// Quando não houver KPIs reais registrados no repositório, o gerador deve produzir um
/// documento honesto informando a ausência de dados, em vez de inventar números.
/// </summary>
public interface IKpiGenerator
{
    /// <summary>
    /// Gera o corpo Markdown com os KPIs do projeto.
    /// </summary>
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}
