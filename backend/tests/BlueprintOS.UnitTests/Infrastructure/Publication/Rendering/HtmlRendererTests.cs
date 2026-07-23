using System.Text;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Core.Publication.Models.Assets;
using BlueprintOS.Infrastructure.Publication.Rendering;

namespace BlueprintOS.UnitTests.Infrastructure.Publication.Rendering;

public class HtmlRendererTests
{
    private static PublicationDocument CreateDocument() => PublicationDocumentTestBuilder.Create(
        slug: "ClientGuide",
        title: "Guia do Cliente",
        subtitle: "Documento de teste",
        category: "client",
        sections: new List<PublicationSection>
        {
            new("Sobre o BlueprintOS", new[]
            {
                ContentBlock.Paragraph("Texto **em negrito** e uma lista:"),
                ContentBlock.BulletList(new[] { "item a", "item b" }),
            }),
        });

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

    [Fact]
    public async Task RenderAsync_Should_Embed_Image_Block_As_Base64_Figure_With_Caption()
    {
        var renderer = new HtmlRenderer();
        var image = new ImageAsset("img-1", "Diagrama", new byte[] { 1, 2, 3 }, "image/png", "Texto alternativo");
        var document = PublicationDocumentTestBuilder.Create(
            slug: "ClientGuide",
            title: "Guia do Cliente",
            subtitle: "Documento de teste",
            category: "client",
            sections: new List<PublicationSection>
            {
                new("Seção com imagem", new[] { ContentBlock.Image("img-1", "Legenda da figura") }),
            },
            assets: PublicationAssets.Empty with { Images = new[] { image } });

        var bytes = await renderer.RenderAsync(document);
        var html = Encoding.UTF8.GetString(bytes);

        Assert.Contains($"data:image/png;base64,{Convert.ToBase64String(new byte[] { 1, 2, 3 })}", html, StringComparison.Ordinal);
        Assert.Contains("alt=\"Texto alternativo\"", html, StringComparison.Ordinal);
        Assert.Contains("<figcaption>Legenda da figura</figcaption>", html, StringComparison.Ordinal);
    }
}
