using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Infrastructure.Publication;
using BlueprintOS.Infrastructure.Publication.Assets;
using BlueprintOS.Infrastructure.Publication.Docs;
using BlueprintOS.Infrastructure.Publication.Rendering;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Infrastructure.Publication.Docs;

public class DocsPublisherTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _docsRoot;
    private readonly string _distRoot;

    public DocsPublisherTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "BlueprintOSDocsPublisherTests_" + Guid.NewGuid().ToString("N"));
        _docsRoot = Path.Combine(_tempRoot, "docs");
        _distRoot = Path.Combine(_tempRoot, "dist");
        Directory.CreateDirectory(_docsRoot);
        Directory.CreateDirectory(_distRoot);
    }

    public void Dispose() => Directory.Delete(_tempRoot, recursive: true);

    private sealed class FakeThemeProvider : IDocumentThemeProvider
    {
        public DocumentPalette GetPalette() => new(
            "#111111", "#222222", "#333333", "#444444", "#555555", "#666666", "#777777", "#888888", "#999999", "#aaaaaa");

        public DocumentTypography GetTypography() => new("Display", "Body", "Mono");

        public string GetStylesheet() => "/* test css */";
    }

    private sealed class FakeQualityMetricsProvider : IQualityMetricsProvider
    {
        public Task<QualityMetrics> GetMetricsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new QualityMetrics(true, 0, 0, 10, "ok"));
    }

    private sealed class FailingRenderer : IContentRenderer
    {
        public PublicationFormat Format => PublicationFormat.Html;

        public Task<byte[]> RenderAsync(PublicationDocument document, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Renderer instável de propósito, para teste.");
    }

    private void WriteDoc(string relativePath, string content)
    {
        var fullPath = Path.Combine(_docsRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    private DocsPublisher CreatePublisher(
        IReadOnlyList<IContentRenderer>? renderers = null,
        HashSet<string>? excluded = null,
        string? docsRootOverride = null,
        string? distRootOverride = null)
    {
        var options = Options.Create(new PublicationOptions
        {
            DocsRootPath = docsRootOverride ?? _docsRoot,
            DistRootPath = distRootOverride ?? _distRoot,
            ExcludedTopLevelDirectories = excluded ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "audits", "demo" },
        });

        var assetsManager = new DocumentationAssetsManager(new FakeThemeProvider());
        var discovery = new DocsDiscoveryService(options.Value.ExcludedTopLevelDirectories);
        var effectiveRenderers = renderers ?? new IContentRenderer[] { new MarkdownRenderer(), new HtmlRenderer(), new PdfRenderer() };

        return new DocsPublisher(discovery, assetsManager, new FakeQualityMetricsProvider(), effectiveRenderers, options);
    }

    [Fact]
    public async Task PublishAllAsync_Should_Generate_Markdown_Html_And_Pdf_For_Each_Document()
    {
        WriteDoc("architecture/Architecture.md", "# Arquitetura\n\nConteúdo real da arquitetura do sistema.");

        var artifacts = await CreatePublisher().PublishAllAsync();

        var docArtifacts = artifacts.Where(a => a.RelativePath.StartsWith("architecture", StringComparison.Ordinal)).ToList();
        Assert.Contains(docArtifacts, a => a.Format == PublicationFormat.Markdown);
        Assert.Contains(docArtifacts, a => a.Format == PublicationFormat.Html);
        Assert.Contains(docArtifacts, a => a.Format == PublicationFormat.Pdf);
        Assert.True(File.Exists(Path.Combine(_distRoot, "architecture", "Architecture.md")));
        Assert.True(File.Exists(Path.Combine(_distRoot, "architecture", "Architecture.html")));
        Assert.True(File.Exists(Path.Combine(_distRoot, "architecture", "Architecture.pdf")));
    }

    [Fact]
    public async Task PublishAllAsync_Should_Preserve_Relative_Domain_Structure_In_Dist()
    {
        WriteDoc("backend/procurement/Procurement.md", "# Procurement\n\nDomínio de fornecedores.");

        await CreatePublisher().PublishAllAsync();

        Assert.True(File.Exists(Path.Combine(_distRoot, "backend", "procurement", "Procurement.md")));
    }

    [Fact]
    public async Task PublishAllAsync_Should_Generate_Navigable_Index_In_All_Formats()
    {
        WriteDoc("architecture/Architecture.md", "# Arquitetura\n\nConteúdo.");
        WriteDoc("frontend/Frontend.md", "# Frontend\n\nConteúdo.");

        var artifacts = await CreatePublisher().PublishAllAsync();

        Assert.Contains(artifacts, a => a.RelativePath == "index.md");
        Assert.Contains(artifacts, a => a.RelativePath == "index.html");
        Assert.Contains(artifacts, a => a.RelativePath == "index.pdf");

        var indexContent = await File.ReadAllTextAsync(Path.Combine(_distRoot, "index.md"));
        Assert.Contains("architecture/Architecture.md", indexContent);
        Assert.Contains("frontend/Frontend.md", indexContent);
    }

    [Fact]
    public async Task PublishAllAsync_Should_Respect_Configured_Exclusions()
    {
        WriteDoc("architecture/Architecture.md", "# Arquitetura\n\nConteúdo.");
        WriteDoc("audits/OldAudit.md", "# Auditoria Antiga\n\nHistórico.");

        var artifacts = await CreatePublisher().PublishAllAsync();

        Assert.DoesNotContain(artifacts, a => a.RelativePath.StartsWith("audits", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PublishAllAsync_Should_Throw_When_Docs_Source_Does_Not_Exist()
    {
        var missing = Path.Combine(_tempRoot, "no-such-docs");
        var publisher = CreatePublisher(docsRootOverride: missing);

        // A validação de segurança de caminhos (PublicationOptions.ValidateSafePaths) verifica a
        // existência da fonte antes mesmo da descoberta ser acionada.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.PublishAllAsync());
        Assert.Contains("DocsRootPath", ex.Message);
    }

    [Fact]
    public async Task PublishAllAsync_Should_Throw_When_Dist_Is_Inside_Docs()
    {
        var nestedDist = Path.Combine(_docsRoot, "dist");
        var publisher = CreatePublisher(distRootOverride: nestedDist);

        await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.PublishAllAsync());
        Assert.False(Directory.Exists(nestedDist));
    }

    [Fact]
    public async Task PublishAllAsync_Should_Throw_When_Dist_Targets_Ai_Directory()
    {
        var aiDirectory = Path.Combine(_tempRoot, ".ai");
        Directory.CreateDirectory(aiDirectory);
        WriteDoc("architecture/Architecture.md", "# Arquitetura\n\nConteúdo.");

        var publisher = CreatePublisher(distRootOverride: Path.Combine(aiDirectory, "leak"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => publisher.PublishAllAsync());
        Assert.False(Directory.Exists(Path.Combine(aiDirectory, "leak")));
    }

    [Fact]
    public async Task PublishAllAsync_Should_Handle_Empty_Document_Without_Failing_The_Run()
    {
        WriteDoc("empty/Empty.md", string.Empty);
        WriteDoc("architecture/Architecture.md", "# Arquitetura\n\nConteúdo real.");

        var artifacts = await CreatePublisher().PublishAllAsync();

        Assert.Contains(artifacts, a => a.RelativePath.StartsWith("empty", StringComparison.Ordinal));
        Assert.Contains(artifacts, a => a.RelativePath.StartsWith("architecture", StringComparison.Ordinal));

        var emptyMarkdown = await File.ReadAllTextAsync(Path.Combine(_distRoot, "empty", "Empty.md"));
        Assert.Contains("sem conteúdo", emptyMarkdown, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PublishAllAsync_Should_Isolate_A_Failing_Renderer_And_Continue_Publishing_Other_Documents()
    {
        WriteDoc("architecture/Architecture.md", "# Arquitetura\n\nConteúdo.");
        WriteDoc("frontend/Frontend.md", "# Frontend\n\nConteúdo.");

        var renderers = new IContentRenderer[] { new MarkdownRenderer(), new FailingRenderer() };
        var artifacts = await CreatePublisher(renderers).PublishAllAsync();

        // Ambos os documentos falham no formato HTML (renderer instável), mas a publicação
        // como um todo não é abortada — o loop de documentos continua.
        Assert.True(File.Exists(Path.Combine(_distRoot, "architecture", "Architecture.md")));
        Assert.True(File.Exists(Path.Combine(_distRoot, "frontend", "Frontend.md")));
        Assert.DoesNotContain(artifacts, a => a.Format == PublicationFormat.Html);
    }

    [Fact]
    public async Task PublishAllAsync_Should_Not_Use_Any_Audience_Named_Category()
    {
        WriteDoc("backend/procurement/Procurement.md", "# Procurement\n\nConteúdo.");
        WriteDoc("agents/Agents.md", "# Agentes\n\nConteúdo.");

        var artifacts = await CreatePublisher().PublishAllAsync();

        Assert.DoesNotContain(artifacts, a =>
            a.RelativePath.StartsWith("executive/", StringComparison.Ordinal)
            || a.RelativePath.StartsWith("client/", StringComparison.Ordinal)
            || a.RelativePath.StartsWith("engineering/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PublishAllAsync_Should_Only_Write_Inside_Configured_Dist_Directory()
    {
        WriteDoc("architecture/Architecture.md", "# Arquitetura\n\nConteúdo.");

        var artifacts = await CreatePublisher().PublishAllAsync();

        Assert.All(artifacts, a => Assert.StartsWith(Path.GetFullPath(_distRoot), Path.GetFullPath(a.FilePath), StringComparison.Ordinal));
    }
}
