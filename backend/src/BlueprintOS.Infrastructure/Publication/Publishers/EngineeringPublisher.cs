using BlueprintOS.Core.Documentation.Contracts.Engineering;
using BlueprintOS.Core.Documentation.Contracts.Executive;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Publication.Publishers;

/// <summary>
/// Implementação de <see cref="IReportPublisher"/> para o Guia de Engenharia
/// (<c>dist/engineering/EngineeringGuide.*</c>). O conteúdo técnico (visão técnica, arquitetura,
/// componentes, runtime de IA, memória, conhecimento, desenvolvimento, DevOps, testes, segurança,
/// roadmap técnico e próximos passos) é autorado como Markdown em
/// <c>.ai/content/engineering/</c> e carregado via <see cref="IEngineeringContentLoader"/>; a
/// montagem do documento (capa, índice, tema, selos, QR Code, apêndice e rodapé) é delegada ao
/// <see cref="DocumentAssembler"/> e ao <see cref="IDocumentationAssetsManager"/> — este
/// publisher não acessa nenhum asset diretamente. Só define o <see cref="DocumentTemplate"/> e a
/// única seção dinâmica específica (roadmap automático).
/// </summary>
public sealed class EngineeringPublisher : IReportPublisher
{
    private static readonly DocumentTemplate Template = new(
        Slug: "EngineeringGuide",
        Category: "engineering",
        Title: "Guia de Engenharia — BlueprintOS",
        Subtitle: "Visão técnica, arquitetura, componentes e roadmap técnico da plataforma",
        Audience: "Equipe de Engenharia",
        Tags: new[] { "engenharia", "arquitetura", "técnico" },
        DocumentClass: PublicationDocumentClass.Engineering);

    private readonly IEngineeringContentLoader _contentLoader;
    private readonly IRoadmapGenerator _roadmapGenerator;
    private readonly IQualityMetricsProvider _qualityMetricsProvider;
    private readonly IDocumentationAssetsManager _assetsManager;
    private readonly IReadOnlyList<IContentRenderer> _renderers;
    private readonly string _distRootPath;
    private readonly string _projectVersion;

    public EngineeringPublisher(
        IEngineeringContentLoader contentLoader,
        IRoadmapGenerator roadmapGenerator,
        IQualityMetricsProvider qualityMetricsProvider,
        IDocumentationAssetsManager assetsManager,
        IEnumerable<IContentRenderer> renderers,
        IOptions<PublicationOptions> publicationOptions)
    {
        _contentLoader = contentLoader;
        _roadmapGenerator = roadmapGenerator;
        _qualityMetricsProvider = qualityMetricsProvider;
        _assetsManager = assetsManager;
        _renderers = renderers.ToList();
        _distRootPath = publicationOptions.Value.DistRootPath;
        _projectVersion = publicationOptions.Value.ProjectVersion;
    }

    /// <inheritdoc />
    public string Category => Template.Category;

    /// <inheritdoc />
    public async Task<IReadOnlyList<PublishedArtifact>> PublishAsync(CancellationToken cancellationToken = default)
    {
        var metrics = await _qualityMetricsProvider.GetMetricsAsync(cancellationToken);
        var contentFiles = await _contentLoader.LoadAsync(cancellationToken);

        var dynamicSections = new[]
        {
            new DocumentSection("Roadmap Automático", _roadmapGenerator.GenerateAsync),
        };

        return await DocumentAssembler.AssembleAsync(
            Template,
            contentFiles.Select(f => (f.FileName, f.Content)).ToList(),
            dynamicSections,
            _assetsManager,
            metrics,
            DateTimeOffset.UtcNow,
            _projectVersion,
            _distRootPath,
            _renderers,
            cancellationToken);
    }
}
