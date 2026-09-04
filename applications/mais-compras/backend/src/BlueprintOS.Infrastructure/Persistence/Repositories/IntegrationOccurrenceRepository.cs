using BlueprintOS.Application.Integrations.Contracts;
using BlueprintOS.Domain.Integrations.Occurrences;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Persistence.Repositories;

public sealed class IntegrationOccurrenceRepository(BlueprintOSDbContext context) : IIntegrationOccurrenceRepository
{
    public async Task AdicionarLoteAsync(IReadOnlyList<IntegrationOccurrence> ocorrencias, CancellationToken cancellationToken = default)
    {
        if (ocorrencias.Count == 0) return;
        context.IntegrationOccurrences.AddRange(ocorrencias);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IntegrationOccurrence>> ListarPorExecucaoAsync(Guid executionId, CancellationToken cancellationToken = default) =>
        await context.IntegrationOccurrences.AsNoTracking()
            .Where(o => o.ExecutionId == executionId)
            .OrderBy(o => o.OcorridoEm)
            .ToListAsync(cancellationToken);
}
