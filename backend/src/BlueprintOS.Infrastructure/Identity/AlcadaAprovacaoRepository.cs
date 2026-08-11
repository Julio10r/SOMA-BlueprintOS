using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Identity;

public sealed class AlcadaAprovacaoRepository(BlueprintOSDbContext db) : IAlcadaAprovacaoRepository
{
    public async Task<IReadOnlyList<AlcadaAprovacao>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
        await db.AlcadasAprovacao
            .Where(x => x.UnidadeNegocioId == unidadeNegocioId)
            .OrderBy(x => x.Nivel).ThenBy(x => x.Nome)
            .ToListAsync(ct);

    public Task<AlcadaAprovacao?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct) =>
        db.AlcadasAprovacao.SingleOrDefaultAsync(x => x.Id == id && x.UnidadeNegocioId == unidadeNegocioId, ct);

    public Task AdicionarAsync(AlcadaAprovacao alcadaAprovacao, CancellationToken ct)
    {
        db.AlcadasAprovacao.Add(alcadaAprovacao);
        return Task.CompletedTask;
    }

    public Task SalvarAlteracoesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
