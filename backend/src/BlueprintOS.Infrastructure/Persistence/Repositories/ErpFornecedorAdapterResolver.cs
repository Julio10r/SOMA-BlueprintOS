using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using Microsoft.Extensions.Configuration;

namespace BlueprintOS.Infrastructure.Persistence.Repositories;

public sealed class ErpFornecedorAdapterResolver(IEnumerable<IErpFornecedorAdapter> adapters, IConfiguration configuration) : IErpFornecedorAdapterResolver
{
    public IErpFornecedorAdapter Resolver(string businessUnit, string erpSistema)
    {
        var configured = configuration[$"ErpIntegration:BusinessUnits:{businessUnit}:ErpSistema"];
        var selected = string.IsNullOrWhiteSpace(erpSistema) ? configured : erpSistema;
        if (string.IsNullOrWhiteSpace(configured) || !string.Equals(configured, selected, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("A BU não está autorizada para o ERP informado.");
        return adapters.SingleOrDefault(x => string.Equals(x.ErpSistema, selected, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Não existe adaptador ERP configurado para a BU informada.");
    }
}
