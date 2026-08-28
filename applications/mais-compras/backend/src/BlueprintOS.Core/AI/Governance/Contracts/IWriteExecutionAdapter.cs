#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;

namespace BlueprintOS.Core.AI.Governance.Contracts;

/// <summary>
/// How a capability can undo one of its own writes, declared by the adapter itself — never inferred from the
/// connection profile, never hardcoded per resource by the framework. This is deliberately separate from
/// <c>WriteVerificationProfile.RollbackSupported</c> (a connection-profile-level policy: "does this environment
/// require rollback capability at all"): a profile can require rollback while a SPECIFIC capability genuinely
/// cannot offer it (or can only offer it in a restricted way), and that must block the write up front with a
/// capability gap, never fail silently later when a rollback is actually requested.
/// </summary>
public enum RollbackStrategy
{
    /// <summary>The generic mechanism applies: rollback restores the recorded before-state via whichever of
    /// insert/update/delete that state objectively requires (see <c>RollbackOrchestrator.BuildEquivalentProposal</c>).</summary>
    RestoreBeforeState,

    /// <summary>Undoing this write is not a state restoration but a separate, capability-specific corrective
    /// action (e.g. issuing a reversing transaction rather than editing the original row). Reserved for a
    /// future capability that declares one; the generic orchestrator does not synthesize this on its own.</summary>
    CompensatingAction,

    /// <summary>This capability has no safe way to undo its own write — because of its own business rules, not
    /// because the generic framework forbids it (see <c>GarantirFornecedorGovernedWriteAdapter</c>, whose
    /// domain rule is "never destroy an existing supplier/role", not an infrastructure limitation). A profile
    /// that requires rollback support blocks BEFORE the write for a capability declaring this, with a recorded
    /// capability gap — it never proceeds and fails later at rollback time.</summary>
    NotSupported,
}

/// <summary>
/// An adapter that can perform a REAL write, not only a dry run. It extends <see cref="IGovernedToolAdapter"/>
/// rather than replacing it: the Tool Gateway still validates capability, owner, connection profile, identity,
/// policy decision and approval exactly as before, and only then — and only when the write safety guarantees
/// are present in the request — calls <see cref="ExecuteAsync"/>.
///
/// An adapter that does NOT implement this interface can never execute live, whatever the request says.
/// </summary>
public interface IWriteExecutionAdapter : IGovernedToolAdapter
{
    /// <summary>
    /// Performs the write. <paramref name="recoveryPackage"/> is the receipt of the package written before
    /// this call (null only when the resolved profile does not require a backup); the full manifest and the
    /// captured before-state are reachable from its <c>PackagePath</c> via <see cref="IRecoveryPackageWriter"/>.
    /// Implementations must return the observed after-state so post-write validation has something to check.
    /// </summary>
    Task<WriteExecutionResult> ExecuteAsync(
        ToolGatewayRequest request,
        RecoveryPackageReceipt? recoveryPackage,
        CancellationToken cancellationToken = default);

    /// <summary>Declared by the adapter, defaulting to <see cref="RollbackStrategy.NotSupported"/> — the safest
    /// default, so an adapter written before this existed never silently gains a rollback guarantee it never
    /// claimed. Only an adapter that explicitly overrides this to <see cref="RollbackStrategy.RestoreBeforeState"/>
    /// (or <see cref="RollbackStrategy.CompensatingAction"/>) can be used under a profile requiring rollback
    /// support.</summary>
    RollbackStrategy RollbackStrategy => RollbackStrategy.NotSupported;
}

/// <summary>
/// An adapter that can read the current state of the records a write is about to touch, so a recovery package
/// can capture a real before-state and post-write validation can compare a real after-state. Snapshot capture
/// is a read: it never mutates anything.
/// </summary>
public interface ISnapshotCapableAdapter
{
    Task<IReadOnlyList<RecoveryDataSet>> CaptureSnapshotAsync(
        IReadOnlyList<string> businessKeys,
        CancellationToken cancellationToken = default);
}
