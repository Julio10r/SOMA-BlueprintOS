using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Identity;

public sealed class SessaoAutenticacaoRepository(BlueprintOSDbContext db) : ISessaoAutenticacaoRepository
{
    public Task<SessaoAutenticacao?> ObterPorIdentificadorHashAsync(string identificadorHash, CancellationToken ct) =>
        db.SessoesAutenticacao.SingleOrDefaultAsync(x => x.IdentificadorHash == identificadorHash, ct);

    public Task AdicionarAsync(SessaoAutenticacao sessao, CancellationToken ct)
    {
        db.SessoesAutenticacao.Add(sessao);
        return Task.CompletedTask;
    }

    public Task AtualizarAsync(SessaoAutenticacao sessao, CancellationToken ct)
    {
        db.SessoesAutenticacao.Update(sessao);
        return Task.CompletedTask;
    }

    public Task SalvarAlteracoesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
