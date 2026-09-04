using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

public sealed record IdentificacaoFornecedorErp(string BusinessUnit, string ErpSistema, string IdentificadorExterno);
public sealed record ErpFornecedorParaEscrita(string Id, string Nome, string Cnpj, string? Cidade, string? Estado, string? Pais,
    bool Ativo = true, DateTimeOffset? UltimaAlteracaoEm = null, string? HashDadosSincronizaveis = null, FornecedorCanonico? DadosCanonicos = null);
public sealed record ErpFornecedorDto(string Id, string Nome, string? Cnpj, string? Cidade, string? Estado, string? Pais,
    bool Ativo = true, DateTimeOffset? UltimaAlteracaoEm = null, string? HashDadosSincronizaveis = null,
    FornecedorCanonico? DadosCanonicos = null);
public sealed record FornecedorParaErpDto(string Id, string Nome, string Cnpj, string? Cidade, string? Estado, string? Pais,
    bool Ativo = true, DateTimeOffset? UltimaAlteracaoEm = null, string? HashDadosSincronizaveis = null,
    FornecedorCanonico? DadosCanonicos = null);
public sealed record ResultadoIntegracaoFornecedorErp(bool Sucesso, string? IdentificadorExterno, string Operacao,
    string ErpSistema, string BusinessUnit, string Status, string? CodigoErro, string? MensagemSanitizada,
    DateTimeOffset ProcessadoEm, DateTimeOffset? UltimaAlteracaoErp, string CorrelationId);

public interface IErpFornecedorAdapter
{
    string ErpSistema { get; }
    Task<ErpFornecedorDto?> ObterAsync(string identificador, CancellationToken cancellationToken = default);
    Task<ErpFornecedorDto> CriarAsync(ErpFornecedorParaEscrita fornecedor, CancellationToken cancellationToken = default);
    Task<ErpFornecedorDto> AtualizarAsync(ErpFornecedorParaEscrita fornecedor, CancellationToken cancellationToken = default);
    Task<ErpFornecedorDto> InativarAsync(string identificador, CancellationToken cancellationToken = default);
}

public interface IIntegracaoFornecedorErp : IErpFornecedorAdapter
{
    Task<ErpFornecedorDto?> ConsultarAsync(IdentificacaoFornecedorErp identificacao, CancellationToken cancellationToken = default);
    Task<ErpFornecedorDto> CriarAsync(FornecedorParaErpDto fornecedor, CancellationToken cancellationToken = default);
    Task<ErpFornecedorDto> AtualizarAsync(FornecedorParaErpDto fornecedor, CancellationToken cancellationToken = default);
    Task<ErpFornecedorDto> InativarAsync(IdentificacaoFornecedorErp identificacao, CancellationToken cancellationToken = default);
}

public interface IErpFornecedorAdapterResolver
{
    IErpFornecedorAdapter Resolver(string businessUnit, string erpSistema);
}

public interface IFornecedorSincronizacaoRepository
{
    Task<Fornecedor?> ObterPorChaveErpAsync(string businessUnit, string erpSistema, string erpFornecedorId, CancellationToken cancellationToken = default);
    Task AdicionarAsync(FornecedorSincronizacao sincronizacao, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FornecedorSincronizacao>> ListarPorFornecedorAsync(Guid fornecedorId, CancellationToken cancellationToken = default);
}
