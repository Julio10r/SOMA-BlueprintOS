using System.Text.RegularExpressions;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Infrastructure.Publication.Content;

namespace BlueprintOS.Infrastructure.Publication.Publishers;

/// <summary>
/// Gera as representações HTML e PDF do Executive Blueprint cuja fonte oficial é o Markdown
/// versionado em <c>docs/executive</c>. Reutiliza os renderizadores do Publication Engine,
/// mantendo um único conteúdo-fonte para os artefatos institucionais.
/// </summary>
public static class ExecutiveBlueprintPublisher
{
    private const string FileName = "BlueprintOS_Executive_Blueprint";
    private static readonly Regex SectionPattern = new(
        @"(?m)^## (?<heading>[^\r\n]+)\r?\n(?<body>[\s\S]*?)(?=^## |\z)",
        RegexOptions.Compiled);

    public static async Task PublishAsync(
        string repositoryRoot,
        IEnumerable<IContentRenderer> renderers,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.Combine(repositoryRoot, "docs", "executive");
        var markdownPath = Path.Combine(directory, $"{FileName}.md");
        var markdown = await File.ReadAllTextAsync(markdownPath, cancellationToken);
        var sections = SectionPattern.Matches(markdown)
            .Select(match => new PublicationSection(
                match.Groups["heading"].Value.Trim(),
                MarkdownContentParser.Parse(match.Groups["body"].Value)))
            .ToArray();

        var generatedAt = new DateTimeOffset(2026, 7, 23, 0, 0, 0, TimeSpan.Zero);
        var document = new PublicationDocument(
            Slug: FileName,
            Category: "executive",
            Metadata: PublicationMetadata.Create(
                title: "BlueprintOS — Executive Blueprint",
                subtitle: "Documento institucional consolidado",
                audience: "Diretoria, clientes e equipes de produto e engenharia",
                version: "1.0.0",
                generatedAt: generatedAt,
                tags: new[] { "executivo", "arquitetura", "fonte-de-verdade" }),
            Sections: sections,
            Assets: PublicationAssets.Empty,
            Appendix: Array.Empty<PublicationSection>(),
            Theme: PublicationTheme.ForExecutive());

        foreach (var renderer in renderers.Where(renderer => renderer.Format != PublicationFormat.Markdown))
        {
            var extension = renderer.Format switch
            {
                PublicationFormat.Html => "html",
                PublicationFormat.Pdf => "pdf",
                _ => throw new NotSupportedException($"Formato não suportado: {renderer.Format}"),
            };

            var content = await renderer.RenderAsync(document, cancellationToken);
            await File.WriteAllBytesAsync(Path.Combine(directory, $"{FileName}.{extension}"), content, cancellationToken);
        }
    }
}
