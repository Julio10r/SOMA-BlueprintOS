namespace BlueprintOS.Domain.Identity;

public sealed class PerfilPermissao
{
    public Guid PerfilId { get; private set; }
    public Guid PermissaoId { get; private set; }

    private PerfilPermissao() { }

    public PerfilPermissao(Guid perfilId, Guid permissaoId)
    {
        PerfilId = perfilId;
        PermissaoId = permissaoId;
    }
}
