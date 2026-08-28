#pragma warning disable CS1591

#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance;

public sealed class AIGovernancePolicyEngine : IAIGovernancePolicyEngine
{
    public PolicyDecision Evaluate(ActionProposal proposal, DateTimeOffset now)
    {
        var reasons = new List<string>();
        var risk = RiskClassification.Green;
        var status = PolicyDecisionStatus.Allowed;
        var materialDeviation = DetectMaterialDeviation(proposal, reasons);

        if (HasBlankRequiredContext(proposal))
        {
            Raise(RiskClassification.Red, PolicyDecisionStatus.Blocked, "Proposta incompleta: agente, sistema, recurso e finalidade sao obrigatorios.");
        }

        if (proposal.ContainsSecrets || proposal.DataClassification == DataClassification.SecretCredential)
        {
            Raise(RiskClassification.Red, PolicyDecisionStatus.Blocked, "Segredo ou credencial envolvido na acao.");
        }

        if (proposal.Operation is ActionOperation.PersistSecret or ActionOperation.LogSecret or ActionOperation.PromptWithSecret)
        {
            Raise(RiskClassification.Red, PolicyDecisionStatus.Blocked, "Tentativa de persistir, logar ou enviar segredo em prompt.");
        }

        if (proposal.Operation is ActionOperation.Truncate or ActionOperation.Drop or ActionOperation.Grant or ActionOperation.Revoke)
        {
            Raise(RiskClassification.Red, PolicyDecisionStatus.Blocked, $"Operacao destrutiva ou de privilegio: {proposal.Operation}.");
        }

        // DELETE is always Red/Blocked UNLESS it is provably "undo the CREATE recorded in a verified Recovery
        // Package" (RollbackOfExecutionId set — see ActionProposal.RollbackOfExecutionId): that is a different,
        // narrower thing than an arbitrary delete, and still never auto-allowed — it only reaches Yellow /
        // RequiresApproval, same as any other governed write. Rollback = restore the recorded before-state,
        // and the mechanism that restoration needs (insert/update/delete) is decided objectively from what the
        // Recovery Package proves, not assumed forbidden.
        if (proposal.Operation == ActionOperation.Delete)
        {
            if (proposal.RollbackOfExecutionId is null)
            {
                Raise(RiskClassification.Red, PolicyDecisionStatus.Blocked, "Operacao destrutiva ou de privilegio: Delete.");
            }
            else
            {
                Raise(RiskClassification.Yellow, PolicyDecisionStatus.RequiresApproval,
                    "DELETE como restauracao de rollback verificado exige aprovacao explicita.");
            }
        }

        if (proposal.Operation == ActionOperation.Alter && proposal.Reversibility != ActionReversibility.Reversible)
        {
            Raise(RiskClassification.Red, PolicyDecisionStatus.Blocked, "ALTER nao reversivel ou sem reversibilidade comprovada.");
        }

        // Fixed rule (Production Write Verification & Recovery Policy): a proposal that REDUCES a write
        // safety guarantee — backup required, rollback supported, or post-write validation — is always
        // Red/Blocked in Production. No approval can unblock it; the only route is a proposal that does not
        // reduce the guarantee. Outside Production the same reduction is still material enough to demand an
        // explicit, specific human authorization.
        if (proposal.ReducesWriteSafetyGuarantees == true)
        {
            if (proposal.Environment == GovernanceEnvironment.Production)
            {
                Raise(RiskClassification.Red, PolicyDecisionStatus.Blocked,
                    "Reducao de garantia de seguranca de escrita (backup/rollback/validacao) em Producao e sempre bloqueada.");
            }
            else
            {
                Raise(RiskClassification.Yellow, PolicyDecisionStatus.RequiresApproval,
                    "Reducao de garantia de seguranca de escrita exige autorizacao humana explicita e especifica.");
            }
        }

        if (proposal.Operation == ActionOperation.ExecuteProcedure && !proposal.IsRunbookApprovedOperation)
        {
            Raise(RiskClassification.Red, PolicyDecisionStatus.Blocked, "Procedure sem runbook aprovado.");
        }

        if (proposal.Operation == ActionOperation.Update && string.IsNullOrWhiteSpace(proposal.FilterSummary))
        {
            Raise(RiskClassification.Red, PolicyDecisionStatus.Blocked, "UPDATE sem filtro/contexto delimitador.");
        }

        if (proposal.Operation == ActionOperation.Update && proposal.ExpectedAffectedRows is null)
        {
            Raise(RiskClassification.Red, PolicyDecisionStatus.Blocked, "UPDATE sem estimativa de registros afetados.");
        }

        if (proposal.Operation == ActionOperation.Merge && (!proposal.IsRunbookApprovedOperation || materialDeviation))
        {
            Raise(RiskClassification.Red, PolicyDecisionStatus.Blocked, "MERGE sem runbook aprovado ou com desvio material.");
        }

        if (proposal.Operation == ActionOperation.Export
            && (proposal.ContainsPersonalData || proposal.ContainsSensitivePersonalData)
            && (proposal.ExpectedAffectedRows is null or > 10000 || string.IsNullOrWhiteSpace(proposal.Purpose)))
        {
            Raise(RiskClassification.Red, PolicyDecisionStatus.Blocked, "Exportacao massiva ou sem finalidade clara envolvendo dados pessoais.");
        }

        if (proposal.ContainsSensitivePersonalData)
        {
            Raise(RiskClassification.Red, PolicyDecisionStatus.Blocked, "Dados pessoais sensiveis exigem fluxo excepcional.");
        }
        else if (proposal.ContainsPersonalData || proposal.DataClassification == DataClassification.PersonalData)
        {
            Raise(RiskClassification.Yellow, PolicyDecisionStatus.RequiresApproval, "Dados pessoais exigem cautela, finalidade e minimizacao.");
        }

        if (proposal.DataClassification == DataClassification.Unknown)
        {
            Raise(RiskClassification.Yellow, PolicyDecisionStatus.RequiresApproval, "Classificacao de dados desconhecida; tratar com cautela.");
        }

        if (materialDeviation)
        {
            Raise(RiskClassification.Yellow, PolicyDecisionStatus.RequiresApproval, "Desvio material em relacao ao runbook/contexto aprovado.");
        }

        if (proposal.Operation is ActionOperation.Insert or ActionOperation.Update or ActionOperation.Merge)
        {
            if (proposal.IsRunbookApprovedOperation && !materialDeviation && HasOperationalContext(proposal))
            {
                Raise(RiskClassification.Yellow, PolicyDecisionStatus.RequiresApproval, "Escrita prevista em runbook aprovado; exige autorizacao especifica antes da execucao.");
            }
            else if (risk != RiskClassification.Red)
            {
                Raise(RiskClassification.Yellow, PolicyDecisionStatus.RequiresApproval, "Operacao de escrita exige avaliacao e autorizacao especifica.");
            }
        }

        if (proposal.Operation is ActionOperation.Select or ActionOperation.SchemaDiscovery or ActionOperation.MetadataRead or ActionOperation.Analyze or ActionOperation.Compare
            && risk == RiskClassification.Green)
        {
            reasons.Add("Operacao sem efeito externo e sem risco material declarado.");
        }

        return new PolicyDecision(
            Guid.NewGuid(),
            proposal.Id,
            proposal.ProposalHash,
            risk,
            status,
            reasons.Distinct(StringComparer.Ordinal).ToArray(),
            now,
            status == PolicyDecisionStatus.RequiresApproval,
            materialDeviation);

        void Raise(RiskClassification newRisk, PolicyDecisionStatus newStatus, string reason)
        {
            if (newRisk > risk)
            {
                risk = newRisk;
            }

            if (newStatus > status)
            {
                status = newStatus;
            }

            reasons.Add(reason);
        }
    }

