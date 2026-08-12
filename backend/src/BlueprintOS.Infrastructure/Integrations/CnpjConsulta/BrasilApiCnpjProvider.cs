using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Integrations.CnpjConsulta;

public sealed class BrasilApiCnpjProvider(HttpClient httpClient, IOptions<CnpjConsultaOptions> options) : ICnpjConsultaProvider
{
    public string FonteConsulta => "BrasilAPI";

    public async Task<ConsultaCnpjResultado> ConsultarAsync(string cnpjCpf, CancellationToken cancellationToken = default)
    {
        var dataConsulta = DateTimeOffset.UtcNow;
        string documento;
        try
        {
            documento = DocumentoFiscal.Create(cnpjCpf).Value;
        }
        catch (ArgumentException)
        {
            return ConsultaCnpjResultado.CriarFalha(cnpjCpf, FonteConsulta, dataConsulta, TipoErroConsultaCnpj.CnpjInvalido);
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, options.Value.TimeoutSeconds)));
        using var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        try
        {
            using var response = await httpClient.GetAsync(documento, linkedToken.Token);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return ConsultaCnpjResultado.CriarFalha(documento, FonteConsulta, dataConsulta, TipoErroConsultaCnpj.NaoEncontrado);
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return ConsultaCnpjResultado.CriarFalha(documento, FonteConsulta, dataConsulta, TipoErroConsultaCnpj.LimiteDeConsultas);
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                return ConsultaCnpjResultado.CriarFalha(documento, FonteConsulta, dataConsulta, TipoErroConsultaCnpj.ErroDeAutenticacaoDoProvider);
            }

            if (!response.IsSuccessStatusCode)
            {
                return ConsultaCnpjResultado.CriarFalha(documento, FonteConsulta, dataConsulta, TipoErroConsultaCnpj.FonteIndisponivel);
            }

            var payload = await response.Content.ReadFromJsonAsync<BrasilApiCnpjResponse>(cancellationToken: linkedToken.Token);
            if (payload is null || string.IsNullOrWhiteSpace(payload.Cnpj))
            {
                return ConsultaCnpjResultado.CriarFalha(documento, FonteConsulta, dataConsulta, TipoErroConsultaCnpj.RespostaInvalida);
            }

            return ConsultaCnpjResultado.CriarSucesso(
                payload.Cnpj,
                FonteConsulta,
                MapSituacao(payload.DescricaoSituacaoCadastral),
                dataConsulta,
                razaoSocial: payload.RazaoSocial,
                nomeFantasia: payload.NomeFantasia,
                tipoPessoa: "Juridica",
                dataSituacaoCadastral: ParseDate(payload.DataSituacaoCadastral),
                dataAbertura: ParseDate(payload.DataInicioAtividade),
                cep: payload.Cep,
                logradouro: payload.Logradouro,
                numero: payload.Numero,
                complemento: payload.Complemento,
                bairro: payload.Bairro,
                cidade: payload.Municipio,
                estado: payload.Uf,
                pais: "Brasil",
                email: payload.Email,
                telefone: FirstNotBlank(payload.DddTelefone1, payload.DddTelefone2),
                naturezaJuridica: payload.NaturezaJuridica,
                porteEmpresa: payload.Porte);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return ConsultaCnpjResultado.CriarFalha(documento, FonteConsulta, dataConsulta, TipoErroConsultaCnpj.Timeout);
        }
        catch (HttpRequestException)
        {
            return ConsultaCnpjResultado.CriarFalha(documento, FonteConsulta, dataConsulta, TipoErroConsultaCnpj.FonteIndisponivel);
        }
        catch (JsonException)
        {
            return ConsultaCnpjResultado.CriarFalha(documento, FonteConsulta, dataConsulta, TipoErroConsultaCnpj.RespostaInvalida);
        }
        catch (Exception)
        {
            return ConsultaCnpjResultado.CriarFalha(documento, FonteConsulta, dataConsulta, TipoErroConsultaCnpj.ErroInterno);
        }
    }

    private static string OnlyDigits(string value) => new(value.Where(char.IsDigit).ToArray());

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParse(value, out var date) ? date : null;

    private static string? FirstNotBlank(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private static SituacaoCadastralCnpj MapSituacao(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        "ATIVA" => SituacaoCadastralCnpj.Ativa,
        "BAIXADA" => SituacaoCadastralCnpj.Baixada,
        "SUSPENSA" => SituacaoCadastralCnpj.Suspensa,
        "INAPTA" => SituacaoCadastralCnpj.Inapta,
        _ => SituacaoCadastralCnpj.NaoEncontrada
    };

    private sealed record BrasilApiCnpjResponse(
        [property: JsonPropertyName("cnpj")] string? Cnpj,
        [property: JsonPropertyName("razao_social")] string? RazaoSocial,
        [property: JsonPropertyName("nome_fantasia")] string? NomeFantasia,
        [property: JsonPropertyName("descricao_situacao_cadastral")] string? DescricaoSituacaoCadastral,
        [property: JsonPropertyName("data_situacao_cadastral")] string? DataSituacaoCadastral,
        [property: JsonPropertyName("data_inicio_atividade")] string? DataInicioAtividade,
        [property: JsonPropertyName("cep")] string? Cep,
        [property: JsonPropertyName("logradouro")] string? Logradouro,
        [property: JsonPropertyName("numero")] string? Numero,
        [property: JsonPropertyName("complemento")] string? Complemento,
        [property: JsonPropertyName("bairro")] string? Bairro,
        [property: JsonPropertyName("municipio")] string? Municipio,
        [property: JsonPropertyName("uf")] string? Uf,
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("ddd_telefone_1")] string? DddTelefone1,
        [property: JsonPropertyName("ddd_telefone_2")] string? DddTelefone2,
        [property: JsonPropertyName("natureza_juridica")] string? NaturezaJuridica,
        [property: JsonPropertyName("porte")] string? Porte);
}
