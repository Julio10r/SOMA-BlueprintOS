using System.Text;
using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Infrastructure.Publication.Publishers;

namespace BlueprintOS.UnitTests.Infrastructure.Publication.Publishers;

public class DocumentAssemblerTests : IDisposable
{
    private readonly string _distRoot = Path.Combine(Path.GetTempPath(), $"document-assembler-tests-{Guid.NewGuid():N}");

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
            return documentClass switch
            {
                PublicationDocumentClass.Client => PublicationTheme.ForClient(),
                PublicationDocumentClass.Engineering => PublicationTheme.ForEngineering(),
                _ => PublicationTheme.ForExecutive(),
            };
        }

        public PublicationAssets BuildStandardAssets(QualityMetrics metrics)
        {
            BuildStandardAssetsCallCount++;
            return PublicationAssets.Empty;
        }

        public IReadOnlyList<PublicationSection> BuildStandardAppendix(PublicationMetadata metadata)
        {
            BuildStandardAppendixCallCount++;
            return new[] { new PublicationSection("Apêndice de Teste", Array.Empty<ContentBlock>()) };
        }

        public Task<string> BuildDiagramMarkdownAsync(
            Func<CancellationToken, Task<string>> mermaidSource, CancellationToken cancellationToken = default) =>
            mermaidSource(cancellationToken);
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

    [Fact]
    public async Task AssembleAsync_Should_Produce_Empty_Document_When_No_Content_And_No_Dynamic_Sections()
    {
        var renderer = new CapturingRenderer();

        await DocumentAssembler.AssembleAsync(
            CreateTemplate(),
            Array.Empty<(string FileName, string Content)>(),
            Array.Empty<DocumentSection>(),
            new FakeAssetsManager(),
            CreateMetrics(),
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            "1.0.0",
            _distRoot,
            new[] { renderer },
            CancellationToken.None);

        Assert.NotNull(renderer.LastDocument);
        Assert.Empty(renderer.LastDocument!.Sections);
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

        await DocumentAssembler.AssembleAsync(
            CreateTemplate(),
            contentFiles,
            Array.Empty<DocumentSection>(),
            new FakeAssetsManager(),
            CreateMetrics(),
            DateTimeOffset.UtcNow,
            "1.0.0",
            _distRoot,
            new[] { renderer },
            CancellationToken.None);

        var sections = renderer.LastDocument!.Sections;
        Assert.Equal(2, sections.Count);
        Assert.Equal("Primeira Seção", sections[0].Heading);
        Assert.Equal("Segunda Seção", sections[1].Heading);
    }

    [Fact]
    public async Task AssembleAsync_Should_Preserve_Order_Content_Files_Then_Dynamic_Sections()
    {
        var renderer = new CapturingRenderer();
        var contentFiles = new[] { ("01-a.md", "# Conteúdo Autoral\n\nCorpo.") };
        var dynamicSections = new[]
        {
            new DocumentSection("Roadmap Automático", _ => Task.FromResult("Corpo do roadmap.")),
            new DocumentSection("Indicadores", _ => Task.FromResult("Corpo dos indicadores.")),
        };

        await DocumentAssembler.AssembleAsync(
            CreateTemplate(),
            contentFiles,
            dynamicSections,
            new FakeAssetsManager(),
            CreateMetrics(),
            DateTimeOffset.UtcNow,
            "1.0.0",
            _distRoot,
            new[] { renderer },
            CancellationToken.None);

        var headings = renderer.LastDocument!.Sections.Select(s => s.Heading).ToArray();
        Assert.Equal(new[] { "Conteúdo Autoral", "Roadmap Automático", "Indicadores" }, headings);
    }

    [Fact]
    public async Task AssembleAsync_Should_Populate_Metadata_From_Template_And_Parameters()
    {
        var renderer = new CapturingRenderer();
        var generatedAt = new DateTimeOffset(2026, 3, 15, 12, 0, 0, TimeSpan.Zero);

        await DocumentAssembler.AssembleAsync(
            CreateTemplate(),
            Array.Empty<(string FileName, string Content)>(),
            Array.Empty<DocumentSection>(),
            new FakeAssetsManager(),
            CreateMetrics(),
            generatedAt,
            "2.5.0",
            _distRoot,
            new[] { renderer },
            CancellationToken.None);

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

        var artifacts = await DocumentAssembler.AssembleAsync(
            CreateTemplate(),
            Array.Empty<(string FileName, string Content)>(),
            Array.Empty<DocumentSection>(),
            new FakeAssetsManager(),
            CreateMetrics(),
            DateTimeOffset.UtcNow,
            "1.0.0",
            _distRoot,
            new[] { renderer },
            CancellationToken.None);

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

        await DocumentAssembler.AssembleAsync(
            CreateTemplate(PublicationDocumentClass.Engineering),
            Array.Empty<(string FileName, string Content)>(),
            Array.Empty<DocumentSection>(),
            assetsManager,
            CreateMetrics(),
            DateTimeOffset.UtcNow,
            "1.0.0",
            _distRoot,
            new[] { renderer },
            CancellationToken.None);

        Assert.Equal(new[] { PublicationDocumentClass.Engineering }, assetsManager.ThemeRequests);
        Assert.Equal(1, assetsManager.BuildStandardAssetsCallCount);
        Assert.Equal(1, assetsManager.BuildStandardAppendixCallCount);
        Assert.Equal(PublicationDocumentClass.Engineering, renderer.LastDocument!.Theme.DocumentClass);
        Assert.Equal("Apêndice de Teste", renderer.LastDocument!.Appendix[0].Heading);
    }

    public void Dispose()
    {
        if (Directory.Exists(_distRoot))
        {
            Directory.Delete(_distRoot, recursive: true);
        }
    }
}
