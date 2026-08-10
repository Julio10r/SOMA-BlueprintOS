namespace BlueprintOS.Domain.Identity;

public sealed class UnidadeNegocio
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public string Slug { get; private set; }
    public bool Ativa { get; private set; }

    private UnidadeNegocio() { Nome = string.Empty; Slug = string.Empty; }

    public UnidadeNegocio(string nome, string slug)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome da Unidade de Negócio é obrigatório.", nameof(nome));
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug da Unidade de Negócio é obrigatório.", nameof(slug));

        Id = Guid.NewGuid();
        Nome = nome;
        Slug = slug.Trim().ToLowerInvariant();
        Ativa = true;
    }
}
