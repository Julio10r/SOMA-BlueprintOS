namespace BlueprintOS.Domain.Procurement.Suppliers.Raw;

/// <summary>Ação a seguir depois que uma execução RAW foi reconciliada — decisão PURA, sem I/O, reutilizada
/// por todo use case RAW→REFINED→DOMÍNIO (Fornecedor e cada cadastro de apoio), para nunca reimplementar a
/// mesma tabela verdade em lugares diferentes.</summary>
public enum ProximaAcaoBaseline
{
    Nenhuma,
    HomologarBaseline,
    AvancarWatermark,
}

/// <summary>
/// B3 — Bloco 5A (preparação de certificação final), decisão do PO: watermark de dataset Incremental nunca
/// avança sem uma execução PASS (Completa + reconciliada + Aprovada) — reconciliação reprovada, execução
/// parcial ou qualquer falha nunca avançam. Full aprovada estabelece/reestabelece a baseline; Incremental
/// aprovada avança o watermark; qualquer outra combinação (reprovada, ainda não reconciliada, ou Full que
/// nunca homologa watermark de incremental) não faz nada aqui — o chamador decide como logar/reportar isso,
/// mas nunca chama <see cref="LinxDatasetLoadState.HomologarBaseline"/>/<see cref="LinxDatasetLoadState.AvancarWatermark"/>
/// fora do caso correspondente.
/// </summary>
public static class DecisaoPosReconciliacao
{
    public static ProximaAcaoBaseline Decidir(RawLoadMode modo, RawReconciliacaoStatus status) => (modo, status) switch
    {
        (RawLoadMode.Full, RawReconciliacaoStatus.Aprovada) => ProximaAcaoBaseline.HomologarBaseline,
        (RawLoadMode.Incremental, RawReconciliacaoStatus.Aprovada) => ProximaAcaoBaseline.AvancarWatermark,
        _ => ProximaAcaoBaseline.Nenhuma,
    };
}
