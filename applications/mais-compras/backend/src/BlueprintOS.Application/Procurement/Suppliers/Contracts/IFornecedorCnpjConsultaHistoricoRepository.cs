using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

public interface IFornecedorCnpjConsultaHistoricoRepository
{
    Task AdicionarAsync(FornecedorCnpjConsultaHistorico consulta, CancellationToken cancellationToken = default);

    /// <summary>Expurgo de retenção (B2.7/ADR-0023): anula <c>PayloadBrutoJson</c> de todo registro
    /// cuja <c>DataConsulta</c> já ultrapassou <see cref="FornecedorCnpjConsultaHistorico.RetencaoPayloadBrutoDias"/>
    /// dias em relação a <paramref name="referenciaUtc"/>. Nunca remove linhas, nunca altera outras
    /// colunas (TipoErro/FonteConsulta/DataConsulta/Status/etc. permanecem intocados), e é idempotente:
    /// registros já expurgados (payload já nulo) não são afetados novamente. Retorna a quantidade de
    /// registros efetivamente alterados nesta execução.</summary>
    Task<int> ExpurgarPayloadBrutoExpiradoAsync(DateTimeOffset referenciaUtc, CancellationToken cancellationToken = default);
}
