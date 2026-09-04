using BlueprintOS.Domain.Procurement.Suppliers.Raw;

namespace BlueprintOS.UnitTests.Domain.Procurement.Suppliers.Raw;

/// <summary>Prova pura (sem banco) da tabela-verdade que decide se uma execução reconciliada homologa
/// baseline, avança watermark, ou não faz nada — em particular, o cenário exigido pelo PO "reconciliação
/// reprovada não avança" nunca depende de nenhuma verificação a mais no use case, é a própria decisão.</summary>
public sealed class DecisaoPosReconciliacaoTests
{
    [Fact]
    public void Full_Aprovada_Homologa_Baseline()
    {
        Assert.Equal(ProximaAcaoBaseline.HomologarBaseline, DecisaoPosReconciliacao.Decidir(RawLoadMode.Full, RawReconciliacaoStatus.Aprovada));
    }

    [Fact]
    public void Incremental_Aprovada_Avanca_Watermark()
    {
        Assert.Equal(ProximaAcaoBaseline.AvancarWatermark, DecisaoPosReconciliacao.Decidir(RawLoadMode.Incremental, RawReconciliacaoStatus.Aprovada));
    }

    [Theory]
    [InlineData(RawLoadMode.Incremental, RawReconciliacaoStatus.Reprovada)]
    [InlineData(RawLoadMode.Full, RawReconciliacaoStatus.Reprovada)]
    [InlineData(RawLoadMode.Incremental, RawReconciliacaoStatus.Pendente)]
    [InlineData(RawLoadMode.Incremental, RawReconciliacaoStatus.NaoRealizada)]
    [InlineData(RawLoadMode.Full, RawReconciliacaoStatus.Pendente)]
    public void Reconciliacao_Nao_Aprovada_Ou_Combinacao_Nao_Prevista_Nunca_Age(RawLoadMode modo, RawReconciliacaoStatus status)
    {
        Assert.Equal(ProximaAcaoBaseline.Nenhuma, DecisaoPosReconciliacao.Decidir(modo, status));
    }
}
