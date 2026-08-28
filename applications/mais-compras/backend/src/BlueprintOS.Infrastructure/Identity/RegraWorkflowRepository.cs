using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Identity;

public sealed class RegraWorkflowRepository(BlueprintOSDbContext db) : IRegraWorkflowRepository
{
    public async Task<IReadOnlyList<RegraWorkflow>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
        await db.RegrasWorkflow
            .Where(x => x.UnidadeNegocioId == unidadeNegocioId)
            .OrderBy(x => x.TipoProcesso).ThenBy(x => x.Ordem)
            .ToListAsync(ct);

    public Task<RegraWorkflow?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct) =>
        db.RegrasWorkflow.SingleOrDefaultAsync(x => x.Id == id && x.UnidadeNegocioId == unidadeNegocioId, ct);

    public Task AdicionarAsync(RegraWorkflow regraWorkflow, CancellationToken ct)
    {
        db.RegrasWorkflow.Add(regraWorkflow);
        return Task.CompletedTask;
    }

    public Task SalvarAlteracoesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
