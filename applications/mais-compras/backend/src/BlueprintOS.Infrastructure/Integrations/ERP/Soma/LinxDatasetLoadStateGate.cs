using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Core.AI.Governance.Contracts;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Soma;

/// <summary>Bridges the Core-owned, domain-agnostic <see cref="IDatasetLoadGate"/> to the real
/// bootstrap/baseline state (<c>LinxDatasetLoadState</c>) — this indirection exists only because
/// BlueprintOS.Core, by design, has zero project references and can never see Domain types directly.</summary>
public sealed class LinxDatasetLoadStateGate(ILinxDatasetLoadStateRepository repository) : IDatasetLoadGate
{
    public async Task<IncrementalAuthorization> AuthorizeIncrementalAsync(string dataset, TimeSpan overlapWindow, CancellationToken cancellationToken = default)
    {
        var estado = await repository.ObterAsync(dataset, cancellationToken);
        if (estado is null || !estado.PodeExecutarIncremental() || estado.UltimoWatermarkValido is null)
            return new IncrementalAuthorization(false, null);

        return new IncrementalAuthorization(true, estado.UltimoWatermarkValido.Value - overlapWindow);
    }
}
