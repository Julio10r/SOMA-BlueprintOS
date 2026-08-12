using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Persistence.Repositories;

/// <summary>O1.13 — Leitura das execuções em lote de sincronização de fornecedores já persistidas por
/// B2.1.3 (<see cref="BlueprintOSDbContext.SincronizacoesFornecedores"/>). Nenhuma escrita.</summary>
public sealed class SincronizacaoFornecedorMonitorRepository(BlueprintOSDbContext db) : ISincronizacaoFornecedorMonitorRepository
{
    public async Task<(IReadOnlyList<SincronizacaoFornecedor> Itens, int TotalRegistros)> ListarAsync(
        Guid unidadeNegocioId, ListarSincronizacoesFornecedoresFiltro filtro, CancellationToken ct)
    {
        var query = db.SincronizacoesFornecedores.Where(x => x.UnidadeNegocioId == unidadeNegocioId);

        if (!string.IsNullOrWhiteSpace(filtro.Status))
        {
            query = query.Where(x => x.Status == filtro.Status.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filtro.BusinessUnit))
        {
            query = query.Where(x => x.BusinessUnit == filtro.BusinessUnit.Trim());
        }

        var total = await query.CountAsync(ct);
        var itens = await query
            .OrderByDescending(x => x.DataInicio)
            .Skip((filtro.Pagina - 1) * filtro.TamanhoPagina)
            .Take(filtro.TamanhoPagina)
            .ToListAsync(ct);

        return (itens, total);
    }

    public Task<SincronizacaoFornecedor?> ObterPorIdComErrosAsync(Guid unidadeNegocioId, Guid id, CancellationToken ct) =>
        db.SincronizacoesFornecedores.Include(x => x.Erros)
            .SingleOrDefaultAsync(x => x.Id == id && x.UnidadeNegocioId == unidadeNegocioId, ct);
}
