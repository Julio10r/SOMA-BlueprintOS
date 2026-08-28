using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Persistence;

namespace BlueprintOS.Infrastructure.Identity;

public sealed class UsuarioPerfilRepository(BlueprintOSDbContext db) : IUsuarioPerfilRepository
{
    public Task AdicionarAsync(UsuarioPerfil vinculo, CancellationToken ct)
    {
        db.UsuariosPerfis.Add(vinculo);
        return Task.CompletedTask;
    }
}
