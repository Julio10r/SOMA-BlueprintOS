using System.Text;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;

namespace BlueprintOS.Infrastructure.Publication.Rendering;

/// <summary>
/// Implementação de <see cref="IContentRenderer"/> que serializa o modelo comum
/// (<see cref="PublicationDocument"/>/<see cref="ContentBlock"/>) de volta para Markdown puro,
/// para versionamento no Git: capa textual, sumário com âncoras e as seções na ordem recebida.
/// Não reprocessa texto — apenas emite cada <see cref="ContentBlock"/> em sua forma Markdown
/// equivalente, o mesmo modelo consumido pelos demais formatos.
/// </summary>
public sealed class MarkdownRenderer : IContentRenderer
{
    /// <inheritdoc />
    public PublicationFormat Format => PublicationFormat.Markdown;

    /// <inheritdoc />
    public Task<byte[]> RenderAsync(PublicationDocument document, CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# {document.Title}");
        builder.AppendLine();
        builder.AppendLine($"_{document.Subtitle}_");
        builder.AppendLine();
        builder.AppendLine($"**Versão:** {document.ProjectVersion} · **Gerado em:** {document.GeneratedAt:yyyy-MM-dd HH:mm} UTC");
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine("## Sumário");
        builder.AppendLine();
        foreach (var section in document.Sections)
        {
            builder.AppendLine($"- [{section.Heading}](#{Slugify(section.Heading)})");
        }

        builder.AppendLine();
        builder.AppendLine("---");

        foreach (var section in document.Sections)
        {
            builder.AppendLine();
            builder.AppendLine($"<a id=\"{Slugify(section.Heading)}\"></a>");
            builder.AppendLine($"## {section.Heading}");
            builder.AppendLine();
            AppendBlocks(builder, section.Blocks);
        }

        return Task.FromResult(Encoding.UTF8.GetBytes(builder.ToString()));
    }

    private static void AppendBlocks(StringBuilder builder, IReadOnlyList<ContentBlock> blocks)
    {
        foreach (var block in blocks)
        {
            switch (block.Kind)
            {
                case ContentBlockKind.Heading:
                    builder.AppendLine(new string('#', Math.Max(3, block.Level)) + " " + block.Text);
                    builder.AppendLine();
                    break;
                case ContentBlockKind.Paragraph:
                    builder.AppendLine(block.Text);
                    builder.AppendLine();
                    break;
                case ContentBlockKind.BulletList:
                    foreach (var item in block.Items ?? Array.Empty<string>())
                    {
                        builder.AppendLine($"- {item}");
                    }

                    builder.AppendLine();
                    break;
                case ContentBlockKind.Table:
                    var header = block.TableHeader ?? Array.Empty<string>();
                    builder.AppendLine($"| {string.Join(" | ", header)} |");
                    builder.AppendLine($"|{string.Concat(header.Select(_ => "---|"))}");
                    foreach (var row in block.TableRows ?? Array.Empty<IReadOnlyList<string>>())
                    {
                        builder.AppendLine($"| {string.Join(" | ", row)} |");
                    }

                    builder.AppendLine();
                    break;
                case ContentBlockKind.CodeBlock:
                    builder.AppendLine("```");
                    builder.AppendLine(block.Text);
                    builder.AppendLine("```");
                    builder.AppendLine();
                    break;
            }
        }
    }

    public static string Slugify(string text)
    {
        var lowered = text.Trim().ToLowerInvariant();
        var chars = lowered.Select(c => char.IsLetterOrDigit(c) ? c : (char.IsWhiteSpace(c) || c == '-' ? '-' : '\0'))
            .Where(c => c != '\0')
            .ToArray();
        var slug = new string(chars);
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug.Trim('-');
    }
}
