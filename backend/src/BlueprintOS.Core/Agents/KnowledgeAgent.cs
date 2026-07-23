using System.Text;
using BlueprintOS.Core.AI.Contracts;
using BlueprintOS.Core.AI.Models;
using BlueprintOS.Core.Agents.Models;
using BlueprintOS.Core.Knowledge.Contracts;

namespace BlueprintOS.Core.Agents;

/// <summary>
/// Agente que enriquece a entrada do usuário com trechos relevantes obtidos de um
/// <see cref="IKnowledgeService"/> antes de encaminhá-la ao <see cref="IAIRuntime"/>.
/// </summary>
public sealed class KnowledgeAgent : BaseAgent
{
    private readonly IKnowledgeService _knowledgeService;

    /// <summary>
    /// Inicializa o agente com o runtime de IA e o serviço de conhecimento a serem utilizados.
    /// </summary>
    /// <param name="runtime">Runtime de IA utilizado pelo agente.</param>
    /// <param name="knowledgeService">Serviço utilizado para buscar trechos relevantes de conhecimento.</param>
    public KnowledgeAgent(IAIRuntime runtime, IKnowledgeService knowledgeService)
        : base(runtime)
    {
        _knowledgeService = knowledgeService;
    }

    /// <inheritdoc />
    public override async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var results = await _knowledgeService.SearchAsync(context.Input, cancellationToken: cancellationToken);
        var prompt = BuildPrompt(context.Input, results);

        var response = await Runtime.ExecuteAsync(new AIRequest(prompt), cancellationToken);
        return new AgentResult(response.Text);
    }

    private static string BuildPrompt(string input, IReadOnlyList<Knowledge.Models.KnowledgeSearchResult> results)
    {
        if (results.Count == 0)
        {
            return input;
        }

        var builder = new StringBuilder();
        builder.AppendLine("Contexto relevante:");

        foreach (var result in results)
        {
            builder.AppendLine($"- ({result.Document.Title}) {result.Snippet}");
        }

        builder.AppendLine();
        builder.AppendLine("Pergunta:");
        builder.Append(input);

        return builder.ToString();
    }
}
