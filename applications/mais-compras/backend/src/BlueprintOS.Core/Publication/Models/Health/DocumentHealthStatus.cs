namespace BlueprintOS.Core.Publication.Models.Health;

/// <summary>
/// Classificação de saúde de um documento publicado, derivada dos problemas encontrados
/// pelo <see cref="Contracts.IDocumentationHealthService"/>.
/// </summary>
public enum DocumentHealthStatus
{
    /// <summary>Nenhum problema encontrado.</summary>
    Healthy,

    /// <summary>Problemas não-bloqueantes (ex.: conteúdo curto, heading duplicado, documentos semelhantes).</summary>
    Warning,

    /// <summary>Problemas estruturais ou de integridade (ex.: documento vazio, link ou imagem quebrados).</summary>
    Error,
}
