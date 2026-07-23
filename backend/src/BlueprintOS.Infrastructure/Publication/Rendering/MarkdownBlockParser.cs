namespace BlueprintOS.Infrastructure.Publication.Rendering;

/// <summary>
/// Tipo de um bloco de Markdown reconhecido por <see cref="MarkdownBlockParser"/>.
/// </summary>
internal enum MarkdownBlockKind
{
    Heading,
    Paragraph,
    BulletItem,
    TableRow,
    CodeBlock,
}

/// <summary>
/// Um bloco de conteúdo já classificado, pronto para ser desenhado pelo <see cref="PdfRenderer"/>.
/// </summary>
/// <param name="Kind">Tipo do bloco.</param>
/// <param name="Text">Texto do bloco (sem os marcadores de Markdown que definem o tipo).</param>
/// <param name="Level">Nível de cabeçalho (quando <see cref="Kind"/> é <see cref="MarkdownBlockKind.Heading"/>).</param>
/// <param name="Cells">Células da linha (quando <see cref="Kind"/> é <see cref="MarkdownBlockKind.TableRow"/>).</param>
/// <param name="IsTableHeader">Indica se a linha de tabela é o cabeçalho.</param>
internal sealed record MarkdownBlock(
    MarkdownBlockKind Kind,
    string Text,
    int Level = 0,
    IReadOnlyList<string>? Cells = null,
    bool IsTableHeader = false);

/// <summary>
/// Parser simples de Markdown que reconhece os elementos usados pelos geradores de
/// documentação do BlueprintOS (cabeçalhos, parágrafos, listas, tabelas e blocos de código),
/// suficiente para reconstruir a estrutura do documento no PDF (QuestPDF não converte HTML).
/// </summary>
internal static class MarkdownBlockParser
{
    public static IReadOnlyList<MarkdownBlock> Parse(string markdown)
    {
        var blocks = new List<MarkdownBlock>();
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var paragraphBuffer = new List<string>();
        var inCodeBlock = false;
        var codeBuffer = new List<string>();

        void FlushParagraph()
        {
            if (paragraphBuffer.Count == 0)
            {
                return;
            }

            blocks.Add(new MarkdownBlock(MarkdownBlockKind.Paragraph, string.Join(" ", paragraphBuffer).Trim()));
            paragraphBuffer.Clear();
        }

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                if (inCodeBlock)
                {
                    blocks.Add(new MarkdownBlock(MarkdownBlockKind.CodeBlock, string.Join("\n", codeBuffer)));
                    codeBuffer.Clear();
                    inCodeBlock = false;
                }
                else
                {
                    FlushParagraph();
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
                FlushParagraph();
                continue;
            }

            if (trimmed.StartsWith('#'))
            {
                FlushParagraph();
                var level = trimmed.TakeWhile(c => c == '#').Count();
                blocks.Add(new MarkdownBlock(MarkdownBlockKind.Heading, trimmed.TrimStart('#').Trim(), level));
                continue;
            }

            if (trimmed.StartsWith("- ", StringComparison.Ordinal) || trimmed.StartsWith("* ", StringComparison.Ordinal))
            {
                FlushParagraph();
                blocks.Add(new MarkdownBlock(MarkdownBlockKind.BulletItem, trimmed[2..].Trim()));
                continue;
            }

            if (trimmed.StartsWith('|') && trimmed.EndsWith('|'))
            {
                FlushParagraph();
                var cells = trimmed.Trim('|').Split('|').Select(c => c.Trim()).ToArray();
                if (cells.All(c => c.All(ch => ch is '-' or ':')))
                {
                    continue;
                }

                var isHeader = blocks.Count == 0 || blocks[^1].Kind != MarkdownBlockKind.TableRow;
                blocks.Add(new MarkdownBlock(MarkdownBlockKind.TableRow, string.Empty, Cells: cells, IsTableHeader: isHeader));
                continue;
            }

            paragraphBuffer.Add(trimmed);
        }

        FlushParagraph();
        if (codeBuffer.Count > 0)
        {
            blocks.Add(new MarkdownBlock(MarkdownBlockKind.CodeBlock, string.Join("\n", codeBuffer)));
        }

        return blocks;
    }
}
