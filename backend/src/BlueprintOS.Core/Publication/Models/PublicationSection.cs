namespace BlueprintOS.Core.Publication.Models;

/// <summary>
/// Representa uma seção de um documento de publicação: um título de alto nível (usado no
/// sumário/índice) e um corpo em Markdown já pronto (tipicamente reaproveitado de um gerador
/// de documentação existente).
/// </summary>
/// <param name="Heading">Título da seção, exibido no índice e como cabeçalho do bloco.</param>
/// <param name="MarkdownBody">Corpo da seção em Markdown.</param>
public sealed record PublicationSection(string Heading, string MarkdownBody);
