namespace BlueprintOS.Core.Publication.Models.Health;

/// <summary>
/// Resultado da análise de saúde de um único documento publicado.
/// </summary>
/// <param name="RelativePath">Caminho relativo do artefato Markdown analisado (relativo a <c>dist/</c>).</param>
/// <param name="Status">Classificação geral do documento.</param>
/// <param name="WordCount">Quantidade de palavras de conteúdo (excluindo marcação Markdown).</param>
/// <param name="Errors">Problemas que classificam o documento como <see cref="DocumentHealthStatus.Error"/>.</param>
/// <param name="Warnings">Problemas que classificam o documento como <see cref="DocumentHealthStatus.Warning"/>.</param>
public sealed record DocumentHealthResult(
    string RelativePath,
    DocumentHealthStatus Status,
    int WordCount,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);
