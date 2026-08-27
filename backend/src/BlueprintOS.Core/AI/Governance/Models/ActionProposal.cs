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
            NormalizeNullable(proposal.AdditionalContext));

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
        string? AdditionalContext);
}

