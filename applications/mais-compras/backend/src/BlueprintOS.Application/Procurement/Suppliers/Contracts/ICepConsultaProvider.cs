using BlueprintOS.Application.Procurement.Suppliers.Models;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

public interface ICepConsultaProvider
{
    string FonteConsulta { get; }
    Task<ConsultaCepResultado> ConsultarAsync(string cep, CancellationToken cancellationToken = default);
}
