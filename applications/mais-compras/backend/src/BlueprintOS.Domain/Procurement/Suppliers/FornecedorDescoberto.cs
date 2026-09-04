namespace BlueprintOS.Domain.Procurement.Suppliers;

/// <summary>Resultado persistido de uma descoberta de fornecedor no ERP para um <c>CodigoItem</c>
/// corporativo. Mesmo resíduo arquitetural de "propriedade pessoal" identificado e corrigido em
/// <see cref="Fornecedor"/> (B3 Bloco 5A.9): a descoberta é determinística a partir do item consultado no
/// ERP, não do usuário que a disparou — dois compradores que descobrem fornecedores para o mesmo item
/// devem ver o mesmo resultado, nunca cópias privadas duplicadas.</summary>
public sealed class FornecedorDescoberto
{
    private FornecedorDescoberto() { }

    public FornecedorDescoberto(Guid id, string codigoItem, string? descricao, string? categoria, string nome,
        string? cnpj, string? codigoFornecedor, decimal score, string criterio,
        DateTimeOffset descobertoEm)
    {
        if (string.IsNullOrWhiteSpace(codigoItem)) throw new ArgumentException("Código do item é obrigatório.", nameof(codigoItem));
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome do fornecedor é obrigatório.", nameof(nome));
        if (score is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(score));
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        CodigoItem = codigoItem.Trim(); Descricao = descricao?.Trim(); Categoria = categoria?.Trim(); Nome = nome.Trim();
        Cnpj = cnpj?.Trim(); CodigoFornecedor = codigoFornecedor?.Trim(); Score = score; Criterio = criterio.Trim();
        DescobertoEm = descobertoEm;
    }

    public Guid Id { get; private set; }
    public string CodigoItem { get; private set; } = null!;
    public string? Descricao { get; private set; }
    public string? Categoria { get; private set; }
    public string Nome { get; private set; } = null!;
    public string? Cnpj { get; private set; }
    public string? CodigoFornecedor { get; private set; }
    public decimal Score { get; private set; }
    public string Criterio { get; private set; } = null!;

    /// <summary>[LEGADO/DEPRECADO — B3 Bloco 5A.9] Ver <see cref="Fornecedor.TemporaryUserId"/>. Nunca mais
    /// populado por código novo; mantido apenas por compatibilidade histórica.</summary>
    public Guid? TemporaryUserId { get; private set; }
    public DateTimeOffset DescobertoEm { get; private set; }
}
