namespace BlueprintOS.Domain.Identity;

public sealed class Usuario
{
    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string Nome { get; private set; }
    public Guid UnidadeNegocioId { get; private set; }
    public StatusUsuario Status { get; private set; }

    private Usuario() { Email = string.Empty; Nome = string.Empty; }

    public Usuario(string email, string nome, Guid unidadeNegocioId)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("E-mail é obrigatório.", nameof(email));
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome é obrigatório.", nameof(nome));

        Id = Guid.NewGuid();
        Email = email.Trim().ToLowerInvariant();
        Nome = nome;
        UnidadeNegocioId = unidadeNegocioId;
        Status = StatusUsuario.Ativo;
    }

    public bool EstaAtivo() => Status == StatusUsuario.Ativo;
}
