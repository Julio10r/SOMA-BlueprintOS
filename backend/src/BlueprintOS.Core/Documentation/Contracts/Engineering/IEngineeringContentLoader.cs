namespace BlueprintOS.Core.Documentation.Contracts.Engineering;

/// <summary>
/// Conteúdo técnico de uma seção do Engineering Handbook, autorado como Markdown em
/// <c>.ai/content/engineering/</c>.
/// </summary>
/// <param name="FileName">Nome do arquivo de origem (ex.: <c>01-overview.md</c>), preservado para diagnóstico.</param>
/// <param name="Content">Conteúdo Markdown bruto do arquivo.</param>
public sealed record EngineeringContentFile(string FileName, string Content);

/// <summary>
/// Carrega o conteúdo técnico do Engineering Handbook a partir dos arquivos Markdown autorados
/// em <c>.ai/content/engineering/</c>, em vez de derivá-lo automaticamente da documentação de
/// código (classes, namespaces, referência de API).
/// </summary>
public interface IEngineeringContentLoader
{
    /// <summary>
    /// Carrega todos os arquivos <c>.md</c> de <c>.ai/content/engineering/</c>, ordenados
    /// alfabeticamente pelo nome do arquivo. Retorna uma lista vazia se a pasta não existir ou
    /// não contiver arquivos <c>.md</c>.
    /// </summary>
    Task<IReadOnlyList<EngineeringContentFile>> LoadAsync(CancellationToken cancellationToken = default);
}
