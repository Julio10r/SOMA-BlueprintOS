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

    private static DocumentTemplate CreateTemplate() => new(
        Slug: "TestDoc",
        Category: "test",
        Title: "Documento de Teste",
        Subtitle: "Subtítulo de teste",
        Audience: "Testadores",
        Tags: new[] { "teste" },
        Theme: PublicationTheme.ForExecutive());

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

    public void Dispose()
    {
        if (Directory.Exists(_distRoot))
        {
            Directory.Delete(_distRoot, recursive: true);
        }
    }
}
