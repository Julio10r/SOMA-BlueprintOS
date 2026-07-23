using BlueprintOS.Core.Publication.Contracts;
using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Infrastructure.Publication.Assets;

namespace BlueprintOS.UnitTests.Infrastructure.Publication.Assets;

public class DocumentationAssetsManagerTests
{
    private sealed class FakeThemeProvider : IDocumentThemeProvider
    {
        public static readonly DocumentPalette Palette = new(
            "#111111", "#222222", "#333333", "#444444", "#555555", "#666666", "#777777", "#888888", "#999999", "#aaaaaa");

        public static readonly DocumentTypography Typography = new("Display Font", "Body Font", "Mono Font");

        public DocumentPalette GetPalette() => Palette;

        public DocumentTypography GetTypography() => Typography;

        public string GetStylesheet() => "/* css de teste */";
    }

    private static DocumentationAssetsManager CreateManager() => new(new FakeThemeProvider());

    private static QualityMetrics CreateMetrics(bool buildSucceeded = true, int testCount = 42) => new(
        BuildSucceeded: buildSucceeded,
        WarningCount: 0,
        ErrorCount: 0,
        TestCount: testCount,
        Summary: "ok");

    [Theory]
    [InlineData(PublicationDocumentClass.Executive)]
    [InlineData(PublicationDocumentClass.Client)]
    [InlineData(PublicationDocumentClass.Engineering)]
    public void GetTheme_Should_Return_Theme_Matching_Requested_DocumentClass(PublicationDocumentClass documentClass)
    {
        var manager = CreateManager();

        var theme = manager.GetTheme(documentClass);

        Assert.Equal(documentClass, theme.DocumentClass);
    }

    [Theory]
    [InlineData(PublicationDocumentClass.Executive)]
    [InlineData(PublicationDocumentClass.Client)]
    [InlineData(PublicationDocumentClass.Engineering)]
    public void GetTheme_Should_Use_The_Same_Unified_Palette_And_Typography_For_Every_DocumentClass(PublicationDocumentClass documentClass)
    {
        var manager = CreateManager();

        var theme = manager.GetTheme(documentClass);

        Assert.Equal(FakeThemeProvider.Palette, theme.Palette);
        Assert.Equal(FakeThemeProvider.Typography, theme.Typography);
    }

    [Fact]
    public void BuildStandardAssets_Should_Include_Build_And_Test_Badges_And_Repository_QrCode()
    {
        var manager = CreateManager();

        var assets = manager.BuildStandardAssets(CreateMetrics(buildSucceeded: true, testCount: 184));

        Assert.Equal(2, assets.Badges.Count);
        Assert.Contains(assets.Badges, b => b.Id == "badge-build" && b.Value == "passing");
        Assert.Contains(assets.Badges, b => b.Id == "badge-tests" && b.Value == "184");
        Assert.Single(assets.QrCodes);
        Assert.Equal("qr-repository", assets.QrCodes[0].Id);
        Assert.NotEmpty(assets.QrCodes[0].ImageBytes);
    }

    [Fact]
    public void BuildStandardAssets_Should_Reflect_Failed_Build_In_Badge_Value()
    {
        var manager = CreateManager();

        var assets = manager.BuildStandardAssets(CreateMetrics(buildSucceeded: false));

        Assert.Contains(assets.Badges, b => b.Id == "badge-build" && b.Value == "failing");
    }

    [Fact]
    public void BuildStandardAppendix_Should_Include_Revision_History_And_Repository_Sections()
    {
        var manager = CreateManager();
        var metadata = PublicationMetadata.Create(
            title: "Título",
            subtitle: "Subtítulo",
            audience: "Testadores",
            version: "1.0.0",
            generatedAt: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var appendix = manager.BuildStandardAppendix(metadata);

        Assert.Equal(2, appendix.Count);
        Assert.Equal("Histórico de Versões", appendix[0].Heading);
        Assert.Equal("Repositório", appendix[1].Heading);
    }

    [Fact]
    public async Task RenderDiagramAsync_Should_Never_Include_Mermaid_Code_In_The_Section_Body()
    {
        var manager = CreateManager();

        var diagram = await manager.RenderDiagramAsync(
            "Visão de Arquitetura",
            "diagram-architecture",
            _ => Task.FromResult("## Diagrama de Dependências\n\n```mermaid\ngraph TD\n    A[Api]\n    B[Core]\n    A --> B\n```"));

        Assert.Equal("Visão de Arquitetura", diagram.Section.Heading);
        Assert.All(diagram.Section.Blocks, block => Assert.NotEqual(ContentBlockKind.CodeBlock, block.Kind));
        Assert.Contains(diagram.Section.Blocks, b => b.Kind == ContentBlockKind.Image && b.AssetId == "diagram-architecture");
    }

    [Fact]
    public async Task RenderDiagramAsync_Should_Produce_A_Renderable_Svg_Image()
    {
        var manager = CreateManager();

        var diagram = await manager.RenderDiagramAsync(
            "Visão de Arquitetura",
            "diagram-architecture",
            _ => Task.FromResult("```mermaid\ngraph TD\n    A[Api]\n    B[Core]\n    A --> B\n```"));

        Assert.Equal("diagram-architecture", diagram.Asset.Id);
        Assert.Equal("image/svg+xml", diagram.Asset.RenderedMediaType);
        Assert.NotNull(diagram.Asset.RenderedImageBytes);
        var svg = System.Text.Encoding.UTF8.GetString(diagram.Asset.RenderedImageBytes!);
        Assert.StartsWith("<svg", svg.TrimStart());
    }

    [Fact]
    public async Task RenderDiagramAsync_Should_Generate_Fallback_Svg_Even_When_Source_Has_No_Fenced_Block()
    {
        var manager = CreateManager();

        var diagram = await manager.RenderDiagramAsync(
            "Visão de Arquitetura", "diagram-architecture", _ => Task.FromResult("Sem definição real."));

        Assert.NotNull(diagram.Asset.RenderedImageBytes);
        Assert.NotEmpty(diagram.Asset.RenderedImageBytes!);
    }
}
