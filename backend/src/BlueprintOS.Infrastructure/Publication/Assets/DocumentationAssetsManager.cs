using System.Text;
using System.Text.RegularExpressions;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Core.Publication.Models.Assets;
using BlueprintOS.Infrastructure.Publication.Content;
using BlueprintOS.Infrastructure.Publication.Publishers;

namespace BlueprintOS.Infrastructure.Publication.Assets;

/// <summary>
/// Implementação de <see cref="IDocumentationAssetsManager"/>: centraliza logo, ícones,
/// QR Codes, diagramas/Mermaid, capa, rodapé, cores e fontes usados pelos três Publishers de
/// relatório. Cores e tipografia vêm sempre de <see cref="IDocumentThemeProvider"/> (Design
/// System oficial) — nenhum valor é hardcoded aqui. Nenhum outro tipo do projeto constrói
/// <see cref="PublicationAssets"/>, <see cref="PublicationTheme"/> ou chama
/// <see cref="QrCodeImageGenerator"/> diretamente.
/// </summary>
public sealed class DocumentationAssetsManager : IDocumentationAssetsManager
{
    private const string RepositoryUrl = "https://github.com/Julio10r/SOMA-BlueprintOS";

    private static readonly Regex FencedBlockPattern = new(
        @"```(?:mermaid)?\s*\n([\s\S]*?)```", RegexOptions.Compiled);

    private readonly IDocumentThemeProvider _themeProvider;

    public DocumentationAssetsManager(IDocumentThemeProvider themeProvider)
    {
        _themeProvider = themeProvider;
    }

    /// <inheritdoc />
    public PublicationTheme GetTheme(PublicationDocumentClass documentClass) => new(
        documentClass,
        _themeProvider.GetPalette(),
        _themeProvider.GetTypography(),
        _themeProvider.GetStylesheet());

    /// <inheritdoc />
    public PublicationAssets BuildStandardAssets(QualityMetrics metrics)
    {
        var badges = new List<BadgeAsset>
        {
            new(
                "badge-build",
                "Build",
                metrics.BuildSucceeded ? "passing" : "failing",
                metrics.BuildSucceeded ? BadgeStatus.Success : BadgeStatus.Failure),
            new(
                "badge-tests",
                "Testes",
                metrics.TestCount.ToString(),
                metrics.TestCount > 0 ? BadgeStatus.Success : BadgeStatus.Neutral),
        };

        var qrCode = new QrCodeAsset(
            "qr-repository",
            RepositoryUrl,
            "Repositório no GitHub",
            QrCodeImageGenerator.GeneratePng(RepositoryUrl));

        return PublicationAssets.Empty with { Badges = badges, QrCodes = new[] { qrCode } };
    }

    /// <inheritdoc />
    public IReadOnlyList<PublicationSection> BuildStandardAppendix(PublicationMetadata metadata)
    {
        var builder = new StringBuilder();
        builder.AppendLine("| Versão | Data | Autor | Resumo |");
        builder.AppendLine("|---|---|---|---|");
        foreach (var revision in metadata.RevisionHistory)
        {
            builder.AppendLine($"| {revision.Version} | {revision.Date:yyyy-MM-dd} | {revision.Author} | {revision.Summary} |");
        }

        var repositorySection = new PublicationSection(
            "Repositório",
            new[]
            {
                ContentBlock.Paragraph($"Código-fonte do BlueprintOS: {RepositoryUrl}"),
                ContentBlock.Image("qr-repository", "Acesse o repositório escaneando o QR Code."),
            });

        return new[]
        {
            ReportPublishingHelper.BuildSection("Histórico de Versões", builder.ToString()),
            repositorySection,
        };
    }

    /// <inheritdoc />
    public async Task<DocumentDiagram> RenderDiagramAsync(
        string title,
        string assetId,
        Func<CancellationToken, Task<string>> mermaidSource,
        CancellationToken cancellationToken = default)
    {
        var raw = await mermaidSource(cancellationToken);
        var stripped = ReportPublishingHelper.StripFirstHeadingLine(raw);

        var fenceMatch = FencedBlockPattern.Match(stripped);
        var definition = fenceMatch.Success ? fenceMatch.Groups[1].Value.Trim() : stripped.Trim();

        var svgBytes = SimpleMermaidSvgRenderer.Render(definition, _themeProvider.GetPalette());
        var asset = new MermaidAsset(assetId, title, definition, svgBytes, "image/svg+xml");
        var section = new PublicationSection(
            title,
            new[] { ContentBlock.Image(assetId, "Diagrama gerado automaticamente a partir da definição Mermaid.") });

        return new DocumentDiagram(section, asset);
    }
}
