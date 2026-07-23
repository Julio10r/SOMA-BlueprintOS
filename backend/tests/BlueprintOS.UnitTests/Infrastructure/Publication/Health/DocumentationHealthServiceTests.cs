using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Core.Publication.Models.Health;
using BlueprintOS.Infrastructure.Publication.Health;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Infrastructure.Publication.Health;

public class DocumentationHealthServiceTests : IDisposable
{
    private readonly string _tempDirectory;

    public DocumentationHealthServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "BlueprintOSHealthTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose() => Directory.Delete(_tempDirectory, recursive: true);

    private DocumentationHealthService CreateService(int minWordCount = 5, double similarityThreshold = 0.85) =>
        new(Options.Create(new DocumentationHealthOptions
        {
            OutputPath = Path.Combine(_tempDirectory, "DocumentationHealth.md"),
            MinWordCount = minWordCount,
            SimilarityThreshold = similarityThreshold,
        }));

    private PublishedArtifact WriteMarkdown(string slug, string content)
    {
        var filePath = Path.Combine(_tempDirectory, $"{slug}.md");
        File.WriteAllText(filePath, content);
        return new PublishedArtifact(PublicationFormat.Markdown, $"{slug}.md", filePath);
    }

    [Fact]
    public async Task AnalyzeAsync_Should_Classify_WellFormed_Document_As_Healthy()
    {
        var artifact = WriteMarkdown("Healthy", """
            # Título Principal

            ## Sumário

            - [Seção A](#seção-a)

            ---

            <a id="seção-a"></a>
            ## Seção A

            Este documento possui conteúdo suficiente para não ser sinalizado como curto pelo verificador de saúde.
            """);

        var report = await CreateService().AnalyzeAsync(new[] { artifact });

        var result = Assert.Single(report.Documents);
        Assert.Equal(DocumentHealthStatus.Healthy, result.Status);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
        Assert.Equal(1, report.HealthyCount);
    }

    [Fact]
    public async Task AnalyzeAsync_Should_Flag_Empty_Document_As_Error()
    {
        var artifact = WriteMarkdown("Empty", string.Empty);

        var report = await CreateService().AnalyzeAsync(new[] { artifact });

        var result = Assert.Single(report.Documents);
        Assert.Equal(DocumentHealthStatus.Error, result.Status);
        Assert.Contains(result.Errors, e => e.Contains("vazio"));
    }

    [Fact]
    public async Task AnalyzeAsync_Should_Flag_Missing_Title_And_Missing_Sections()
    {
        var artifact = WriteMarkdown("NoStructure", "Apenas um parágrafo solto, sem nenhum heading nesse documento inteiro.");

        var report = await CreateService().AnalyzeAsync(new[] { artifact });

        var result = Assert.Single(report.Documents);
        Assert.Equal(DocumentHealthStatus.Error, result.Status);
        Assert.Contains(result.Errors, e => e.Contains("título principal"));
        Assert.Contains(result.Errors, e => e.Contains("nenhuma seção"));
    }

    [Fact]
    public async Task AnalyzeAsync_Should_Flag_Broken_Internal_Anchor_Link()
    {
        var artifact = WriteMarkdown("BrokenAnchor", """
            # Título

            ## Seção

            Veja [outra seção](#nao-existe) para mais detalhes sobre este assunto específico.
            """);

        var report = await CreateService().AnalyzeAsync(new[] { artifact });

        var result = Assert.Single(report.Documents);
        Assert.Equal(DocumentHealthStatus.Error, result.Status);
        Assert.Contains(result.Errors, e => e.Contains("âncora inexistente"));
    }

    [Fact]
    public async Task AnalyzeAsync_Should_Flag_Missing_Referenced_File_And_Image()
    {
        var artifact = WriteMarkdown("BrokenRefs", """
            # Título

            ## Seção

            Consulte o [anexo](./attachments/nao-existe.pdf) e a imagem abaixo, ambos relevantes para este relatório.

            ![Diagrama](./assets/nao-existe.png)
            """);

        var report = await CreateService().AnalyzeAsync(new[] { artifact });

        var result = Assert.Single(report.Documents);
        Assert.Equal(DocumentHealthStatus.Error, result.Status);
        Assert.Contains(result.Errors, e => e.Contains("Link quebrado"));
        Assert.Contains(result.Errors, e => e.Contains("Imagem inexistente"));
    }

    [Fact]
    public async Task AnalyzeAsync_Should_Warn_On_Content_Below_Minimum_And_Duplicate_Heading()
    {
        var artifact = WriteMarkdown("ShortDuplicate", """
            # Título

            ## Seção

            Curto.

            ## Seção

            Fim.
            """);

        var report = await CreateService(minWordCount: 50).AnalyzeAsync(new[] { artifact });

        var result = Assert.Single(report.Documents);
        Assert.Equal(DocumentHealthStatus.Warning, result.Status);
        Assert.Contains(result.Warnings, w => w.Contains("abaixo do limite mínimo"));
        Assert.Contains(result.Warnings, w => w.Contains("Heading duplicado"));
    }

    [Fact]
    public async Task AnalyzeAsync_Should_Flag_Identical_Documents_As_Error_On_Both()
    {
        const string content = """
            # Título

            ## Seção

            Conteúdo idêntico repetido em dois documentos publicados diferentes para este teste.
            """;

        var first = WriteMarkdown("First", content);
        var second = WriteMarkdown("Second", content);

        var report = await CreateService().AnalyzeAsync(new[] { first, second });

        Assert.All(report.Documents, d => Assert.Equal(DocumentHealthStatus.Error, d.Status));
        Assert.All(report.Documents, d => Assert.Contains(d.Errors, e => e.Contains("idêntico")));
    }

    [Fact]
    public async Task AnalyzeAsync_Should_Ignore_NonMarkdown_Artifacts()
    {
        var htmlPath = Path.Combine(_tempDirectory, "Report.html");
        File.WriteAllText(htmlPath, "<html></html>");
        var artifact = new PublishedArtifact(PublicationFormat.Html, "Report.html", htmlPath);

        var report = await CreateService().AnalyzeAsync(new[] { artifact });

        Assert.Empty(report.Documents);
        Assert.Equal(0, report.TotalDocuments);
    }

    [Fact]
    public async Task WriteReportAsync_Should_Write_Markdown_File_With_Summary_Sections()
    {
        var artifact = WriteMarkdown("Healthy", """
            # Título Principal

            ## Seção A

            Este documento possui conteúdo suficiente para não ser sinalizado como curto pelo verificador de saúde.
            """);

        var service = CreateService();
        var report = await service.AnalyzeAsync(new[] { artifact });
        var path = await service.WriteReportAsync(report);

        Assert.True(File.Exists(path));
        var written = await File.ReadAllTextAsync(path);
        Assert.Contains("# Documentation Health", written);
        Assert.Contains("## Resumo", written);
        Assert.Contains("## Cobertura", written);
        Assert.Contains("## Recomendações", written);
    }
}
