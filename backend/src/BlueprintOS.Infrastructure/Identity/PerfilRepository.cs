using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Identity;

public sealed class PerfilRepository(BlueprintOSDbContext db) : IPerfilRepository
{
    public Task<Perfil?> ObterPorNomeEUnidadeNegocioAsync(string nome, Guid unidadeNegocioId, CancellationToken ct) =>
        db.Perfis.SingleOrDefaultAsync(x => x.Nome == nome && x.UnidadeNegocioId == unidadeNegocioId, ct);

    public Task AdicionarAsync(Perfil perfil, CancellationToken ct)
    {
        db.Perfis.Add(perfil);
        return Task.CompletedTask;
    }
}
