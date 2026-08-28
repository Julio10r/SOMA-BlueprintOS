using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Identity;

public sealed class CodigoVerificacaoOtpRepository(BlueprintOSDbContext db) : ICodigoVerificacaoOtpRepository
{
    public Task<CodigoVerificacaoOtp?> ObterPendentePorUsuarioAsync(Guid usuarioId, CancellationToken ct) =>
        db.CodigosVerificacaoOtp
            .Where(x => x.UsuarioId == usuarioId && x.Status == StatusCodigoVerificacaoOtp.Pendente)
            .OrderByDescending(x => x.CriadoEm)
            .FirstOrDefaultAsync(ct);

    public Task<CodigoVerificacaoOtp?> ObterMaisRecentePorUsuarioAsync(Guid usuarioId, CancellationToken ct) =>
        db.CodigosVerificacaoOtp
            .Where(x => x.UsuarioId == usuarioId)
            .OrderByDescending(x => x.CriadoEm)
            .FirstOrDefaultAsync(ct);

    public Task<CodigoVerificacaoOtp?> ObterPendentePorEmailCandidatoAsync(string emailCandidato, CancellationToken ct) =>
        db.CodigosVerificacaoOtp
            .Where(x => x.EmailCandidato == emailCandidato && x.Status == StatusCodigoVerificacaoOtp.Pendente)
            .OrderByDescending(x => x.CriadoEm)
            .FirstOrDefaultAsync(ct);

    public Task AdicionarAsync(CodigoVerificacaoOtp codigo, CancellationToken ct)
    {
        db.CodigosVerificacaoOtp.Add(codigo);
        return Task.CompletedTask;
    }

    public Task AtualizarAsync(CodigoVerificacaoOtp codigo, CancellationToken ct)
    {
        db.CodigosVerificacaoOtp.Update(codigo);
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
            throw new ConcurrencyConflictException("O código OTP foi modificado por outra requisição concorrente.");
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new DuplicateRecordException("Já existe um código OTP pendente para este usuário.");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("IX_CodigosVerificacaoOtp_UsuarioId_Pendente", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("IX_CodigosVerificacaoOtp_EmailCandidato_Pendente", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true;
}
