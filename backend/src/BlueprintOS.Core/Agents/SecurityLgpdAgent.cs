#pragma warning disable CS1591

using System.Text;
using BlueprintOS.Core.AI.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Models;
using BlueprintOS.Core.Agents.Models;

namespace BlueprintOS.Core.Agents;

/// <summary>
/// Especialista consultivo em seguranca, privacidade e LGPD. A decisao bloqueante pertence ao
/// Policy Engine deterministico; este agent interpreta contexto e explica riscos para humanos.
/// </summary>
public sealed class SecurityLgpdAgent(IAIRuntime runtime) : BaseAgent(runtime)
{
    public Task<AgentResult> ReviewAsync(ActionProposal proposal, PolicyDecision decision, CancellationToken cancellationToken = default)
    {
        var prompt = BuildPrompt(proposal, decision);
        return ExecuteAsync(new AgentContext { Input = prompt }, cancellationToken);
    }

    public override async Task<AgentResult> ExecuteAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var response = await Runtime.ExecuteAsync(new AIRequest(context.Input), cancellationToken);
        return new AgentResult(response.Text);
    }

    private static string BuildPrompt(ActionProposal proposal, PolicyDecision decision)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Voce e o Security/LGPD Agent do SOMA BlueprintOS.");
        builder.AppendLine("Sua funcao e interpretar contexto e explicar riscos. Nao aprove execucoes e nao substitua o Policy Engine deterministico.");
        builder.AppendLine();
        builder.AppendLine("Proposta de acao:");
        builder.AppendLine($"- Agent solicitante: {proposal.RequestingAgent}");
        builder.AppendLine($"- Ambiente: {proposal.Environment}");
        builder.AppendLine($"- Sistema: {proposal.System}");
        builder.AppendLine($"- Recurso: {proposal.ResourceType} {proposal.Resource}");
        builder.AppendLine($"- Operacao: {proposal.Operation}");
        builder.AppendLine($"- Campos: {string.Join(", ", proposal.Fields)}");
        builder.AppendLine($"- Filtro: {proposal.FilterSummary ?? "(nao informado)"}");
        builder.AppendLine($"- Registros previstos: {proposal.ExpectedAffectedRows?.ToString() ?? "(nao informado)"}");
        builder.AppendLine($"- Finalidade: {proposal.Purpose}");
        builder.AppendLine($"- Classificacao de dados: {proposal.DataClassification}");
        builder.AppendLine($"- Dados pessoais: {proposal.ContainsPersonalData}");
        builder.AppendLine($"- Dados pessoais sensiveis: {proposal.ContainsSensitivePersonalData}");
        builder.AppendLine($"- Segredos: {proposal.ContainsSecrets}");
        builder.AppendLine($"- Reversibilidade: {proposal.Reversibility}");
        builder.AppendLine($"- Runbook: {proposal.RunbookReference ?? "(nao informado)"}");
        builder.AppendLine($"- Hash: {proposal.ProposalHash}");
        builder.AppendLine();
        builder.AppendLine("Decisao deterministica:");
        builder.AppendLine($"- Risco: {decision.RiskClassification}");
        builder.AppendLine($"- Status: {decision.Status}");
        builder.AppendLine($"- Exige aprovacao humana: {decision.RequiresHumanApproval}");
        builder.AppendLine($"- Desvio material: {decision.IsMaterialDeviation}");
        builder.AppendLine("Motivos:");
        foreach (var reason in decision.Reasons)
        {
            builder.AppendLine($"- {reason}");
        }

        builder.AppendLine();
        builder.AppendLine("Responda com uma avaliacao curta de seguranca/LGPD, citando minimizacao, finalidade, exposicao em logs/prompts/planilhas e o que o humano deve conferir antes de autorizar.");
        return builder.ToString();
    }
}

