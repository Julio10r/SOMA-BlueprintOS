using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.Infrastructure.Persistence.Repositories;

public sealed class FornecedorCnpjConsultaHistoricoRepository(BlueprintOSDbContext context) : IFornecedorCnpjConsultaHistoricoRepository
{
    public async Task AdicionarAsync(FornecedorCnpjConsultaHistorico consulta, CancellationToken cancellationToken = default)
    {
        await context.Set<FornecedorCnpjConsultaHistorico>().AddAsync(consulta, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
