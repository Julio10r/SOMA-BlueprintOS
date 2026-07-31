using BlueprintOS.Application.Procurement.Suppliers.Models;

namespace BlueprintOS.Api.Suppliers;

public sealed record FornecedorRequest(string Nome, string Cnpj, string? Categoria, string? Email, string? Telefone,
    string? Website, string? Cidade, string? Estado, string? Pais, string? Status, decimal? ScoreIA)
{
    public CadastrarFornecedorDto ToCreateDto() => new(Nome, Cnpj, Categoria, Email, Telefone, Website, Cidade, Estado, Pais, Status, ScoreIA);
}

public sealed record AtualizarFornecedorRequest(string Nome, string? Categoria, string? Email, string? Telefone,
    string? Website, string? Cidade, string? Estado, string? Pais, string? Status, decimal? ScoreIA)
{
    public AtualizarFornecedorDto ToDto() => new(Nome, Categoria, Email, Telefone, Website, Cidade, Estado, Pais, Status, ScoreIA);
}
