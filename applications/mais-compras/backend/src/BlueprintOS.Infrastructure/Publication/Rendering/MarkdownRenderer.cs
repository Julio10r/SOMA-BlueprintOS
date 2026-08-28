using System.Text;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;

namespace BlueprintOS.Infrastructure.Publication.Rendering;

/// <summary>
/// Implementação de <see cref="IContentRenderer"/> que serializa o modelo comum
/// (<see cref="PublicationDocument"/>/<see cref="ContentBlock"/>) de volta para Markdown puro,
/// para versionamento no Git: capa textual, metadados, selos, sumário com âncoras, seções,
/// apêndice e imagens (embutidas como data URI). Não reprocessa texto — apenas emite cada
/// <see cref="ContentBlock"/> em sua forma Markdown equivalente, o mesmo modelo consumido
/// pelos demais formatos.
/// </summary>
public sealed class MarkdownRenderer : IContentRenderer
{
    /// <inheritdoc />
    public PublicationFormat Format => PublicationFormat.Markdown;

    /// <inheritdoc />
    public Task<byte[]> RenderAsync(PublicationDocument document, CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        var metadata = document.Metadata;

        builder.AppendLine($"# {metadata.Title}");
        builder.AppendLine();
        builder.AppendLine($"_{metadata.Subtitle}_");
        builder.AppendLine();
        builder.AppendLine($"**Público-alvo:** {metadata.Audience} · **Versão:** {metadata.Version} · **Gerado em:** {metadata.GeneratedAt:yyyy-MM-dd HH:mm} UTC");
        builder.AppendLine($"**Autor:** {metadata.Author} · **Empresa:** {metadata.Company} · **Classificação:** {metadata.Classification}");
        if (metadata.Tags.Count > 0)
        {
            builder.AppendLine($"**Tags:** {string.Join(", ", metadata.Tags)}");
        }

        if (document.Assets.Badges.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine(string.Join(" &nbsp;·&nbsp; ", document.Assets.Badges.Select(b => $"`{b.Label}: {b.Value}`")));
        }

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
            AppendBlocks(builder, section.Blocks, document.Assets);
        }

        if (document.Appendix.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("---");
            builder.AppendLine();
            builder.AppendLine("## Apêndice");

            foreach (var section in document.Appendix)
            {
                builder.AppendLine();
                builder.AppendLine($"### {section.Heading}");
                builder.AppendLine();
                AppendBlocks(builder, section.Blocks, document.Assets);
            }
        }

        if (document.Assets.Attachments.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("---");
            builder.AppendLine();
            builder.AppendLine("## Anexos");
            builder.AppendLine();
            foreach (var attachment in document.Assets.Attachments)
            {
                builder.AppendLine($"- [{attachment.FileName}](./attachments/{attachment.FileName}) — {attachment.Description}");
            }
        }

        return Task.FromResult(Encoding.UTF8.GetBytes(builder.ToString()));
    }

    private static void AppendBlocks(StringBuilder builder, IReadOnlyList<ContentBlock> blocks, PublicationAssets assets)
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

                    if (!string.IsNullOrEmpty(block.Caption))
                    {
                        builder.AppendLine();
                        builder.AppendLine($"_{block.Caption}_");
                    }

                    builder.AppendLine();
                    break;
                case ContentBlockKind.CodeBlock:
                    builder.AppendLine("```");
                    builder.AppendLine(block.Text);
                    builder.AppendLine("```");
                    builder.AppendLine();
                    break;
                case ContentBlockKind.Image:
                    var image = block.AssetId is not null ? assets.FindEmbeddableImage(block.AssetId) : null;
                    if (image is not null)
                    {
                        var (bytes, mediaType, altText) = image.Value;
                        builder.AppendLine($"![{altText}](data:{mediaType};base64,{Convert.ToBase64String(bytes)})");
                        if (!string.IsNullOrEmpty(block.Caption))
                        {
                            builder.AppendLine();
                            builder.AppendLine($"_{block.Caption}_");
                        }

                        builder.AppendLine();
                    }

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
