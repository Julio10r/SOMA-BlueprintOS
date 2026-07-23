using System.Text;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Core.Publication.Models.Assets;
using BlueprintOS.Infrastructure.Publication.Content;
using BlueprintOS.Infrastructure.Publication.Publishers;

namespace BlueprintOS.Infrastructure.Publication.Assets;

/// <summary>
/// Implementação de <see cref="IDocumentationAssetsManager"/>: centraliza logo, ícones,
/// QR Codes, diagramas/Mermaid, capa, rodapé, cores e fontes usados pelos três Publishers de
/// relatório. Nenhum outro tipo do projeto constrói <see cref="PublicationAssets"/>,
/// <see cref="PublicationTheme"/> ou chama <see cref="QrCodeImageGenerator"/> diretamente.
/// </summary>
public sealed class DocumentationAssetsManager : IDocumentationAssetsManager
{
    private const string RepositoryUrl = "https://github.com/Julio10r/SOMA-BlueprintOS";

    /// <summary>
    /// Fonte de corpo padrão dos documentos publicados (hoje aplicada apenas pelo
    /// <c>PdfRenderer</c>) — centralizada aqui como referência única de tipografia da
    /// plataforma, ponto de extensão para temas por categoria de documento.
    /// </summary>
    public const string BodyFontFamily = "Helvetica";

    /// <summary>Fonte monoespaçada padrão para blocos de código.</summary>
    public const string MonospaceFontFamily = "Courier";

    /// <inheritdoc />
    public PublicationTheme GetTheme(PublicationDocumentClass documentClass) => documentClass switch
    {
        PublicationDocumentClass.Executive => PublicationTheme.ForExecutive(),
        PublicationDocumentClass.Client => PublicationTheme.ForClient(),
        PublicationDocumentClass.Engineering => PublicationTheme.ForEngineering(),
        _ => throw new ArgumentOutOfRangeException(nameof(documentClass), documentClass, "Categoria de documento desconhecida."),
    };

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
    public async Task<string> BuildDiagramMarkdownAsync(
        Func<CancellationToken, Task<string>> mermaidSource,
        CancellationToken cancellationToken = default)
    {
        var raw = await mermaidSource(cancellationToken);
        return ReportPublishingHelper.StripFirstHeadingLine(raw);
    }
}
