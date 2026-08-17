namespace BlueprintOS.Application.Procurement.Suppliers.Models;

using BlueprintOS.Domain.Procurement.Suppliers;

public sealed record CadastrarFornecedorDto(string Nome, string Cnpj, string? Categoria, string? Email, string? Telefone,
    string? Website, string? Cidade, string? Estado, string? Pais, string? Status, decimal? ScoreIA,
    FornecedorCanonico? DadosCanonicos = null, string? Cnpj_Cpf = null, string? TipoPessoa = null, string? RazaoSocial = null,
    bool Beneficiador = false, bool Licenciado = false, string? CnaePrincipalCodigo = null, string? CnaePrincipalDescricao = null,
    string? NomeFantasia = null, string? Cep = null, string? Logradouro = null, string? Numero = null,
    string? Complemento = null, string? Bairro = null);
public sealed record AtualizarFornecedorDto(string Nome, string? Categoria, string? Email, string? Telefone,
    string? Website, string? Cidade, string? Estado, string? Pais, string? Status, decimal? ScoreIA,
    FornecedorCanonico? DadosCanonicos = null, string? Cnpj = null, string? Cnpj_Cpf = null, string? TipoPessoa = null,
    string? RazaoSocial = null, bool? Beneficiador = null, bool? Licenciado = null, string? CnaePrincipalCodigo = null,
    string? CnaePrincipalDescricao = null, string? NomeFantasia = null, string? Cep = null, string? Logradouro = null,
    string? Numero = null, string? Complemento = null, string? Bairro = null);
public sealed record FornecedorDto(Guid Id, string Nome, string Cnpj, string? Categoria, string? Email, string? Telefone,
    string? Website, string? Cidade, string? Estado, string? Pais, string Status, decimal? ScoreIA, Guid TemporaryUserId,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string? NomeFantasia = null, string? TipoPessoa = null,
    string? InscricaoEstadual = null, string? InscricaoMunicipal = null, string? Cep = null, string? Logradouro = null,
    string? Numero = null, string? Complemento = null, string? Bairro = null, string? CodigoMunicipio = null, string? Ddd = null,
    string? EmailFiscal = null, string? Banco = null, string? Agencia = null, string? Conta = null, string? DigitosConta = null,
    string? CondicaoPagamento = null, string? TipoFornecedor = null, string? SubtipoFornecedor = null, string? ContaContabil = null,
    string? RegimeFiscal = null, bool? SimplesNacional = null, string? CategoriasFornecimento = null,
    bool ForneceMateriais = false, bool ForneceConsumo = false, bool ForneceServicos = false, bool ForneceProdutos = false,
    string? BusinessUnit = null, string? ErpSistema = null, string? ErpFornecedorId = null, int Versao = 1,
    string? HashDadosSincronizaveis = null, string? Cnpj_Cpf = null, string? RazaoSocial = null, bool Beneficiador = false,
    bool Licenciado = false, Guid? CondicaoPagamentoDominioId = null, Guid? TipoFornecedorDominioId = null,
    Guid? SubtipoFornecedorDominioId = null, string? CnaePrincipalCodigo = null, string? CnaePrincipalDescricao = null);
