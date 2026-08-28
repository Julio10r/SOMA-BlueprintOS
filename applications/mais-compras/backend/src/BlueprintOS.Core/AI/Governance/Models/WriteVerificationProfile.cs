#pragma warning disable CS1591

namespace BlueprintOS.Core.AI.Governance.Models;

/// <summary>
/// Versionable WRITE VERIFICATION POLICY for a logical connection profile.
///
/// This is deliberately NOT <c>LinxConnectionProfile</c>: that type is physical infrastructure
/// (server, database, VPN requirement). This type is policy — what safety guarantees a live write
/// through that profile must carry (backup, rollback, retention, post-write validation), who approved
/// them, and since when. It is resolved exclusively through <c>IWriteVerificationProfileStore</c>;
/// no code path may infer these guarantees from a database name, a server address, or any hardcoded
/// switch over environments.
///
/// Versioning is append-only. A change never edits an existing row: it appends a new version with a
/// new <see cref="PolicyVersion"/> and <see cref="EffectiveFrom"/>, and the earlier version stays
/// readable for audit. Resolution picks the newest version whose <see cref="EffectiveFrom"/> is at or
/// before the instant being resolved.
/// </summary>
public sealed record WriteVerificationProfile(
    string ConnectionProfile,
    bool BackupRequired,
    bool RollbackSupported,
    int BackupRetentionDays,
    bool PostWriteValidationRequired,
    string PolicyVersion,
    string ApprovedBy,
    DateTimeOffset EffectiveFrom)
{
    /// <summary>True when <paramref name="candidate"/> would remove a safety guarantee this version grants.</summary>
    public bool ReducesGuaranteesComparedTo(WriteVerificationProfile candidate) =>
        (BackupRequired && !candidate.BackupRequired)
        || (RollbackSupported && !candidate.RollbackSupported)
        || (PostWriteValidationRequired && !candidate.PostWriteValidationRequired);
}

/// <summary>
/// Canonical seed set for the write verification policy. These values are the source used both by the
/// EF migration seed and by in-memory stores in tests, so policy is defined once. They are seeds, not a
/// resolution mechanism: production code always reads the store.
/// </summary>
public static class WriteVerificationProfileSeeds
{
    public const string LinxDevelopment = "linx-development";
    public const string LinxProduction = "linx-production";
    public const string Wise = "wise";

    /// <summary>Instant from which the initial (phase A) policy set is effective.</summary>
    public static readonly DateTimeOffset PhaseAEffectiveFrom = new(2026, 8, 28, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Phase B for <c>linx-development</c> exists as a SEPARATE, already-recorded version rather than as
    /// an edit of phase A — the whole point of versioning. It is deliberately NOT yet effective: its
    /// EffectiveFrom is a far-future sentinel, so resolution today still returns the stricter phase A.
    /// Activating phase B means proposing a version whose EffectiveFrom is a real date, which is itself
    /// a governed ActionProposal (see <c>WriteVerificationProfileGovernanceService</c>).
    /// </summary>
    public static readonly DateTimeOffset PhaseBEffectiveFrom = new(2099, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>linx-development, phase A: full protection while the live-write path is being proven.</summary>
    public static readonly WriteVerificationProfile LinxDevelopmentPhaseA = new(
        LinxDevelopment, BackupRequired: true, RollbackSupported: true, BackupRetentionDays: 30,
        PostWriteValidationRequired: true, PolicyVersion: "1.0-phase-a",
        ApprovedBy: "product-owner", EffectiveFrom: PhaseAEffectiveFrom);

    /// <summary>linx-development, phase B: backup/rollback relaxed for a disposable DEV database, but
    /// post-write validation is NEVER relaxed.</summary>
    public static readonly WriteVerificationProfile LinxDevelopmentPhaseB = new(
        LinxDevelopment, BackupRequired: false, RollbackSupported: false, BackupRetentionDays: 0,
        PostWriteValidationRequired: true, PolicyVersion: "2.0-phase-b",
        ApprovedBy: "product-owner", EffectiveFrom: PhaseBEffectiveFrom);

    public static readonly WriteVerificationProfile LinxProductionV1 = new(
        LinxProduction, BackupRequired: true, RollbackSupported: true, BackupRetentionDays: 90,
        PostWriteValidationRequired: true, PolicyVersion: "1.0",
        ApprovedBy: "product-owner", EffectiveFrom: PhaseAEffectiveFrom);

    /// <summary>WISE is configuration-only: no data backup/rollback semantics, validation still required.</summary>
    public static readonly WriteVerificationProfile WiseV1 = new(
        Wise, BackupRequired: false, RollbackSupported: false, BackupRetentionDays: 0,
        PostWriteValidationRequired: true, PolicyVersion: "1.0-config-only",
        ApprovedBy: "product-owner", EffectiveFrom: PhaseAEffectiveFrom);

    public static IReadOnlyList<WriteVerificationProfile> All =>
    [
        LinxDevelopmentPhaseA,
        LinxDevelopmentPhaseB,
        LinxProductionV1,
        WiseV1,
    ];
}
