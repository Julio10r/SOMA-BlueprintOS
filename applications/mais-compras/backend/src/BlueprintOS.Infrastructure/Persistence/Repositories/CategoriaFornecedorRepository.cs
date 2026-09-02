using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Persistence.Repositories;

public sealed class CategoriaFornecedorRepository(BlueprintOSDbContext context) : ICategoriaFornecedorRepository
{
    public async Task<IReadOnlyList<CategoriaFornecedor>> ListarAtivasAsync(CancellationToken cancellationToken = default) =>
        await context.CategoriasFornecedor
            .AsNoTracking()
            .Where(categoria => categoria.Ativo)
            .ToListAsync(cancellationToken);
}
