namespace BlueprintOS.Core.Documentation.Contracts;

/// <summary>
/// Define o contrato de leitura de metadados de histórico Git relacionados a um arquivo.
/// Trata-se apenas de leitura local (sem rede, sem commit/push).
/// </summary>
public interface IGitLogReader
{
    /// <summary>
    /// Obtém a data do último commit que alterou o arquivo informado, ou <c>null</c> se o arquivo
    /// não possuir histórico Git (não versionado ou repositório indisponível).
    /// </summary>
    Task<DateTimeOffset?> GetLastCommitDateAsync(string filePath, CancellationToken cancellationToken = default);
}
