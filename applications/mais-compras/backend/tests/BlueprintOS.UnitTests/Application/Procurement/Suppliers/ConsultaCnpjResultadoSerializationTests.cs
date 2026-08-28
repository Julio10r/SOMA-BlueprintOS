using System.Text.Json;
using System.Text.Json.Serialization;
using BlueprintOS.Application.Procurement.Suppliers.Models;

namespace BlueprintOS.UnitTests.Application.Procurement.Suppliers;

/// <summary>Contrato HTTP central da B2.5: a serialização real usada pela API (registrada em
/// <c>Program.cs</c> via <c>ConfigureHttpJsonOptions</c>) adiciona <see cref="JsonStringEnumConverter"/>
/// para que enums nunca atravessem a fronteira HTTP como inteiro bruto. Estes testes fixam esse
/// contrato para <see cref="SituacaoCadastralCnpj"/> — sem o converter, o frontend receberia "0", "1",
/// etc. em vez de "Ativa"/"Baixada"/....</summary>
public sealed class ConsultaCnpjResultadoSerializationTests
{
    private static readonly JsonSerializerOptions HttpOptions = CreateHttpOptions();

    [Theory]
    [InlineData(SituacaoCadastralCnpj.Ativa, "\"Ativa\"")]
    [InlineData(SituacaoCadastralCnpj.Baixada, "\"Baixada\"")]
    [InlineData(SituacaoCadastralCnpj.Suspensa, "\"Suspensa\"")]
    [InlineData(SituacaoCadastralCnpj.Inapta, "\"Inapta\"")]
    [InlineData(SituacaoCadastralCnpj.Nula, "\"Nula\"")]
    [InlineData(SituacaoCadastralCnpj.Desconhecida, "\"Desconhecida\"")]
    public void SituacaoCadastral_Should_Serialize_As_Explicit_String_Never_As_Raw_Number(
        SituacaoCadastralCnpj situacao, string esperado)
    {
        var resultado = ConsultaCnpjResultado.CriarSucesso("12345678000195", "ConsultaTeste", situacao, DateTimeOffset.UtcNow);

        var json = JsonSerializer.Serialize(resultado, HttpOptions);
        var situacaoCadastralJson = ExtractPropertyRawJson(json, "situacaoCadastral");

        Assert.Equal(esperado, situacaoCadastralJson);
    }

    [Fact]
    public void SituacaoCadastral_Should_Serialize_As_Null_On_Failure()
    {
        var resultado = ConsultaCnpjResultado.CriarFalha("12345678000195", "ConsultaTeste", DateTimeOffset.UtcNow,
            TipoErroConsultaCnpj.NaoEncontrado);

        var json = JsonSerializer.Serialize(resultado, HttpOptions);
        var situacaoCadastralJson = ExtractPropertyRawJson(json, "situacaoCadastral");

        Assert.Equal("null", situacaoCadastralJson);
    }

    [Fact]
    public void TipoErro_Should_Also_Serialize_As_Explicit_String_Preserving_B2_4_Contract()
    {
        var resultado = ConsultaCnpjResultado.CriarFalha("12345678000195", "ConsultaTeste", DateTimeOffset.UtcNow,
            TipoErroConsultaCnpj.CnpjInvalido);

        var json = JsonSerializer.Serialize(resultado, HttpOptions);

        Assert.Equal("\"CnpjInvalido\"", ExtractPropertyRawJson(json, "tipoErro"));
        Assert.Equal("\"Falha\"", ExtractPropertyRawJson(json, "statusConsulta"));
    }

    private static JsonSerializerOptions CreateHttpOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    private static string ExtractPropertyRawJson(string json, string propertyName)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty(propertyName).GetRawText();
    }
}
