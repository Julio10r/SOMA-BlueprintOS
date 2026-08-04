using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.Api.Suppliers;

public sealed record FornecedorRequest(string Nome, string Cnpj, string? Categoria, string? Email, string? Telefone,
    string? Website, string? Cidade, string? Estado, string? Pais, string? Status, decimal? ScoreIA,
    FornecedorCanonico? DadosCanonicos = null, string? Cnpj_Cpf = null, string? TipoPessoa = null, string? RazaoSocial = null,
    bool Beneficiador = false, bool Licenciado = false)
{
    public CadastrarFornecedorDto ToCreateDto() => new(Nome, Cnpj, Categoria, Email, Telefone, Website, Cidade, Estado, Pais,
        Status, ScoreIA, DadosCanonicos, Cnpj_Cpf, TipoPessoa, RazaoSocial, Beneficiador, Licenciado);
}

public sealed record AtualizarFornecedorRequest(string Nome, string? Categoria, string? Email, string? Telefone,
    string? Website, string? Cidade, string? Estado, string? Pais, string? Status, decimal? ScoreIA,
    FornecedorCanonico? DadosCanonicos = null, string? Cnpj = null, string? Cnpj_Cpf = null, string? TipoPessoa = null,
    string? RazaoSocial = null, bool? Beneficiador = null, bool? Licenciado = null)
{
    public AtualizarFornecedorDto ToDto() => new(Nome, Categoria, Email, Telefone, Website, Cidade, Estado, Pais, Status,
        ScoreIA, DadosCanonicos, Cnpj, Cnpj_Cpf, TipoPessoa, RazaoSocial, Beneficiador, Licenciado);
}
