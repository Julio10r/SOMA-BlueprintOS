using BlueprintOS.Application.Procurement.Suppliers.Models;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

public interface IConsultarCnpjFornecedorUseCase
{
    Task<ConsultaCnpjResultado> ExecuteAsync(ConsultarCnpjFornecedorDto dto, CancellationToken cancellationToken = default);
}
