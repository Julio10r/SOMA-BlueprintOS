namespace BlueprintOS.Infrastructure.Publication.Health;

/// <summary>
/// Configuração utilizada pelo Documentation Health Report (Sprint A7.2).
/// </summary>
public sealed class DocumentationHealthOptions
{
    /// <summary>
    /// Seção do appsettings onde esta configuração é lida.
    /// </summary>
    public const string SectionName = "DocumentationHealth";

    /// <summary>
    /// Caminho, relativo ou absoluto, do relatório de saúde gerado ao final da publicação.
    /// </summary>
    public string OutputPath { get; set; } = "docs/DocumentationHealth.md";

    /// <summary>
    /// Quantidade mínima de palavras de conteúdo para um documento não ser sinalizado como
    /// abaixo do limite mínimo.
    /// </summary>
    public int MinWordCount { get; set; } = 80;

    /// <summary>
    /// Similaridade (Jaccard, 0 a 1) de conteúdo acima da qual dois documentos distintos são
    /// sinalizados como "muito semelhantes".
    /// </summary>
    public double SimilarityThreshold { get; set; } = 0.85;

    /// <summary>
    /// Títulos de heading que se repetem por design (ex.: um bloco "Contratos"/"Classes" por
    /// módulo, concatenado pelos geradores de documentação técnica) e por isso não devem ser
    /// sinalizados como "Heading duplicado" — ver ADR-0009 em <c>.ai/DECISIONS.md</c>.
    /// </summary>
    public HashSet<string> ExpectedDuplicateHeadings { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "Contratos",
        "Classes",
        "Banco de Dados",
    };
}
