#pragma warning disable CS1591

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlueprintOS.Core.AI.Governance.Models;

public sealed record ActionProposal
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string RequestingAgent { get; init; }
    public required GovernanceEnvironment Environment { get; init; }
    public required string System { get; init; }
    public required ActionResourceType ResourceType { get; init; }
    public required string Resource { get; init; }
    public required ActionOperation Operation { get; init; }
    public IReadOnlyList<string> Fields { get; init; } = Array.Empty<string>();
    public string? FilterSummary { get; init; }
    public int? ExpectedAffectedRows { get; init; }
    public required string Purpose { get; init; }
    public required DataClassification DataClassification { get; init; }
    public required bool ContainsPersonalData { get; init; }
    public required bool ContainsSensitivePersonalData { get; init; }
    public required bool ContainsSecrets { get; init; }
    public required ActionReversibility Reversibility { get; init; }
    public string? RunbookReference { get; init; }
    public bool IsRunbookApprovedOperation { get; init; }
    public int? RunbookExpectedAffectedRows { get; init; }
    public string? AdditionalContext { get; init; }

    /// <summary>
    /// True when this proposal would REDUCE a write-safety guarantee (backup required, rollback supported,
    /// post-write validation) rather than merely change unrelated data. Null means "not applicable / not
    /// declared" and is the additive default: a null value is omitted from the canonical hash payload, so
    /// hashes of proposals created before this field existed remain byte-identical.
    /// </summary>
    public bool? ReducesWriteSafetyGuarantees { get; init; }

    /// <summary>
    /// Set ONLY by <c>RollbackOrchestrator</c>, to the ORIGINAL execution's id, when this proposal is the
    /// objectively-derived restoration of that execution's recorded before-state — never set by inference or
    /// convenience elsewhere. Its sole effect on policy is narrow and typed: a <see cref="ActionOperation.Delete"/>
    /// proposal is normally always Red/Blocked (see <c>AIGovernancePolicyEngine</c>), because an arbitrary
    /// delete anywhere else in the system is exactly the risk that rule exists to stop. A delete that is
    /// provably "undo the CREATE recorded in this verified Recovery Package" is a different, narrower thing —
    /// this field is how a proposal proves that provenance, so the policy can require approval instead of an
    /// unconditional block, without loosening the rule for anything else. Null is the additive default and is
    /// omitted from the hash, so every existing proposal keeps its hash.
    /// </summary>
    public Guid? RollbackOfExecutionId { get; init; }

    [JsonIgnore]
    public string ProposalHash => ComputeHash(this);

    public static string ComputeHash(ActionProposal proposal)
    {
        var payload = new CanonicalActionProposal(
            Normalize(proposal.RequestingAgent),
            proposal.Environment,
            Normalize(proposal.System),
            proposal.ResourceType,
            Normalize(proposal.Resource),
            proposal.Operation,
            proposal.Fields.Select(Normalize).Order(StringComparer.Ordinal).ToArray(),
            NormalizeNullable(proposal.FilterSummary),
            proposal.ExpectedAffectedRows,
            Normalize(proposal.Purpose),
            proposal.DataClassification,
            proposal.ContainsPersonalData,
            proposal.ContainsSensitivePersonalData,
            proposal.ContainsSecrets,
            proposal.Reversibility,
            NormalizeNullable(proposal.RunbookReference),
            proposal.IsRunbookApprovedOperation,
            proposal.RunbookExpectedAffectedRows,
            NormalizeNullable(proposal.AdditionalContext),
            proposal.ReducesWriteSafetyGuarantees,
            proposal.RollbackOfExecutionId);

        var json = JsonSerializer.Serialize(payload, HashJsonOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Normalize(string value) => value.Trim();

    private static string? NormalizeNullable(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static readonly JsonSerializerOptions HashJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private sealed record CanonicalActionProposal(
        string RequestingAgent,
        GovernanceEnvironment Environment,
        string System,
        ActionResourceType ResourceType,
        string Resource,
        ActionOperation Operation,
        IReadOnlyList<string> Fields,
        string? FilterSummary,
        int? ExpectedAffectedRows,
        string Purpose,
        DataClassification DataClassification,
        bool ContainsPersonalData,
        bool ContainsSensitivePersonalData,
        bool ContainsSecrets,
        ActionReversibility Reversibility,
        string? RunbookReference,
        bool IsRunbookApprovedOperation,
        int? RunbookExpectedAffectedRows,
        string? AdditionalContext,
        // Appended LAST and omitted when null, so every proposal that predates this field keeps its
        // original hash. Only a proposal that explicitly declares the flag changes its hash.
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        bool? ReducesWriteSafetyGuarantees,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        Guid? RollbackOfExecutionId);
}

