using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Identity;

public sealed class FilialMetadadoRepository(BlueprintOSDbContext db) : IFilialMetadadoRepository
{
    public async Task<IReadOnlyDictionary<string, FilialMetadado>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct)
    {
        var registros = await db.FiliaisMetadados
            .Where(x => x.UnidadeNegocioId == unidadeNegocioId)
            .ToListAsync(ct);
        return registros.ToDictionary(x => x.CodigoErp, StringComparer.OrdinalIgnoreCase);
    }

    public Task<FilialMetadado?> ObterPorCodigoErpAsync(string codigoErp, Guid unidadeNegocioId, CancellationToken ct) =>
        db.FiliaisMetadados.SingleOrDefaultAsync(x => x.CodigoErp == codigoErp && x.UnidadeNegocioId == unidadeNegocioId, ct);

    public Task AdicionarAsync(FilialMetadado metadado, CancellationToken ct)
    {
        db.FiliaisMetadados.Add(metadado);
        return Task.CompletedTask;
    }

    public Task SalvarAlteracoesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
