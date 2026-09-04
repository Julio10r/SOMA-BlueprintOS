using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Identity;

public sealed class ItemFiscalReferenciaFornecedorRepository(BlueprintOSDbContext db) : IItemFiscalReferenciaFornecedorRepository
{
    public async Task<IReadOnlyList<ItemFiscalReferenciaFornecedor>> ListarPorItemFiscalAsync(Guid itemFiscalId, CancellationToken ct) =>
        await db.ItensFiscaisReferenciasFornecedor
            .Where(x => x.ItemFiscalId == itemFiscalId)
            .OrderBy(x => x.CriadoEm)
            .ToListAsync(ct);

    public Task<ItemFiscalReferenciaFornecedor?> ObterPorIdAsync(Guid id, Guid itemFiscalId, CancellationToken ct) =>
        db.ItensFiscaisReferenciasFornecedor.SingleOrDefaultAsync(x => x.Id == id && x.ItemFiscalId == itemFiscalId, ct);

    public Task<ItemFiscalReferenciaFornecedor?> ObterPorItemEFornecedorAsync(Guid itemFiscalId, Guid fornecedorId, CancellationToken ct) =>
        db.ItensFiscaisReferenciasFornecedor.SingleOrDefaultAsync(x => x.ItemFiscalId == itemFiscalId && x.FornecedorId == fornecedorId, ct);

    public Task<bool> ExisteParaFornecedorNoItemAsync(Guid itemFiscalId, Guid fornecedorId, Guid? excluirId, CancellationToken ct)
    {
        var query = db.ItensFiscaisReferenciasFornecedor
            .Where(x => x.ItemFiscalId == itemFiscalId && x.FornecedorId == fornecedorId);
        if (excluirId is not null)
        {
            query = query.Where(x => x.Id != excluirId.Value);
        }
        return query.AnyAsync(ct);
    }

    public Task<bool> ExisteCodigoParaFornecedorAsync(Guid fornecedorId, string codigoItemFornecedor, Guid? excluirId, CancellationToken ct)
    {
        var codigoNormalizado = codigoItemFornecedor.ToLower();
        var query = db.ItensFiscaisReferenciasFornecedor
            .Where(x => x.FornecedorId == fornecedorId && x.CodigoItemFornecedor.ToLower() == codigoNormalizado);
        if (excluirId is not null)
        {
            query = query.Where(x => x.Id != excluirId.Value);
        }
        return query.AnyAsync(ct);
    }

    public Task AdicionarAsync(ItemFiscalReferenciaFornecedor referencia, CancellationToken ct)
    {
        db.ItensFiscaisReferenciasFornecedor.Add(referencia);
        return Task.CompletedTask;
    }

    public Task RemoverAsync(ItemFiscalReferenciaFornecedor referencia, CancellationToken ct)
    {
        db.ItensFiscaisReferenciasFornecedor.Remove(referencia);
        return Task.CompletedTask;
    }

    public Task SalvarAlteracoesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
