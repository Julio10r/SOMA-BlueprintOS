#pragma warning disable CS1591

using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.Core.AI.Governance.Contracts;

/// <summary>
/// The plain, deterministic service that actually streams a <see cref="ReadDatasetDefinition"/> from Linx
/// into its RAW destination table — deliberately OUTSIDE all Agent/LLM infrastructure (B3/Bloco 5A.9,
/// "Agent ≠ LLM"). Its concrete implementation lives in BlueprintOS.Infrastructure (it needs
/// <c>Microsoft.Data.SqlClient</c>, which BlueprintOS.Core never references); only this contract is visible
/// to the governed adapter that calls it, so the Gateway/Policy Engine authorize the call exactly once for
/// the whole dataset and never see individual rows.
/// </summary>
public interface ISomaLinxDatasetBulkReader
{
    /// <param name="dataset">The registered dataset to read.</param>
    /// <param name="executionId">Correlates this streaming attempt with its governance audit trail.</param>
    /// <param name="modo">Which command text to resolve — see <see cref="ReadDatasetDefinition.ResolveCommandText"/>.</param>
    /// <param name="watermark">Required (and used to resolve <see cref="ReadDatasetDefinition.IncrementalCommandTextFactory"/>'s
    /// <c>@watermark</c> parameter) when <paramref name="modo"/> is <see cref="DatasetLoadKind.Incremental"/>;
    /// ignored for <see cref="DatasetLoadKind.Full"/>.</param>
    /// <param name="cancellationToken">Cancels the streaming attempt cooperatively.</param>
    Task<ReadExecutionResult> StreamAsync(ReadDatasetDefinition dataset, Guid executionId, DatasetLoadKind modo, DateTimeOffset? watermark, CancellationToken cancellationToken = default);
}
