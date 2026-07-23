namespace BlueprintOS.Core.Publication.Models;

/// <summary>
/// Representa um documento completo a ser publicado (relatório executivo, guia de cliente ou
/// guia de engenharia), independente do formato final de renderização.
/// </summary>
/// <param name="Slug">Nome base do arquivo de saída, sem extensão (ex.: <c>ExecutiveReport</c>).</param>
/// <param name="Title">Título do documento, exibido na capa e nos cabeçalhos.</param>
/// <param name="Subtitle">Subtítulo exibido na capa, descrevendo o público-alvo do documento.</param>
/// <param name="Category">Categoria de publicação (ex.: <c>executive</c>, <c>client</c>, <c>engineering</c>).</param>
/// <param name="Sections">Seções do documento, na ordem em que devem ser exibidas.</param>
/// <param name="ProjectVersion">Versão do projeto exibida na capa e no rodapé.</param>
/// <param name="GeneratedAt">Data e hora de geração do documento.</param>
public sealed record PublicationDocument(
    string Slug,
    string Title,
    string Subtitle,
    string Category,
    IReadOnlyList<PublicationSection> Sections,
    string ProjectVersion,
    DateTimeOffset GeneratedAt);
