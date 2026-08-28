namespace BlueprintOS.Core.Knowledge.Models;

/// <summary>
/// Representa um documento de conhecimento carregado a partir de uma fonte externa.
/// </summary>
/// <param name="Id">Identificador único do documento, geralmente derivado do caminho de origem.</param>
/// <param name="Title">Título do documento.</param>
/// <param name="Content">Conteúdo textual completo do documento.</param>
/// <param name="SourcePath">Caminho de origem do documento.</param>
public sealed record KnowledgeDocument(string Id, string Title, string Content, string SourcePath);
