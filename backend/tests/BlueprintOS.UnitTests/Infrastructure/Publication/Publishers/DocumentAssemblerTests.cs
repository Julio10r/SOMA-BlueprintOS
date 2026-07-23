using System.Text;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Core.Publication.Models.Assets;
using BlueprintOS.Infrastructure.Publication.Publishers;

namespace BlueprintOS.UnitTests.Infrastructure.Publication.Publishers;

public class DocumentAssemblerTests : IDisposable
{
    private readonly string _distRoot = Path.Combine(Path.GetTempPath(), $"document-assembler-tests-{Guid.NewGuid():N}");

    private static readonly DocumentPalette DummyPalette = new(
        "#111111", "#222222", "#333333", "#444444", "#555555", "#666666", "#777777", "#888888", "#999999", "#aaaaaa");

    private static readonly DocumentTypography DummyTypography = new("Display Font", "Body Font", "Mono Font");

    private sealed class CapturingRenderer : IContentRenderer
    {
        public PublicationDocument? LastDocument { get; private set; }

        public PublicationFormat Format => PublicationFormat.Markdown;

        public Task<byte[]> RenderAsync(PublicationDocument document, CancellationToken cancellationToken = default)
        {
            LastDocument = document;
            return Task.FromResult(Encoding.UTF8.GetBytes($"markdown:{document.Slug}"));
        }
    }

    /// <summary>
    /// Dublê de <see cref="IDocumentationAssetsManager"/> que registra as chamadas recebidas,
    /// permitindo verificar que o assembler nunca constrói tema/assets/apêndice por conta
    /// própria — sempre delega ao serviço injetado.
    /// </summary>
    private sealed class FakeAssetsManager : IDocumentationAssetsManager
    {
        public List<PublicationDocumentClass> ThemeRequests { get; } = new();

        public int BuildStandardAssetsCallCount { get; private set; }

        public int BuildStandardAppendixCallCount { get; private set; }

        public PublicationTheme GetTheme(PublicationDocumentClass documentClass)
        {
            ThemeRequests.Add(documentClass);
            return new PublicationTheme(documentClass, DummyPalette, DummyTypography, "/* css */");
        }

        public PublicationAssets BuildStandardAssets(QualityMetrics metrics)
        {
            BuildStandardAssetsCallCount++;
            return PublicationAssets.Empty with
            {
                Badges = new[] { new BadgeAsset("badge-build", "Build", "passing", BadgeStatus.Success) },
            };
        }

        public IReadOnlyList<PublicationSection> BuildStandardAppendix(PublicationMetadata metadata)
        {
            BuildStandardAppendixCallCount++;
            return new[] { new PublicationSection("Apêndice de Teste", Array.Empty<ContentBlock>()) };
        }

        public Task<DocumentDiagram> RenderDiagramAsync(
            string title, string assetId, Func<CancellationToken, Task<string>> mermaidSource, CancellationToken cancellationToken = default)
        {
            var section = new PublicationSection(title, new[] { ContentBlock.Image(assetId) });
            var asset = new MermaidAsset(assetId, title, "graph TD", new byte[] { 1, 2, 3 }, "image/svg+xml");
            return Task.FromResult(new DocumentDiagram(section, asset));
        }
    }

    private static DocumentTemplate CreateTemplate(PublicationDocumentClass documentClass = PublicationDocumentClass.Executive) => new(
        Slug: "TestDoc",
        Category: "test",
        Title: "Documento de Teste",
        Subtitle: "Subtítulo de teste",
        Audience: "Testadores",
        Tags: new[] { "teste" },
        DocumentClass: documentClass);

    private static QualityMetrics CreateMetrics() => new(
        BuildSucceeded: true,
        WarningCount: 0,
        ErrorCount: 0,
        TestCount: 42,
        Summary: "ok");

