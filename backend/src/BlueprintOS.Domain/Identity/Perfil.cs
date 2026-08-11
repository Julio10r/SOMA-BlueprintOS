namespace BlueprintOS.Domain.Identity;

/// <summary>Perfil de RBAC, configurado por Unidade de Negócio (ADR-0020, item 8/9/10). Suas permissões
/// efetivas são a união das <see cref="Permissao"/> vinculadas; usuários nunca recebem permissão direta.
///
/// O1.5 (RBAC Real) acrescentou <see cref="Descricao"/>, <see cref="CriadoEm"/>/<see cref="AtualizadoEm"/>
/// e os comportamentos de edição/ativação. Não existe exclusão física: `ComprasFuncional.md`
/// ("Gestão de Perfis") lista como ações oficiais apenas Criar, Editar e Ativar/Inativar — o mesmo padrão
/// já adotado nos outros módulos administrativos.</summary>
public sealed class Perfil
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public string Descricao { get; private set; }
    public Guid UnidadeNegocioId { get; private set; }
    public bool Ativo { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    private Perfil() { Nome = string.Empty; Descricao = string.Empty; }

    public Perfil(string nome, string descricao, Guid unidadeNegocioId, DateTimeOffset agora)
    {
        Id = Guid.NewGuid();
        Nome = NormalizarNome(nome);
        Descricao = (descricao ?? string.Empty).Trim();
        UnidadeNegocioId = unidadeNegocioId;
        Ativo = true;
        CriadoEm = agora;
        AtualizadoEm = agora;
    }

    public void Atualizar(string nome, string descricao, DateTimeOffset agora)
    {
        Nome = NormalizarNome(nome);
        Descricao = (descricao ?? string.Empty).Trim();
        AtualizadoEm = agora;
    }

    public void Ativar(DateTimeOffset agora)
    {
        if (Ativo) return;
        Ativo = true;
        AtualizadoEm = agora;
    }

    /// <summary>Inativação lógica. Um Perfil inativo nunca contribui permissões efetivas
    /// (ver <c>PermissoesEfetivasResolver</c>) — é o mecanismo de revogação de acesso em massa.</summary>
    public void Inativar(DateTimeOffset agora)
    {
        if (!Ativo) return;
        Ativo = false;
        AtualizadoEm = agora;
    }

    /// <summary>Marca a alteração do conjunto de permissões vinculadas, que vive em
    /// <c>PerfisPermissoes</c> e por isso não é um campo desta entidade.</summary>
    public void RegistrarAlteracaoDePermissoes(DateTimeOffset agora) => AtualizadoEm = agora;

    private static string NormalizarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome do Perfil é obrigatório.", nameof(nome));
        return nome.Trim();
    }

    /// <summary>Perfil especial de plataforma, criado pelo Bootstrap (O1.4.3.2) e reaproveitado pelo
    /// índice único (<c>UnidadeNegocioId</c>, <c>Nome</c>).</summary>
    public const string AdministradorSenior = "Administrador Sênior";
}
