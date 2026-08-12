using System.Text.Json.Nodes;
using BlueprintOS.Domain.Procurement.Suppliers;
using BlueprintOS.Infrastructure.Integrations.CnpjConsulta;

namespace BlueprintOS.UnitTests.Infrastructure.Integrations.CnpjConsulta;

public sealed class BrasilApiSnapshotSanitizerTests
{
    [Fact]
    public void Sanitizar_Should_Remove_Qsa_Field_Entirely()
    {
        // QSA sintetico — nunca dados reais (regra de testes B2.7/ADR-0023).
        const string raw = """
        {
          "cnpj": "12345678000195",
          "razao_social": "Fornecedor Teste Ltda",
          "qsa": [ { "nome_socio": "Socio Fake", "cpf_cnpj_socio": "***999999**" } ]
        }
        """;

        var (snapshot, descartado) = BrasilApiSnapshotSanitizer.Sanitizar(raw);

        Assert.NotNull(snapshot);
        Assert.False(descartado);
        var node = JsonNode.Parse(snapshot)!.AsObject();
        Assert.False(node.ContainsKey("qsa"));
        Assert.DoesNotContain("Socio Fake", snapshot);
        Assert.Equal("Fornecedor Teste Ltda", node["razao_social"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("QSA")]
    [InlineData("Qsa")]
    [InlineData("qsa")]
    public void Sanitizar_Should_Remove_Qsa_Key_Case_Insensitively(string chave)
    {
        var raw = $$"""{"cnpj":"12345678000195","{{chave}}":[{"nome_socio":"Socio Fake"}]}""";

        var (snapshot, _) = BrasilApiSnapshotSanitizer.Sanitizar(raw);

        Assert.NotNull(snapshot);
        Assert.DoesNotContain("Socio Fake", snapshot);
    }

    [Fact]
    public void Sanitizar_Should_Remove_Defensive_Secret_Like_Keys()
    {
        var raw = """
        {
          "cnpj": "12345678000195",
          "authorization": "Bearer super-secret-token",
          "api_key": "sk-fake-12345",
          "senha": "fake-password"
        }
        """;

        var (snapshot, _) = BrasilApiSnapshotSanitizer.Sanitizar(raw);

        Assert.NotNull(snapshot);
        Assert.DoesNotContain("super-secret-token", snapshot);
        Assert.DoesNotContain("sk-fake-12345", snapshot);
        Assert.DoesNotContain("fake-password", snapshot);
    }

    [Fact]
    public void Sanitizar_Should_Return_Null_For_Null_Or_Blank_Body()
    {
        Assert.Equal((null, false), BrasilApiSnapshotSanitizer.Sanitizar(null));
        Assert.Equal((null, false), BrasilApiSnapshotSanitizer.Sanitizar(""));
        Assert.Equal((null, false), BrasilApiSnapshotSanitizer.Sanitizar("   "));
    }

    [Fact]
    public void Sanitizar_Should_Return_Null_For_Malformed_Json()
    {
        var (snapshot, descartado) = BrasilApiSnapshotSanitizer.Sanitizar("not-json-at-all");

        Assert.Null(snapshot);
        Assert.False(descartado);
    }

    [Fact]
    public void Sanitizar_Should_Return_Null_For_Non_Object_Json()
    {
        var (snapshot, _) = BrasilApiSnapshotSanitizer.Sanitizar("[1,2,3]");

        Assert.Null(snapshot);
    }

    [Fact]
    public void Sanitizar_Should_Discard_And_Flag_When_Sanitized_Body_Exceeds_Limit()
    {
        var valorEnorme = new string('a', FornecedorCnpjConsultaHistorico.LimitePayloadBrutoCaracteres + 100);
        var raw = $$"""{"cnpj":"12345678000195","razao_social":"{{valorEnorme}}"}""";

        var (snapshot, descartado) = BrasilApiSnapshotSanitizer.Sanitizar(raw);

        Assert.Null(snapshot);
        Assert.True(descartado);
    }

    [Fact]
    public void Sanitizar_Should_Keep_Company_Registration_Fields()
    {
        var raw = """
        {
          "cnpj": "12345678000195",
          "razao_social": "Fornecedor Teste Ltda",
          "descricao_situacao_cadastral": "ATIVA",
          "logradouro": "Rua Teste",
          "municipio": "Sao Paulo"
        }
        """;

        var (snapshot, _) = BrasilApiSnapshotSanitizer.Sanitizar(raw);

        Assert.NotNull(snapshot);
        var node = JsonNode.Parse(snapshot)!.AsObject();
        Assert.Equal("12345678000195", node["cnpj"]!.GetValue<string>());
        Assert.Equal("ATIVA", node["descricao_situacao_cadastral"]!.GetValue<string>());
        Assert.Equal("Rua Teste", node["logradouro"]!.GetValue<string>());
    }
}
