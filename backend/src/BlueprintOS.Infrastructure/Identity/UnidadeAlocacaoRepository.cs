using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Identity;

public sealed class UnidadeAlocacaoRepository(BlueprintOSDbContext db) : IUnidadeAlocacaoRepository
{
    public async Task<IReadOnlyList<UnidadeAlocacao>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
        await db.UnidadesAlocacao
            .Where(x => x.UnidadeNegocioId == unidadeNegocioId)
            .OrderBy(x => x.Nome)
            .ToListAsync(ct);

    public Task<UnidadeAlocacao?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct) =>
        db.UnidadesAlocacao.SingleOrDefaultAsync(x => x.Id == id && x.UnidadeNegocioId == unidadeNegocioId, ct);

    public async Task<IReadOnlyList<UnidadeAlocacao>> ObterPorIdsEUnidadeNegocioAsync(
        IReadOnlyCollection<Guid> ids, Guid unidadeNegocioId, CancellationToken ct)
    {
        if (ids.Count == 0) return [];
        return await db.UnidadesAlocacao
            .Where(x => ids.Contains(x.Id) && x.UnidadeNegocioId == unidadeNegocioId)
            .ToListAsync(ct);
    }

    public Task<bool> ExisteComNomeAsync(string nome, Guid unidadeNegocioId, Guid? excluirId, CancellationToken ct)
    {
        var nomeNormalizado = nome.ToLower();
        var query = db.UnidadesAlocacao.Where(x =>
            x.UnidadeNegocioId == unidadeNegocioId && x.Nome.ToLower() == nomeNormalizado);
        if (excluirId is not null)
        {
            query = query.Where(x => x.Id != excluirId.Value);
        }
        return query.AnyAsync(ct);
    }

    public Task AdicionarAsync(UnidadeAlocacao unidadeAlocacao, CancellationToken ct)
    {
        db.UnidadesAlocacao.Add(unidadeAlocacao);
        return Task.CompletedTask;
    }

    public Task SalvarAlteracoesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
