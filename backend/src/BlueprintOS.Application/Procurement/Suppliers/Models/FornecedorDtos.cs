namespace BlueprintOS.Application.Procurement.Suppliers.Models;

public sealed record CadastrarFornecedorDto(string Nome, string Cnpj, string? Categoria, string? Email, string? Telefone,
    string? Website, string? Cidade, string? Estado, string? Pais, string? Status, decimal? ScoreIA);
public sealed record AtualizarFornecedorDto(string Nome, string? Categoria, string? Email, string? Telefone,
    string? Website, string? Cidade, string? Estado, string? Pais, string? Status, decimal? ScoreIA);
public sealed record FornecedorDto(Guid Id, string Nome, string Cnpj, string? Categoria, string? Email, string? Telefone,
    string? Website, string? Cidade, string? Estado, string? Pais, string Status, decimal? ScoreIA, Guid TemporaryUserId,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
