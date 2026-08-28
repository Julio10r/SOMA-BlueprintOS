using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Identity;

public sealed class IdentityProviderRepository(BlueprintOSDbContext db) : IIdentityProviderRepository
{
    public async Task<IReadOnlyList<IdentityProvider>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
        await db.IdentityProviders.Where(x => x.UnidadeNegocioId == unidadeNegocioId).OrderBy(x => x.Tipo).ToListAsync(ct);

    public Task<IdentityProvider?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct) =>
        db.IdentityProviders.SingleOrDefaultAsync(x => x.Id == id && x.UnidadeNegocioId == unidadeNegocioId, ct);

    public Task AdicionarAsync(IdentityProvider identityProvider, CancellationToken ct)
    {
        db.IdentityProviders.Add(identityProvider);
        return Task.CompletedTask;
    }

    public Task SalvarAlteracoesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
