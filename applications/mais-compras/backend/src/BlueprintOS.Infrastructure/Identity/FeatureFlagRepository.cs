using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Identity;

public sealed class FeatureFlagRepository(BlueprintOSDbContext db) : IFeatureFlagRepository
{
    public async Task<IReadOnlyList<FeatureFlag>> ListarAsync(CancellationToken ct) =>
        await db.FeatureFlags.OrderBy(x => x.Nome).ToListAsync(ct);

    public Task<FeatureFlag?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.FeatureFlags.SingleOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> ExisteComNomeAsync(string nome, CancellationToken ct) =>
        db.FeatureFlags.AnyAsync(x => x.Nome == nome, ct);

    public async Task<IReadOnlyList<FeatureFlagUnidadeNegocio>> ListarStatusPorFlagAsync(Guid featureFlagId, CancellationToken ct) =>
        await db.FeatureFlagsUnidadesNegocio.Where(x => x.FeatureFlagId == featureFlagId).ToListAsync(ct);

    public Task<FeatureFlagUnidadeNegocio?> ObterStatusAsync(Guid featureFlagId, Guid unidadeNegocioId, CancellationToken ct) =>
        db.FeatureFlagsUnidadesNegocio.SingleOrDefaultAsync(
            x => x.FeatureFlagId == featureFlagId && x.UnidadeNegocioId == unidadeNegocioId, ct);

    public Task AdicionarAsync(FeatureFlag featureFlag, CancellationToken ct)
    {
        db.FeatureFlags.Add(featureFlag);
        return Task.CompletedTask;
    }

    public Task AdicionarStatusAsync(FeatureFlagUnidadeNegocio status, CancellationToken ct)
    {
        db.FeatureFlagsUnidadesNegocio.Add(status);
        return Task.CompletedTask;
    }

    public Task SalvarAlteracoesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
