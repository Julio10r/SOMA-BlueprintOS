namespace BlueprintOS.Domain.Identity;

/// <summary>Usuário do +Compras. Criado originalmente pelo Bootstrap (O1.4.3.2); a Gestão de Usuários
/// (O1.6) acrescenta <see cref="TodosCentrosCusto"/>, <see cref="CriadoEm"/>/<see cref="AtualizadoEm"/> e
/// os comportamentos de edição/ativação, seguindo o mesmo padrão físico já usado em <c>Perfil</c> (O1.5).
///
/// Não existe exclusão física: o mesmo padrão de Perfis/Filiais/Centros de Custo/Unidades de Alocação —
/// apenas Ativo/Inativo.</summary>
public sealed class Usuario
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string Nome { get; private set; }
    public Guid UnidadeNegocioId { get; private set; }
    public StatusUsuario Status { get; private set; }

    /// <summary>Quando verdadeiro, o usuário tem acesso a todos os Centros de Custo ativos da Unidade de
    /// Negócio, independentemente do que estiver vinculado em <c>UsuariosCentrosCusto</c> — vínculos
    /// explícitos são ignorados enquanto esta flag estiver ativa (O1.6, escopo declarado da Work Order).</summary>
    public bool TodosCentrosCusto { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    private Usuario() { Email = string.Empty; Nome = string.Empty; }

    public Usuario(string email, string nome, Guid unidadeNegocioId, bool todosCentrosCusto = false, DateTimeOffset? agora = null)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("E-mail é obrigatório.", nameof(email));
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome é obrigatório.", nameof(nome));

        var momento = agora ?? DateTimeOffset.UtcNow;

        Id = Guid.NewGuid();
        Email = email.Trim().ToLowerInvariant();
        Nome = nome.Trim();
        UnidadeNegocioId = unidadeNegocioId;
        Status = StatusUsuario.Ativo;
        TodosCentrosCusto = todosCentrosCusto;
        CriadoEm = momento;
        AtualizadoEm = momento;
    }

    public bool EstaAtivo() => Status == StatusUsuario.Ativo;

    public void Atualizar(string nome, bool todosCentrosCusto, DateTimeOffset agora)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome é obrigatório.", nameof(nome));
        Nome = nome.Trim();
        TodosCentrosCusto = todosCentrosCusto;
        AtualizadoEm = agora;
    }

    public void Ativar(DateTimeOffset agora)
    {
        if (Status == StatusUsuario.Ativo) return;
        Status = StatusUsuario.Ativo;
        AtualizadoEm = agora;
    }

    /// <summary>Inativação lógica — revoga o acesso do usuário. A invariante do último Administrador
    /// Sênior ativo (<see cref="AdministradorSeniorInvariantService"/>) deve ser verificada pelo caso de
    /// uso ANTES de chamar este método.</summary>
    public void Inativar(DateTimeOffset agora)
    {
        if (Status == StatusUsuario.Inativo) return;
        Status = StatusUsuario.Inativo;
        AtualizadoEm = agora;
    }

    public void RegistrarAlteracaoDeVinculos(DateTimeOffset agora) => AtualizadoEm = agora;
}
