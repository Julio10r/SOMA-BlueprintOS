using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Identity;

public sealed class ConfiguracaoNotificacaoRepository(BlueprintOSDbContext db) : IConfiguracaoNotificacaoRepository
{
    public Task<ConfiguracaoNotificacao?> ObterPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
        db.ConfiguracoesNotificacao.SingleOrDefaultAsync(x => x.UnidadeNegocioId == unidadeNegocioId, ct);

    public Task AdicionarAsync(ConfiguracaoNotificacao configuracaoNotificacao, CancellationToken ct)
    {
        db.ConfiguracoesNotificacao.Add(configuracaoNotificacao);
        return Task.CompletedTask;
    }

    public Task SalvarAlteracoesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
