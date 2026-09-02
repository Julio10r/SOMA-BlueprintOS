using BlueprintOS.Application.Procurement.Suppliers.Models;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

public interface IConsultarCepFornecedorUseCase
{
    Task<ConsultaCepResultado> ExecuteAsync(ConsultarCepFornecedorDto dto, CancellationToken cancellationToken = default);
}
