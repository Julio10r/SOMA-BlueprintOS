using BlueprintOS.Core.Documentation.Contracts.Client;
using BlueprintOS.Core.Documentation.Contracts.Engineering;
using BlueprintOS.Core.Documentation.Contracts.Executive;
using BlueprintOS.Infrastructure.Documentation;
using BlueprintOS.Infrastructure.Documentation.Assets;
using BlueprintOS.Infrastructure.Documentation.Publishing;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation;

public class DocumentationPublishServiceTests : IDisposable
{
    private sealed class FakeGenerator :
        IDashboardGenerator, IKpiGenerator, IRoadmapGenerator, ISprintStatusGenerator, IReleaseGenerator,
        IProductOverviewGenerator, IUserGuideGenerator, IFunctionalGuideGenerator, IApiDocumentationGenerator, IChangelogGenerator, IFaqGenerator,
        IArchitectureGenerator, IDatabaseGenerator, IAgentsGenerator, IApiGenerator, IDeployGenerator, IRunbookGenerator, IMermaidGenerator, IDecisionsGenerator
    {
        public Task<string> GenerateAsync(CancellationToken cancellationToken = default) => Task.FromResult("conteúdo gerado de teste");
    }

    private readonly string _aiRoot;
    private readonly string _docsRoot;

    public DocumentationPublishServiceTests()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _aiRoot = Path.Combine(root, ".ai");
        _docsRoot = Path.Combine(root, "docs");
        Directory.CreateDirectory(Path.Combine(_aiRoot, "memory"));

        File.WriteAllText(Path.Combine(_aiRoot, "ROADMAP.md"), "# ROADMAP.md\n\n## Fase 0 - Fundação (status: em andamento)\n\n- item existente.\n\n---\n\n## Fase 1\n");
        File.WriteAllText(Path.Combine(_aiRoot, "memory", "completed_sprints.md"), "# completed_sprints.md\n\n## Sprint A7 — Existente\n\ntexto\n");
        File.WriteAllText(Path.Combine(_aiRoot, "memory", "known_issues.md"), "# known_issues.md\n\n## Sprint A7 — Existente\n\n- item existente.\n");
    }

    private DocumentationPublishService CreateService()
    {
        var assetsRoot = Path.Combine(Path.GetDirectoryName(_docsRoot)!, "assets");
        var options = Options.Create(new DocumentationOptions { AiRootPath = _aiRoot, DocsRootPath = _docsRoot, AssetsRootPath = assetsRoot });
        var publisher = new DocumentationPublisher(new MarkdownPublisher(options));
        var fake = new FakeGenerator();

        return new DocumentationPublishService(
            publisher, options,
            fake, fake, fake, fake, fake,
            fake, fake, fake, fake, fake, fake,
            fake, fake, fake, fake, fake, fake, fake, fake,
            new DocumentationAssetGenerator(new MermaidDiagramGenerator()),
            new AssetFilePublisher(options));
    }

    [Fact]
    public async Task PublishAllAsync_Should_Publish_All_Nineteen_Documents()
    {
        var results = await CreateService().PublishAllAsync();

        Assert.Equal(19, results.Count);
        Assert.True(File.Exists(Path.Combine(_docsRoot, "executive/Dashboard.md")));
        Assert.True(File.Exists(Path.Combine(_docsRoot, "engineering/Mermaid/ArchitectureDiagram.md")));
    }

    [Fact]
    public async Task PublishAllAsync_Should_Generate_Reusable_Documentation_Assets()
    {
        await CreateService().PublishAllAsync();

        var assetsRoot = Path.Combine(Path.GetDirectoryName(_docsRoot)!, "assets");
        Assert.True(File.Exists(Path.Combine(assetsRoot, "architecture.mmd")));
        Assert.True(File.Exists(Path.Combine(assetsRoot, "dependencies.mmd")));
        Assert.True(File.Exists(Path.Combine(assetsRoot, "agents.mmd")));
        Assert.True(File.Exists(Path.Combine(assetsRoot, "solution-tree.md")));
    }

    [Fact]
    public async Task PublishAllAsync_Should_Append_New_Sprint_Section_To_CompletedSprints_Without_Removing_Existing()
    {
        await CreateService().PublishAllAsync();

        var content = await File.ReadAllTextAsync(Path.Combine(_aiRoot, "memory", "completed_sprints.md"));

        Assert.Contains("Sprint A7 — Existente", content);
        Assert.Contains("Sprint A8 — Portal de Documentação Viva", content);
    }

    [Fact]
    public async Task PublishAllAsync_Should_Append_KnownIssues_Section_Without_Removing_Existing()
    {
        await CreateService().PublishAllAsync();

        var content = await File.ReadAllTextAsync(Path.Combine(_aiRoot, "memory", "known_issues.md"));

        Assert.Contains("Sprint A7 — Existente", content);
        Assert.Contains("Sprint A8 — Portal de Documentação Viva", content);
    }

    [Fact]
    public async Task PublishAllAsync_Should_Append_Roadmap_Marker_Without_Removing_Existing_Content()
    {
        await CreateService().PublishAllAsync();

        var content = await File.ReadAllTextAsync(Path.Combine(_aiRoot, "ROADMAP.md"));

        Assert.Contains("item existente.", content);
        Assert.Contains("Portal de documentação viva", content);
    }

    [Fact]
    public async Task PublishAllAsync_Should_Be_Idempotent_When_Run_Twice()
    {
        var service = CreateService();
        await service.PublishAllAsync();
        await service.PublishAllAsync();

        var content = await File.ReadAllTextAsync(Path.Combine(_aiRoot, "memory", "completed_sprints.md"));
        var occurrences = content.Split("Sprint A8 — Portal de Documentação Viva").Length - 1;

        Assert.Equal(1, occurrences);
    }

    public void Dispose()
    {
        var root = Path.GetDirectoryName(_aiRoot);
        if (root is not null && Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
