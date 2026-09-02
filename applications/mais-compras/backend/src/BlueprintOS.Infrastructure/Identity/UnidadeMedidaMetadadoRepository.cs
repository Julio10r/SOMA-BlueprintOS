using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Identity;

public sealed class UnidadeMedidaMetadadoRepository(BlueprintOSDbContext db) : IUnidadeMedidaMetadadoRepository
{
    public async Task<IReadOnlyDictionary<string, UnidadeMedidaMetadado>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct)
    {
        var registros = await db.UnidadesMedidaMetadados
            .Where(x => x.UnidadeNegocioId == unidadeNegocioId)
            .ToListAsync(ct);
        return registros.ToDictionary(x => x.CodigoErp, StringComparer.OrdinalIgnoreCase);
    }

    public Task<UnidadeMedidaMetadado?> ObterPorCodigoErpAsync(string codigoErp, Guid unidadeNegocioId, CancellationToken ct) =>
        db.UnidadesMedidaMetadados.SingleOrDefaultAsync(x => x.CodigoErp == codigoErp && x.UnidadeNegocioId == unidadeNegocioId, ct);

    public Task AdicionarAsync(UnidadeMedidaMetadado metadado, CancellationToken ct)
    {
        db.UnidadesMedidaMetadados.Add(metadado);
        return Task.CompletedTask;
    }

    public Task SalvarAlteracoesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
