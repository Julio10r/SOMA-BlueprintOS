namespace BlueprintOS.Domain.Identity;

/// <summary>Perfil de RBAC, configurado por Unidade de Negócio (ADR-0020, item 8/9/10). Suas permissões
/// efetivas são a união das <see cref="Permissao"/> vinculadas; usuários nunca recebem permissão direta.</summary>
public sealed class Perfil
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public Guid UnidadeNegocioId { get; private set; }
    public bool Ativo { get; private set; }

    private Perfil() { Nome = string.Empty; }

    public Perfil(string nome, Guid unidadeNegocioId)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome do Perfil é obrigatório.", nameof(nome));

        Id = Guid.NewGuid();
        Nome = nome;
        UnidadeNegocioId = unidadeNegocioId;
        Ativo = true;
    }

    /// <summary>Perfil especial de plataforma, usado pelo Bootstrap e por funções administrativas
    /// globais (modelo preparado nesta sprint; Bootstrap completo não implementado — ver O1.4.2).</summary>
    public const string AdministradorSenior = "Administrador Sênior";
}
