using System.Text;
using BlueprintOS.Core.Documentation.Contracts;
using BlueprintOS.Core.Documentation.Contracts.Engineering;
using BlueprintOS.Core.Documentation.Models;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Engineering;

/// <summary>
/// Implementação de <see cref="IArchitectureGenerator"/> que agrega, via
/// <see cref="ITechnicalDocumentationGenerator"/>, a documentação técnica dos módulos reais
/// já existentes no backend (Documentation, Knowledge, Agentes de IA e Negociação).
/// </summary>
public sealed class ArchitectureGenerator : IArchitectureGenerator
{
    private static readonly IReadOnlyList<ModuleMetadata> Modules = new[]
    {
        new ModuleMetadata(
            "Documentation",
            "Gerencia a documentação viva do próprio BlueprintOS: entradas de documento, versionamento, changelog, ADRs e geração de documentação técnica/funcional/IA/desenvolvedor.",
            new[] { "IDocumentationRepository", "IDocumentVersioningService", "IChangeLogService", "IAdrService", "ITechnicalDocumentationGenerator", "IMermaidDiagramGenerator", "IDocumentationSyncService", "IStaleDocumentationDetector", "IGitLogReader" },
            new[] { "MarkdownAdrService", "TechnicalDocumentationGenerator", "MermaidDiagramGenerator", "DocumentationSyncService" }),
        new ModuleMetadata(
            "Knowledge",
            "Ingestão e recuperação de conhecimento organizacional a partir de conteúdo Markdown.",
            new[] { "IKnowledgeProvider", "IKnowledgeService" },
            new[] { "MarkdownKnowledgeProvider", "KnowledgeService" }),
        new ModuleMetadata(
            "Agents",
            "Runtime de agentes de IA especializados, construídos sobre um runtime de IA comum.",
            new[] { "IAgent", "IAIRuntime" },
            new[] { "BaseAgent", "EchoAgent", "KnowledgeAgent", "AgentFactory" }),
        new ModuleMetadata(
            "AI.Negotiation",
            "Memória de negociação e motor de estratégia de negociação baseado em regras para o agente Buyer sênior.",
            new[] { "INegotiationMemory", "INegotiationMemoryStore", "INegotiationStrategy", "INegotiationStrategyRule" },
            new[] { "NegotiationMemory", "InMemoryNegotiationMemoryStore", "NegotiationStrategy" }),
    };

    private readonly ITechnicalDocumentationGenerator _technicalDocumentationGenerator;

    public ArchitectureGenerator(ITechnicalDocumentationGenerator technicalDocumentationGenerator)
    {
        _technicalDocumentationGenerator = technicalDocumentationGenerator;
    }

    /// <inheritdoc />
    public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## Arquitetura de engenharia");
        builder.AppendLine();
        builder.AppendLine("O backend do BlueprintOS segue Modular Monolith + Clean Architecture (ver ADR-0001).");
        builder.AppendLine("Hoje os módulos de domínio são organizados como `Core/{Módulo}/{Contracts,Models}` +");
        builder.AppendLine("`Infrastructure/{Módulo}/...`, e não ainda a estrutura alvo `Modules/` (ver ADR-0006).");
        builder.AppendLine();

        foreach (var module in Modules)
        {
            builder.AppendLine(_technicalDocumentationGenerator.Generate(module));
            builder.AppendLine("---");
            builder.AppendLine();
        }

        return Task.FromResult(builder.ToString());
    }
}
