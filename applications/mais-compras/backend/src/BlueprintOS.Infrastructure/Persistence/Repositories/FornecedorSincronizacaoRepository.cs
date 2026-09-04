using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Persistence.Repositories;

public sealed class FornecedorSincronizacaoRepository(
    BlueprintOSDbContext context) : IFornecedorSincronizacaoRepository
{
    public Task<Fornecedor?> ObterPorChaveErpAsync(
        string businessUnit,
        string erpSistema,
        string erpFornecedorId,
        CancellationToken cancellationToken = default)
    {
        return context.Fornecedores
            .FirstOrDefaultAsync(
                fornecedor =>
                    fornecedor.BusinessUnit == businessUnit &&
                    fornecedor.ErpSistema == erpSistema &&
                    fornecedor.ErpFornecedorId == erpFornecedorId,
                cancellationToken);
    }

    public async Task AdicionarAsync(
        FornecedorSincronizacao sincronizacao,
        CancellationToken cancellationToken = default)
    {
        await context.FornecedoresSincronizacoes
            .AddAsync(sincronizacao, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FornecedorSincronizacao>> ListarPorFornecedorAsync(
        Guid fornecedorId,
        CancellationToken cancellationToken = default)
    {
        return await context.FornecedoresSincronizacoes
            .AsNoTracking()
            .Where(sincronizacao => sincronizacao.FornecedorId == fornecedorId)
            .OrderByDescending(sincronizacao => sincronizacao.ExecutadaEm)
            .ToListAsync(cancellationToken);
    }
}