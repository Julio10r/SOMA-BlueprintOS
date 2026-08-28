#pragma warning disable CS1591

using BlueprintOS.Infrastructure.Persistence.Governance;
using Xunit;

namespace BlueprintOS.UnitTests.Infrastructure.Persistence.Governance;

/// <summary>
/// Proves <see cref="RuntimeRootLocator"/> resolves the SOMA-BlueprintOS REPOSITORY root — never the +Compras
/// backend folder, never the process's current working directory — so <c>runtime/backups</c> and
/// <c>runtime/governance</c> always land at <c>{repository-root}/runtime</c> regardless of where
/// `dotnet run`/`dotnet test` was launched from.
/// </summary>
public sealed class RuntimeRootLocatorTests
{
    [Fact]
    public void ResolveRepositoryRoot_Finds_A_Directory_With_Both_Git_And_ClaudeMd_Markers()
    {
        var root = RuntimeRootLocator.ResolveRepositoryRoot();

        Assert.True(Directory.Exists(Path.Combine(root, ".git")) || File.Exists(Path.Combine(root, ".git")),
            $"Expected a .git marker under resolved repository root '{root}'.");
        Assert.True(File.Exists(Path.Combine(root, "CLAUDE.md")),
            $"Expected CLAUDE.md under resolved repository root '{root}'.");
    }

    [Fact]
    public void ResolveRepositoryRoot_Is_Not_The_MaisCompras_Backend_Folder()
    {
        var root = RuntimeRootLocator.ResolveRepositoryRoot();

        // The backend's own BlueprintOS.sln lives several levels below the repository root — the resolved
        // root must be an ANCESTOR of that folder, never equal to it.
        Assert.False(File.Exists(Path.Combine(root, "BlueprintOS.sln")),
            "RuntimeRootLocator resolved the +Compras backend folder instead of the repository root.");
        Assert.True(Directory.Exists(Path.Combine(root, "applications", "mais-compras", "backend")),
            "Resolved repository root does not contain the expected applications/mais-compras/backend path.");
    }

    [Fact]
    public void ResolveRuntimeRoot_Is_RepositoryRoot_Plus_Runtime()
    {
        var expected = Path.Combine(RuntimeRootLocator.ResolveRepositoryRoot(), "runtime");
        Assert.Equal(expected, RuntimeRootLocator.ResolveRuntimeRoot());
    }

    [Fact]
    public void Resolution_Is_Independent_Of_The_Current_Working_Directory()
    {
        var originalCwd = Directory.GetCurrentDirectory();
        var fromOriginalCwd = RuntimeRootLocator.ResolveRuntimeRoot();

        var probeDir = Path.GetTempPath();
        try
        {
            Directory.SetCurrentDirectory(probeDir);
            RuntimeRootLocator.ResetCacheForTests();
            var fromTempCwd = RuntimeRootLocator.ResolveRuntimeRoot();

            Assert.Equal(fromOriginalCwd, fromTempCwd);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalCwd);
            RuntimeRootLocator.ResetCacheForTests();
        }
    }
}
