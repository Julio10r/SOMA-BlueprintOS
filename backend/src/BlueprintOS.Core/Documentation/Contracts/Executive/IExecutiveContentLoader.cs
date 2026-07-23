namespace BlueprintOS.Core.Documentation.Contracts.Executive;

/// <summary>
/// Conteúdo estratégico de uma seção do Executive Blueprint, autorado como Markdown em
/// <c>.ai/content/executive/</c>.
/// </summary>
/// <param name="FileName">Nome do arquivo de origem (ex.: <c>01-vision.md</c>), preservado para diagnóstico.</param>
/// <param name="Content">Conteúdo Markdown bruto do arquivo.</param>
public sealed record ExecutiveContentFile(string FileName, string Content);

/// <summary>
/// Carrega o conteúdo estratégico do Executive Blueprint a partir dos arquivos Markdown
/// autorados em <c>.ai/content/executive/</c>, em vez de derivá-lo da documentação técnica.
/// </summary>
public interface IExecutiveContentLoader
{
    /// <summary>
    /// Carrega todos os arquivos <c>.md</c> de <c>.ai/content/executive/</c>, ordenados
    /// alfabeticamente pelo nome do arquivo. Retorna uma lista vazia se a pasta não existir ou
    /// não contiver arquivos <c>.md</c>.
    /// </summary>
    Task<IReadOnlyList<ExecutiveContentFile>> LoadAsync(CancellationToken cancellationToken = default);
}
