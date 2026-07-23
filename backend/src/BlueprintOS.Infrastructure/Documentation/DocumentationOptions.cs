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
}
