using BlueprintOS.Core.Documentation.Models;
using BlueprintOS.Infrastructure.Documentation;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation;

public class DocumentationSyncServiceTests : IDisposable
{
    private readonly string _directory;

    public DocumentationSyncServiceTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_directory);
    }

    private string CreateFile(string name, DateTime lastWriteUtc)
    {
        var path = Path.Combine(_directory, name);
        File.WriteAllText(path, "conteúdo");
        File.SetLastWriteTimeUtc(path, lastWriteUtc);
        return path;
    }

    [Fact]
    public async Task CheckAsync_Should_Mark_Stale_When_Source_Newer_Than_Doc()
    {
        var docPath = CreateFile("doc.md", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var sourcePath = CreateFile("source.cs", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        var service = new DocumentationSyncService();

        var result = await service.CheckAsync(new DocumentationSyncCheck(docPath, new[] { sourcePath }));

        Assert.True(result.IsStale);
    }

    [Fact]
    public async Task CheckAsync_Should_Not_Mark_Stale_When_Doc_Newer_Than_Source()
    {
        var sourcePath = CreateFile("source.cs", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var docPath = CreateFile("doc.md", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        var service = new DocumentationSyncService();

        var result = await service.CheckAsync(new DocumentationSyncCheck(docPath, new[] { sourcePath }));

        Assert.False(result.IsStale);
    }

    [Fact]
    public async Task CheckAsync_Should_Mark_Stale_When_Doc_Missing_But_Source_Exists()
    {
        var sourcePath = CreateFile("source.cs", DateTime.UtcNow);
        var docPath = Path.Combine(_directory, "inexistente.md");
        var service = new DocumentationSyncService();

        var result = await service.CheckAsync(new DocumentationSyncCheck(docPath, new[] { sourcePath }));

        Assert.True(result.IsStale);
        Assert.Null(result.DocLastWriteUtc);
    }

    [Fact]
    public async Task CheckAsync_Should_Not_Be_Stale_When_No_Source_Files_Exist()
    {
        var docPath = CreateFile("doc.md", DateTime.UtcNow);
        var service = new DocumentationSyncService();

        var result = await service.CheckAsync(new DocumentationSyncCheck(docPath, new[] { "inexistente.cs" }));

        Assert.False(result.IsStale);
    }

    [Fact]
    public async Task CheckAllAsync_Should_Return_One_Result_Per_Check()
    {
        var docPath = CreateFile("doc.md", DateTime.UtcNow);
        var service = new DocumentationSyncService();
        var checks = new[]
        {
            new DocumentationSyncCheck(docPath, Array.Empty<string>()),
            new DocumentationSyncCheck(docPath, Array.Empty<string>()),
        };

        var results = await service.CheckAllAsync(checks);

        Assert.Equal(2, results.Count);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
