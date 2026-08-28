#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance;

/// <summary>
/// Seeded, in-memory catalog of post-write validation rules. Deliberately closed: an (operation, resource)
/// pair that is not seeded resolves to null, which the orchestrator turns into a
/// <see cref="WriteValidationKnowledgeGap"/> and a blocked write. Growing the catalog is a knowledge-ingestion
/// act by a human, not something the runtime may do for itself.
/// </summary>
public sealed class PostWriteValidationRuleCatalog : IPostWriteValidationRuleCatalog
{
    public const string CadastroCliForResource = "CADASTRO_CLI_FOR";
    public const string FornecedoresResource = "FORNECEDORES";

    /// <summary>Verifies the base registration row after a create or role-add: the row must exist for the
    /// CNPJ and carry the supplier flag.</summary>
    public static readonly PostWriteValidationRule CadastroCliForRule = new(
        RuleId: "post-write-validation.cadastro-cli-for.v1",
        Resource: CadastroCliForResource,
        Operations: [ActionOperation.Insert, ActionOperation.Update],
        BusinessKeyFields: ["CGC_CPF", "COD_CLIFOR"],
        FieldsToCompare: ["NOME_CLIFOR", "CGC_CPF", "INDICA_FORNECEDOR"],
        Description: "Reconsulta CADASTRO_CLI_FOR pela chave de negocio e confirma que os campos gravados correspondem ao estado esperado da proposta.",
        PolicyVersion: "1.0");

    /// <summary>Verifies the supplier role row after a create, role-add, or update.</summary>
    public static readonly PostWriteValidationRule FornecedoresRule = new(
        RuleId: "post-write-validation.fornecedores.v1",
        Resource: FornecedoresResource,
        Operations: [ActionOperation.Insert, ActionOperation.Update],
        BusinessKeyFields: ["COD_FORNECEDOR", "CGC_CPF"],
        FieldsToCompare: ["FORNECEDOR", "CGC_CPF", "INATIVO"],
        Description: "Reconsulta FORNECEDORES pela chave de negocio e confirma que o papel de fornecedor existe com os valores esperados.",
        PolicyVersion: "1.0");

    public const string RecoveryHomologationResource = "BLUEPRINTOS_RECOVERY_HOMOLOGATION";

    /// <summary>Verifies the generic recovery-homologation row (see agents/DATABASE_CONNECTION_POLICY.md §24):
    /// a disposable, non-business table that exists only in SOMA_DESENV to prove backup/rollback/retention
    /// without exercising any real ERP rule. Update only — insert/delete outcomes (rollback restoring or
    /// undoing a row) are proven by direct existence checks, not field comparison.</summary>
    public static readonly PostWriteValidationRule RecoveryHomologationRule = new(
        RuleId: "post-write-validation.recovery-homologation.v1",
        Resource: RecoveryHomologationResource,
        Operations: [ActionOperation.Update],
        BusinessKeyFields: ["ID"],
        FieldsToCompare: ["VALOR"],
        Description: "Reconsulta a linha de homologacao pela chave ID e confirma que VALOR corresponde ao estado esperado.",
        PolicyVersion: "1.0");

    private readonly List<PostWriteValidationRule> _rules;

    public PostWriteValidationRuleCatalog(IEnumerable<PostWriteValidationRule>? rules = null) =>
        _rules = [.. rules ?? [CadastroCliForRule, FornecedoresRule, RecoveryHomologationRule]];

    public PostWriteValidationRule? Resolve(ActionOperation operation, string resource) =>
        string.IsNullOrWhiteSpace(resource)
            ? null
            : _rules.FirstOrDefault(rule => rule.Covers(operation, resource.Trim()));

    public IReadOnlyList<PostWriteValidationRule> ListRules() => _rules.ToArray();
}

/// <summary>In-memory knowledge gap store for hosts and tests without a relational store.</summary>
public sealed class InMemoryWriteValidationKnowledgeGapStore : IWriteValidationKnowledgeGapStore
{
    private readonly List<WriteValidationKnowledgeGap> _gaps = [];

    public Task RecordAsync(WriteValidationKnowledgeGap gap, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gap);
        lock (_gaps) _gaps.Add(gap);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<WriteValidationKnowledgeGap>> ListAsync(CancellationToken cancellationToken = default)
    {
        lock (_gaps) return Task.FromResult<IReadOnlyList<WriteValidationKnowledgeGap>>(_gaps.ToArray());
    }
}

/// <summary>In-memory rollback-capability gap store for hosts and tests without a relational store.</summary>
public sealed class InMemoryRollbackCapabilityGapStore : IRollbackCapabilityGapStore
{
    private readonly List<RollbackCapabilityGap> _gaps = [];

    public Task RecordAsync(RollbackCapabilityGap gap, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(gap);
        lock (_gaps) _gaps.Add(gap);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RollbackCapabilityGap>> ListAsync(CancellationToken cancellationToken = default)
    {
        lock (_gaps) return Task.FromResult<IReadOnlyList<RollbackCapabilityGap>>(_gaps.ToArray());
    }
}
