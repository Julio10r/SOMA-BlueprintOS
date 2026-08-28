using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Identity;

public sealed class ParametroRepository(BlueprintOSDbContext db) : IParametroRepository
{
    public async Task<IReadOnlyList<Parametro>> ListarAsync(Guid? unidadeNegocioId, CancellationToken ct)
    {
        var query = db.Parametros.AsQueryable();
        if (unidadeNegocioId is { } id) query = query.Where(x => x.UnidadeNegocioId == id);
        return await query.OrderBy(x => x.Chave).ToListAsync(ct);
    }

    public Task<Parametro?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.Parametros.SingleOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> ExisteComChaveAsync(string chave, Guid? unidadeNegocioId, Guid? excluirId, CancellationToken ct) =>
        db.Parametros.AnyAsync(x =>
            x.Chave == chave && x.UnidadeNegocioId == unidadeNegocioId && (excluirId == null || x.Id != excluirId), ct);

    public Task AdicionarAsync(Parametro parametro, CancellationToken ct)
    {
        db.Parametros.Add(parametro);
        return Task.CompletedTask;
    }

    public Task RemoverAsync(Parametro parametro, CancellationToken ct)
    {
        db.Parametros.Remove(parametro);
        return Task.CompletedTask;
    }

    public Task SalvarAlteracoesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
