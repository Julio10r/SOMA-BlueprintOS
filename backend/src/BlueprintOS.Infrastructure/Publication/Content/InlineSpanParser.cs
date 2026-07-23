using System.Text.RegularExpressions;
using BlueprintOS.Core.Publication.Models;

namespace BlueprintOS.Infrastructure.Publication.Content;

/// <summary>
/// Decompõe o texto de um <see cref="ContentBlock"/> (que pode conter ênfase inline em
/// Markdown: <c>**negrito**</c> e <c>`código`</c>) em uma sequência de <see cref="InlineSpan"/>.
/// Compartilhado por todos os renderizadores que precisam de ênfase real (HTML, PDF e futuros
/// formatos como Word/PowerPoint), evitando que cada um reimplemente sua própria interpretação
/// de Markdown inline. O <c>MarkdownRenderer</c> não usa este parser: como sua saída já é
/// Markdown, ele emite o texto original sem alterações.
/// </summary>
public static class InlineSpanParser
{
    private static readonly Regex SpanPattern = new(@"\*\*(.+?)\*\*|`(.+?)`", RegexOptions.Compiled);

    public static IReadOnlyList<InlineSpan> Parse(string text)
    {
        var spans = new List<InlineSpan>();
        var lastIndex = 0;

        foreach (Match match in SpanPattern.Matches(text))
        {
            if (match.Index > lastIndex)
            {
                spans.Add(new InlineSpan(InlineSpanKind.Plain, text[lastIndex..match.Index]));
            }

            if (match.Groups[1].Success)
            {
                spans.Add(new InlineSpan(InlineSpanKind.Bold, match.Groups[1].Value));
            }
            else
            {
                spans.Add(new InlineSpan(InlineSpanKind.Code, match.Groups[2].Value));
            }

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < text.Length)
        {
            spans.Add(new InlineSpan(InlineSpanKind.Plain, text[lastIndex..]));
        }

        return spans;
    }
}
