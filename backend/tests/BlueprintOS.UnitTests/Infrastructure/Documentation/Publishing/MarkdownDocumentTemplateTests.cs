using BlueprintOS.Infrastructure.Documentation.Publishing;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Publishing;

public class MarkdownDocumentTemplateTests
{
    [Fact]
    public void Render_Should_Include_Title_As_Heading()
    {
        var result = MarkdownDocumentTemplate.Render("Meu Título", "1.0.0", DateTimeOffset.UtcNow, "corpo");

        Assert.StartsWith("# Meu Título", result);
    }

    [Fact]
    public void Render_Should_Include_Version_And_Body()
    {
        var result = MarkdownDocumentTemplate.Render("T", "2.3.4", DateTimeOffset.UtcNow, "conteúdo do corpo");

        Assert.Contains("2.3.4", result);
        Assert.Contains("conteúdo do corpo", result);
    }
}
