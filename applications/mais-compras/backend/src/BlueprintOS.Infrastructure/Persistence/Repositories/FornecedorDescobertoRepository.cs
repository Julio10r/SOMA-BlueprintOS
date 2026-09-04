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

    public async Task<IReadOnlyList<FornecedorDescoberto>> ListarAsync(CancellationToken cancellationToken = default) =>
        await context.FornecedoresDescobertos.AsNoTracking()
            .OrderByDescending(x => x.DescobertoEm).ThenByDescending(x => x.Score).ToListAsync(cancellationToken);

    public Task<FornecedorDescoberto?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.FornecedoresDescobertos.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
}
