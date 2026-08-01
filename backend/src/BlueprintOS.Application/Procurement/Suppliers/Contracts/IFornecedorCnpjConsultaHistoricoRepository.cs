using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

public interface IFornecedorCnpjConsultaHistoricoRepository
{
    Task AdicionarAsync(FornecedorCnpjConsultaHistorico consulta, CancellationToken cancellationToken = default);
}
