namespace BlueprintOS.Infrastructure.Knowledge;

/// <summary>
/// Configuração utilizada para localizar os documentos Markdown de conhecimento.
/// </summary>
public sealed class KnowledgeOptions
{
    /// <summary>
    /// Seção do appsettings onde esta configuração é lida.
    /// </summary>
    public const string SectionName = "Knowledge";

    /// <summary>
    /// Diretório, relativo ou absoluto, onde os arquivos Markdown de conhecimento estão armazenados.
    /// </summary>
    public string DirectoryPath { get; set; } = "knowledge";
}
