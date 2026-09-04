using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Domain.Procurement.Suppliers.Raw;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Persistence.Repositories;

public sealed class LinxDatasetLoadStateRepository(BlueprintOSDbContext context) : ILinxDatasetLoadStateRepository
{
    public Task<LinxDatasetLoadState?> ObterAsync(string dataset, CancellationToken cancellationToken = default) =>
        context.LinxDatasetLoadStates.SingleOrDefaultAsync(x => x.Dataset == dataset, cancellationToken);

    public async Task SalvarAsync(LinxDatasetLoadState estado, CancellationToken cancellationToken = default)
    {
        var existente = await context.LinxDatasetLoadStates
            .AsNoTracking()
            .AnyAsync(x => x.Dataset == estado.Dataset, cancellationToken);

        if (existente) context.LinxDatasetLoadStates.Update(estado);
        else context.LinxDatasetLoadStates.Add(estado);

        await context.SaveChangesAsync(cancellationToken);
    }
}
