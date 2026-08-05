using BlueprintOS.Infrastructure.Documentation;
using BlueprintOS.Infrastructure.Documentation.Assets;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation.Assets;

public class DocumentationAssetGeneratorTests
{
    private readonly DocumentationAssetGenerator _generator = new(new MermaidDiagramGenerator());

    [Fact]
    public async Task GenerateAllAsync_Should_Return_The_Four_Expected_Assets()
    {
        var assets = await _generator.GenerateAllAsync();

        Assert.Equal(4, assets.Count);
        Assert.Contains(assets, a => a.RelativePath == "architecture.mmd");
        Assert.Contains(assets, a => a.RelativePath == "dependencies.mmd");
        Assert.Contains(assets, a => a.RelativePath == "agents.mmd");
        Assert.Contains(assets, a => a.RelativePath == "solution-tree.md");
    }

    [Fact]
    public async Task GenerateAllAsync_Should_Produce_Valid_Mermaid_FlowCharts()
    {
        var assets = await _generator.GenerateAllAsync();

        var architecture = assets.Single(a => a.RelativePath == "architecture.mmd");
        var dependencies = assets.Single(a => a.RelativePath == "dependencies.mmd");
        var agents = assets.Single(a => a.RelativePath == "agents.mmd");

        Assert.StartsWith("graph TD", architecture.Content);
        Assert.Contains("Agents", architecture.Content);
        Assert.Contains("Knowledge", architecture.Content);

        Assert.StartsWith("graph TD", dependencies.Content);
        Assert.Contains("BlueprintOS.Api", dependencies.Content);
        Assert.Contains("BlueprintOS.Core", dependencies.Content);

        Assert.StartsWith("graph TD", agents.Content);
        Assert.Contains("AgentFactory", agents.Content);
        Assert.Contains("DocumentationPublisher", agents.Content);
    }

    [Fact]
    public async Task GenerateAllAsync_Should_Produce_A_Solution_Tree_Ignoring_Build_Artifacts()
    {
        var treeBody = await GetSolutionTreeBodyAsync();

        Assert.DoesNotContain("bin/", treeBody);
        Assert.DoesNotContain("obj/", treeBody);
        Assert.DoesNotContain("node_modules/", treeBody);
    }

    [Fact]
    public async Task GenerateAllAsync_Should_Produce_A_Solution_Tree_Excluding_Git_Ignored_Entries()
    {
        var treeBody = await GetSolutionTreeBodyAsync();

        Assert.DoesNotContain(".myNotes", treeBody);
        Assert.DoesNotContain(".DS_Store", treeBody);
        Assert.DoesNotContain(".git/", treeBody);
    }

    [Fact]
    public async Task GenerateAllAsync_Should_Produce_A_Solution_Tree_Containing_Real_Tracked_Entries()
    {
        var assets = await _generator.GenerateAllAsync();
        var solutionTree = assets.Single(a => a.RelativePath == "solution-tree.md").Content;
        var treeBody = await GetSolutionTreeBodyAsync();

        Assert.Contains("# Árvore da Solução", solutionTree);
        Assert.Contains("backend/", treeBody);
        Assert.Contains("mcp/", treeBody);
    }

    [Fact]
    public async Task GenerateAllAsync_Should_Produce_A_Solution_Tree_Excluding_Untracked_Local_Files()
    {
        var repoRoot = FindRepoRoot();
        var scratchFileName = $"_untracked-scratch-{Guid.NewGuid():N}.md";
        var scratchFilePath = Path.Combine(repoRoot, scratchFileName);

        // Confirma a premissa do teste: este nome não deve estar coberto por nenhuma regra de
        // .gitignore, para que a exclusão observada seja por não-rastreamento, não por ignore.
        Assert.False(IsGitIgnored(repoRoot, scratchFilePath));

        await File.WriteAllTextAsync(scratchFilePath, "rascunho local não rastreado, não ignorado");
        try
        {
            var treeBody = await GetSolutionTreeBodyAsync();

            Assert.DoesNotContain(scratchFileName, treeBody);
        }
        finally
        {
            File.Delete(scratchFilePath);
        }
    }

    /// <summary>
    /// Retorna apenas o conteúdo dentro do bloco de código da árvore (entre os delimitadores
    /// ```), sem o parágrafo explicativo — que legitimamente cita "bin/", ".myNotes" etc. como
    /// exemplos do que é excluído, e por isso não deve ser usado para as asserções de exclusão.
    /// </summary>
    private async Task<string> GetSolutionTreeBodyAsync()
    {
        var assets = await _generator.GenerateAllAsync();
        var solutionTree = assets.Single(a => a.RelativePath == "solution-tree.md").Content;

        var firstFence = solutionTree.IndexOf("```", StringComparison.Ordinal);
        var lastFence = solutionTree.LastIndexOf("```", StringComparison.Ordinal);
        return solutionTree[(firstFence + 3)..lastFence];
    }

    private static string FindRepoRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory, ".git")))
            {
                return directory;
            }

            directory = Path.GetDirectoryName(directory.TrimEnd(Path.DirectorySeparatorChar));
        }

        throw new InvalidOperationException("Repository root (.git) not found from test base directory.");
    }

    private static bool IsGitIgnored(string repoRoot, string fullPath)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo(
            "git",
            $"check-ignore -q \"{fullPath}\"")
        {
            WorkingDirectory = repoRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = System.Diagnostics.Process.Start(startInfo)!;
        process.WaitForExit();
        return process.ExitCode == 0;
    }
}