    private static Task<IReadOnlyList<PublishedArtifact>> Assemble(
        CapturingRenderer renderer,
        string distRoot,
        DocumentTemplate? template = null,
        IReadOnlyList<(string FileName, string Content)>? contentFiles = null,
        IReadOnlyList<DocumentSection>? dynamicSections = null,
        IReadOnlyList<DocumentDiagram>? diagrams = null,
        IDocumentationAssetsManager? assetsManager = null,
        DateTimeOffset? generatedAt = null,
        string projectVersion = "1.0.0") =>
        DocumentAssembler.AssembleAsync(
            template ?? CreateTemplate(),
            contentFiles ?? Array.Empty<(string FileName, string Content)>(),
            dynamicSections ?? Array.Empty<DocumentSection>(),
            diagrams ?? Array.Empty<DocumentDiagram>(),
            assetsManager ?? new FakeAssetsManager(),
            CreateMetrics(),
            generatedAt ?? DateTimeOffset.UtcNow,
            projectVersion,
            distRoot,
            new[] { renderer },
            CancellationToken.None);

    [Fact]
    public async Task AssembleAsync_Should_Produce_Only_Executive_Summary_When_No_Content_And_No_Dynamic_Sections()
    {
        var renderer = new CapturingRenderer();

        await Assemble(renderer, _distRoot, generatedAt: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var sections = renderer.LastDocument!.Sections;
        Assert.Single(sections);
        Assert.Equal("Resumo Executivo", sections[0].Heading);
    }

    [Fact]
    public async Task AssembleAsync_Should_Build_One_Section_Per_Content_File_Using_Its_Own_Heading()
    {
        var renderer = new CapturingRenderer();
        var contentFiles = new[]
        {
            ("01-a.md", "# Primeira Seção\n\nCorpo da primeira."),
            ("02-b.md", "# Segunda Seção\n\nCorpo da segunda."),
        };

        await Assemble(renderer, _distRoot, contentFiles: contentFiles);

        var sections = renderer.LastDocument!.Sections;
        Assert.Equal(3, sections.Count);
        Assert.Equal("Resumo Executivo", sections[0].Heading);
        Assert.Equal("Primeira Seção", sections[1].Heading);
        Assert.Equal("Segunda Seção", sections[2].Heading);
    }

    [Fact]
    public async Task AssembleAsync_Should_Condense_Roadmap_In_Body_And_Move_Full_Roadmap_To_Appendix()
    {
        var renderer = new CapturingRenderer();
        var fullRoadmap = "### Fase 0 - Fundação\n\nObjetivo: base.\n\n### Fase 1 - Core\n\nObjetivo: módulos.";
        var dynamicSections = new[] { new DocumentSection("Roadmap Automático", _ => Task.FromResult(fullRoadmap)) };

        await Assemble(renderer, _distRoot, dynamicSections: dynamicSections);

        var sections = renderer.LastDocument!.Sections;
        Assert.Contains(sections, s => s.Heading == "Linha do Tempo");
        Assert.DoesNotContain(sections, s => s.Heading == "Roadmap Automático");

        var appendix = renderer.LastDocument!.Appendix;
        Assert.Contains(appendix, s => s.Heading == "Roadmap Completo");
    }

    [Fact]
    public async Task AssembleAsync_Should_Preserve_Order_Content_Files_Then_Dynamic_Sections()
    {
        var renderer = new CapturingRenderer();
        var contentFiles = new[] { ("01-a.md", "# Conteúdo Autoral\n\nCorpo.") };
        var dynamicSections = new[]
        {
            new DocumentSection("Roadmap Automático", _ => Task.FromResult("### Fase 0\n\nObjetivo: x.")),
            new DocumentSection("Indicadores", _ => Task.FromResult("Corpo dos indicadores.")),
        };

        await Assemble(renderer, _distRoot, contentFiles: contentFiles, dynamicSections: dynamicSections);

        var headings = renderer.LastDocument!.Sections.Select(s => s.Heading).ToArray();
        Assert.Equal(new[] { "Resumo Executivo", "Conteúdo Autoral", "Linha do Tempo", "Indicadores" }, headings);
    }

    [Fact]
    public async Task AssembleAsync_Should_Merge_Rendered_Diagrams_Into_Sections_And_Mermaid_Assets()
    {
        var renderer = new CapturingRenderer();
        var diagram = await new FakeAssetsManager().RenderDiagramAsync("Visão de Arquitetura", "diagram-1", _ => Task.FromResult("graph TD"));

        await Assemble(renderer, _distRoot, diagrams: new[] { diagram });

        Assert.Contains(renderer.LastDocument!.Sections, s => s.Heading == "Visão de Arquitetura");
        Assert.Single(renderer.LastDocument!.Assets.Mermaid);
        Assert.Equal("diagram-1", renderer.LastDocument!.Assets.Mermaid[0].Id);
    }

    [Fact]
    public async Task AssembleAsync_Should_Add_Version_Documents_And_Status_Badges_On_Top_Of_Standard_Badges()
    {
        var renderer = new CapturingRenderer();

        await Assemble(renderer, _distRoot, projectVersion: "3.2.1");

        var badges = renderer.LastDocument!.Assets.Badges;
        Assert.Contains(badges, b => b.Id == "badge-build"); // vindo do IDocumentationAssetsManager
        Assert.Contains(badges, b => b.Id == "badge-version" && b.Value == "3.2.1");
        Assert.Contains(badges, b => b.Id == "badge-sections");
        Assert.Contains(badges, b => b.Id == "badge-status" && b.Value == "Operacional");
    }

    [Fact]
    public async Task AssembleAsync_Should_Populate_Metadata_From_Template_And_Parameters()
    {
        var renderer = new CapturingRenderer();
        var generatedAt = new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);

        await Assemble(renderer, _distRoot, generatedAt: generatedAt, projectVersion: "2.5.0");

        var metadata = renderer.LastDocument!.Metadata;
        Assert.Equal("Documento de Teste", metadata.Title);
        Assert.Equal("Subtítulo de teste", metadata.Subtitle);
        Assert.Equal("Testadores", metadata.Audience);
        Assert.Equal("2.5.0", metadata.Version);
        Assert.Equal(generatedAt, metadata.GeneratedAt);
        Assert.Contains("teste", metadata.Tags);
    }

