using System.Diagnostics;
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
    /// <summary>
    /// Sempre excluído da árvore, independentemente do Git: é o próprio diretório de
    /// controle de versão, não um artefato que o Git possa reportar como ignorado.
    /// </summary>
    private const string GitDirectoryName = ".git";

    /// <summary>
    /// Diretórios vazios (sem nenhum arquivo rastreado) que representam estrutura planejada e
    /// explicitamente reservada para fases futuras do roadmap (ver <c>docs/INDEX.md</c>), não
    /// rascunho local. São os únicos diretórios sem conteúdo rastreado que a árvore preserva.
    /// </summary>
    private static readonly IReadOnlySet<string> AllowedEmptyDirectories = new HashSet<string>(StringComparer.Ordinal)
    {
        "infrastructure/terraform",
        "infrastructure/kubernetes",
        "infrastructure/nginx",
        "infrastructure/monitoring",
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
        var trackedFiles = GetGitTrackedFiles(repoRoot);
        var trackedDirectories = BuildTrackedDirectorySet(trackedFiles);

        var builder = new StringBuilder();
        builder.AppendLine("# Árvore da Solução");
        builder.AppendLine();
        builder.AppendLine("Estrutura real de diretórios e projetos do repositório, restrita ao que é");
        builder.AppendLine("versionável: arquivos rastreados pelo Git, mais os diretórios vazios");
        builder.AppendLine("explicitamente reservados para fases futuras do roadmap. Arquivos ignorados,");
        builder.AppendLine("não rastreados ou pessoais (ex.: `.myNotes`, `.DS_Store`, `bin/`, `obj/`,");
        builder.AppendLine("`node_modules/`, logs, artefatos temporários) não aparecem.");
        builder.AppendLine();
        builder.AppendLine("```");
        builder.AppendLine(Path.GetFileName(repoRoot.TrimEnd(Path.DirectorySeparatorChar)));
        AppendTree(builder, repoRoot, repoRoot, trackedFiles, trackedDirectories, indent: "", depth: 0, maxDepth: 3);
        builder.AppendLine("```");

        return builder.ToString();
    }

    private static void AppendTree(
        StringBuilder builder,
        string repoRoot,
        string directoryPath,
        IReadOnlySet<string> trackedFiles,
        IReadOnlySet<string> trackedDirectories,
        string indent,
        int depth,
        int maxDepth)
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
                RelativePath = Path.GetRelativePath(repoRoot, entry).Replace('\\', '/'),
            })
            .Where(entry => entry.Name != GitDirectoryName)
            .Where(entry => entry.IsDirectory
                ? trackedDirectories.Contains(entry.RelativePath) || AllowedEmptyDirectories.Contains(entry.RelativePath)
                : trackedFiles.Contains(entry.RelativePath))
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
                AppendTree(builder, repoRoot, entry.Path, trackedFiles, trackedDirectories, childIndent, depth + 1, maxDepth);
            }
        }
    }

    /// <summary>
    /// Consulta o próprio Git (<c>git ls-files</c>) para obter os caminhos de arquivo
    /// atualmente rastreados, em vez de reimplementar as regras de <c>.gitignore</c> ou tentar
    /// distinguir "arquivo pessoal" por convenção. Só o que o Git rastreia é considerado
    /// versionável; qualquer arquivo local não rastreado (ignorado ou não) fica de fora.
    /// </summary>
    private static IReadOnlySet<string> GetGitTrackedFiles(string repoRoot)
    {
        var tracked = new HashSet<string>(StringComparer.Ordinal);

        try
        {
            var startInfo = new ProcessStartInfo("git", "ls-files")
            {
                WorkingDirectory = repoRoot,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return tracked;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            foreach (var line in output.Split('\n'))
            {
                var path = line.Trim().Trim('"').Replace('\\', '/');
                if (path.Length > 0)
                {
                    tracked.Add(path);
                }
            }
        }
        catch (Exception)
        {
            // Git indisponível ou repositório não inicializado: nenhum arquivo é considerado
            // rastreado, então a árvore só mostrará os diretórios explicitamente reservados.
        }

        return tracked;
    }

    /// <summary>
    /// A partir dos arquivos rastreados, deriva o conjunto de diretórios que os contêm
    /// (diretamente ou em qualquer nível acima), para decidir quais diretórios "têm conteúdo
    /// versionável" e por isso devem aparecer na árvore mesmo sem serem, eles mesmos, rastreados
    /// (o Git nunca rastreia diretórios, só arquivos).
    /// </summary>
    private static IReadOnlySet<string> BuildTrackedDirectorySet(IReadOnlySet<string> trackedFiles)
    {
        var directories = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in trackedFiles)
        {
            var slashIndex = file.LastIndexOf('/');
            while (slashIndex > 0)
            {
                var directory = file[..slashIndex];
                if (!directories.Add(directory))
                {
                    break;
                }

                slashIndex = directory.LastIndexOf('/');
            }
        }

        return directories;
    }

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
