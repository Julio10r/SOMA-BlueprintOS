#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance;

/// <summary>
/// Provider-agnostic implementation of the Capability Gap and Agent Evolution Policy
/// (agents/CAPABILITY_GAP_AND_AGENT_EVOLUTION_POLICY.md). No branch here depends on which LLM/provider is
/// executing the flow.
/// </summary>
public sealed class CapabilityGapAndAgentEvolutionPolicy
{
    /// <summary>
    /// Resolves a capability request against REQUEST -> REGISTRY -> AGENT OWNER? -> CAPABILITY COBERTA? ->
    /// KNOWLEDGE SUFICIENTE?. Any gap interrupts the flow; nothing here authorizes automatic execution.
    /// </summary>
    public GapResolution Resolve(CapabilityRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.CapabilityDeclaredByAnyAgent)
        {
            return request.ExistingAgentIsNaturalOwnerForEvolution
                ? new GapResolution
                {
                    Outcome = GapResolutionOutcome.CapabilityGap,
                    FlowInterrupted = true,
                    AutomaticExecutionAllowed = false,
                    Explanation = "CAPABILITY GAP: nenhum Agent declara esta capability. Existe Agent com responsabilidade coerente para evoluir; propor UPDATE via Agent Factory com autorizacao humana explicita antes de qualquer execucao.",
                }
                : new GapResolution
                {
                    Outcome = GapResolutionOutcome.NoNaturalOwnerProposeNewAgent,
                    FlowInterrupted = true,
                    AutomaticExecutionAllowed = false,
                    Explanation = "Nenhum Agent existente e owner natural. Deve-se propor um novo Agent (responsabilidade, capabilities, dados, tools, riscos, Security/LGPD) e aguardar autorizacao humana explicita antes de qualquer Agent Factory CREATE.",
                };
        }

        if (!request.KnowledgeSufficient)
        {
            return new GapResolution
            {
                Outcome = GapResolutionOutcome.KnowledgeGap,
                FlowInterrupted = true,
                AutomaticExecutionAllowed = false,
                Explanation = $"KNOWLEDGE GAP: capability '{request.CapabilityId}' e coberta por {request.OwningAgentId}, mas o conhecimento disponivel e insuficiente para agir com seguranca. Investigar fontes autorizadas, perguntar ao Product Owner quando necessario, aprender e validar antes de continuar.",
            };
        }

        return new GapResolution
        {
            Outcome = GapResolutionOutcome.Covered,
            FlowInterrupted = false,
            AutomaticExecutionAllowed = false,
            Explanation = $"Capability '{request.CapabilityId}' coberta por {request.OwningAgentId} com conhecimento suficiente; prosseguir pelo fluxo normal de ActionProposal/Policy Engine/Approval.",
        };
    }

    /// <summary>
    /// Decides whether an Agent evolution proposal may be applied. Material changes always require explicit
    /// human approval; nothing here auto-applies a material capability change.
    /// </summary>
    public AgentEvolutionDecision EvaluateEvolution(AgentEvolutionProposal proposal)
    {
        ArgumentNullException.ThrowIfNull(proposal);

        if (!proposal.IsMaterialChange)
        {
            return new AgentEvolutionDecision
            {
                CanApply = true,
                RequiresHumanApproval = false,
                Reason = "Mudanca nao material; pode prosseguir para validacao pela Agent Factory sem exigir novo ciclo de aprovacao humana.",
            };
        }

        if (!proposal.HumanApprovalGranted || string.IsNullOrWhiteSpace(proposal.ApprovedBy))
        {
            return new AgentEvolutionDecision
            {
                CanApply = false,
                RequiresHumanApproval = true,
                Reason = "Mudanca material de capability exige autorizacao humana explicita (approved, approved_by) antes de qualquer Agent Factory UPDATE.",
            };
        }

        return new AgentEvolutionDecision
        {
            CanApply = true,
            RequiresHumanApproval = true,
            Reason = $"Mudanca material autorizada explicitamente por {proposal.ApprovedBy}; pode prosseguir para Agent Factory UPDATE e reauditoria.",
        };
    }

    /// <summary>
    /// Decides whether a new-Agent proposal may proceed to Agent Factory CREATE. Creation is never automatic:
    /// it requires evaluated-and-rejected existing Agents, capability gap evidence, and explicit human approval.
    /// </summary>
    public NewAgentDecision EvaluateNewAgentProposal(NewAgentProposal proposal)
    {
        ArgumentNullException.ThrowIfNull(proposal);

        if (proposal.ExistingAgentsEvaluatedAndRejected.Count == 0 || string.IsNullOrWhiteSpace(proposal.CapabilityGapEvidence))
        {
            return new NewAgentDecision
            {
                CanCreate = false,
                Reason = "Criacao de novo Agent exige evidencia do Capability Gap e lista dos Agents existentes avaliados e rejeitados como owner.",
            };
        }

        if (!proposal.HumanApprovalGranted)
        {
            return new NewAgentDecision
            {
                CanCreate = false,
                Reason = "Novo Agent nunca pode ser criado silenciosamente; aguardando autorizacao humana explicita antes de qualquer Agent Factory CREATE.",
            };
        }

        return new NewAgentDecision
        {
            CanCreate = true,
            Reason = $"Proposta de novo Agent '{proposal.ProposedAgentId}' autorizada explicitamente; pode prosseguir para Agent Factory CREATE.",
        };
    }
}
