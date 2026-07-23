using BlueprintOS.Core.Documentation.Models;
using BlueprintOS.Infrastructure.Documentation;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation;

public class InMemoryDocumentationRepositoryTests
{
    private static DocumentationEntry CreateEntry(string id = "doc-1") => new(
        id,
        "Título",
        DocumentationType.Technical,
        "Conteúdo",
        "path/doc.md",
        1,
        DateTimeOffset.UtcNow,
        DateTimeOffset.UtcNow);

    [Fact]
    public async Task AddAsync_Should_Store_Entry_Retrievable_By_Id()
    {
        var repository = new InMemoryDocumentationRepository();
        var entry = CreateEntry();

        await repository.AddAsync(entry);
        var found = await repository.GetByIdAsync(entry.Id);

        Assert.NotNull(found);
        Assert.Equal(entry.Title, found!.Title);
    }

    [Fact]
    public async Task AddAsync_Should_Throw_When_Id_Already_Exists()
    {
        var repository = new InMemoryDocumentationRepository();
        await repository.AddAsync(CreateEntry());

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.AddAsync(CreateEntry()));
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_When_Not_Found()
    {
        var repository = new InMemoryDocumentationRepository();

        var result = await repository.GetByIdAsync("inexistente");

        Assert.Null(result);
    }

    [Fact]
    public async Task ListAllAsync_Should_Return_All_Stored_Entries()
    {
        var repository = new InMemoryDocumentationRepository();
        await repository.AddAsync(CreateEntry("doc-1"));
        await repository.AddAsync(CreateEntry("doc-2"));

        var all = await repository.ListAllAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task UpdateAsync_Should_Replace_Existing_Entry()
    {
        var repository = new InMemoryDocumentationRepository();
        var entry = CreateEntry();
        await repository.AddAsync(entry);

        var updated = entry with { Title = "Novo Título", Version = 2 };
        await repository.UpdateAsync(updated);
        var found = await repository.GetByIdAsync(entry.Id);

        Assert.Equal("Novo Título", found!.Title);
        Assert.Equal(2, found.Version);
    }

    [Fact]
    public async Task UpdateAsync_Should_Throw_When_Entry_Does_Not_Exist()
    {
        var repository = new InMemoryDocumentationRepository();

        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateAsync(CreateEntry()));
    }

    [Fact]
    public async Task DeleteAsync_Should_Remove_Entry_And_Return_True()
    {
        var repository = new InMemoryDocumentationRepository();
        var entry = CreateEntry();
        await repository.AddAsync(entry);

        var deleted = await repository.DeleteAsync(entry.Id);
        var found = await repository.GetByIdAsync(entry.Id);

        Assert.True(deleted);
        Assert.Null(found);
    }

    [Fact]
    public async Task DeleteAsync_Should_Return_False_When_Entry_Does_Not_Exist()
    {
        var repository = new InMemoryDocumentationRepository();

        var deleted = await repository.DeleteAsync("inexistente");

        Assert.False(deleted);
    }
}
