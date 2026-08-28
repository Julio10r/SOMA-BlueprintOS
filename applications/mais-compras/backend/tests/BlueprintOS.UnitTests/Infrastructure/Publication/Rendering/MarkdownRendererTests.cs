using System.Text;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Core.Publication.Models.Assets;
using BlueprintOS.Infrastructure.Publication.Rendering;

namespace BlueprintOS.UnitTests.Infrastructure.Publication.Rendering;

public class MarkdownRendererTests
{
    private static PublicationDocument CreateDocument() => PublicationDocumentTestBuilder.Create(
        slug: "ExecutiveReport",
        title: "Relatório Executivo",
        subtitle: "Subtítulo de teste",
        category: "executive",
        sections: new List<PublicationSection>
        {
            new("Resumo Executivo", new[] { ContentBlock.Paragraph("Corpo do resumo.") }),
            new("Roadmap", new[] { ContentBlock.Paragraph("Corpo do roadmap.") }),
        });

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

    [Fact]
    public async Task RenderAsync_Should_Include_Badges_Appendix_And_Attachments_When_Present()
    {
        var attachment = new AttachmentAsset("att-1", "planilha.xlsx", new byte[] { 1 }, "application/octet-stream", "Planilha de apoio");
        var document = PublicationDocumentTestBuilder.Create(
            slug: "ExecutiveReport",
            title: "Relatório Executivo",
            subtitle: "Subtítulo de teste",
            category: "executive",
            sections: new List<PublicationSection> { new("Resumo Executivo", new[] { ContentBlock.Paragraph("Corpo.") }) },
            assets: PublicationAssets.Empty with
            {
                Badges = new[] { new BadgeAsset("badge-build", "Build", "passing", BadgeStatus.Success) },
                Attachments = new[] { attachment },
            },
            appendix: new[] { new PublicationSection("Glossário", new[] { ContentBlock.Paragraph("Termo: definição.") }) });

        var renderer = new MarkdownRenderer();
        var bytes = await renderer.RenderAsync(document);
        var content = Encoding.UTF8.GetString(bytes);

        Assert.Contains("`Build: passing`", content, StringComparison.Ordinal);
        Assert.Contains("## Apêndice", content, StringComparison.Ordinal);
        Assert.Contains("### Glossário", content, StringComparison.Ordinal);
        Assert.Contains("## Anexos", content, StringComparison.Ordinal);
        Assert.Contains("[planilha.xlsx](./attachments/planilha.xlsx) — Planilha de apoio", content, StringComparison.Ordinal);
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
