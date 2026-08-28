using BlueprintOS.Core.Publication.Models;

namespace BlueprintOS.Infrastructure.Publication.Content;

/// <summary>
/// Converte o Markdown bruto produzido pelos geradores de documentação existentes (Portal de
/// Documentação Viva, Sprint A8) em uma sequência de <see cref="ContentBlock"/> — o modelo
/// comum (ViewModel) consumido por todos os <see cref="Core.Publication.Contracts.IContentRenderer"/>.
/// Essa conversão acontece uma única vez, no momento em que o <c>IReportPublisher</c> monta o
/// <see cref="PublicationDocument"/>; nenhum renderizador reprocessa Markdown por conta própria.
/// </summary>
public static class MarkdownContentParser
{
    public static IReadOnlyList<ContentBlock> Parse(string markdown)
    {
        var blocks = new List<ContentBlock>();
        var lines = markdown.Replace("\r\n", "\n").Split('\n');

        var paragraphBuffer = new List<string>();
        var bulletBuffer = new List<string>();
        var tableHeader = (IReadOnlyList<string>?)null;
        var tableRows = new List<IReadOnlyList<string>>();
        var codeBuffer = new List<string>();
        var inCodeBlock = false;

        void FlushParagraph()
        {
            if (paragraphBuffer.Count == 0)
            {
                return;
            }

            blocks.Add(ContentBlock.Paragraph(string.Join(" ", paragraphBuffer).Trim()));
            paragraphBuffer.Clear();
        }

        void FlushBulletList()
        {
            if (bulletBuffer.Count == 0)
            {
                return;
            }

            blocks.Add(ContentBlock.BulletList(bulletBuffer.ToArray()));
            bulletBuffer.Clear();
        }

        void FlushTable()
        {
            if (tableHeader is null || tableRows.Count == 0)
            {
                tableHeader = null;
                tableRows.Clear();
                return;
            }

            blocks.Add(ContentBlock.Table(tableHeader, tableRows.ToArray()));
            tableHeader = null;
            tableRows.Clear();
        }

        void FlushAllExceptCode()
        {
            FlushParagraph();
            FlushBulletList();
            FlushTable();
        }

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                if (inCodeBlock)
                {
                    blocks.Add(ContentBlock.CodeBlock(string.Join("\n", codeBuffer)));
                    codeBuffer.Clear();
                    inCodeBlock = false;
                }
                else
                {
                    FlushAllExceptCode();
                    inCodeBlock = true;
                }

                continue;
            }

            if (inCodeBlock)
            {
                codeBuffer.Add(rawLine);
                continue;
            }

            var trimmed = line.Trim();

            if (trimmed.Length == 0)
            {
                FlushAllExceptCode();
                continue;
            }

            if (trimmed.StartsWith('#'))
            {
                FlushAllExceptCode();
                var level = trimmed.TakeWhile(c => c == '#').Count();
                blocks.Add(ContentBlock.Heading(trimmed.TrimStart('#').Trim(), level));
                continue;
            }

            if (trimmed.StartsWith("- ", StringComparison.Ordinal) || trimmed.StartsWith("* ", StringComparison.Ordinal))
            {
                FlushParagraph();
                FlushTable();
                bulletBuffer.Add(trimmed[2..].Trim());
                continue;
            }

            if (trimmed.StartsWith('|') && trimmed.EndsWith('|'))
            {
                FlushParagraph();
                FlushBulletList();

                var cells = trimmed.Trim('|').Split('|').Select(c => c.Trim()).ToArray();
                if (cells.All(c => c.Length > 0 && c.All(ch => ch is '-' or ':')))
                {
                    continue;
                }

                if (tableHeader is null)
                {
                    tableHeader = cells;
                }
                else
                {
                    tableRows.Add(cells);
                }

                continue;
            }

            FlushBulletList();
            FlushTable();
            paragraphBuffer.Add(trimmed);
        }

        FlushAllExceptCode();
        if (codeBuffer.Count > 0)
        {
            blocks.Add(ContentBlock.CodeBlock(string.Join("\n", codeBuffer)));
        }

        return blocks;
    }
}
