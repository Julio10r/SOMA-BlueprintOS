using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

public sealed record ErpFornecedorDto(string Id, string Nome, string? Cnpj, string? Cidade, string? Estado, string? Pais);
public sealed record ErpFornecedorParaEscrita(string Id, string Nome, string Cnpj, string? Cidade, string? Estado, string? Pais);

public interface IErpFornecedorAdapter
{
    string ErpSistema { get; }
    Task<ErpFornecedorDto?> ObterAsync(string identificador, CancellationToken cancellationToken = default);
    Task<ErpFornecedorDto> CriarAsync(ErpFornecedorParaEscrita fornecedor, CancellationToken cancellationToken = default);
    Task<ErpFornecedorDto> AtualizarAsync(ErpFornecedorParaEscrita fornecedor, CancellationToken cancellationToken = default);
}

public interface IErpFornecedorAdapterResolver
{
    IErpFornecedorAdapter Resolver(string businessUnit, string erpSistema);
}

public interface IFornecedorSincronizacaoRepository
{
    Task<Fornecedor?> ObterPorChaveErpAsync(string businessUnit, string erpSistema, string erpFornecedorId, Guid userId, CancellationToken cancellationToken = default);
    Task AdicionarAsync(FornecedorSincronizacao sincronizacao, CancellationToken cancellationToken = default);
}
