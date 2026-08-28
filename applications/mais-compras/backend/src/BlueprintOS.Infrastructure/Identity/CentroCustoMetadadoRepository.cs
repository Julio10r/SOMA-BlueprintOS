using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Identity;

public sealed class CentroCustoMetadadoRepository(BlueprintOSDbContext db) : ICentroCustoMetadadoRepository
{
    public async Task<IReadOnlyDictionary<string, CentroCustoMetadado>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct)
    {
        var registros = await db.CentrosCustoMetadados
            .Where(x => x.UnidadeNegocioId == unidadeNegocioId)
            .ToListAsync(ct);
        return registros.ToDictionary(x => x.CodigoErp, StringComparer.OrdinalIgnoreCase);
    }

    public Task<CentroCustoMetadado?> ObterPorCodigoErpAsync(string codigoErp, Guid unidadeNegocioId, CancellationToken ct) =>
        db.CentrosCustoMetadados.SingleOrDefaultAsync(x => x.CodigoErp == codigoErp && x.UnidadeNegocioId == unidadeNegocioId, ct);

    public Task<CentroCustoMetadado?> ObterPorCodigoErpGlobalAsync(string codigoErp, CancellationToken ct) =>
        db.CentrosCustoMetadados.FirstOrDefaultAsync(x => x.CodigoErp == codigoErp, ct);

    public Task<CentroCustoMetadado?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct) =>
        db.CentrosCustoMetadados.SingleOrDefaultAsync(x => x.Id == id && x.UnidadeNegocioId == unidadeNegocioId, ct);

    public Task AdicionarAsync(CentroCustoMetadado metadado, CancellationToken ct)
    {
        db.CentrosCustoMetadados.Add(metadado);
        return Task.CompletedTask;
    }

    public async Task SalvarAlteracoesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new DuplicateRecordException("Já existe um Centro de Custo ancorado com este código ERP (possível corrida entre requisições concorrentes).");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true;
}
