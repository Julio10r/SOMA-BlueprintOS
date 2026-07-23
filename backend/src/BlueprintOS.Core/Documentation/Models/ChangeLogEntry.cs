namespace BlueprintOS.Core.Documentation.Models;

/// <summary>
/// Representa uma entrada de registro de alterações (changelog) associada a um documento.
/// </summary>
/// <param name="Id">Identificador único da entrada de changelog.</param>
/// <param name="DocumentId">Identificador do documento alterado.</param>
/// <param name="ChangedAt">Data e hora em que a alteração ocorreu.</param>
/// <param name="Summary">Resumo descritivo da alteração realizada.</param>
/// <param name="Author">Autor da alteração, quando conhecido.</param>
public sealed record ChangeLogEntry(
    string Id,
    string DocumentId,
    DateTimeOffset ChangedAt,
    string Summary,
    string? Author);
