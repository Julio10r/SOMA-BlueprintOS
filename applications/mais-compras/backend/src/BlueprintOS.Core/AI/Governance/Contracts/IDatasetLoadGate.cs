#pragma warning disable CS1591

namespace BlueprintOS.Core.AI.Governance.Contracts;

/// <summary>Whether Incremental mode is currently allowed for a dataset, and — when it is — the effective
/// watermark to filter by (the last validated watermark minus the dataset's configured overlap window; see
/// <see cref="WatermarkDefinition.OverlapWindow"/>). <see cref="Permitido"/> false and
/// <see cref="WatermarkEfetivo"/> null together mean either the dataset has never completed its mandatory
/// Full bootstrap, or Incremental has not been liberated for it yet.</summary>
public sealed record IncrementalAuthorization(bool Permitido, DateTimeOffset? WatermarkEfetivo);

/// <summary>
/// The narrow, domain-agnostic question a governed read adapter needs answered before ever running a
/// dataset in <see cref="DatasetLoadKind.Incremental"/> mode: "has this dataset's mandatory Full bootstrap
/// already been reconciled and homologated, and if so, from which watermark should this run start?"
/// Deliberately primitive-only — the real bootstrap/baseline state (<c>LinxDatasetLoadState</c>) is a domain
/// concept that BlueprintOS.Core, by design, never references (Core has zero project dependencies); this
/// interface is the seam Infrastructure implements by delegating to that real state.
/// </summary>
public interface IDatasetLoadGate
{
    Task<IncrementalAuthorization> AuthorizeIncrementalAsync(string dataset, TimeSpan overlapWindow, CancellationToken cancellationToken = default);
}
