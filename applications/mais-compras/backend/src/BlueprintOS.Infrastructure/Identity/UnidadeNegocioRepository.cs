using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Identity;

public sealed class UnidadeNegocioRepository(BlueprintOSDbContext db) : IUnidadeNegocioRepository
{
    public Task<UnidadeNegocio?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.UnidadesNegocio.SingleOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> PossuiAdministradorSeniorAtivoAsync(Guid unidadeNegocioId, CancellationToken ct) =>
        (from usuario in db.Usuarios
         join vinculo in db.UsuariosPerfis on usuario.Id equals vinculo.UsuarioId
         join perfil in db.Perfis on vinculo.PerfilId equals perfil.Id
         where perfil.UnidadeNegocioId == unidadeNegocioId
               && perfil.Nome == Perfil.AdministradorSenior
               && usuario.Status == StatusUsuario.Ativo
         select usuario.Id).AnyAsync(ct);

    public Task AdicionarAsync(UnidadeNegocio unidadeNegocio, CancellationToken ct)
    {
        db.UnidadesNegocio.Add(unidadeNegocio);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<UnidadeNegocio>> ListarTodasAsync(CancellationToken ct) =>
        await db.UnidadesNegocio.OrderBy(x => x.Nome).ToListAsync(ct);

    public Task<bool> ExisteComSlugAsync(string slug, Guid? excluirId, CancellationToken ct) =>
        db.UnidadesNegocio.AnyAsync(x => x.Slug == slug && (excluirId == null || x.Id != excluirId), ct);

    public Task SalvarAlteracoesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
