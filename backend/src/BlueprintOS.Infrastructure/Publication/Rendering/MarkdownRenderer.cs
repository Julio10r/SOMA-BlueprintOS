using System.Text;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;

namespace BlueprintOS.Infrastructure.Publication.Rendering;

/// <summary>
/// Implementação de <see cref="IContentRenderer"/> que produz o documento em Markdown puro,
/// para versionamento no Git: capa textual, sumário com âncoras e as seções na ordem recebida.
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
            builder.AppendLine(section.MarkdownBody.TrimEnd());
        }

        return Task.FromResult(Encoding.UTF8.GetBytes(builder.ToString()));
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
