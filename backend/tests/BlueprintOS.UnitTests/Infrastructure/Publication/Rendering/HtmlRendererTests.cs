using System.Text;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Infrastructure.Publication.Rendering;

namespace BlueprintOS.UnitTests.Infrastructure.Publication.Rendering;

public class HtmlRendererTests
{
    private static PublicationDocument CreateDocument() => new(
        Slug: "ClientGuide",
        Title: "Guia do Cliente",
        Subtitle: "Documento de teste",
        Category: "client",
        Sections: new List<PublicationSection>
        {
            new("Sobre o BlueprintOS", new[]
            {
                ContentBlock.Paragraph("Texto **em negrito** e uma lista:"),
                ContentBlock.BulletList(new[] { "item a", "item b" }),
            }),
        },
        ProjectVersion: "1.0.0",
        GeneratedAt: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Format_Should_Be_Html()
    {
        var renderer = new HtmlRenderer();

        Assert.Equal(PublicationFormat.Html, renderer.Format);
    }

    [Fact]
    public async Task RenderAsync_Should_Produce_Valid_Html_With_Cover_Toc_And_Rendered_Markdown()
    {
        var renderer = new HtmlRenderer();
        var document = CreateDocument();

        var bytes = await renderer.RenderAsync(document);
        var html = Encoding.UTF8.GetString(bytes);

        Assert.StartsWith("<!doctype html>", html, StringComparison.Ordinal);
        Assert.Contains("<h1>Guia do Cliente</h1>", html, StringComparison.Ordinal);
        Assert.Contains("class=\"toc card\"", html, StringComparison.Ordinal);
        Assert.Contains($"id=\"{MarkdownRenderer.Slugify("Sobre o BlueprintOS")}\"", html, StringComparison.Ordinal);
        Assert.Contains("<strong>em negrito</strong>", html, StringComparison.Ordinal);
        Assert.Contains("<li>item a</li>", html, StringComparison.Ordinal);
    }
}
