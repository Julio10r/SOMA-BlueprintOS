using BlueprintOS.Application.Procurement.Suppliers.Models;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

public interface IAnalisarEnriquecimentoFornecedorUseCase
{
    Task<FornecedorEnriquecimentoAnaliseDto?> ExecuteAsync(Guid fornecedorId, AnalisarEnriquecimentoFornecedorDto dto, CancellationToken cancellationToken = default);
}

public interface IAprovarEnriquecimentoFornecedorUseCase
{
    Task<FornecedorEnriquecimentoAnaliseDto?> ExecuteAsync(Guid fornecedorId, DecidirEnriquecimentoFornecedorDto dto, CancellationToken cancellationToken = default);
}

public interface IRejeitarEnriquecimentoFornecedorUseCase
{
    Task<FornecedorEnriquecimentoAnaliseDto?> ExecuteAsync(Guid fornecedorId, DecidirEnriquecimentoFornecedorDto dto, CancellationToken cancellationToken = default);
}
