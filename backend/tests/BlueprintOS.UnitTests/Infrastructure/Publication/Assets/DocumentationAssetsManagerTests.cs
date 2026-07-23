using BlueprintOS.Core.Publication.Models;
using BlueprintOS.Infrastructure.Publication.Assets;

namespace BlueprintOS.UnitTests.Infrastructure.Publication.Assets;

public class DocumentationAssetsManagerTests
{
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
        var manager = new DocumentationAssetsManager();

        var theme = manager.GetTheme(documentClass);

        Assert.Equal(documentClass, theme.DocumentClass);
    }

    [Fact]
    public void BuildStandardAssets_Should_Include_Build_And_Test_Badges_And_Repository_QrCode()
    {
        var manager = new DocumentationAssetsManager();

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
        var manager = new DocumentationAssetsManager();

        var assets = manager.BuildStandardAssets(CreateMetrics(buildSucceeded: false));

        Assert.Contains(assets.Badges, b => b.Id == "badge-build" && b.Value == "failing");
    }

    [Fact]
    public void BuildStandardAppendix_Should_Include_Revision_History_And_Repository_Sections()
    {
        var manager = new DocumentationAssetsManager();
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
    public async Task BuildDiagramMarkdownAsync_Should_Strip_First_Heading_From_Source()
    {
        var manager = new DocumentationAssetsManager();

        var result = await manager.BuildDiagramMarkdownAsync(
            _ => Task.FromResult("## Diagrama de Dependências\n\n```mermaid\ngraph TD\n```"));

        Assert.DoesNotContain("## Diagrama de Dependências", result);
        Assert.Contains("```mermaid", result);
    }

    [Fact]
    public async Task BuildDiagramMarkdownAsync_Should_Return_Source_Unchanged_When_It_Has_No_Heading()
    {
        var manager = new DocumentationAssetsManager();

        var result = await manager.BuildDiagramMarkdownAsync(_ => Task.FromResult("```mermaid\ngraph TD\n```"));

        Assert.Equal("```mermaid\ngraph TD\n```", result);
    }
}
