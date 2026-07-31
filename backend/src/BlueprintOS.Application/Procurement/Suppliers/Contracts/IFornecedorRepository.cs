using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

public interface IFornecedorRepository
{
    Task AdicionarAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default);
    Task AtualizarAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default);
    Task ExcluirAsync(Fornecedor fornecedor, CancellationToken cancellationToken = default);
    Task<Fornecedor?> ObterPorIdAsync(Guid id, Guid temporaryUserId, CancellationToken cancellationToken = default);
    Task<Fornecedor?> ObterPorCnpjAsync(string cnpj, Guid temporaryUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Fornecedor>> PesquisarAsync(string termo, Guid temporaryUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Fornecedor>> ListarAsync(Guid temporaryUserId, CancellationToken cancellationToken = default);
    Task<bool> ExisteAsync(string cnpj, CancellationToken cancellationToken = default);
}
