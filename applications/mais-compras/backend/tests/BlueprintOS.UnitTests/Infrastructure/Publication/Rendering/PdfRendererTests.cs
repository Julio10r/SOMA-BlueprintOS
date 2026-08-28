using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Infrastructure.Publication.Rendering;

namespace BlueprintOS.UnitTests.Infrastructure.Publication.Rendering;

public class PdfRendererTests
{
    private static PublicationDocument CreateDocument() => PublicationDocumentTestBuilder.Create(
        slug: "EngineeringGuide",
        title: "Guia de Engenharia",
        subtitle: "Documento de teste",
        category: "engineering",
        sections: new List<PublicationSection>
        {
            new("Arquitetura", new[]
            {
                ContentBlock.Paragraph("Texto de exemplo."),
                ContentBlock.BulletList(new[] { "item a", "item b" }),
                ContentBlock.Table(new[] { "Coluna", "Valor" }, new IReadOnlyList<string>[] { new[] { "A", "1" } }),
            }),
        });

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
