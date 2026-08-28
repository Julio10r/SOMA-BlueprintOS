using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.UnitTests.Domain.Procurement.Suppliers;

public sealed class FornecedorCnpjConsultaHistoricoTests
{
    private static readonly DateTimeOffset Referencia = new(2026, 8, 12, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void PayloadBrutoExpirado_Should_Be_False_At_179_Days()
    {
        var historico = CriarComIdade(dias: 179);
        Assert.False(historico.PayloadBrutoExpirado(Referencia));
    }

    [Fact]
    public void PayloadBrutoExpirado_Should_Be_False_Exactly_At_180_Days_Boundary()
    {
        // Semantica de fronteira formalizada (ADR-0023, addendo B2.7): o payload e preservado no
        // dia exato do 180o aniversario da consulta; so se torna elegivel para expurgo a partir do
        // 181o dia. "Retencao de 180 dias" significa "preservado durante 180 dias completos".
        var historico = CriarComIdade(dias: 180);
        Assert.False(historico.PayloadBrutoExpirado(Referencia));
    }

    [Fact]
    public void PayloadBrutoExpirado_Should_Be_True_At_181_Days()
    {
        var historico = CriarComIdade(dias: 181);
        Assert.True(historico.PayloadBrutoExpirado(Referencia));
    }

    [Fact]
    public void ExpirarPayloadBruto_Should_Null_Only_The_Payload_And_Keep_Structural_Fields()
    {
        var historico = CriarComIdade(dias: 181, payload: "{\"razao_social\":\"Fornecedor Teste\"}");

        historico.ExpirarPayloadBruto();

        Assert.Null(historico.PayloadBrutoJson);
        Assert.Equal("12345678000195", historico.Cnpj_Cpf);
        Assert.Equal("BrasilAPI", historico.FonteConsulta);
        Assert.Equal(TipoErroConsultaCnpjHistorico.NaoEncontrado, historico.TipoErro);
        Assert.Equal("corr-1", historico.CorrelationId);
    }

    [Fact]
    public void ExpirarPayloadBruto_Should_Be_Idempotent_When_Payload_Already_Null()
    {
        var historico = CriarComIdade(dias: 181, payload: null);

        historico.ExpirarPayloadBruto();
        historico.ExpirarPayloadBruto();

        Assert.Null(historico.PayloadBrutoJson);
    }

    [Fact]
    public void Constructor_Should_Reject_Payload_Above_Size_Limit()
    {
        var payloadGigante = new string('a', FornecedorCnpjConsultaHistorico.LimitePayloadBrutoCaracteres + 1);

        Assert.Throws<ArgumentException>(() => new FornecedorCnpjConsultaHistorico(
            Guid.NewGuid(), "12345678000195", "BrasilAPI", DateTimeOffset.UtcNow, Guid.NewGuid(),
            "Sucesso", "Ativa", null, "corr-limite", "BU-A", null, null, payloadGigante));
    }

    [Fact]
    public void Constructor_Should_Keep_TipoErro_Null_On_Success()
    {
        var historico = new FornecedorCnpjConsultaHistorico(
            Guid.NewGuid(), "12345678000195", "BrasilAPI", DateTimeOffset.UtcNow, Guid.NewGuid(),
            "Sucesso", "Ativa", null, "corr-sucesso", "BU-A", null, tipoErro: null,
            payloadBrutoJson: "{\"razao_social\":\"X\"}");

        Assert.Null(historico.TipoErro);
        Assert.Equal("{\"razao_social\":\"X\"}", historico.PayloadBrutoJson);
    }

    private static FornecedorCnpjConsultaHistorico CriarComIdade(int dias, string? payload = "{}") =>
        new(Guid.NewGuid(), "12345678000195", "BrasilAPI", Referencia.AddDays(-dias), Guid.NewGuid(),
            "Falha", "N/A", "CNPJ não encontrado.", "corr-1", "BU-A", null,
            TipoErroConsultaCnpjHistorico.NaoEncontrado, payload);
}
