namespace BlueprintOS.Infrastructure.Persistence.Governance;

/// <summary>
/// Resolves the ROOT of the SOMA-BlueprintOS git repository, independent of the process's current working
/// directory, the +Compras backend, or where the executing assembly happens to live under `bin/`.
///
/// The AI Agents' governance runtime (<c>runtime/backups/</c>, <c>runtime/governance/</c>) belongs to the
/// SOMA BlueprintOS platform, not to any one application inside it (e.g. +Compras) — so its default location
/// is always <c>{repository-root}/runtime</c>, never <c>{cwd}/runtime</c> and never relative to the +Compras
/// backend folder. This mirrors the walk-up-to-a-known-marker pattern already used by
/// <c>PedGradeAdjustmentE2EIntegrationTests.FindBackendRoot()</c> (which walks up to find
/// <c>BlueprintOS.sln</c>) — here walking up further, to the REPOSITORY root instead of the backend root.
///
/// Resolution starts from <see cref="AppContext.BaseDirectory"/> (the directory the running assembly was
/// loaded from — stable regardless of <c>Directory.GetCurrentDirectory()</c>) and walks upward looking for a
/// directory that contains BOTH a <c>.git</c> entry (folder for a normal clone, or file for a worktree/submodule)
/// AND a top-level <c>CLAUDE.md</c> file — the same two markers a human would use to recognize this repository's
/// root. Falls back to walking up from <see cref="Directory.GetCurrentDirectory()"/> if the base-directory walk
/// does not find it (covers `dotnet run`/test-host layouts where the marker is not an ancestor of the bin output),
/// and only then falls back to the current working directory itself so callers never crash outright.
/// </summary>
public static class RuntimeRootLocator
{
    private const string GitMarker = ".git";
    private const string RepoRootMarker = "CLAUDE.md";

    private static string? _cachedRepositoryRoot;

    /// <summary>Absolute path to the repository root (e.g. <c>/Users/x/Projects/SOMA-BlueprintOS</c>).</summary>
    public static string ResolveRepositoryRoot()
    {
        if (_cachedRepositoryRoot is not null) return _cachedRepositoryRoot;

        var fromBaseDirectory = WalkUpFrom(AppContext.BaseDirectory);
        var fromCurrentDirectory = fromBaseDirectory ?? WalkUpFrom(Directory.GetCurrentDirectory());

        _cachedRepositoryRoot = fromCurrentDirectory ?? Directory.GetCurrentDirectory();
        return _cachedRepositoryRoot;
    }

    /// <summary>The default runtime root, <c>{repository-root}/runtime</c>.</summary>
    public static string ResolveRuntimeRoot() => Path.Combine(ResolveRepositoryRoot(), "runtime");

    private static string? WalkUpFrom(string startDirectory)
    {
        var current = string.IsNullOrWhiteSpace(startDirectory) ? null : new DirectoryInfo(startDirectory);
        while (current is not null)
        {
            var hasGitMarker = Directory.Exists(Path.Combine(current.FullName, GitMarker))
                || File.Exists(Path.Combine(current.FullName, GitMarker));
            var hasRepoRootMarker = File.Exists(Path.Combine(current.FullName, RepoRootMarker));

            if (hasGitMarker && hasRepoRootMarker) return current.FullName;

            current = current.Parent;
        }

        return null;
    }

    /// <summary>Test-only escape hatch: clears the cached root so a test that manipulates markers/cwd can
    /// force re-resolution. Production code never calls this.</summary>
    internal static void ResetCacheForTests() => _cachedRepositoryRoot = null;
}
