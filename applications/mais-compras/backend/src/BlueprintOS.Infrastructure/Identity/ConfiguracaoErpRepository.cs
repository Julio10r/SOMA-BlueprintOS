using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Identity;

public sealed class ConfiguracaoErpRepository(BlueprintOSDbContext db) : IConfiguracaoErpRepository
{
    public Task<ConfiguracaoErp?> ObterPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
        db.ConfiguracoesErp.SingleOrDefaultAsync(x => x.UnidadeNegocioId == unidadeNegocioId, ct);

    public Task AdicionarAsync(ConfiguracaoErp configuracaoErp, CancellationToken ct)
    {
        db.ConfiguracoesErp.Add(configuracaoErp);
        return Task.CompletedTask;
    }

    public Task SalvarAlteracoesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
