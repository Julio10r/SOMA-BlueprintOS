using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Application.Procurement.Suppliers;

public interface IExpurgarPayloadBrutoConsultaCnpjUseCase
{
    /// <summary>Executa uma passagem de expurgo de retenção do payload bruto (B2.7/ADR-0023). Seguro
    /// para invocação repetida (idempotente) e pronto para ser acionado por um agendador futuro (ex.:
    /// rotina periódica, endpoint administrativo) sem exigir Hangfire/Quartz — este sprint entrega
    /// apenas o mecanismo, não o agendamento automático.</summary>
    Task<int> ExecuteAsync(CancellationToken cancellationToken = default);
}

/// <summary>Expurga (anula) o snapshot bruto de consultas de CNPJ cuja retenção de
/// <c>FornecedorCnpjConsultaHistorico.RetencaoPayloadBrutoDias</c> (180 dias) já expirou. Nunca remove
/// o registro de histórico em si — apenas <c>PayloadBrutoJson</c>. A trilha estrutural (CNPJ, Fonte,
/// Timestamp, Status, TipoErro, CorrelationId) é permanente e nunca é tocada por este mecanismo.</summary>
public sealed class ExpurgarPayloadBrutoConsultaCnpjUseCase(
    IFornecedorCnpjConsultaHistoricoRepository repository,
    ILogger<ExpurgarPayloadBrutoConsultaCnpjUseCase> logger) : IExpurgarPayloadBrutoConsultaCnpjUseCase
{
    public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var referenciaUtc = DateTimeOffset.UtcNow;
        var quantidadeExpurgada = await repository.ExpurgarPayloadBrutoExpiradoAsync(referenciaUtc, cancellationToken);

        // Log deliberadamente livre de conteúdo de payload (nunca logamos o snapshot em si) — apenas
        // a contagem estrutural de registros afetados, para observabilidade sem risco de LGPD/segurança.
        logger.LogInformation(
            "Expurgo de payload bruto de consulta CNPJ concluido. Referencia UTC: {ReferenciaUtc}. Registros com payload anulado: {Quantidade}.",
            referenciaUtc, quantidadeExpurgada);

        return quantidadeExpurgada;
    }
}
