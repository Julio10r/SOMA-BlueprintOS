using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Identity;

public sealed class ItemFiscalRepository(BlueprintOSDbContext db) : IItemFiscalRepository
{
    public async Task<IReadOnlyList<ItemFiscal>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
        await db.ItensFiscais
            .Where(x => x.UnidadeNegocioId == unidadeNegocioId)
            .OrderBy(x => x.Codigo)
            .ToListAsync(ct);

    public Task<ItemFiscal?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct) =>
        db.ItensFiscais.SingleOrDefaultAsync(x => x.Id == id && x.UnidadeNegocioId == unidadeNegocioId, ct);

    public Task<bool> ExisteComCodigoAsync(string codigo, Guid? excluirId, CancellationToken ct)
    {
        var codigoNormalizado = codigo.ToLower();
        var query = db.ItensFiscais.Where(x => x.Codigo.ToLower() == codigoNormalizado);
        if (excluirId is not null)
        {
            query = query.Where(x => x.Id != excluirId.Value);
        }
        return query.AnyAsync(ct);
    }

    public Task AdicionarAsync(ItemFiscal itemFiscal, CancellationToken ct)
    {
        db.ItensFiscais.Add(itemFiscal);
        return Task.CompletedTask;
    }

    public Task SalvarAlteracoesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
