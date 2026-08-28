using BlueprintOS.Infrastructure.Documentation;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation;

public class ChangeLogServiceTests
{
    [Fact]
    public async Task RecordChangeAsync_Should_Create_Entry_With_DocumentId_And_Summary()
    {
        var service = new ChangeLogService();

        var entry = await service.RecordChangeAsync("doc-1", "Atualização de conteúdo", "julio");

        Assert.Equal("doc-1", entry.DocumentId);
        Assert.Equal("Atualização de conteúdo", entry.Summary);
        Assert.Equal("julio", entry.Author);
    }

    [Fact]
    public async Task GetChangesAsync_Should_Return_Only_Changes_For_Document()
    {
        var service = new ChangeLogService();
        await service.RecordChangeAsync("doc-1", "mudança 1");
        await service.RecordChangeAsync("doc-2", "mudança 2");

        var changes = await service.GetChangesAsync("doc-1");

        Assert.Single(changes);
        Assert.Equal("mudança 1", changes[0].Summary);
    }

    [Fact]
    public async Task GetAllAsync_Should_Return_All_Recorded_Changes()
    {
        var service = new ChangeLogService();
        await service.RecordChangeAsync("doc-1", "mudança 1");
        await service.RecordChangeAsync("doc-2", "mudança 2");

        var all = await service.GetAllAsync();

        Assert.Equal(2, all.Count);
    }
}
