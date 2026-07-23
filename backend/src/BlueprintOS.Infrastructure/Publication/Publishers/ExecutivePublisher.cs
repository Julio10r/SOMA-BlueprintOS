using System.Text;
using System.Text.RegularExpressions;
using BlueprintOS.Core.Documentation.Contracts.Executive;
using BlueprintOS.Core.Documentation.Contracts.Engineering;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Core.Publication.Models.Assets;
using BlueprintOS.Infrastructure.Documentation;
using BlueprintOS.Infrastructure.Publication.Content;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Publication.Publishers;

/// <summary>
/// Implementação de <see cref="IReportPublisher"/> para o Relatório Executivo
/// (<c>dist/executive/ExecutiveReport.*</c>): reaproveita os geradores executivos e de
/// engenharia já existentes do Portal de Documentação Viva, acrescenta indicadores reais de
/// build/testes (via <see cref="IQualityMetricsProvider"/>) exibidos como selos na capa, um QR
/// Code apontando para o repositório real, e dívidas técnicas/próximos passos extraídos
/// diretamente de <c>.ai/memory/known_issues.md</c> e <c>.ai/ROADMAP.md</c>.
/// </summary>
public sealed class ExecutivePublisher : IReportPublisher
{
    private const string RepositoryUrl = "https://github.com/Julio10r/SOMA-BlueprintOS";

