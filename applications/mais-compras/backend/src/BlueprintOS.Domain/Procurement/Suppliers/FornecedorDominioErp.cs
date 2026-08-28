namespace BlueprintOS.Domain.Procurement.Suppliers;

public sealed class FornecedorDominioErp
{
    private FornecedorDominioErp() { }

    public FornecedorDominioErp(Guid id, string tipo, string codigoErp, string descricao, string businessUnit,
        string erpSistema, string status, DateTimeOffset ultimaSincronizacaoEm)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id is required.", nameof(id));
        if (string.IsNullOrWhiteSpace(tipo)) throw new ArgumentException("Tipo is required.", nameof(tipo));
        if (string.IsNullOrWhiteSpace(codigoErp)) throw new ArgumentException("CodigoERP is required.", nameof(codigoErp));
        if (string.IsNullOrWhiteSpace(descricao)) throw new ArgumentException("Descricao is required.", nameof(descricao));
        if (string.IsNullOrWhiteSpace(businessUnit)) throw new ArgumentException("BusinessUnit is required.", nameof(businessUnit));
        if (string.IsNullOrWhiteSpace(erpSistema)) throw new ArgumentException("ErpSistema is required.", nameof(erpSistema));
        Id = id; Tipo = tipo.Trim(); CodigoERP = codigoErp.Trim(); Descricao = descricao.Trim();
        BusinessUnit = businessUnit.Trim(); ErpSistema = erpSistema.Trim();
        Status = string.IsNullOrWhiteSpace(status) ? "Ativo" : status.Trim();
        UltimaSincronizacaoEm = ultimaSincronizacaoEm;
        CreatedAt = ultimaSincronizacaoEm; UpdatedAt = ultimaSincronizacaoEm;
    }

    public Guid Id { get; private set; }
    public string Tipo { get; private set; } = null!;
    public string CodigoERP { get; private set; } = null!;
    public string Descricao { get; private set; } = null!;
    public string BusinessUnit { get; private set; } = null!;
    public string ErpSistema { get; private set; } = null!;
    public string Status { get; private set; } = "Ativo";
    public DateTimeOffset UltimaSincronizacaoEm { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Atualizar(string descricao, string status, DateTimeOffset sincronizadoEm)
    {
        if (string.IsNullOrWhiteSpace(descricao)) throw new ArgumentException("Descricao is required.", nameof(descricao));
        Descricao = descricao.Trim(); Status = string.IsNullOrWhiteSpace(status) ? "Ativo" : status.Trim();
        UltimaSincronizacaoEm = sincronizadoEm; UpdatedAt = sincronizadoEm;
    }
}
