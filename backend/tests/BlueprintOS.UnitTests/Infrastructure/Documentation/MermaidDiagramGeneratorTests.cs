using BlueprintOS.Core.Documentation.Models;
using BlueprintOS.Infrastructure.Documentation;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation;

public class MermaidDiagramGeneratorTests
{
    private static ModuleDependencyGraph CreateGraph() => new(
        new[]
        {
            new MermaidNode("Knowledge", "Knowledge"),
            new MermaidNode("Memory", "Memory"),
        },
        new[]
        {
            new MermaidRelation("Knowledge", "Memory", "usa"),
        });

    [Fact]
    public void Generate_FlowChart_Should_Start_With_Graph_TD_And_Include_Nodes_And_Edges()
    {
        var generator = new MermaidDiagramGenerator();

        var diagram = generator.Generate(CreateGraph(), MermaidDiagramType.FlowChart);

        Assert.StartsWith("graph TD", diagram);
        Assert.Contains("Knowledge[Knowledge]", diagram);
        Assert.Contains("Knowledge -->|usa| Memory", diagram);
    }

    [Fact]
    public void Generate_SequenceDiagram_Should_Include_Participants_And_Messages()
    {
        var generator = new MermaidDiagramGenerator();

        var diagram = generator.Generate(CreateGraph(), MermaidDiagramType.SequenceDiagram);

        Assert.StartsWith("sequenceDiagram", diagram);
        Assert.Contains("participant Knowledge as Knowledge", diagram);
        Assert.Contains("Knowledge->>Memory: usa", diagram);
    }

    [Fact]
    public void Generate_ClassDiagram_Should_Include_Classes_And_Relations()
    {
        var generator = new MermaidDiagramGenerator();

        var diagram = generator.Generate(CreateGraph(), MermaidDiagramType.ClassDiagram);

        Assert.StartsWith("classDiagram", diagram);
        Assert.Contains("class Knowledge {", diagram);
        Assert.Contains("Knowledge --> Memory : usa", diagram);
    }

    [Fact]
    public void Generate_Should_Default_To_FlowChart()
    {
        var generator = new MermaidDiagramGenerator();

        var diagram = generator.Generate(CreateGraph());

        Assert.StartsWith("graph TD", diagram);
    }
}
