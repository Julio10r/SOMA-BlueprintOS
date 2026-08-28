using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Identity;

public sealed class RegraOrcamentariaRepository(BlueprintOSDbContext db) : IRegraOrcamentariaRepository
{
    public async Task<IReadOnlyList<RegraOrcamentaria>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
        await db.RegrasOrcamentarias
            .Where(x => x.UnidadeNegocioId == unidadeNegocioId)
            .OrderBy(x => x.Nome)
            .ToListAsync(ct);

    public Task<RegraOrcamentaria?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct) =>
        db.RegrasOrcamentarias.SingleOrDefaultAsync(x => x.Id == id && x.UnidadeNegocioId == unidadeNegocioId, ct);

    public Task AdicionarAsync(RegraOrcamentaria regraOrcamentaria, CancellationToken ct)
    {
        db.RegrasOrcamentarias.Add(regraOrcamentaria);
        return Task.CompletedTask;
    }

    public Task SalvarAlteracoesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
