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
        var assets = await _generator.GenerateAllAsync();

        var solutionTree = assets.Single(a => a.RelativePath == "solution-tree.md").Content;

        Assert.Contains("# Árvore da Solução", solutionTree);
        Assert.DoesNotContain("bin/", solutionTree);
        Assert.DoesNotContain("obj/", solutionTree);
        Assert.DoesNotContain("node_modules/", solutionTree);
    }
}
