using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Integrations.CepConsulta;

/// <summary>ViaCEP (https://viacep.com.br/ws/{cep}/json/) — mesma fonte usada pela tela Linx
/// `001016G1` para consulta de CEP (achado 2, docs/audits/Discovery-Fornecedor-Tela-001016G1.md).
/// Deliberadamente distinta de <c>BrasilApiCnpjProvider</c> (CNPJ) — são provedores diferentes.</summary>
public sealed class ViaCepProvider(HttpClient httpClient, IOptions<CepConsultaOptions> options) : ICepConsultaProvider
{
    public string FonteConsulta => "ViaCEP";

    public async Task<ConsultaCepResultado> ConsultarAsync(string cep, CancellationToken cancellationToken = default)
    {
        var dataConsulta = DateTimeOffset.UtcNow;
        var digitos = OnlyDigits(cep ?? string.Empty);
        if (digitos.Length != 8)
        {
            return ConsultaCepResultado.CriarFalha(digitos, FonteConsulta, dataConsulta, TipoErroConsultaCep.CepInvalido);
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, options.Value.TimeoutSeconds)));
        using var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        try
        {
            using var response = await httpClient.GetAsync($"{digitos}/json/", linkedToken.Token);
            if (!response.IsSuccessStatusCode)
            {
                return ConsultaCepResultado.CriarFalha(digitos, FonteConsulta, dataConsulta, TipoErroConsultaCep.FonteIndisponivel);
            }

            var corpo = await response.Content.ReadAsStringAsync(linkedToken.Token);
            ViaCepResponse? payload;
            try
            {
                payload = JsonSerializer.Deserialize<ViaCepResponse>(corpo);
            }
            catch (JsonException)
            {
                return ConsultaCepResultado.CriarFalha(digitos, FonteConsulta, dataConsulta, TipoErroConsultaCep.RespostaInvalida);
            }

            // ViaCEP responde 200 com {"erro": true} para CEP inexistente — nunca 404 (achado 2).
            if (payload is null || payload.Erro == true)
            {
                return ConsultaCepResultado.CriarFalha(digitos, FonteConsulta, dataConsulta, TipoErroConsultaCep.NaoEncontrado);
            }

            return ConsultaCepResultado.CriarSucesso(digitos, FonteConsulta, dataConsulta,
                logradouro: payload.Logradouro, bairro: payload.Bairro, complemento: payload.Complemento,
                cidade: payload.Localidade, estado: payload.Uf);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return ConsultaCepResultado.CriarFalha(digitos, FonteConsulta, dataConsulta, TipoErroConsultaCep.Timeout);
        }
        catch (HttpRequestException)
        {
            return ConsultaCepResultado.CriarFalha(digitos, FonteConsulta, dataConsulta, TipoErroConsultaCep.FonteIndisponivel);
        }
        catch (Exception)
        {
            return ConsultaCepResultado.CriarFalha(digitos, FonteConsulta, dataConsulta, TipoErroConsultaCep.ErroInterno);
        }
    }

    private static string OnlyDigits(string value) => new(value.Where(char.IsDigit).ToArray());

    private sealed record ViaCepResponse(
        [property: JsonPropertyName("cep")] string? Cep,
        [property: JsonPropertyName("logradouro")] string? Logradouro,
        [property: JsonPropertyName("complemento")] string? Complemento,
        [property: JsonPropertyName("bairro")] string? Bairro,
        [property: JsonPropertyName("localidade")] string? Localidade,
        [property: JsonPropertyName("uf")] string? Uf,
        [property: JsonPropertyName("erro")] bool? Erro);
}
