namespace BlueprintOS.Core.Publication.Models;

/// <summary>
/// Representa o resultado da publicação de um documento em um formato específico.
/// </summary>
/// <param name="Format">Formato em que o artefato foi gerado.</param>
/// <param name="RelativePath">Caminho relativo do arquivo publicado (relativo à raiz de <c>dist/</c>).</param>
/// <param name="FilePath">Caminho absoluto do arquivo escrito em disco.</param>
public sealed record PublishedArtifact(
    PublicationFormat Format,
    string RelativePath,
    string FilePath);