    private static readonly Regex PendingPhasePattern =
        new(@"^##\s+(Fase .+\(status:\s*(?!concluíd).+\))$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private readonly IDashboardGenerator _dashboardGenerator;
    private readonly ISprintStatusGenerator _sprintStatusGenerator;
    private readonly IReleaseGenerator _releaseGenerator;
    private readonly IRoadmapGenerator _roadmapGenerator;
    private readonly IArchitectureGenerator _architectureGenerator;
    private readonly IQualityMetricsProvider _qualityMetricsProvider;
    private readonly IReadOnlyList<IContentRenderer> _renderers;
    private readonly string _distRootPath;
    private readonly string _projectVersion;
    private readonly string _aiRootPath;

    public ExecutivePublisher(
        IDashboardGenerator dashboardGenerator,
        ISprintStatusGenerator sprintStatusGenerator,
        IReleaseGenerator releaseGenerator,
        IRoadmapGenerator roadmapGenerator,
        IArchitectureGenerator architectureGenerator,
        IQualityMetricsProvider qualityMetricsProvider,
        IEnumerable<IContentRenderer> renderers,
        IOptions<PublicationOptions> publicationOptions,
        IOptions<DocumentationOptions> documentationOptions)
    {
        _dashboardGenerator = dashboardGenerator;
        _sprintStatusGenerator = sprintStatusGenerator;
        _releaseGenerator = releaseGenerator;
        _roadmapGenerator = roadmapGenerator;
        _architectureGenerator = architectureGenerator;
        _qualityMetricsProvider = qualityMetricsProvider;
        _renderers = renderers.ToList();
        _distRootPath = publicationOptions.Value.DistRootPath;
        _projectVersion = publicationOptions.Value.ProjectVersion;
        _aiRootPath = documentationOptions.Value.AiRootPath;
    }

    /// <inheritdoc />
    public string Category => "executive";

    /// <inheritdoc />
    public async Task<IReadOnlyList<PublishedArtifact>> PublishAsync(CancellationToken cancellationToken = default)
    {
        var metrics = await _qualityMetricsProvider.GetMetricsAsync(cancellationToken);
        var generatedAt = DateTimeOffset.UtcNow;

        var sections = new List<PublicationSection>
        {
            ReportPublishingHelper.BuildSection("Resumo Executivo", await _dashboardGenerator.GenerateAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("Status do Projeto (Build, Testes e Qualidade)", BuildProjectStatusSection(metrics)),
            ReportPublishingHelper.BuildSection("Roadmap", await _roadmapGenerator.GenerateAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("Sprint Atual", await _sprintStatusGenerator.GenerateAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("Sprints Concluídas", await _releaseGenerator.GenerateAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("Módulos Implementados", await _architectureGenerator.GenerateAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("Dívidas Técnicas", await BuildKnownIssuesSectionAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("Próximos Passos", await BuildNextStepsSectionAsync(cancellationToken)),
        };

        var metadata = PublicationMetadata.Create(
            title: "Relatório Executivo — BlueprintOS",
            subtitle: "Visão consolidada de status, build e roadmap para apresentação à diretoria",
            audience: "Diretoria",
            version: _projectVersion,
            generatedAt: generatedAt,
            tags: new[] { "executivo", "status", "build" });

        var document = new PublicationDocument(
            Slug: "ExecutiveReport",
            Category: Category,
            Metadata: metadata,
            Sections: sections,
            Assets: BuildAssets(metrics),
            Appendix: BuildAppendix(metadata),
            Theme: PublicationTheme.ForExecutive());

        return await ReportPublishingHelper.WriteAllFormatsAsync(document, Category, _distRootPath, _renderers, cancellationToken);
    }

    private static PublicationAssets BuildAssets(QualityMetrics metrics)
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
            new(
                "badge-warnings",
                "Warnings",
                metrics.WarningCount.ToString(),
                metrics.WarningCount == 0 ? BadgeStatus.Success : BadgeStatus.Warning),
        };

        var qrCode = new QrCodeAsset(
            "qr-repository",
            RepositoryUrl,
            "Repositório no GitHub",
            QrCodeImageGenerator.GeneratePng(RepositoryUrl));

        return PublicationAssets.Empty with { Badges = badges, QrCodes = new[] { qrCode } };
    }

    private static IReadOnlyList<PublicationSection> BuildAppendix(PublicationMetadata metadata)
    {
        var builder = new StringBuilder();
        builder.AppendLine("| Versão | Data | Autor | Resumo |");
        builder.AppendLine("|---|---|---|---|");
        foreach (var revision in metadata.RevisionHistory)
        {
            builder.AppendLine($"| {revision.Version} | {revision.Date:yyyy-MM-dd} | {revision.Author} | {revision.Summary} |");
        }

        return new[] { ReportPublishingHelper.BuildSection("Histórico de Versões", builder.ToString()) };
    }

    private static string BuildProjectStatusSection(QualityMetrics metrics)
    {
        var builder = new StringBuilder();
        builder.AppendLine(metrics.Summary);
        builder.AppendLine();
        builder.AppendLine("| Indicador | Valor |");
        builder.AppendLine("|---|---|");
        builder.AppendLine($"| Build Status | {(metrics.BuildSucceeded ? "✅ Sucesso" : "❌ Falhou")} |");
        builder.AppendLine($"| Warnings | {metrics.WarningCount} |");
        builder.AppendLine($"| Erros | {metrics.ErrorCount} |");
        builder.AppendLine($"| Quantidade de Testes | {metrics.TestCount} |");

        return builder.ToString();
    }

    private async Task<string> BuildKnownIssuesSectionAsync(CancellationToken cancellationToken)
    {
        var path = Path.Combine(_aiRootPath, "memory", "known_issues.md");
        if (!File.Exists(path))
        {
            return "Nenhuma dívida técnica registrada.";
        }

        var content = await File.ReadAllTextAsync(path, cancellationToken);
        return content.Trim();
    }

    private async Task<string> BuildNextStepsSectionAsync(CancellationToken cancellationToken)
    {
        var path = Path.Combine(_aiRootPath, "ROADMAP.md");
        if (!File.Exists(path))
        {
            return "Roadmap não encontrado; próximos passos não puderam ser derivados.";
        }

        var content = await File.ReadAllTextAsync(path, cancellationToken);
        var matches = PendingPhasePattern.Matches(content);
        if (matches.Count == 0)
        {
            return "Nenhuma fase pendente identificada em `.ai/ROADMAP.md`.";
        }

        var builder = new StringBuilder();
        builder.AppendLine("Próximas fases do roadmap ainda não concluídas:");
        builder.AppendLine();
        foreach (Match match in matches)
        {
            builder.AppendLine($"- {match.Groups[1].Value.Trim()}");
        }

        return builder.ToString();
    }
}
