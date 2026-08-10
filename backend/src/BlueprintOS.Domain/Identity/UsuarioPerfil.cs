namespace BlueprintOS.Domain.Identity;

public sealed class UsuarioPerfil
{
    public Guid UsuarioId { get; private set; }
    public Guid PerfilId { get; private set; }

    private UsuarioPerfil() { }

    public UsuarioPerfil(Guid usuarioId, Guid perfilId)
    {
        UsuarioId = usuarioId;
        PerfilId = perfilId;
    }
}
