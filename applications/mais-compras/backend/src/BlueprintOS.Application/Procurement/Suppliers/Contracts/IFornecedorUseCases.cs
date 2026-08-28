using BlueprintOS.Application.Procurement.Suppliers.Models;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

public interface ICadastrarFornecedorUseCase { Task<FornecedorDto> ExecuteAsync(CadastrarFornecedorDto dto, CancellationToken cancellationToken = default); }
public interface IAtualizarFornecedorUseCase { Task<FornecedorDto?> ExecuteAsync(Guid id, AtualizarFornecedorDto dto, CancellationToken cancellationToken = default); }
/// <summary>Inativa um Fornecedor (rota HTTP <c>DELETE /fornecedores/{id}</c>, contrato externo mantido)
/// marcando <c>Status="Inativo"</c> — nunca remove a linha fisicamente (DR-18).</summary>
public interface IInativarFornecedorUseCase { Task<bool> ExecuteAsync(Guid id, CancellationToken cancellationToken = default); }

/// <summary>Ativa/inativa um Fornecedor de forma explícita e bidirecional (rota semântica
/// <c>PATCH /fornecedores/{id}/status</c>) — reaproveita <see cref="BlueprintOS.Domain.Procurement.Suppliers.Fornecedor.AlterarStatus"/>,
/// o mesmo mecanismo usado por <see cref="IInativarFornecedorUseCase"/> e pela sincronização com o ERP.
/// Nunca remove a linha fisicamente (DR-18).</summary>
public interface IAlterarStatusFornecedorUseCase { Task<FornecedorDto?> ExecuteAsync(Guid id, bool ativo, CancellationToken cancellationToken = default); }
public interface IObterFornecedorUseCase { Task<FornecedorDto?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default); }
public interface IPesquisarFornecedorUseCase { Task<IReadOnlyList<FornecedorDto>> ExecuteAsync(string? termo, CancellationToken cancellationToken = default); }

public sealed record PesquisarFornecedorPaginadoParametros(string? Termo, string? Status, string? Sort, int Page = 1, int PageSize = 20);
public sealed record FornecedorPesquisaPaginadaDto(IReadOnlyList<FornecedorDto> Items, int TotalCount, int Page, int PageSize);
public interface IPesquisarFornecedorPaginadoUseCase
{
    Task<FornecedorPesquisaPaginadaDto> ExecuteAsync(PesquisarFornecedorPaginadoParametros parametros, CancellationToken cancellationToken = default);
}
