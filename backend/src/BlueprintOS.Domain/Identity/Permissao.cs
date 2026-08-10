namespace BlueprintOS.Domain.Identity;

/// <summary>Catálogo global de permissões (ADR-0020, item 8; ratificado como catálogo global em 07/08/2026).</summary>
public sealed class Permissao
{
    public Guid Id { get; private set; }
    public string Codigo { get; private set; }
    public string Descricao { get; private set; }

    private Permissao() { Codigo = string.Empty; Descricao = string.Empty; }

    public Permissao(string codigo, string descricao)
    {
        if (string.IsNullOrWhiteSpace(codigo)) throw new ArgumentException("Código da permissão é obrigatório.", nameof(codigo));

        Id = Guid.NewGuid();
        Codigo = codigo.Trim().ToUpperInvariant();
        Descricao = descricao;
    }
}
