#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Core.AI.Governance.Recovery;

namespace BlueprintOS.Core.AI.Governance.Contracts;

/// <summary>
/// Writes and reads the on-disk recovery package for one live execution. The write of the manifest plus the
/// before-state happens BEFORE the live write; after-data and the validation report are appended once the
/// write has run.
/// </summary>
public interface IRecoveryPackageWriter
{
    /// <summary>Creates the package directory and writes manifest.json, before-data.json and
    /// expected-after.json. Returns the receipt the Tool Gateway requires. <paramref name="allowsMissingBeforeState"/>
    /// is the caller's explicit declaration (see <see cref="Recovery.BeforeStateEvaluator.Evaluate"/>) that an
    /// empty <paramref name="beforeData"/> is expected for this write, not a capture failure — it never
    /// defaults to true, so every existing caller keeps today's strict behavior.</summary>
    Task<RecoveryPackageReceipt> CreateAsync(
        RecoveryPackageManifest manifest,
        IReadOnlyList<RecoveryDataSet> beforeData,
        IReadOnlyList<RecoveryDataSet> expectedAfter,
        bool allowsMissingBeforeState = false,
        CancellationToken cancellationToken = default);

    Task WriteAfterDataAsync(RecoveryPackageReceipt receipt, IReadOnlyList<RecoveryDataSet> afterData, CancellationToken cancellationToken = default);

    Task WriteValidationReportAsync(RecoveryPackageReceipt receipt, PostWriteValidationReport report, CancellationToken cancellationToken = default);

    Task<RecoveryPackageManifest?> ReadManifestAsync(string packagePath, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecoveryDataSet>> ReadBeforeDataAsync(string packagePath, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecoveryDataSet>> ReadAfterDataAsync(string packagePath, CancellationToken cancellationToken = default);

    bool PackageExists(string packagePath);

    /// <summary>Permanently removes the package directory. Used only by retention cleanup; never touches audit.</summary>
    Task DeletePackageAsync(string packagePath, CancellationToken cancellationToken = default);
}
