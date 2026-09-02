using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Identity;

public sealed class ContaContabilMetadadoRepository(BlueprintOSDbContext db) : IContaContabilMetadadoRepository
{
    public async Task<IReadOnlyDictionary<string, ContaContabilMetadado>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct)
    {
        var registros = await db.ContasContabeisMetadados
            .Where(x => x.UnidadeNegocioId == unidadeNegocioId)
            .ToListAsync(ct);
        return registros.ToDictionary(x => x.CodigoErp, StringComparer.OrdinalIgnoreCase);
    }

    public Task<ContaContabilMetadado?> ObterPorCodigoErpAsync(string codigoErp, Guid unidadeNegocioId, CancellationToken ct) =>
        db.ContasContabeisMetadados.SingleOrDefaultAsync(x => x.CodigoErp == codigoErp && x.UnidadeNegocioId == unidadeNegocioId, ct);

    public Task AdicionarAsync(ContaContabilMetadado metadado, CancellationToken ct)
    {
        db.ContasContabeisMetadados.Add(metadado);
        return Task.CompletedTask;
    }

    public Task SalvarAlteracoesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
