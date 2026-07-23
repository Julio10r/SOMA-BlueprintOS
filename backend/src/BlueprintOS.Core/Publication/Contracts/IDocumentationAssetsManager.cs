using BlueprintOS.Core.Publication.Models;

namespace BlueprintOS.Core.Publication.Contracts;

/// <summary>
/// Ponto único de acesso a todos os ativos gráficos e de identidade visual usados pelos
/// Publishers (Executive, Client, Engineering): logo, ícones, QR Codes, diagramas/Mermaid,
/// capa, rodapé, cores e fontes. Nenhum Publisher constrói <see cref="PublicationAssets"/>,
/// <see cref="PublicationTheme"/> ou chama geradores de QR Code/Mermaid diretamente — todos
/// consomem apenas este serviço.
/// </summary>
public interface IDocumentationAssetsManager
{
    /// <summary>
    /// Resolve a identidade visual (cores de capa/rodapé) para a categoria de documento
    /// informada — único ponto de seleção de tema/cores da plataforma.
    /// </summary>
    PublicationTheme GetTheme(PublicationDocumentClass documentClass);

    /// <summary>
    /// Constrói o conjunto padrão de ativos de um documento (selos de build/testes, QR Code do
    /// repositório, e os grupos de logo/ícones — hoje vazios, ponto de extensão futuro).
    /// </summary>
    PublicationAssets BuildStandardAssets(QualityMetrics metrics);

    /// <summary>
    /// Constrói o apêndice padrão (histórico de versões e QR Code do repositório) reaproveitado
    /// por todos os documentos.
    /// </summary>
    IReadOnlyList<PublicationSection> BuildStandardAppendix(PublicationMetadata metadata);

    /// <summary>
    /// Obtém o Markdown de um diagrama Mermaid a partir de uma fonte de conteúdo (ex.: um
    /// gerador de documentação), removendo o título que a fonte já inclui — único ponto de
    /// empacotamento de diagramas para uso como seção de documento.
    /// </summary>
    Task<string> BuildDiagramMarkdownAsync(
        Func<CancellationToken, Task<string>> mermaidSource,
        CancellationToken cancellationToken = default);
}
