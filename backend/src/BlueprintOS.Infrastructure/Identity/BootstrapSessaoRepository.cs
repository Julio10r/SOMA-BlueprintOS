using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Identity;

public sealed class BootstrapSessaoRepository(BlueprintOSDbContext db) : IBootstrapSessaoRepository
{
    public Task<BootstrapSessao?> ObterPorIdentificadorHashAsync(string identificadorHash, CancellationToken ct) =>
        db.BootstrapSessoes.SingleOrDefaultAsync(x => x.IdentificadorHash == identificadorHash, ct);

    public Task<BootstrapSessao?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.BootstrapSessoes.SingleOrDefaultAsync(x => x.Id == id, ct);

    public Task<BootstrapSessao?> ObterAtivaPorEmailCandidatoAsync(string emailCandidato, CancellationToken ct) =>
        db.BootstrapSessoes
            .Where(x => x.EmailCandidato == emailCandidato && x.UsadaEm == null && x.RevokedAt == null)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public Task AdicionarAsync(BootstrapSessao sessao, CancellationToken ct)
    {
        db.BootstrapSessoes.Add(sessao);
        return Task.CompletedTask;
    }

    public Task AtualizarAsync(BootstrapSessao sessao, CancellationToken ct)
    {
        db.BootstrapSessoes.Update(sessao);
        return Task.CompletedTask;
    }

    public Task SalvarAlteracoesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
