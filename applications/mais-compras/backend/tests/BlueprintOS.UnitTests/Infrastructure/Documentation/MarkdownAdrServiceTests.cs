using BlueprintOS.Core.Documentation.Models;
using BlueprintOS.Infrastructure.Documentation;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation;

public class MarkdownAdrServiceTests : IDisposable
{
    private readonly string _directory;

    public MarkdownAdrServiceTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    }

    private MarkdownAdrService CreateService() =>
        new(Options.Create(new DocumentationOptions { AdrDirectoryPath = _directory }));

    [Fact]
    public async Task CreateAsync_Should_Persist_File_With_Sequential_Id()
    {
        var service = CreateService();

        var first = await service.CreateAsync("Primeira decisão", "contexto", "decisão", "consequências");
        var second = await service.CreateAsync("Segunda decisão", "contexto", "decisão", "consequências");

        Assert.Equal("ADR-0001", first.Id);
        Assert.Equal("ADR-0002", second.Id);
        Assert.True(File.Exists(Path.Combine(_directory, "ADR-0001.md")));
    }

    [Fact]
    public async Task CreateAsync_Should_Default_Status_To_Proposed()
    {
        var service = CreateService();

        var adr = await service.CreateAsync("Decisão", "contexto", "decisão", "consequências");

        Assert.Equal(AdrStatus.Proposed, adr.Status);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Adr_Matching_Created_Content()
    {
        var service = CreateService();
        var created = await service.CreateAsync("Decisão sobre cache", "precisamos de cache", "usar Redis", "melhora performance", AdrStatus.Accepted);

        var found = await service.GetByIdAsync(created.Id);

        Assert.NotNull(found);
        Assert.Equal("Decisão sobre cache", found!.Title);
        Assert.Equal(AdrStatus.Accepted, found.Status);
        Assert.Equal("precisamos de cache", found.Context);
        Assert.Equal("usar Redis", found.Decision);
        Assert.Equal("melhora performance", found.Consequences);
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_When_Not_Found()
    {
        var service = CreateService();

        var found = await service.GetByIdAsync("ADR-9999");

        Assert.Null(found);
    }

    [Fact]
    public async Task ListAllAsync_Should_Return_Empty_When_Directory_Does_Not_Exist()
    {
        var service = CreateService();

        var all = await service.ListAllAsync();

        Assert.Empty(all);
    }

    [Fact]
    public async Task ListAllAsync_Should_Return_All_Created_Adrs_Ordered_By_Id()
    {
        var service = CreateService();
        await service.CreateAsync("Primeira", "c", "d", "cons");
        await service.CreateAsync("Segunda", "c", "d", "cons");

        var all = await service.ListAllAsync();

        Assert.Equal(2, all.Count);
        Assert.Equal("ADR-0001", all[0].Id);
        Assert.Equal("ADR-0002", all[1].Id);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
