namespace BlueprintOS.Core.Documentation.Contracts.Client;

/// <summary>
/// Conteúdo estratégico de uma seção do Client Guide, autorado como Markdown em
/// <c>.ai/content/client/</c>.
/// </summary>
/// <param name="FileName">Nome do arquivo de origem (ex.: <c>01-overview.md</c>), preservado para diagnóstico.</param>
/// <param name="Content">Conteúdo Markdown bruto do arquivo.</param>
public sealed record ClientContentFile(string FileName, string Content);

/// <summary>
/// Carrega o conteúdo estratégico do Client Guide a partir dos arquivos Markdown autorados em
/// <c>.ai/content/client/</c>, em vez de derivá-lo da documentação técnica.
/// </summary>
public interface IClientContentLoader
{
    /// <summary>
    /// Carrega todos os arquivos <c>.md</c> de <c>.ai/content/client/</c>, ordenados
    /// alfabeticamente pelo nome do arquivo. Retorna uma lista vazia se a pasta não existir ou
    /// não contiver arquivos <c>.md</c>.
    /// </summary>
    Task<IReadOnlyList<ClientContentFile>> LoadAsync(CancellationToken cancellationToken = default);
}
