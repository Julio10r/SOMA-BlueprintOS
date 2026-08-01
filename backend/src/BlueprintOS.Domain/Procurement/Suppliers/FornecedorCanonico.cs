namespace BlueprintOS.Domain.Procurement.Suppliers;

/// <summary>Contrato neutro de dados sincronizáveis; não contém conceitos do ERP.</summary>
public sealed record FornecedorCanonico(string RazaoSocial, string? NomeFantasia, string DocumentoFiscal, string? TipoPessoa,
    string? Pais, string? InscricaoEstadual, string? InscricaoMunicipal, string? Cep, string? Logradouro, string? Numero,
    string? Complemento, string? Bairro, string? Cidade, string? Uf, string? CodigoMunicipio, string? Ddd, string? Telefone,
    string? EmailComercial, string? EmailFiscal, string? Banco, string? Agencia, string? Conta, string? DigitosConta,
    string? CondicaoPagamento, string? TipoFornecedor, string? SubtipoFornecedor, string? ContaContabil, string? RegimeFiscal,
    bool? SimplesNacional, string? CategoriasFornecimento, bool ForneceMateriais, bool ForneceConsumo, bool ForneceServicos,
    bool ForneceProdutos, bool Ativo, DateTimeOffset DataUltimaAlteracao, string HashDadosSincronizaveis);
