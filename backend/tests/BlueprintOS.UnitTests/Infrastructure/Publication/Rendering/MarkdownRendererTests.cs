using System.Text;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Infrastructure.Publication.Rendering;

namespace BlueprintOS.UnitTests.Infrastructure.Publication.Rendering;

public class MarkdownRendererTests
{
    private static PublicationDocument CreateDocument() => new(
        Slug: "ExecutiveReport",
        Title: "Relatório Executivo",
        Subtitle: "Subtítulo de teste",
        Category: "executive",
        Sections: new List<PublicationSection>
        {
            new("Resumo Executivo", "Corpo do resumo."),
            new("Roadmap", "Corpo do roadmap."),
        },
        ProjectVersion: "1.0.0",
        GeneratedAt: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Format_Should_Be_Markdown()
    {
        var renderer = new MarkdownRenderer();

        Assert.Equal(PublicationFormat.Markdown, renderer.Format);
    }

    [Fact]
    public async Task RenderAsync_Should_Include_Title_Toc_And_All_Section_Bodies()
    {
        var renderer = new MarkdownRenderer();
        var document = CreateDocument();

        var bytes = await renderer.RenderAsync(document);
        var content = Encoding.UTF8.GetString(bytes);

        Assert.Contains("# Relatório Executivo", content, StringComparison.Ordinal);
        Assert.Contains("## Sumário", content, StringComparison.Ordinal);
        Assert.Contains("[Resumo Executivo](#resumo-executivo)", content, StringComparison.Ordinal);
        Assert.Contains("Corpo do resumo.", content, StringComparison.Ordinal);
        Assert.Contains("Corpo do roadmap.", content, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Resumo Executivo", "resumo-executivo")]
    [InlineData("Build, Testes e Qualidade", "build-testes-e-qualidade")]
    [InlineData("  Espaços   Extras  ", "espaços-extras")]
    public void Slugify_Should_Normalize_Headings(string heading, string expected)
    {
        Assert.Equal(expected, MarkdownRenderer.Slugify(heading));
    }
}
