namespace BlueprintOS.Core.Documentation.Models;

/// <summary>
/// Representa um par (documento, arquivos de código-fonte relacionados) a ser verificado
/// quanto à atualidade da documentação.
/// </summary>
/// <param name="DocPath">Caminho do arquivo de documentação.</param>
/// <param name="SourcePaths">Caminhos dos arquivos de código-fonte relacionados ao documento.</param>
public sealed record DocumentationSyncCheck(string DocPath, IReadOnlyList<string> SourcePaths);

/// <summary>
/// Resultado da verificação de atualidade de um documento em relação ao código-fonte relacionado.
/// </summary>
/// <param name="DocPath">Caminho do arquivo de documentação verificado.</param>
/// <param name="DocLastWriteUtc">Data/hora da última escrita do arquivo de documentação, se existir.</param>
/// <param name="NewestSourceLastWriteUtc">Data/hora da última escrita mais recente entre os arquivos de origem, se algum existir.</param>
/// <param name="IsStale">Indica se a documentação está desatualizada em relação ao código-fonte.</param>
public sealed record StaleDocumentationInfo(
    string DocPath,
    DateTime? DocLastWriteUtc,
    DateTime? NewestSourceLastWriteUtc,
    bool IsStale);
