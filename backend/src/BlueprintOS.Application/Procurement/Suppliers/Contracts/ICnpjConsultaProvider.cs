using BlueprintOS.Application.Procurement.Suppliers.Models;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

public interface ICnpjConsultaProvider
{
    string FonteConsulta { get; }
    Task<ConsultaCnpjResultado> ConsultarAsync(string cnpjCpf, CancellationToken cancellationToken = default);
}