    [Fact]
    public async Task AssembleAsync_Should_Write_Rendered_Artifact_To_Disk()
    {
        var renderer = new CapturingRenderer();

        var artifacts = await Assemble(renderer, _distRoot);

        Assert.Single(artifacts);
        var filePath = Path.Combine(_distRoot, "test", "TestDoc.md");
        Assert.True(File.Exists(filePath));
        Assert.Equal("markdown:TestDoc", await File.ReadAllTextAsync(filePath));
    }

    [Fact]
    public async Task AssembleAsync_Should_Never_Build_Theme_Assets_Or_Appendix_Itself()
    {
        var renderer = new CapturingRenderer();
        var assetsManager = new FakeAssetsManager();

        await Assemble(renderer, _distRoot, template: CreateTemplate(PublicationDocumentClass.Engineering), assetsManager: assetsManager);

        Assert.Equal(new[] { PublicationDocumentClass.Engineering }, assetsManager.ThemeRequests);
        Assert.Equal(1, assetsManager.BuildStandardAssetsCallCount);
        Assert.Equal(1, assetsManager.BuildStandardAppendixCallCount);
        Assert.Equal(PublicationDocumentClass.Engineering, renderer.LastDocument!.Theme.DocumentClass);
        Assert.Contains(renderer.LastDocument!.Appendix, s => s.Heading == "Apêndice de Teste");
    }

    public void Dispose()
    {
        if (Directory.Exists(_distRoot))
        {
            Directory.Delete(_distRoot, recursive: true);
        }
    }
}
