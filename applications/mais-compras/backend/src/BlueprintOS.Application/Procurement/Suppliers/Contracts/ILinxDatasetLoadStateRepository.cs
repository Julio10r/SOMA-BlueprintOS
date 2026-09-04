using BlueprintOS.Domain.Procurement.Suppliers.Raw;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

/// <summary>Persistence for the one-row-per-dataset bootstrap/baseline state (<see cref="LinxDatasetLoadState"/>)
/// that gates whether a dataset's Incremental mode is currently allowed to run. Deliberately separate from
/// execution history (<see cref="RawLinxFornecedorSnapshotExecucao"/>): this is dataset-level STATE, not a
/// per-run fact.</summary>
public interface ILinxDatasetLoadStateRepository
{
    Task<LinxDatasetLoadState?> ObterAsync(string dataset, CancellationToken cancellationToken = default);

    Task SalvarAsync(LinxDatasetLoadState estado, CancellationToken cancellationToken = default);
}
