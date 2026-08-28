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
        Nome = nome.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Ativa = true;
    }

    /// <summary>O1.11 — Cadastro de Unidades de Negócio. O <see cref="Slug"/> é imutável após a criação
    /// (regra de negócio explícita da spec funcional) — apenas o <see cref="Nome"/> pode ser alterado.</summary>
    public void Renomear(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome da Unidade de Negócio é obrigatório.", nameof(nome));
        Nome = nome.Trim();
    }

    /// <summary>Nunca há exclusão física de Unidade de Negócio (preserva histórico) — apenas
    /// Ativo/Inativo, mesmo padrão de Perfis/Usuários/Unidades de Alocação.</summary>
    public void Ativar() => Ativa = true;

    public void Inativar() => Ativa = false;
}
