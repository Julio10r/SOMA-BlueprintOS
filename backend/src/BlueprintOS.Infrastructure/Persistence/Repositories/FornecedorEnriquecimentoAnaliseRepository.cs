using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Persistence.Repositories;

public sealed class FornecedorEnriquecimentoAnaliseRepository(BlueprintOSDbContext context) : IFornecedorEnriquecimentoAnaliseRepository
{
    public async Task AdicionarAsync(FornecedorEnriquecimentoAnalise analise, CancellationToken cancellationToken = default)
    {
        await context.FornecedoresEnriquecimentoAnalises.AddAsync(analise, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FornecedorEnriquecimentoAnalise>> ListarPorFornecedorAsync(Guid fornecedorId, CancellationToken cancellationToken = default) =>
        await context.FornecedoresEnriquecimentoAnalises.Where(x => x.FornecedorId == fornecedorId)
            .OrderByDescending(x => x.DataHora).ToArrayAsync(cancellationToken);
}
