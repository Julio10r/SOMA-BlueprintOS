using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Infrastructure.Publication.Content;

namespace BlueprintOS.Infrastructure.Publication.Publishers;

/// <summary>
/// Escreve um <see cref="PublicationDocument"/> em disco em todos os formatos suportados
/// (Markdown, HTML, PDF), sob <c>dist/{Category}/{Slug}.{extensão}</c>. Compartilhado pelos
/// três publicadores de relatório para evitar duplicação da lógica de escrita em disco.
/// </summary>
internal static class ReportPublishingHelper
{
    /// <summary>
    /// Constrói uma <see cref="PublicationSection"/> a partir do Markdown bruto retornado por
    /// um gerador de documentação existente, convertendo-o uma única vez para o modelo comum
    /// (<see cref="ContentBlock"/>) consumido por todos os formatos de saída.
    /// </summary>
    public static PublicationSection BuildSection(string heading, string markdown) =>
        new(heading, MarkdownContentParser.Parse(markdown));

    public static async Task<IReadOnlyList<PublishedArtifact>> WriteAllFormatsAsync(
        PublicationDocument document,
        string category,
        string distRootPath,
        IReadOnlyList<IContentRenderer> renderers,
        CancellationToken cancellationToken)
    {
        var categoryDirectory = Path.Combine(distRootPath, category);
        Directory.CreateDirectory(categoryDirectory);

        var artifacts = new List<PublishedArtifact>(renderers.Count);
        foreach (var renderer in renderers)
        {
            var extension = renderer.Format switch
            {
                PublicationFormat.Markdown => "md",
                PublicationFormat.Html => "html",
                PublicationFormat.Pdf => "pdf",
                _ => throw new NotSupportedException($"Formato de publicação não suportado: {renderer.Format}"),
            };

            var relativePath = Path.Combine(category, $"{document.Slug}.{extension}");
            var filePath = Path.Combine(distRootPath, relativePath);

            var content = await renderer.RenderAsync(document, cancellationToken);
            await File.WriteAllBytesAsync(filePath, content, cancellationToken);

            artifacts.Add(new PublishedArtifact(renderer.Format, relativePath, filePath));
        }

        await WriteAttachmentsAsync(document, category, distRootPath, cancellationToken);

        return artifacts;
    }

    /// <summary>
    /// Copia os anexos do documento (se houver) para <c>dist/{Category}/attachments/</c>, uma
    /// única vez por documento (não por formato) — todos os renderizadores apenas referenciam
    /// esse caminho relativo por link, em vez de embuti-los inline.
    /// </summary>
    private static async Task WriteAttachmentsAsync(
        PublicationDocument document,
        string category,
        string distRootPath,
        CancellationToken cancellationToken)
    {
        if (document.Assets.Attachments.Count == 0)
        {
            return;
        }

        var attachmentsDirectory = Path.Combine(distRootPath, category, "attachments");
        Directory.CreateDirectory(attachmentsDirectory);

        foreach (var attachment in document.Assets.Attachments)
        {
            var filePath = Path.Combine(attachmentsDirectory, attachment.FileName);
            await File.WriteAllBytesAsync(filePath, attachment.Bytes, cancellationToken);
        }
    }
}
