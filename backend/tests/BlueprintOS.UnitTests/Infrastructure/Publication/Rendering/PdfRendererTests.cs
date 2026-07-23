using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Infrastructure.Publication.Rendering;

namespace BlueprintOS.UnitTests.Infrastructure.Publication.Rendering;

public class PdfRendererTests
{
    private static PublicationDocument CreateDocument() => new(
        Slug: "EngineeringGuide",
        Title: "Guia de Engenharia",
        Subtitle: "Documento de teste",
        Category: "engineering",
        Sections: new List<PublicationSection>
        {
            new("Arquitetura", "Texto de exemplo.\n\n- item a\n- item b\n\n| Coluna | Valor |\n|---|---|\n| A | 1 |"),
        },
        ProjectVersion: "1.0.0",
        GeneratedAt: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Format_Should_Be_Pdf()
    {
        var renderer = new PdfRenderer();

        Assert.Equal(PublicationFormat.Pdf, renderer.Format);
    }

    [Fact]
    public async Task RenderAsync_Should_Produce_Non_Empty_Pdf_Bytes()
    {
        var renderer = new PdfRenderer();
        var document = CreateDocument();

        var bytes = await renderer.RenderAsync(document);

        Assert.NotEmpty(bytes);
        var header = System.Text.Encoding.ASCII.GetString(bytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }
}
