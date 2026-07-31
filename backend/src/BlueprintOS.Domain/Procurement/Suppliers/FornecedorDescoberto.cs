namespace BlueprintOS.Domain.Procurement.Suppliers;

/// <summary>Resultado persistido de uma descoberta de fornecedor no ERP.</summary>
public sealed class FornecedorDescoberto
{
    private FornecedorDescoberto() { }

    public FornecedorDescoberto(Guid id, string codigoItem, string? descricao, string? categoria, string nome,
        string? cnpj, string? codigoFornecedor, decimal score, string criterio, Guid temporaryUserId,
        DateTimeOffset descobertoEm)
    {
        if (string.IsNullOrWhiteSpace(codigoItem)) throw new ArgumentException("Código do item é obrigatório.", nameof(codigoItem));
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome do fornecedor é obrigatório.", nameof(nome));
        if (temporaryUserId == Guid.Empty) throw new ArgumentException("TemporaryUserId é obrigatório.", nameof(temporaryUserId));
        if (score is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(score));
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        CodigoItem = codigoItem.Trim(); Descricao = descricao?.Trim(); Categoria = categoria?.Trim(); Nome = nome.Trim();
        Cnpj = cnpj?.Trim(); CodigoFornecedor = codigoFornecedor?.Trim(); Score = score; Criterio = criterio.Trim();
        TemporaryUserId = temporaryUserId; DescobertoEm = descobertoEm;
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
    public Guid TemporaryUserId { get; private set; }
    public DateTimeOffset DescobertoEm { get; private set; }
}
