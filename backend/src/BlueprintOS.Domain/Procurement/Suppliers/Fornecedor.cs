namespace BlueprintOS.Domain.Procurement.Suppliers;

/// <summary>Aggregate root representing a supplier owned by a procurement user.</summary>
public sealed class Fornecedor
{
    private Fornecedor() { }

    public Fornecedor(Guid id, string nome, Cnpj cnpj, string? categoria, string? email, string? telefone,
        string? website, string? cidade, string? estado, string? pais, string status, decimal? scoreIA,
        Guid temporaryUserId, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome is required.", nameof(nome));
        if (temporaryUserId == Guid.Empty) throw new ArgumentException("TemporaryUserId is required.", nameof(temporaryUserId));
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Nome = nome.Trim(); Cnpj = cnpj.Value; Categoria = categoria?.Trim(); Email = email?.Trim();
        Telefone = telefone?.Trim(); Website = website?.Trim(); Cidade = cidade?.Trim(); Estado = estado?.Trim();
        Pais = pais?.Trim(); Status = string.IsNullOrWhiteSpace(status) ? "Ativo" : status.Trim(); ScoreIA = scoreIA;
        TemporaryUserId = temporaryUserId; CreatedAt = createdAt; UpdatedAt = createdAt;
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

    public void Atualizar(string nome, string? categoria, string? email, string? telefone, string? website,
        string? cidade, string? estado, string? pais, string status, decimal? scoreIA, DateTimeOffset updatedAt)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome is required.", nameof(nome));
        Nome = nome.Trim(); Categoria = categoria?.Trim(); Email = email?.Trim(); Telefone = telefone?.Trim();
        Website = website?.Trim(); Cidade = cidade?.Trim(); Estado = estado?.Trim(); Pais = pais?.Trim();
        Status = string.IsNullOrWhiteSpace(status) ? "Ativo" : status.Trim(); ScoreIA = scoreIA; UpdatedAt = updatedAt;
    }
}
