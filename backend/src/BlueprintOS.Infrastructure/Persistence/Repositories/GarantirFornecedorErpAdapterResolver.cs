using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using Microsoft.Extensions.Configuration;

namespace BlueprintOS.Infrastructure.Persistence.Repositories;

/// <summary>Resolve o Adapter Linx a partir da BU — BU é a fronteira do ERP (Gate Pré-B2.9, seção 6-B):
/// uma BU corresponde a um ERP/banco. Falha fechado se a BU não estiver configurada ou não houver adapter
/// registrado para o ERP configurado.</summary>
public sealed class GarantirFornecedorErpAdapterResolver(IEnumerable<IGarantirFornecedorErpAdapter> adapters, IConfiguration configuration) : IGarantirFornecedorErpAdapterResolver
{
    public IGarantirFornecedorErpAdapter Resolver(string businessUnit)
    {
        var erpSistema = configuration[$"ErpIntegration:BusinessUnits:{businessUnit}:ErpSistema"];
        if (string.IsNullOrWhiteSpace(erpSistema))
            throw new ErpFornecedorEscritaException(ErpFornecedorErro.Validacao, "A Unidade de Negócio informada não está configurada para nenhum ERP.");
        return adapters.SingleOrDefault(x => string.Equals(x.ErpSistema, erpSistema, StringComparison.OrdinalIgnoreCase))
            ?? throw new ErpFornecedorEscritaException(ErpFornecedorErro.Validacao, "Não existe adaptador ERP configurado para a Unidade de Negócio informada.");
    }
}
