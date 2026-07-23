using BlueprintOS.Core.Documentation.Contracts.Client;
using BlueprintOS.Core.Documentation.Contracts.Engineering;
using BlueprintOS.Core.Documentation.Contracts.Executive;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Publication.Publishers;

/// <summary>
/// Implementação de <see cref="IReportPublisher"/> para o Guia do Cliente
/// (<c>dist/client/ClientGuide.*</c>): reaproveita os geradores de documentação para cliente
/// (linguagem não técnica) já existentes no Portal de Documentação Viva.
/// </summary>
public sealed class ClientPublisher : IReportPublisher
{
    private readonly IProductOverviewGenerator _productOverviewGenerator;
    private readonly IFunctionalGuideGenerator _functionalGuideGenerator;
    private readonly IArchitectureGenerator _architectureGenerator;
    private readonly IUserGuideGenerator _userGuideGenerator;
    private readonly IApiDocumentationGenerator _apiDocumentationGenerator;
    private readonly IRoadmapGenerator _roadmapGenerator;
    private readonly IFaqGenerator _faqGenerator;
    private readonly IChangelogGenerator _changelogGenerator;
    private readonly IReadOnlyList<IContentRenderer> _renderers;
    private readonly string _distRootPath;
    private readonly string _projectVersion;

    public ClientPublisher(
        IProductOverviewGenerator productOverviewGenerator,
        IFunctionalGuideGenerator functionalGuideGenerator,
        IArchitectureGenerator architectureGenerator,
        IUserGuideGenerator userGuideGenerator,
        IApiDocumentationGenerator apiDocumentationGenerator,
        IRoadmapGenerator roadmapGenerator,
        IFaqGenerator faqGenerator,
        IChangelogGenerator changelogGenerator,
        IEnumerable<IContentRenderer> renderers,
        IOptions<PublicationOptions> publicationOptions)
    {
        _productOverviewGenerator = productOverviewGenerator;
        _functionalGuideGenerator = functionalGuideGenerator;
        _architectureGenerator = architectureGenerator;
        _userGuideGenerator = userGuideGenerator;
        _apiDocumentationGenerator = apiDocumentationGenerator;
        _roadmapGenerator = roadmapGenerator;
        _faqGenerator = faqGenerator;
        _changelogGenerator = changelogGenerator;
        _renderers = renderers.ToList();
        _distRootPath = publicationOptions.Value.DistRootPath;
        _projectVersion = publicationOptions.Value.ProjectVersion;
    }

    /// <inheritdoc />
    public string Category => "client";

    /// <inheritdoc />
    public async Task<IReadOnlyList<PublishedArtifact>> PublishAsync(CancellationToken cancellationToken = default)
    {
        var sections = new List<PublicationSection>
        {
            ReportPublishingHelper.BuildSection("Sobre o BlueprintOS, Objetivos e Benefícios", await _productOverviewGenerator.GenerateAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("Funcionalidades Disponíveis", await _functionalGuideGenerator.GenerateAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("Arquitetura em Alto Nível", await _architectureGenerator.GenerateAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("Casos de Uso", await _userGuideGenerator.GenerateAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("APIs Públicas", await _apiDocumentationGenerator.GenerateAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("Roadmap", await _roadmapGenerator.GenerateAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("Perguntas Frequentes (FAQ)", await _faqGenerator.GenerateAsync(cancellationToken)),
            ReportPublishingHelper.BuildSection("Changelog", await _changelogGenerator.GenerateAsync(cancellationToken)),
        };

        var metadata = PublicationMetadata.Create(
            title: "Guia do Cliente — BlueprintOS",
            subtitle: "Visão geral do produto, funcionalidades e roadmap para clientes",
            audience: "Clientes",
            version: _projectVersion,
            generatedAt: DateTimeOffset.UtcNow,
            tags: new[] { "cliente", "produto", "guia" });

        var document = new PublicationDocument(
            Slug: "ClientGuide",
            Category: Category,
            Metadata: metadata,
            Sections: sections,
            Assets: PublicationAssets.Empty,
            Appendix: Array.Empty<PublicationSection>(),
            Theme: PublicationTheme.ForClient());

        return await ReportPublishingHelper.WriteAllFormatsAsync(document, Category, _distRootPath, _renderers, cancellationToken);
    }
}
