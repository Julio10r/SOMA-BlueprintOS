using BlueprintOS.Infrastructure.Documentation;
using BlueprintOS.Infrastructure.Documentation.Publishing;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Publishing;

public class MarkdownPublisherTests : IDisposable
{
    private readonly string _directory;

    public MarkdownPublisherTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    }

    private MarkdownPublisher CreatePublisher() =>
        new(Options.Create(new DocumentationOptions { DocsRootPath = _directory, ProjectVersion = "9.9.9" }));

    [Fact]
    public async Task PublishAsync_Should_Create_File_At_Relative_Path_Under_DocsRoot()
    {
        var publisher = CreatePublisher();

        var result = await publisher.PublishAsync("executive/dashboard.md", "Dashboard", "corpo do documento");

        Assert.True(File.Exists(result.FilePath));
        Assert.Equal(Path.Combine(_directory, "executive/dashboard.md"), result.FilePath);
    }

    [Fact]
    public async Task PublishAsync_Should_Create_Missing_Directories()
    {
        var publisher = CreatePublisher();

        await publisher.PublishAsync("engineering/Mermaid/diagram.md", "Diagrama", "corpo");

        Assert.True(File.Exists(Path.Combine(_directory, "engineering/Mermaid/diagram.md")));
    }

    [Fact]
    public async Task PublishAsync_Should_Write_Header_With_Title_And_Version_And_Body()
    {
        var publisher = CreatePublisher();

        await publisher.PublishAsync("client/faq.md", "Perguntas Frequentes", "Nenhuma pergunta registrada.");

        var content = await File.ReadAllTextAsync(Path.Combine(_directory, "client/faq.md"));

        Assert.Contains("# Perguntas Frequentes", content);
        Assert.Contains("9.9.9", content);
        Assert.Contains("Nenhuma pergunta registrada.", content);
    }

    [Fact]
    public async Task PublishAsync_Should_Return_PublishedDocument_With_Title_And_RelativePath()
    {
        var publisher = CreatePublisher();

        var result = await publisher.PublishAsync("executive/kpis.md", "KPIs", "corpo");

        Assert.Equal("KPIs", result.Title);
        Assert.Equal("executive/kpis.md", result.RelativePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
