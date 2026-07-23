using BlueprintOS.Infrastructure.Documentation;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation;

public class DocumentVersioningServiceTests
{
    [Fact]
    public async Task RegisterVersionAsync_Should_Start_Numbering_At_One()
    {
        var service = new DocumentVersioningService();

        var version = await service.RegisterVersionAsync("doc-1", "conteúdo v1", "criação inicial");

        Assert.Equal(1, version.VersionNumber);
        Assert.Equal("doc-1", version.DocumentId);
    }

    [Fact]
    public async Task RegisterVersionAsync_Should_Increment_Version_Number_Per_Document()
    {
        var service = new DocumentVersioningService();

        await service.RegisterVersionAsync("doc-1", "v1", "primeira");
        var second = await service.RegisterVersionAsync("doc-1", "v2", "segunda");

        Assert.Equal(2, second.VersionNumber);
    }

    [Fact]
    public async Task GetHistoryAsync_Should_Return_Versions_Ordered_By_Number()
    {
        var service = new DocumentVersioningService();
        await service.RegisterVersionAsync("doc-1", "v1", "primeira");
        await service.RegisterVersionAsync("doc-1", "v2", "segunda");

        var history = await service.GetHistoryAsync("doc-1");

        Assert.Equal(2, history.Count);
        Assert.Equal(1, history[0].VersionNumber);
        Assert.Equal(2, history[1].VersionNumber);
    }

    [Fact]
    public async Task GetHistoryAsync_Should_Return_Empty_When_Document_Unknown()
    {
        var service = new DocumentVersioningService();

        var history = await service.GetHistoryAsync("inexistente");

        Assert.Empty(history);
    }

    [Fact]
    public async Task GetVersionAsync_Should_Return_Specific_Version()
    {
        var service = new DocumentVersioningService();
        await service.RegisterVersionAsync("doc-1", "v1", "primeira");
        await service.RegisterVersionAsync("doc-1", "v2", "segunda");

        var version = await service.GetVersionAsync("doc-1", 2);

        Assert.NotNull(version);
        Assert.Equal("v2", version!.Content);
    }

    [Fact]
    public async Task GetVersionAsync_Should_Return_Null_When_Version_Does_Not_Exist()
    {
        var service = new DocumentVersioningService();
        await service.RegisterVersionAsync("doc-1", "v1", "primeira");

        var version = await service.GetVersionAsync("doc-1", 99);

        Assert.Null(version);
    }
}
