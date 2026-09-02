namespace BlueprintOS.Domain.Procurement.Suppliers;

/// <summary>Catálogo pré-cadastrado de categorias de Fornecedor do +Compras (Gate de homologação,
/// 2026-09-01) — substitui o campo Categoria antes livre por uma tabela de referência própria do
/// +Compras (não é um domínio sincronizado do ERP, ao contrário de <see cref="FornecedorDominioErp"/>).</summary>
public sealed class CategoriaFornecedor
{
    private CategoriaFornecedor() { }

    public CategoriaFornecedor(Guid id, string codigo, string descricao, bool ativo = true)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(codigo)) throw new ArgumentException("Codigo is required.", nameof(codigo));
        if (string.IsNullOrWhiteSpace(descricao)) throw new ArgumentException("Descricao is required.", nameof(descricao));
        Id = id;
        Codigo = codigo.Trim();
        Descricao = descricao.Trim();
        Ativo = ativo;
    }

    public Guid Id { get; private set; }
    public string Codigo { get; private set; } = null!;
    public string Descricao { get; private set; } = null!;
    public bool Ativo { get; private set; } = true;
}
