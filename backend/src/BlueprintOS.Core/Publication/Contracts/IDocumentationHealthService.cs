using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Core.Publication.Models.Health;

namespace BlueprintOS.Core.Publication.Contracts;

/// <summary>
/// Analisa a documentação já gerada pelo Publication Engine (artefatos Markdown em <c>dist/</c>)
/// e produz um relatório de saúde (cobertura, estrutura, links e conteúdo), sem alterar nenhum
/// documento existente.
/// </summary>
public interface IDocumentationHealthService
{
    /// <summary>
    /// Analisa os artefatos Markdown publicados e produz o relatório de saúde correspondente.
    /// </summary>
    Task<DocumentationHealthReport> AnalyzeAsync(
        IReadOnlyList<PublishedArtifact> artifacts,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Serializa o relatório em Markdown e o grava em disco. Retorna o caminho absoluto do
    /// arquivo escrito.
    /// </summary>
    Task<string> WriteReportAsync(
        DocumentationHealthReport report,
        CancellationToken cancellationToken = default);
}
