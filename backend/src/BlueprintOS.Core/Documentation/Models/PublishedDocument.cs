namespace BlueprintOS.Core.Documentation.Models;

/// <summary>
/// Representa o resultado da publicação de um documento Markdown em disco.
/// </summary>
/// <param name="RelativePath">Caminho relativo do arquivo publicado (relativo à raiz de documentação).</param>
/// <param name="FilePath">Caminho absoluto do arquivo escrito em disco.</param>
/// <param name="Title">Título do documento publicado.</param>
/// <param name="GeneratedAt">Data e hora em que o documento foi gerado/publicado.</param>
public sealed record PublishedDocument(
    string RelativePath,
    string FilePath,
    string Title,
    DateTimeOffset GeneratedAt);
