namespace BlueprintOS.Core.Publication.Models;

/// <summary>
/// Representa uma seção de um documento de publicação: um título de alto nível (usado no
/// sumário/índice) e o corpo já estruturado em <see cref="ContentBlock"/>s — o mesmo modelo
/// consumido por todos os formatos de saída (Markdown, HTML, PDF e futuros formatos), sem que
/// nenhum deles precise reinterpretar texto bruto.
/// </summary>
/// <param name="Heading">Título da seção, exibido no índice e como cabeçalho do bloco.</param>
/// <param name="Blocks">Corpo da seção, já decomposto em blocos estruturados.</param>
public sealed record PublicationSection(string Heading, IReadOnlyList<ContentBlock> Blocks);