    private static bool HasBlankRequiredContext(ActionProposal proposal) =>
        string.IsNullOrWhiteSpace(proposal.RequestingAgent)
        || string.IsNullOrWhiteSpace(proposal.System)
        || string.IsNullOrWhiteSpace(proposal.Resource)
        || string.IsNullOrWhiteSpace(proposal.Purpose);

    private static bool HasOperationalContext(ActionProposal proposal) =>
        !string.IsNullOrWhiteSpace(proposal.FilterSummary)
        && proposal.ExpectedAffectedRows is > 0
        && !string.IsNullOrWhiteSpace(proposal.RunbookReference);

    private static bool DetectMaterialDeviation(ActionProposal proposal, List<string> reasons)
    {
        if (!proposal.IsRunbookApprovedOperation)
        {
            return false;
        }

        var deviated = false;
        if (string.IsNullOrWhiteSpace(proposal.RunbookReference))
        {
            reasons.Add("Operacao marcada como runbook aprovado sem referencia de runbook.");
            deviated = true;
        }

        if (proposal.ExpectedAffectedRows is not null && proposal.RunbookExpectedAffectedRows is not null)
        {
            var expected = Math.Max(1, proposal.RunbookExpectedAffectedRows.Value);
            var actual = proposal.ExpectedAffectedRows.Value;
            var absoluteDelta = Math.Abs(actual - expected);
            var materialDelta = Math.Max(1000, expected);
            if (absoluteDelta > materialDelta)
            {
                reasons.Add($"Quantidade prevista ({actual}) diverge materialmente do padrao do runbook ({expected}).");
                deviated = true;
            }
        }

        return deviated;
    }
}

