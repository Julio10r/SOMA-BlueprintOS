using BlueprintOS.Application.Procurement.Suppliers.Models;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

public interface ICadastrarFornecedorUseCase { Task<FornecedorDto> ExecuteAsync(CadastrarFornecedorDto dto, CancellationToken cancellationToken = default); }
public interface IAtualizarFornecedorUseCase { Task<FornecedorDto?> ExecuteAsync(Guid id, AtualizarFornecedorDto dto, CancellationToken cancellationToken = default); }
/// <summary>Inativa um Fornecedor (rota HTTP <c>DELETE /fornecedores/{id}</c>, contrato externo mantido)
/// marcando <c>Status="Inativo"</c> — nunca remove a linha fisicamente (DR-18).</summary>
public interface IInativarFornecedorUseCase { Task<bool> ExecuteAsync(Guid id, CancellationToken cancellationToken = default); }
public interface IObterFornecedorUseCase { Task<FornecedorDto?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default); }
public interface IPesquisarFornecedorUseCase { Task<IReadOnlyList<FornecedorDto>> ExecuteAsync(string? termo, CancellationToken cancellationToken = default); }
