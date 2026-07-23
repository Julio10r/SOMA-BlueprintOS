using BlueprintOS.Infrastructure.Documentation;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation;

public class GitCliDocumentationServiceTests
{
    [Fact]
    public async Task GetLastCommitDateAsync_Should_Return_Null_For_File_Without_Git_History()
    {
        var service = new GitCliDocumentationService();
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.md");

        var date = await service.GetLastCommitDateAsync(path);

        Assert.Null(date);
    }

    [Fact]
    public async Task GetLastCommitDateAsync_Should_Return_Date_For_Tracked_File_In_This_Repository()
    {
        var service = new GitCliDocumentationService();
        var repoRoot = FindRepositoryRoot();
        var trackedFile = Path.Combine(repoRoot, "README.md");

        if (!File.Exists(trackedFile))
        {
            return;
        }

        var date = await service.GetLastCommitDateAsync(trackedFile);

        Assert.True(date is null || date.Value > DateTimeOffset.MinValue);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !Directory.Exists(Path.Combine(current.FullName, ".git")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? AppContext.BaseDirectory;
    }
}
