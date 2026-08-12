using System.Net;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Infrastructure.Integrations.CnpjConsulta;
using Microsoft.Extensions.Options;

namespace BlueprintOS.UnitTests.Infrastructure.Integrations.CnpjConsulta;

public sealed class BrasilApiCnpjProviderTests
{
    [Fact]
    public async Task ConsultarAsync_Should_Map_Success_Response()
    {
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.OK, """
        {
          "cnpj": "12345678000195",
          "razao_social": "Fornecedor Brasil API Ltda",
          "nome_fantasia": "Fornecedor API",
          "descricao_situacao_cadastral": "ATIVA",
          "data_situacao_cadastral": "2024-01-10",
          "cep": "01311000",
          "logradouro": "Avenida Paulista",
          "numero": "1000",
          "complemento": "10 andar",
          "bairro": "Bela Vista",
          "municipio": "São Paulo",
          "uf": "SP",
          "email": "contato@example.com",
          "ddd_telefone_1": "1133334444",
          "natureza_juridica": "Sociedade Empresária Limitada",
          "porte": "DEMAIS"
        }
        """));

        var result = await provider.ConsultarAsync("12.345.678/0001-95");

        Assert.True(result.Sucesso);
        Assert.Equal("12345678000195", result.Cnpj_Cpf);
        Assert.Equal("Fornecedor Brasil API Ltda", result.RazaoSocial);
        Assert.Equal("Fornecedor API", result.NomeFantasia);
        Assert.Equal("Juridica", result.TipoPessoa);
        Assert.Equal(SituacaoCadastralCnpj.Ativa, result.SituacaoCadastral);
        Assert.Equal(new DateOnly(2024, 1, 10), result.DataSituacaoCadastral);
        Assert.Equal("01311000", result.Cep);
        Assert.Equal("Avenida Paulista", result.Logradouro);
        Assert.Equal("1000", result.Numero);
        Assert.Equal("10 andar", result.Complemento);
        Assert.Equal("Bela Vista", result.Bairro);
        Assert.Equal("São Paulo", result.Cidade);
        Assert.Equal("SP", result.Estado);
        Assert.Equal("contato@example.com", result.Email);
        Assert.Equal("1133334444", result.Telefone);
        Assert.Equal("BrasilAPI", result.FonteConsulta);
        Assert.Null(result.TipoErro);
        Assert.False(result.PermiteRetry);
    }

    [Fact]
    public async Task ConsultarAsync_Should_Return_Timeout_Failure()
    {
        var provider = CreateProvider(new DelayedHandler(TimeSpan.FromSeconds(5)), timeoutSeconds: 1);

        var result = await provider.ConsultarAsync("12345678000195");

        Assert.False(result.Sucesso);
        Assert.Equal(StatusConsultaCnpj.Falha, result.StatusConsulta);
        Assert.Equal(TipoErroConsultaCnpj.Timeout, result.TipoErro);
        Assert.True(result.PermiteRetry);
    }

    [Fact]
    public async Task ConsultarAsync_Should_Return_External_Error_Failure()
    {
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.ServiceUnavailable, "{}"));

        var result = await provider.ConsultarAsync("12345678000195");

        Assert.False(result.Sucesso);
        Assert.Equal(TipoErroConsultaCnpj.FonteIndisponivel, result.TipoErro);
    }

    [Fact]
    public async Task ConsultarAsync_Should_Return_NotFound_Failure()
    {
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.NotFound, "{}"));

        var result = await provider.ConsultarAsync("12345678000195");

        Assert.False(result.Sucesso);
        Assert.Equal(TipoErroConsultaCnpj.NaoEncontrado, result.TipoErro);
        Assert.False(result.PermiteRetry);
    }

    [Fact]
    public async Task ConsultarAsync_Should_Return_TooManyRequests_Failure()
    {
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.TooManyRequests, "{}"));

        var result = await provider.ConsultarAsync("12345678000195");

        Assert.False(result.Sucesso);
        Assert.Equal(TipoErroConsultaCnpj.LimiteDeConsultas, result.TipoErro);
        Assert.True(result.PermiteRetry);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task ConsultarAsync_Should_Return_AuthError_Failure(HttpStatusCode statusCode)
    {
        var provider = CreateProvider(new JsonHandler(statusCode, "{}"));

        var result = await provider.ConsultarAsync("12345678000195");

        Assert.False(result.Sucesso);
        Assert.Equal(TipoErroConsultaCnpj.ErroDeAutenticacaoDoProvider, result.TipoErro);
        Assert.False(result.PermiteRetry);
    }

    [Fact]
    public async Task ConsultarAsync_Should_Return_RespostaInvalida_When_Payload_Is_Malformed()
    {
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.OK, "not-json"));

        var result = await provider.ConsultarAsync("12345678000195");

        Assert.False(result.Sucesso);
        Assert.Equal(TipoErroConsultaCnpj.RespostaInvalida, result.TipoErro);
        Assert.True(result.PermiteRetry);
    }

    [Fact]
    public async Task ConsultarAsync_Should_Return_RespostaInvalida_When_Cnpj_Field_Is_Blank()
    {
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.OK, "{}"));

        var result = await provider.ConsultarAsync("12345678000195");

        Assert.False(result.Sucesso);
        Assert.Equal(TipoErroConsultaCnpj.RespostaInvalida, result.TipoErro);
    }

    [Fact]
    public async Task ConsultarAsync_Should_Return_Invalid_Format_Failure_Without_Calling_Provider()
    {
        var provider = CreateProvider(new ThrowingHandler());

        var result = await provider.ConsultarAsync("123");

        Assert.False(result.Sucesso);
        Assert.Equal(TipoErroConsultaCnpj.CnpjInvalido, result.TipoErro);
        Assert.False(result.PermiteRetry);
    }

    [Fact]
    public async Task ConsultarAsync_Should_Reject_Checksum_Invalid_Cnpj_Without_Calling_Provider()
    {
        // Regressao do BUG-3: um CNPJ com 14 digitos mas digito verificador incorreto
        // nao pode chegar a chamar o provider externo.
        var provider = CreateProvider(new ThrowingHandler());

        var result = await provider.ConsultarAsync("12345678000100");

        Assert.False(result.Sucesso);
        Assert.Equal(TipoErroConsultaCnpj.CnpjInvalido, result.TipoErro);
        Assert.False(result.PermiteRetry);
    }

    [Fact]
    public async Task ConsultarAsync_Should_Respect_CancellationToken()
    {
        var provider = CreateProvider(new DelayedHandler(TimeSpan.FromSeconds(5)), timeoutSeconds: 10);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.ConsultarAsync("12345678000195", cancellation.Token));
    }

    [Fact]
    public async Task ConsultarAsync_Should_Map_Baixada_As_Success_With_Warning_State()
    {
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.OK, """
        {
          "cnpj": "12345678000195",
          "razao_social": "Fornecedor Baixado",
          "descricao_situacao_cadastral": "BAIXADA"
        }
        """));

        var result = await provider.ConsultarAsync("12345678000195");

        Assert.True(result.Sucesso);
        Assert.Equal(SituacaoCadastralCnpj.Baixada, result.SituacaoCadastral);
        Assert.Null(result.MensagemErro);
        Assert.Null(result.TipoErro);
    }

    [Theory]
    [InlineData("SUSPENSA", SituacaoCadastralCnpj.Suspensa)]
    [InlineData("INAPTA", SituacaoCadastralCnpj.Inapta)]
    [InlineData("NULA", SituacaoCadastralCnpj.Nula)]
    [InlineData("suspensa", SituacaoCadastralCnpj.Suspensa)]
    public async Task ConsultarAsync_Should_Map_Each_Known_Situacao_Cadastral(string descricao, SituacaoCadastralCnpj esperado)
    {
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.OK, $$"""
        {
          "cnpj": "12345678000195",
          "razao_social": "Fornecedor Situacao",
          "descricao_situacao_cadastral": "{{descricao}}"
        }
        """));

        var result = await provider.ConsultarAsync("12345678000195");

        Assert.True(result.Sucesso);
        Assert.Equal(esperado, result.SituacaoCadastral);
        Assert.Null(result.TipoErro);
    }

    [Fact]
    public async Task ConsultarAsync_Should_Map_Unrecognized_Situacao_Cadastral_To_Desconhecida()
    {
        // A fonte externa pode alterar seu vocabulario de situacao cadastral sem aviso;
        // o +Compras nunca deve lancar excecao de apresentacao por isso.
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.OK, """
        {
          "cnpj": "12345678000195",
          "razao_social": "Fornecedor Situacao Nova",
          "descricao_situacao_cadastral": "SITUACAO_NOVA_DA_RECEITA"
        }
        """));

        var result = await provider.ConsultarAsync("12345678000195");

        Assert.True(result.Sucesso);
        Assert.Equal(SituacaoCadastralCnpj.Desconhecida, result.SituacaoCadastral);
    }

    [Fact]
    public async Task ConsultarAsync_Should_Map_Missing_Situacao_Cadastral_To_Desconhecida()
    {
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.OK, """
        {
          "cnpj": "12345678000195",
          "razao_social": "Fornecedor Sem Situacao"
        }
        """));

        var result = await provider.ConsultarAsync("12345678000195");

        Assert.True(result.Sucesso);
        Assert.Equal(SituacaoCadastralCnpj.Desconhecida, result.SituacaoCadastral);
    }

    [Fact]
    public async Task ConsultarAsync_Failure_Should_Never_Carry_A_Situacao_Cadastral()
    {
        // SituacaoCadastral so existe em consultas bem-sucedidas — falha (qualquer TipoErro)
        // nunca deve carregar um valor de situacao cadastral (nem um "placeholder" sobrecarregado).
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.NotFound, "{}"));

        var result = await provider.ConsultarAsync("12345678000195");

        Assert.False(result.Sucesso);
        Assert.Null(result.SituacaoCadastral);
    }

    [Fact]
    public async Task ConsultarComSnapshotAsync_Should_Return_Sanitized_Snapshot_Without_Qsa_On_Success()
    {
        // Fixture sintetica com QSA falso — nunca dados reais de fornecedor (regra de testes B2.7).
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.OK, """
        {
          "cnpj": "12345678000195",
          "razao_social": "Fornecedor Brasil API Ltda",
          "descricao_situacao_cadastral": "ATIVA",
          "qsa": [
            { "nome_socio": "Socio Fake Um", "cpf_cnpj_socio": "***111111**", "qualificacao_socio": "Socio-Administrador" },
            { "nome_socio": "Socio Fake Dois", "cpf_cnpj_socio": "***222222**", "qualificacao_socio": "Socio" }
          ]
        }
        """));

        var resposta = await provider.ConsultarComSnapshotAsync("12345678000195");

        Assert.True(resposta.Resultado.Sucesso);
        Assert.NotNull(resposta.SnapshotBrutoSanitizado);
        Assert.DoesNotContain("qsa", resposta.SnapshotBrutoSanitizado, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Socio Fake Um", resposta.SnapshotBrutoSanitizado);
        Assert.DoesNotContain("Socio Fake Dois", resposta.SnapshotBrutoSanitizado);
        Assert.Contains("Fornecedor Brasil API Ltda", resposta.SnapshotBrutoSanitizado);
        Assert.False(resposta.SnapshotDescartadoPorTamanho);
    }

    [Fact]
    public async Task ConsultarComSnapshotAsync_Should_Return_Null_Snapshot_On_Timeout()
    {
        var provider = CreateProvider(new DelayedHandler(TimeSpan.FromSeconds(5)), timeoutSeconds: 1);

        var resposta = await provider.ConsultarComSnapshotAsync("12345678000195");

        Assert.False(resposta.Resultado.Sucesso);
        Assert.Equal(TipoErroConsultaCnpj.Timeout, resposta.Resultado.TipoErro);
        Assert.Null(resposta.SnapshotBrutoSanitizado);
        Assert.False(resposta.SnapshotDescartadoPorTamanho);
    }

    [Fact]
    public async Task ConsultarComSnapshotAsync_Should_Return_Null_Snapshot_For_CnpjInvalido_Without_Calling_Provider()
    {
        var provider = CreateProvider(new ThrowingHandler());

        var resposta = await provider.ConsultarComSnapshotAsync("123");

        Assert.Equal(TipoErroConsultaCnpj.CnpjInvalido, resposta.Resultado.TipoErro);
        Assert.Null(resposta.SnapshotBrutoSanitizado);
    }

    [Fact]
    public async Task ConsultarComSnapshotAsync_Should_Sanitize_Snapshot_On_NotFound_With_Useful_Body()
    {
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.NotFound, """{"message":"CNPJ nao encontrado na base."}"""));

        var resposta = await provider.ConsultarComSnapshotAsync("12345678000195");

        Assert.Equal(TipoErroConsultaCnpj.NaoEncontrado, resposta.Resultado.TipoErro);
        Assert.NotNull(resposta.SnapshotBrutoSanitizado);
        Assert.Contains("CNPJ nao encontrado", resposta.SnapshotBrutoSanitizado);
    }

    [Fact]
    public async Task ConsultarComSnapshotAsync_Should_Return_Null_Snapshot_For_Malformed_Non_Json_Body()
    {
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.OK, "not-json"));

        var resposta = await provider.ConsultarComSnapshotAsync("12345678000195");

        Assert.Equal(TipoErroConsultaCnpj.RespostaInvalida, resposta.Resultado.TipoErro);
        Assert.Null(resposta.SnapshotBrutoSanitizado);
    }

    [Fact]
    public async Task ConsultarComSnapshotAsync_Should_Discard_Oversized_Snapshot_With_Flag()
    {
        var razaoSocialEnorme = new string('X', 40_000);
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.OK,
            $$"""{"cnpj":"12345678000195","razao_social":"{{razaoSocialEnorme}}","descricao_situacao_cadastral":"ATIVA"}"""));

        var resposta = await provider.ConsultarComSnapshotAsync("12345678000195");

        Assert.True(resposta.Resultado.Sucesso);
        Assert.Null(resposta.SnapshotBrutoSanitizado);
        Assert.True(resposta.SnapshotDescartadoPorTamanho);
    }

    [Fact]
    public async Task ConsultarComSnapshotAsync_Should_Return_Null_Snapshot_On_Provider_AuthError()
    {
        // Corpo de 401/403 e diagnostico de infraestrutura da propria integracao, nao de negocio —
        // nunca retido como snapshot de proveniencia.
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.Unauthorized, """{"error":"invalid api key"}"""));

        var resposta = await provider.ConsultarComSnapshotAsync("12345678000195");

        Assert.Equal(TipoErroConsultaCnpj.ErroDeAutenticacaoDoProvider, resposta.Resultado.TipoErro);
        Assert.Null(resposta.SnapshotBrutoSanitizado);
    }

    [Fact]
    public async Task ConsultarAsync_Should_Return_Same_Canonical_Result_As_ConsultarComSnapshotAsync()
    {
        // O contrato canonico (ConsultaCnpjResultado) nunca deve variar dependendo de qual dos dois
        // metodos foi chamado — o snapshot e estritamente aditivo, nunca uma segunda fonte de verdade.
        var provider = CreateProvider(new JsonHandler(HttpStatusCode.OK, """
        {"cnpj":"12345678000195","razao_social":"Fornecedor Brasil API Ltda","descricao_situacao_cadastral":"ATIVA"}
        """));

        var direto = await provider.ConsultarAsync("12345678000195");
        var comSnapshot = await provider.ConsultarComSnapshotAsync("12345678000195");

        // DataConsulta naturalmente varia entre as duas chamadas (UtcNow independente) — comparamos
        // o conteudo canonico, nao o timestamp de cada chamada isolada.
        Assert.Equal(direto.Cnpj_Cpf, comSnapshot.Resultado.Cnpj_Cpf);
        Assert.Equal(direto.RazaoSocial, comSnapshot.Resultado.RazaoSocial);
        Assert.Equal(direto.SituacaoCadastral, comSnapshot.Resultado.SituacaoCadastral);
        Assert.Equal(direto.Sucesso, comSnapshot.Resultado.Sucesso);
        Assert.Equal(direto.FonteConsulta, comSnapshot.Resultado.FonteConsulta);
    }

    private static BrasilApiCnpjProvider CreateProvider(HttpMessageHandler handler, int timeoutSeconds = 10)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/api/cnpj/v1/") };
        return new BrasilApiCnpjProvider(httpClient, Options.Create(new CnpjConsultaOptions
        {
            BaseUrl = "https://example.test/api/cnpj/v1/",
            TimeoutSeconds = timeoutSeconds
        }));
    }

    private sealed class JsonHandler(HttpStatusCode statusCode, string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json)
            };
            return Task.FromResult(response);
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("HTTP call should not have been made for a locally-invalid CNPJ.");
    }

    private sealed class DelayedHandler(TimeSpan delay) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
