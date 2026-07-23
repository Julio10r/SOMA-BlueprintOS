namespace BlueprintOS.Core.Publication.Models.Health;

/// <summary>
/// Relatório consolidado de saúde da documentação gerada pelo Publication Engine: cobertura,
/// estrutura, links e conteúdo de todos os documentos Markdown publicados em <c>dist/</c>.
/// </summary>
/// <param name="Documents">Resultado individual de cada documento analisado.</param>
/// <param name="Recommendations">Recomendações de ação derivadas dos problemas encontrados.</param>
public sealed record DocumentationHealthReport(
    IReadOnlyList<DocumentHealthResult> Documents,
    IReadOnlyList<string> Recommendations)
{
    /// <summary>Total de documentos analisados.</summary>
    public int TotalDocuments => Documents.Count;

    /// <summary>Quantidade de documentos sem nenhum problema.</summary>
    public int HealthyCount => Documents.Count(d => d.Status == DocumentHealthStatus.Healthy);

    /// <summary>Quantidade de documentos com apenas avisos (não-bloqueantes).</summary>
    public int WarningCount => Documents.Count(d => d.Status == DocumentHealthStatus.Warning);

    /// <summary>Quantidade de documentos com erros estruturais ou de integridade.</summary>
    public int ErrorCount => Documents.Count(d => d.Status == DocumentHealthStatus.Error);
}
