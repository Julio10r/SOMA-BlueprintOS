#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance.Recovery;

public enum WriteExecutionOutcome
{
    Blocked = 1,
    AwaitingApproval = 2,
    Completed = 3,
    ExecutionFailed = 4,
    ValidationFailed = 5,
    RolledBack = 6,
}

/// <summary>
/// The PERMANENT record of one governed write execution. It lives in its own table and is never touched by
/// retention cleanup: recovery packages expire, the fact that a write happened does not. It deliberately
/// stores a COMPACT before→after summary and the list of changed fields rather than full payloads, so it can
/// be kept forever without becoming a second copy of the database (or of personal data).
/// </summary>
public sealed record WriteExecutionAuditRecord
{
    public required Guid ExecutionId { get; init; }
    public required string ExecutionName { get; init; }
    public required string AgentId { get; init; }
    public required string ConnectionProfile { get; init; }
    public required string WriteVerificationPolicyVersion { get; init; }
    public required string Server { get; init; }
    public required string Database { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public required DateTimeOffset CompletedAt { get; init; }
    public required string Requester { get; init; }
    public required string Intent { get; init; }
    public required IReadOnlyList<ActionOperation> Operations { get; init; }
    public required IReadOnlyList<string> TablesAffected { get; init; }
    public required IReadOnlyList<string> BusinessKeys { get; init; }
    public required int RecordsAffected { get; init; }

    /// <summary>Stored procedures invoked as part of the execution (e.g. sequence generators).</summary>
    public IReadOnlyList<string> ProceduresInvoked { get; init; } = [];

    /// <summary>Compact human-readable before→after summary. Never the full row payload.</summary>
    public required string BeforeAfterSummary { get; init; }

    public IReadOnlyList<string> ChangedFields { get; init; } = [];

    public required string ValidationRuleId { get; init; }
    public required int RecordsValidated { get; init; }
    public required int RecordsWithErrors { get; init; }
    public required bool PostWriteValidationPassed { get; init; }

    public required bool BackupRequired { get; init; }
    public required bool BackupCreated { get; init; }
    public required int RetentionDays { get; init; }
    public DateTimeOffset? BackupExpiresAt { get; init; }
    public required RecoveryPackageStatus RecoveryPackageStatus { get; init; }

    public required bool RollbackAvailable { get; init; }
    public bool RollbackExecuted { get; init; }
    public string? RollbackResult { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyList<string> KnowledgeGaps { get; init; } = [];
    public required WriteExecutionOutcome Outcome { get; init; }
    public string? ProposalHash { get; init; }
}
