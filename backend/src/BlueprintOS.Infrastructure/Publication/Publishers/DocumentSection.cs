namespace BlueprintOS.Infrastructure.Publication.Publishers;

/// <summary>
/// Uma seção dinâmica ainda não resolvida: um título fixo e uma função que produz o Markdown do
/// corpo em tempo de publicação (ex.: roadmap automático, diagrama de arquitetura, indicadores).
/// Usada pelo <see cref="DocumentAssembler"/> para acrescentar, ao conteúdo autoral carregado de
/// <c>.ai/content/{categoria}/</c>, as poucas seções que cada publisher continua gerando em
/// código.
/// </summary>
/// <param name="Title">Título da seção, exibido no índice.</param>
/// <param name="BuildMarkdownAsync">Função que produz o Markdown do corpo da seção.</param>
public sealed record DocumentSection(string Title, Func<CancellationToken, Task<string>> BuildMarkdownAsync)
{
    /// <summary>
    /// Cria uma <see cref="DocumentSection"/> com conteúdo já conhecido (não depende de I/O nem
    /// de outros serviços) — atalho para seções estáticas simples.
    /// </summary>
    public static DocumentSection Static(string title, string markdown) =>
        new(title, _ => Task.FromResult(markdown));
}
