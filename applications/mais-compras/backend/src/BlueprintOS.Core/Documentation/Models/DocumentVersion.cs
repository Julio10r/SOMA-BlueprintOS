namespace BlueprintOS.Core.Documentation.Models;

/// <summary>
/// Representa uma versão histórica do conteúdo de um <see cref="DocumentationEntry"/>.
/// </summary>
/// <param name="DocumentId">Identificador do documento ao qual esta versão pertence.</param>
/// <param name="VersionNumber">Número sequencial da versão, iniciando em 1.</param>
/// <param name="Content">Conteúdo do documento nesta versão.</param>
/// <param name="ChangedAt">Data e hora em que esta versão foi registrada.</param>
/// <param name="ChangeSummary">Resumo da alteração que originou esta versão.</param>
public sealed record DocumentVersion(
    string DocumentId,
    int VersionNumber,
    string Content,
    DateTimeOffset ChangedAt,
    string ChangeSummary);
