using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Core.Publication.Models.Assets;

namespace BlueprintOS.UnitTests.Core.Publication;

public class PublicationAssetsTests
{
    [Fact]
    public void FindEmbeddableImage_Should_Find_Image_Asset_By_Id()
    {
        var image = new ImageAsset("img-1", "Título", new byte[] { 1, 2, 3 }, "image/png", "Alt");
        var assets = PublicationAssets.Empty with { Images = new[] { image } };

        var found = assets.FindEmbeddableImage("img-1");

        Assert.NotNull(found);
        Assert.Equal("image/png", found!.Value.MediaType);
        Assert.Equal("Alt", found.Value.AltText);
    }

    [Fact]
    public void FindEmbeddableImage_Should_Find_Rendered_Mermaid_Diagram_By_Id()
    {
        var mermaid = new MermaidAsset("mmd-1", "Diagrama", "graph TD; A-->B;", new byte[] { 9, 9 }, "image/svg+xml");
        var assets = PublicationAssets.Empty with { Mermaid = new[] { mermaid } };

        var found = assets.FindEmbeddableImage("mmd-1");

        Assert.NotNull(found);
        Assert.Equal("image/svg+xml", found!.Value.MediaType);
    }

    [Fact]
    public void FindEmbeddableImage_Should_Not_Find_Mermaid_Diagram_Without_Rendered_Bytes()
    {
        var mermaid = new MermaidAsset("mmd-2", "Diagrama sem render", "graph TD; A-->B;");
        var assets = PublicationAssets.Empty with { Mermaid = new[] { mermaid } };

        var found = assets.FindEmbeddableImage("mmd-2");

        Assert.Null(found);
    }

    [Fact]
    public void FindEmbeddableImage_Should_Find_QrCode_By_Id()
    {
        var qrCode = new QrCodeAsset("qr-1", "https://example.com", "Rótulo", new byte[] { 5, 5 });
        var assets = PublicationAssets.Empty with { QrCodes = new[] { qrCode } };

        var found = assets.FindEmbeddableImage("qr-1");

        Assert.NotNull(found);
        Assert.Equal("image/png", found!.Value.MediaType);
        Assert.Equal("Rótulo", found.Value.AltText);
    }

    [Fact]
    public void FindEmbeddableImage_Should_Return_Null_When_Id_Not_Found()
    {
        Assert.Null(PublicationAssets.Empty.FindEmbeddableImage("missing"));
    }
}
