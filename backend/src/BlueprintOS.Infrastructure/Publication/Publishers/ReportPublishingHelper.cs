using System.Text;
using System.Text.RegularExpressions;
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
    private static readonly Regex PhaseObjectivePattern = new(
        @"^##\s+(Fase [^\n]+)\r?\n\r?\nObjetivo:\s*(.+)$",
        RegexOptions.Multiline | RegexOptions.Compiled);

    /// <summary>
    /// Constrói uma <see cref="PublicationSection"/> a partir do Markdown bruto retornado por
    /// um gerador de documentação existente, convertendo-o uma única vez para o modelo comum
    /// (<see cref="ContentBlock"/>) consumido por todos os formatos de saída.
    /// </summary>
    public static PublicationSection BuildSection(string heading, string markdown) =>
        new(heading, MarkdownContentParser.Parse(markdown));

    /// <summary>
    /// Remove a primeira linha de título (<c>#</c>/<c>##</c>) de um Markdown gerado por um
    /// gerador existente, permitindo reaproveitar seu conteúdo sob um título diferente (definido
    /// pelo publisher) sem duplicar a lógica de geração em si.
    /// </summary>
    public static string StripFirstHeadingLine(string markdown)
    {
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        if (lines.Length == 0 || !lines[0].TrimStart().StartsWith('#'))
        {
            return markdown;
        }

        return string.Join('\n', lines.Skip(1)).TrimStart('\n');
    }

    /// <summary>
    /// Separa a primeira linha de título (<c>#</c>) de um arquivo de conteúdo estratégico
    /// autorado (ex.: <c>.ai/content/executive/</c> ou <c>.ai/content/client/</c>), usando-a como
    /// título da <see cref="PublicationSection"/> — preservando o título definido por quem
    /// autora o conteúdo, em vez de fixá-lo no código do publisher.
    /// </summary>
    public static (string Heading, string Body) SplitHeading(string markdown)
    {
        var normalized = markdown.Replace("\r\n", "\n").TrimStart('\n');
        var firstLineBreak = normalized.IndexOf('\n');
        var firstLine = firstLineBreak >= 0 ? normalized[..firstLineBreak] : normalized;

        var heading = firstLine.TrimStart().StartsWith('#')
            ? firstLine.TrimStart('#', ' ').Trim()
            : "Seção";

        return (heading, StripFirstHeadingLine(normalized));
    }

    /// <summary>
    /// Extrai, a partir de <c>.ai/ROADMAP.md</c>, o objetivo declarado de cada fase e o
    /// apresenta como benefício esperado — reaproveitado tanto pelo Relatório Executivo quanto
    /// pelo Guia do Cliente para evitar duplicar a leitura/parsing do roadmap em dois lugares.
    /// </summary>
    public static async Task<string> BuildExpectedBenefitsMarkdownAsync(string aiRootPath, CancellationToken cancellationToken)
    {
        var path = Path.Combine(aiRootPath, "ROADMAP.md");
        if (!File.Exists(path))
        {
            return "Roadmap não encontrado; benefícios esperados não puderam ser derivados.";
        }

        var content = await File.ReadAllTextAsync(path, cancellationToken);
        var matches = PhaseObjectivePattern.Matches(content);
        if (matches.Count == 0)
        {
            return "Nenhum objetivo de fase identificado em `.ai/ROADMAP.md`.";
        }

        var builder = new StringBuilder();
        foreach (Match match in matches)
        {
            builder.AppendLine($"- **{match.Groups[1].Value.Trim()}** — {match.Groups[2].Value.Trim()}");
        }

        return builder.ToString();
    }

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
