using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Identity;

public sealed class OtpRequestThrottleRepository(BlueprintOSDbContext db) : IOtpRequestThrottleRepository
{
    public Task<OtpRequestThrottle?> ObterPorEmailAsync(string emailNormalizado, CancellationToken ct) =>
        db.OtpRequestThrottles.SingleOrDefaultAsync(x => x.EmailNormalizado == emailNormalizado, ct);

    public Task AdicionarAsync(OtpRequestThrottle throttle, CancellationToken ct)
    {
        db.OtpRequestThrottles.Add(throttle);
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
            throw new ConcurrencyConflictException("O contador de throttle foi modificado por outra requisição concorrente.");
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new DuplicateRecordException("Já existe um registro de throttle para este e-mail.");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true;
}
