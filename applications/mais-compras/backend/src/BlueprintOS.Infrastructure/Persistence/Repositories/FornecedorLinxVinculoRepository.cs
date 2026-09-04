using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Persistence.Repositories;

public sealed class FornecedorLinxVinculoRepository(BlueprintOSDbContext context) : IFornecedorLinxVinculoRepository
{
    public async Task AdicionarAsync(FornecedorLinxVinculo vinculo, CancellationToken cancellationToken = default) =>
        await context.FornecedorLinxVinculos.AddAsync(vinculo, cancellationToken);

    public Task<FornecedorLinxVinculo?> ObterPorErpSistemaECodigoAsync(string erpSistema, string codigoErp, Guid unidadeNegocioId, CancellationToken cancellationToken = default) =>
        context.FornecedorLinxVinculos.SingleOrDefaultAsync(x => x.ErpSistema == erpSistema && x.CodigoErp == codigoErp && x.UnidadeNegocioId == unidadeNegocioId, cancellationToken);

    public Task<FornecedorLinxVinculo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.FornecedorLinxVinculos.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<FornecedorLinxVinculo>> ListarPorFornecedorAsync(Guid fornecedorId, CancellationToken cancellationToken = default) =>
        await context.FornecedorLinxVinculos.Where(x => x.FornecedorId == fornecedorId).ToListAsync(cancellationToken);

    public Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default) => context.SaveChangesAsync(cancellationToken);
}
