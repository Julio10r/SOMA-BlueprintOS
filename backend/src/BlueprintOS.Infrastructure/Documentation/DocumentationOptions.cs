namespace BlueprintOS.Infrastructure.Documentation;

/// <summary>
/// Configuração utilizada pelos serviços de documentação, incluindo o diretório onde as ADRs
/// são persistidas como arquivos Markdown.
/// </summary>
public sealed class DocumentationOptions
{
    /// <summary>
    /// Seção do appsettings onde esta configuração é lida.
    /// </summary>
    public const string SectionName = "Documentation";

    /// <summary>
    /// Diretório, relativo ou absoluto, onde os arquivos Markdown de ADR são armazenados.
    /// </summary>
    public string AdrDirectoryPath { get; set; } = "docs/adr";

    /// <summary>
    /// Diretório raiz, relativo ou absoluto, onde o portal de documentação viva publica os
    /// documentos Markdown gerados (executivo, cliente e engenharia).
    /// </summary>
    public string DocsRootPath { get; set; } = "docs";

    /// <summary>
    /// Diretório raiz, relativo ou absoluto, do Engineering Handbook da AI Factory
    /// (<c>.ai/</c>), utilizado como fonte real de dados pelos geradores executivos.
    /// </summary>
    public string AiRootPath { get; set; } = ".ai";

    /// <summary>
    /// Versão do projeto exibida no cabeçalho dos documentos publicados.
    /// </summary>
    public string ProjectVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Diretório raiz, relativo ou absoluto, onde o Asset Generator publica os ativos de
    /// documentação reutilizáveis (diagramas Mermaid e Markdown auxiliares) consumidos
    /// futuramente pelos Publishers.
    /// </summary>
    public string AssetsRootPath { get; set; } = "docs/assets";

    /// <summary>
    /// Diretório raiz, relativo ou absoluto, do Design System oficial da plataforma (AZZAS 2154
    /// - GDT Design System), consumido pelo Publication Engine como única fonte de cores,
    /// tipografia e demais tokens visuais.
    /// </summary>
    public string DesignSystemRootPath { get; set; } = "docs/design-system";
}
