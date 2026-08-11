using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.Infrastructure.Identity;

public sealed class UsuarioRepository(BlueprintOSDbContext db) : IUsuarioRepository
{
    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct) =>
        db.Usuarios.SingleOrDefaultAsync(x => x.Email == email, ct);

    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct) =>
        db.Usuarios.SingleOrDefaultAsync(x => x.Id == id, ct);

    public Task AdicionarAsync(Usuario usuario, CancellationToken ct)
    {
        db.Usuarios.Add(usuario);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Usuario>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct) =>
        await db.Usuarios
            .Where(x => x.UnidadeNegocioId == unidadeNegocioId)
            .OrderBy(x => x.Nome)
            .ToListAsync(ct);

    public Task<Usuario?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct) =>
        db.Usuarios.SingleOrDefaultAsync(x => x.Id == id && x.UnidadeNegocioId == unidadeNegocioId, ct);

    public Task<Usuario?> ObterPorEmailEUnidadeNegocioAsync(string email, Guid unidadeNegocioId, CancellationToken ct) =>
        db.Usuarios.SingleOrDefaultAsync(x => x.Email == email && x.UnidadeNegocioId == unidadeNegocioId, ct);

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<UsuarioPerfilResumoDto>>> ObterPerfisPorUsuarioAsync(
        IReadOnlyCollection<Guid> usuarioIds, CancellationToken ct)
    {
        if (usuarioIds.Count == 0) return new Dictionary<Guid, IReadOnlyList<UsuarioPerfilResumoDto>>();

        var pares = await db.UsuariosPerfis
            .Where(vinculo => usuarioIds.Contains(vinculo.UsuarioId))
            .Join(db.Perfis, vinculo => vinculo.PerfilId, perfil => perfil.Id,
                (vinculo, perfil) => new { vinculo.UsuarioId, perfil.Id, perfil.Nome, perfil.Ativo })
            .ToListAsync(ct);

        return pares
            .GroupBy(x => x.UsuarioId)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => (IReadOnlyList<UsuarioPerfilResumoDto>)grupo
                    .OrderBy(x => x.Nome, StringComparer.Ordinal)
                    .Select(x => new UsuarioPerfilResumoDto(x.Id, x.Nome, x.Ativo))
                    .ToArray());
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> ObterCentrosCustoPorUsuarioAsync(
        IReadOnlyCollection<Guid> usuarioIds, CancellationToken ct)
    {
        if (usuarioIds.Count == 0) return new Dictionary<Guid, IReadOnlyList<string>>();

        var vinculos = await db.UsuariosCentrosCusto
            .Where(x => usuarioIds.Contains(x.UsuarioId))
            .ToListAsync(ct);

        return vinculos
            .GroupBy(x => x.UsuarioId)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => (IReadOnlyList<string>)grupo
                    .Select(x => x.CentroCustoCodigoErp)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToArray());
    }

    public async Task SubstituirPerfisAsync(Guid usuarioId, IReadOnlyCollection<Guid> perfilIds, CancellationToken ct)
    {
        var atuais = await db.UsuariosPerfis.Where(x => x.UsuarioId == usuarioId).ToListAsync(ct);

        var remover = atuais.Where(x => !perfilIds.Contains(x.PerfilId)).ToArray();
        if (remover.Length > 0) db.UsuariosPerfis.RemoveRange(remover);

        var jaVinculados = atuais.Select(x => x.PerfilId).ToHashSet();
        foreach (var perfilId in perfilIds.Where(x => !jaVinculados.Contains(x)))
        {
            db.UsuariosPerfis.Add(new UsuarioPerfil(usuarioId, perfilId));
        }
    }

    public async Task SubstituirCentrosCustoAsync(Guid usuarioId, IReadOnlyCollection<string> codigosErp, CancellationToken ct)
    {
        var atuais = await db.UsuariosCentrosCusto.Where(x => x.UsuarioId == usuarioId).ToListAsync(ct);

        var remover = atuais.Where(x => !codigosErp.Contains(x.CentroCustoCodigoErp, StringComparer.OrdinalIgnoreCase)).ToArray();
        if (remover.Length > 0) db.UsuariosCentrosCusto.RemoveRange(remover);

        var jaVinculados = atuais.Select(x => x.CentroCustoCodigoErp).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var codigo in codigosErp.Where(x => !jaVinculados.Contains(x)))
        {
            db.UsuariosCentrosCusto.Add(new UsuarioCentroCusto(usuarioId, codigo));
        }
    }

    public async Task<int> ContarAdministradoresSeniorAtivosAsync(Guid unidadeNegocioId, Guid? excluirUsuarioId, CancellationToken ct)
    {
        var query = db.UsuariosPerfis
            .Join(db.Perfis.Where(p =>
                    p.UnidadeNegocioId == unidadeNegocioId
                    && p.Ativo
                    && p.Nome == Perfil.AdministradorSenior),
                vinculo => vinculo.PerfilId, perfil => perfil.Id, (vinculo, perfil) => vinculo.UsuarioId)
            .Join(db.Usuarios.Where(u => u.Status == StatusUsuario.Ativo), id => id, usuario => usuario.Id, (id, usuario) => usuario.Id)
            .Distinct();

        if (excluirUsuarioId is not null)
        {
            query = query.Where(id => id != excluirUsuarioId.Value);
        }

        return await query.CountAsync(ct);
    }

    public Task SalvarAlteracoesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
