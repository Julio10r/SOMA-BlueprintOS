using System.Text;
using BlueprintOS.Core.Documentation.Contracts;
using BlueprintOS.Core.Documentation.Contracts.Assets;
using BlueprintOS.Core.Documentation.Models;
using BlueprintOS.Core.Documentation.Models.Assets;

namespace BlueprintOS.Infrastructure.Documentation.Assets;

/// <summary>
/// Implementação de <see cref="IDocumentationAssetGenerator"/>: produz os ativos de
/// documentação reutilizáveis (diagrama de arquitetura, diagrama de dependências entre
/// projetos, árvore da solução e relação entre agentes) a partir de informações reais da
/// solução (módulos existentes em <c>BlueprintOS.Core</c>, referências reais entre os
/// projetos <c>.csproj</c> do backend e a estrutura real de diretórios do repositório).
/// Não gera imagens nem depende de bibliotecas externas — apenas texto (Mermaid/Markdown).
/// </summary>
public sealed class DocumentationAssetGenerator : IDocumentationAssetGenerator
{
    private static readonly IReadOnlySet<string> IgnoredDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", ".git", "node_modules",
    };

    private static readonly ModuleDependencyGraph ArchitectureGraph = new(
        new[]
        {
            new MermaidNode("Documentation", "Documentation"),
            new MermaidNode("Knowledge", "Knowledge"),
            new MermaidNode("Agents", "Agents"),
            new MermaidNode("Negotiation", "AI.Negotiation"),
        },
        new[]
        {
            new MermaidRelation("Agents", "Knowledge", "consulta"),
            new MermaidRelation("Negotiation", "Agents", "estende"),
        });

    private static readonly ModuleDependencyGraph ProjectDependencyGraph = new(
        new[]
        {
            new MermaidNode("Api", "BlueprintOS.Api"),
            new MermaidNode("Application", "BlueprintOS.Application"),
            new MermaidNode("Domain", "BlueprintOS.Domain"),
            new MermaidNode("Infrastructure", "BlueprintOS.Infrastructure"),
            new MermaidNode("Core", "BlueprintOS.Core"),
            new MermaidNode("Shared", "BlueprintOS.Shared"),
        },
        new[]
        {
            new MermaidRelation("Api", "Application", "referencia"),
            new MermaidRelation("Api", "Infrastructure", "referencia"),
            new MermaidRelation("Api", "Shared", "referencia"),
            new MermaidRelation("Application", "Domain", "referencia"),
            new MermaidRelation("Application", "Shared", "referencia"),
            new MermaidRelation("Domain", "Shared", "referencia"),
            new MermaidRelation("Infrastructure", "Application", "referencia"),
            new MermaidRelation("Infrastructure", "Core", "referencia"),
            new MermaidRelation("Infrastructure", "Domain", "referencia"),
            new MermaidRelation("Infrastructure", "Shared", "referencia"),
        });

    private static readonly ModuleDependencyGraph AgentsRelationGraph = new(
        new[]
        {
            new MermaidNode("Planner", "AgentFactory (Planner)"),
            new MermaidNode("Especialistas", "EchoAgent / KnowledgeAgent (Especialistas)"),
            new MermaidNode("Executores", "DocumentationPublishService (Executores)"),
            new MermaidNode("Publicacao", "DocumentationPublisher (Publicação)"),
        },
        new[]
        {
            new MermaidRelation("Planner", "Especialistas", "cria"),
            new MermaidRelation("Especialistas", "Executores", "executa"),
            new MermaidRelation("Executores", "Publicacao", "publica"),
        });

    private readonly IMermaidDiagramGenerator _mermaidDiagramGenerator;

    public DocumentationAssetGenerator(IMermaidDiagramGenerator mermaidDiagramGenerator)
    {
        _mermaidDiagramGenerator = mermaidDiagramGenerator;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DocumentationAsset>> GenerateAllAsync(CancellationToken cancellationToken = default)
    {
        var assets = new List<DocumentationAsset>
        {
            new("architecture.mmd", _mermaidDiagramGenerator.Generate(ArchitectureGraph, MermaidDiagramType.FlowChart)),
            new("dependencies.mmd", _mermaidDiagramGenerator.Generate(ProjectDependencyGraph, MermaidDiagramType.FlowChart)),
            new("agents.mmd", _mermaidDiagramGenerator.Generate(AgentsRelationGraph, MermaidDiagramType.FlowChart)),
            new("solution-tree.md", GenerateSolutionTree()),
        };

        return Task.FromResult<IReadOnlyList<DocumentationAsset>>(assets);
    }

    private static string GenerateSolutionTree()
    {
        var repoRoot = FindRepoRoot(AppContext.BaseDirectory) ?? Directory.GetCurrentDirectory();

        var builder = new StringBuilder();
        builder.AppendLine("# Árvore da Solução");
        builder.AppendLine();
        builder.AppendLine("Estrutura real de diretórios e projetos do repositório (ignorando `bin`, `obj`,");
        builder.AppendLine("`.git` e `node_modules`):");
        builder.AppendLine();
        builder.AppendLine("```");
        builder.AppendLine(Path.GetFileName(repoRoot.TrimEnd(Path.DirectorySeparatorChar)));
        AppendTree(builder, repoRoot, indent: "", depth: 0, maxDepth: 3);
        builder.AppendLine("```");

        return builder.ToString();
    }

    private static void AppendTree(StringBuilder builder, string directoryPath, string indent, int depth, int maxDepth)
    {
        if (depth >= maxDepth)
        {
            return;
        }

        var entries = Directory.EnumerateFileSystemEntries(directoryPath)
            .Select(entry => new
            {
                Path = entry,
                Name = Path.GetFileName(entry),
                IsDirectory = Directory.Exists(entry),
            })
            .Where(entry => !entry.IsDirectory || !IsIgnored(entry.Name))
            .OrderByDescending(entry => entry.IsDirectory)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var isLast = i == entries.Count - 1;
            var connector = isLast ? "└── " : "├── ";
            builder.AppendLine($"{indent}{connector}{entry.Name}{(entry.IsDirectory ? "/" : string.Empty)}");

            if (entry.IsDirectory)
            {
                var childIndent = indent + (isLast ? "    " : "│   ");
                AppendTree(builder, entry.Path, childIndent, depth + 1, maxDepth);
            }
        }
    }

    private static bool IsIgnored(string directoryName) =>
        IgnoredDirectories.Contains(directoryName);

    private static string? FindRepoRoot(string startDirectory)
    {
        var directory = startDirectory;
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory, ".git")))
            {
                return directory;
            }

            directory = Path.GetDirectoryName(directory.TrimEnd(Path.DirectorySeparatorChar));
        }

        return null;
    }
}
