namespace BlueprintOS.Core.Publication.Models;

/// <summary>
/// Tipo de ênfase de um <see cref="InlineSpan"/> dentro do texto de um <see cref="ContentBlock"/>.
/// </summary>
public enum InlineSpanKind
{
    /// <summary>Texto sem ênfase.</summary>
    Plain,

    /// <summary>Texto em negrito.</summary>
    Bold,

    /// <summary>Texto em fonte monoespaçada (código inline).</summary>
    Code,
}

/// <summary>
/// Um trecho de texto inline já classificado (plano, negrito ou código), parte do modelo
/// comum consumido por todos os renderizadores para reconstruir ênfase textual sem cada um
/// reimplementar sua própria interpretação de Markdown.
/// </summary>
/// <param name="Kind">Tipo de ênfase do trecho.</param>
/// <param name="Text">Texto do trecho, sem os marcadores de ênfase.</param>
public sealed record InlineSpan(InlineSpanKind Kind, string Text);
