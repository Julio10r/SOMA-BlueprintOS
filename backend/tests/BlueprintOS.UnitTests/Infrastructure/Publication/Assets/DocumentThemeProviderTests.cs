using BlueprintOS.Infrastructure.Documentation;
using BlueprintOS.Infrastructure.Publication.Assets;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Infrastructure.Publication.Assets;

public class DocumentThemeProviderTests : IDisposable
{
    private readonly string _designSystemRoot = Path.Combine(Path.GetTempPath(), $"design-system-tests-{Guid.NewGuid():N}");

    private const string RealisticColorsAndTypeCss = """
        :root {
            --text-primary: #1A1916;
            --accent: #1A1916;
            --text-secondary: #6B6860;
            --border: #E2E0DB;
            --surface: #FFFFFF;
            --bg: #F7F6F3;
            --aprovado: #2D6A4F;
            --avaliacao: #E09B3D;
            --rejeitado: #C0392B;
            --novo: #4A90D9;
            --font-display: "Inter Tight", "Helvetica Neue", Arial, sans-serif;
            --font: "DM Sans", "Inter", system-ui, sans-serif;
            --mono: "DM Mono", "JetBrains Mono", ui-monospace, monospace;
        }
        """;

    private DocumentThemeProvider CreateProvider() =>
        new(Options.Create(new DocumentationOptions { DesignSystemRootPath = _designSystemRoot }));

    [Fact]
    public void GetPalette_Should_Load_Real_Tokens_From_Design_System_Css()
    {
        Directory.CreateDirectory(_designSystemRoot);
        File.WriteAllText(Path.Combine(_designSystemRoot, "colors_and_type.css"), RealisticColorsAndTypeCss);
        var provider = CreateProvider();

        var palette = provider.GetPalette();

        Assert.Equal("#1A1916", palette.TextPrimaryHex);
        Assert.Equal("#E2E0DB", palette.BorderHex);
        Assert.Equal("#2D6A4F", palette.SuccessHex);
        Assert.Equal("#C0392B", palette.ErrorHex);
    }

    [Fact]
    public void GetTypography_Should_Load_Real_Font_Families_From_Design_System_Css()
    {
        Directory.CreateDirectory(_designSystemRoot);
        File.WriteAllText(Path.Combine(_designSystemRoot, "colors_and_type.css"), RealisticColorsAndTypeCss);
        var provider = CreateProvider();

        var typography = provider.GetTypography();

        Assert.Equal("Inter Tight", typography.DisplayFontFamily);
        Assert.Equal("DM Sans", typography.BodyFontFamily);
        Assert.Equal("DM Mono", typography.MonoFontFamily);
    }

    [Fact]
    public void GetStylesheet_Should_Embed_The_Official_Css_Verbatim()
    {
        Directory.CreateDirectory(_designSystemRoot);
        File.WriteAllText(Path.Combine(_designSystemRoot, "colors_and_type.css"), RealisticColorsAndTypeCss);
        var provider = CreateProvider();

        var stylesheet = provider.GetStylesheet();

        Assert.Contains("--text-primary: #1A1916;", stylesheet);
    }

    [Fact]
    public void GetPalette_Should_Return_Safe_Fallback_When_Design_System_Folder_Is_Missing()
    {
        var provider = CreateProvider();

        var palette = provider.GetPalette();

        Assert.False(string.IsNullOrWhiteSpace(palette.TextPrimaryHex));
        Assert.StartsWith("#", palette.TextPrimaryHex);
    }

    [Fact]
    public void GetTypography_Should_Return_Safe_Fallback_When_Design_System_Folder_Is_Missing()
    {
        var provider = CreateProvider();

        var typography = provider.GetTypography();

        Assert.False(string.IsNullOrWhiteSpace(typography.BodyFontFamily));
    }

    [Fact]
    public void GetStylesheet_Should_Return_Working_Fallback_Css_When_Design_System_Folder_Is_Missing()
    {
        var provider = CreateProvider();

        var stylesheet = provider.GetStylesheet();

        Assert.Contains("--text-primary:", stylesheet);
        Assert.Contains(".cover", stylesheet);
    }

    [Fact]
    public void GetPalette_Should_Fallback_When_Css_File_Exists_But_Is_Empty()
    {
        Directory.CreateDirectory(_designSystemRoot);
        File.WriteAllText(Path.Combine(_designSystemRoot, "colors_and_type.css"), string.Empty);
        var provider = CreateProvider();

        var palette = provider.GetPalette();

        Assert.StartsWith("#", palette.TextPrimaryHex);
    }

    public void Dispose()
    {
        if (Directory.Exists(_designSystemRoot))
        {
            Directory.Delete(_designSystemRoot, recursive: true);
        }
    }
}
