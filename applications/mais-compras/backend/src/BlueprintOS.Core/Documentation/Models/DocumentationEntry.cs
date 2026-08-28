namespace BlueprintOS.Core.Documentation.Models;

/// <summary>
/// Representa um documento gerenciado pelo sistema de documentação do BlueprintOS.
/// </summary>
/// <param name="Id">Identificador único do documento.</param>
/// <param name="Title">Título do documento.</param>
/// <param name="Type">Tipo de documentação representado.</param>
/// <param name="Content">Conteúdo textual completo do documento, em Markdown.</param>
/// <param name="FilePath">Caminho de origem/destino do documento, quando persistido em arquivo.</param>
/// <param name="Version">Número da versão atual do documento.</param>
/// <param name="CreatedAt">Data e hora de criação do documento.</param>
/// <param name="UpdatedAt">Data e hora da última atualização do documento.</param>
public sealed record DocumentationEntry(
    string Id,
    string Title,
    DocumentationType Type,
    string Content,
    string FilePath,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
