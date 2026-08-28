using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Persistence.Repositories;

public sealed class FornecedorDescobertoRepository(BlueprintOSDbContext context) : IFornecedorDescobertoRepository
{
    public async Task AdicionarAsync(FornecedorDescoberto descoberta, CancellationToken cancellationToken = default)
    {
        await context.FornecedoresDescobertos.AddAsync(descoberta, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FornecedorDescoberto>> ListarAsync(Guid temporaryUserId, CancellationToken cancellationToken = default) =>
        await context.FornecedoresDescobertos.AsNoTracking().Where(x => x.TemporaryUserId == temporaryUserId)
            .OrderByDescending(x => x.DescobertoEm).ThenByDescending(x => x.Score).ToListAsync(cancellationToken);

    public Task<FornecedorDescoberto?> ObterPorIdAsync(Guid id, Guid temporaryUserId, CancellationToken cancellationToken = default) =>
        context.FornecedoresDescobertos.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.TemporaryUserId == temporaryUserId, cancellationToken);
}
