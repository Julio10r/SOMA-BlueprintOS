namespace BlueprintOS.Domain.Procurement.Suppliers;

/// <summary>Aggregate root representing a supplier owned by a procurement user.</summary>
public sealed class Fornecedor
{
    private Fornecedor() { }

    public Fornecedor(Guid id, string nome, Cnpj cnpj, string? categoria, string? email, string? telefone,
        string? website, string? cidade, string? estado, string? pais, string status, decimal? scoreIA,
        Guid temporaryUserId, DateTimeOffset createdAt)
        : this(id, nome, cnpj, categoria, email, telefone, website, cidade, estado, pais, status, scoreIA,
            temporaryUserId, createdAt, null, null, null)
    {
    }

    public Fornecedor(Guid id, string nome, Cnpj cnpj, string? categoria, string? email, string? telefone,
        string? website, string? cidade, string? estado, string? pais, string status, decimal? scoreIA,
        Guid temporaryUserId, DateTimeOffset createdAt, string? businessUnit, string? erpSistema, string? erpFornecedorId)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome is required.", nameof(nome));
        if (temporaryUserId == Guid.Empty) throw new ArgumentException("TemporaryUserId is required.", nameof(temporaryUserId));
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Nome = nome.Trim(); Cnpj = cnpj.Value; Categoria = categoria?.Trim(); Email = email?.Trim();
        Telefone = telefone?.Trim(); Website = website?.Trim(); Cidade = cidade?.Trim(); Estado = estado?.Trim();
        Pais = pais?.Trim(); Status = string.IsNullOrWhiteSpace(status) ? "Ativo" : status.Trim(); ScoreIA = scoreIA;
        TemporaryUserId = temporaryUserId; CreatedAt = createdAt; UpdatedAt = createdAt;
        BusinessUnit = businessUnit?.Trim(); ErpSistema = erpSistema?.Trim(); ErpFornecedorId = erpFornecedorId?.Trim();
        OrigemInformacao = "MaisCompras"; StatusSincronizacao = "Pendente";
    }

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = null!;
    public string Cnpj { get; private set; } = null!;
    public string? Categoria { get; private set; }
    public string? Email { get; private set; }
    public string? Telefone { get; private set; }
    public string? Website { get; private set; }
    public string? Cidade { get; private set; }
    public string? Estado { get; private set; }
    public string? Pais { get; private set; }
    public string Status { get; private set; } = null!;
    public decimal? ScoreIA { get; private set; }
    public Guid TemporaryUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public string? BusinessUnit { get; private set; }
    public string? ErpSistema { get; private set; }
    public string? ErpFornecedorId { get; private set; }
    public string OrigemInformacao { get; private set; } = "MaisCompras";
    public DateTimeOffset? UltimaSincronizacaoEm { get; private set; }
    public string StatusSincronizacao { get; private set; } = "Pendente";
    public string? MensagemErroSincronizacao { get; private set; }

    public void Atualizar(string nome, string? categoria, string? email, string? telefone, string? website,
        string? cidade, string? estado, string? pais, string status, decimal? scoreIA, DateTimeOffset updatedAt)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome is required.", nameof(nome));
        Nome = nome.Trim(); Categoria = categoria?.Trim(); Email = email?.Trim(); Telefone = telefone?.Trim();
        Website = website?.Trim(); Cidade = cidade?.Trim(); Estado = estado?.Trim(); Pais = pais?.Trim();
        Status = string.IsNullOrWhiteSpace(status) ? "Ativo" : status.Trim(); ScoreIA = scoreIA; UpdatedAt = updatedAt;
    }

    public void AplicarDadosCorporativos(string nome, string? cnpj, string? cidade, string? estado, string? pais,
        string businessUnit, string erpSistema, string erpFornecedorId, DateTimeOffset updatedAt)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome is required.", nameof(nome));
        Nome = nome.Trim();
        if (!string.IsNullOrWhiteSpace(cnpj)) Cnpj = global::BlueprintOS.Domain.Procurement.Suppliers.Cnpj.Create(cnpj).Value;
        Cidade = cidade?.Trim(); Estado = estado?.Trim(); Pais = pais?.Trim();
        BusinessUnit = businessUnit.Trim(); ErpSistema = erpSistema.Trim(); ErpFornecedorId = erpFornecedorId.Trim();
        OrigemInformacao = "ERP"; UpdatedAt = updatedAt;
    }

    public void RegistrarSincronizacao(string status, DateTimeOffset quando, string? mensagem = null)
    {
        StatusSincronizacao = status.Trim(); UltimaSincronizacaoEm = quando;
        MensagemErroSincronizacao = string.IsNullOrWhiteSpace(mensagem) ? null : mensagem.Trim();
    }
}
