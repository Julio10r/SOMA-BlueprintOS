using System.Net;
using System.Text;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Infrastructure.Publication.Content;

namespace BlueprintOS.Infrastructure.Publication.Rendering;

/// <summary>
/// Escreve uma sequência de <see cref="ContentBlock"/> como HTML, usando exatamente o mesmo
/// modelo estruturado consumido pelo <see cref="PdfRenderer"/> — nenhum dos dois deriva do
/// outro, ambos derivam do mesmo <see cref="ContentBlock"/>. Ênfase inline é resolvida via
/// <see cref="InlineSpanParser"/>, compartilhado entre os dois renderizadores.
/// </summary>
internal static class ContentBlockHtmlWriter
{
    public static void Write(StringBuilder builder, IReadOnlyList<ContentBlock> blocks, PublicationAssets assets)
    {
        foreach (var block in blocks)
        {
            switch (block.Kind)
            {
                case ContentBlockKind.Heading:
                    var tag = block.Level <= 3 ? "h3" : "h4";
                    builder.AppendLine($"<{tag}>{WriteInline(block.Text ?? string.Empty)}</{tag}>");
                    break;
                case ContentBlockKind.Paragraph:
                    builder.AppendLine($"<p>{WriteInline(block.Text ?? string.Empty)}</p>");
                    break;
                case ContentBlockKind.BulletList:
                    builder.AppendLine("<ul>");
                    foreach (var item in block.Items ?? Array.Empty<string>())
                    {
                        builder.AppendLine($"<li>{WriteInline(item)}</li>");
                    }

                    builder.AppendLine("</ul>");
                    break;
                case ContentBlockKind.Table:
                    builder.AppendLine("<table>");
                    builder.AppendLine("<thead><tr>");
                    foreach (var header in block.TableHeader ?? Array.Empty<string>())
                    {
                        builder.AppendLine($"<th>{WriteInline(header)}</th>");
                    }

                    builder.AppendLine("</tr></thead>");
                    builder.AppendLine("<tbody>");
                    foreach (var row in block.TableRows ?? Array.Empty<IReadOnlyList<string>>())
                    {
                        builder.AppendLine("<tr>");
                        foreach (var cell in row)
                        {
                            builder.AppendLine($"<td>{WriteInline(cell)}</td>");
                        }

                        builder.AppendLine("</tr>");
                    }

                    builder.AppendLine("</tbody>");
                    builder.AppendLine("</table>");
                    break;
                case ContentBlockKind.CodeBlock:
                    builder.AppendLine($"<pre><code>{WebUtility.HtmlEncode(block.Text ?? string.Empty)}</code></pre>");
                    break;
                case ContentBlockKind.Image:
                    var image = block.AssetId is not null ? assets.FindEmbeddableImage(block.AssetId) : null;
                    if (image is not null)
                    {
                        var (bytes, mediaType, altText) = image.Value;
                        builder.AppendLine("<figure>");
                        builder.AppendLine($"<img src=\"data:{mediaType};base64,{Convert.ToBase64String(bytes)}\" alt=\"{WebUtility.HtmlEncode(altText)}\" />");
                        if (!string.IsNullOrEmpty(block.Caption))
                        {
                            builder.AppendLine($"<figcaption>{WriteInline(block.Caption)}</figcaption>");
                        }

                        builder.AppendLine("</figure>");
                    }

                    break;
            }
        }
    }

    private static string WriteInline(string text)
    {
        var spans = InlineSpanParser.Parse(text);
        var builder = new StringBuilder();
        foreach (var span in spans)
        {
            var encoded = WebUtility.HtmlEncode(span.Text);
            builder.Append(span.Kind switch
            {
                InlineSpanKind.Bold => $"<strong>{encoded}</strong>",
                InlineSpanKind.Code => $"<code>{encoded}</code>",
                _ => encoded,
            });
        }

        return builder.ToString();
    }
}
