using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Identity;

public sealed class BootstrapEstadoRepository(BlueprintOSDbContext db) : IBootstrapEstadoRepository
{
    // Sempre filtra explicitamente pela chave fixa (Work Order O1.4.3, seção 12) — nunca
    // SingleOrDefaultAsync()/FirstOrDefaultAsync() sem filtro por Id.
    public Task<BootstrapEstado?> ObterAsync(CancellationToken ct) =>
        db.BootstrapEstados.SingleOrDefaultAsync(x => x.Id == BootstrapEstado.IdFixo, ct);

    public Task AtualizarAsync(BootstrapEstado estado, CancellationToken ct)
    {
        db.BootstrapEstados.Update(estado);
        return Task.CompletedTask;
    }

    public async Task SalvarAlteracoesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException("O Bootstrap foi concluído por outra requisição concorrente.");
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new DuplicateRecordException("Já existe um registro equivalente criado por outra requisição concorrente.");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("IX_Perfis_UnidadeNegocioId_Nome", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("IX_UnidadesNegocio_Slug", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("IX_Usuarios_Email", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true;
}
