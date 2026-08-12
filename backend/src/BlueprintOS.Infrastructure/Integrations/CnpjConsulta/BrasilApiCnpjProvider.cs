using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.Extensions.Options;

namespace BlueprintOS.Infrastructure.Integrations.CnpjConsulta;

public sealed class BrasilApiCnpjProvider(HttpClient httpClient, IOptions<CnpjConsultaOptions> options) : ICnpjConsultaProviderComSnapshot
{
    public string FonteConsulta => "BrasilAPI";

    public async Task<ConsultaCnpjResultado> ConsultarAsync(string cnpjCpf, CancellationToken cancellationToken = default) =>
        (await ConsultarInternoAsync(cnpjCpf, cancellationToken)).Resultado;

    /// <summary>Mesma consulta de <see cref="ConsultarAsync"/>, também expondo o snapshot bruto já
    /// sanitizado (QSA e segredos removidos por <see cref="BrasilApiSnapshotSanitizer"/>) para registro
    /// de proveniência em <c>FornecedorCnpjConsultaHistorico</c> (B2.7/ADR-0023). O contrato canônico
    /// (<see cref="ConsultaCnpjResultado"/>) nunca depende deste snapshot.</summary>
    public Task<CnpjConsultaProviderResposta> ConsultarComSnapshotAsync(string cnpjCpf, CancellationToken cancellationToken = default) =>
        ConsultarInternoAsync(cnpjCpf, cancellationToken);

    private async Task<CnpjConsultaProviderResposta> ConsultarInternoAsync(string cnpjCpf, CancellationToken cancellationToken)
    {
        var dataConsulta = DateTimeOffset.UtcNow;
        string documento;
        try
        {
            documento = DocumentoFiscal.Create(cnpjCpf).Value;
        }
        catch (ArgumentException)
        {
            // CnpjInvalido é rejeitado antes de qualquer chamada externa: nunca há corpo de resposta,
            // logo nunca há snapshot bruto a capturar.
            return SemSnapshot(ConsultaCnpjResultado.CriarFalha(cnpjCpf, FonteConsulta, dataConsulta, TipoErroConsultaCnpj.CnpjInvalido));
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, options.Value.TimeoutSeconds)));
        using var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        try
        {
            using var response = await httpClient.GetAsync(documento, linkedToken.Token);
            var corpoBruto = await LerCorpoComSegurancaAsync(response, linkedToken.Token);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // 404 com corpo útil: a BrasilAPI retorna um corpo JSON de erro (ex.: {"message": "..."}),
                // que pode ter valor diagnóstico e não contém QSA — sanitizado e retido como qualquer outro.
                return ComSnapshot(ConsultaCnpjResultado.CriarFalha(documento, FonteConsulta, dataConsulta, TipoErroConsultaCnpj.NaoEncontrado), corpoBruto);
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return ComSnapshot(ConsultaCnpjResultado.CriarFalha(documento, FonteConsulta, dataConsulta, TipoErroConsultaCnpj.LimiteDeConsultas), corpoBruto);
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                // Falha de autenticação do PROVIDER (nossa própria credencial/config) nunca deve reter
                // snapshot — o corpo de um 401/403 tende a ser diagnóstico de infraestrutura, não de negócio.
                return SemSnapshot(ConsultaCnpjResultado.CriarFalha(documento, FonteConsulta, dataConsulta, TipoErroConsultaCnpj.ErroDeAutenticacaoDoProvider));
            }

            if (!response.IsSuccessStatusCode)
            {
                return SemSnapshot(ConsultaCnpjResultado.CriarFalha(documento, FonteConsulta, dataConsulta, TipoErroConsultaCnpj.FonteIndisponivel));
            }

            BrasilApiCnpjResponse? payload;
            try
            {
                payload = JsonSerializer.Deserialize<BrasilApiCnpjResponse>(corpoBruto ?? string.Empty);
            }
            catch (JsonException)
            {
                // Resposta 2xx mas corpo malformado: ainda assim tentamos reter um snapshot sanitizado
                // para diagnóstico (útil para entender o que a fonte realmente devolveu).
                return ComSnapshot(ConsultaCnpjResultado.CriarFalha(documento, FonteConsulta, dataConsulta, TipoErroConsultaCnpj.RespostaInvalida), corpoBruto);
            }

            if (payload is null || string.IsNullOrWhiteSpace(payload.Cnpj))
            {
                return ComSnapshot(ConsultaCnpjResultado.CriarFalha(documento, FonteConsulta, dataConsulta, TipoErroConsultaCnpj.RespostaInvalida), corpoBruto);
            }

            var resultadoSucesso = ConsultaCnpjResultado.CriarSucesso(
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
                porteEmpresa: payload.Porte,
                cnaePrincipalCodigo: payload.CnaeFiscal?.ToString(),
                cnaePrincipalDescricao: payload.CnaeFiscalDescricao);
                // payload.CnaesSecundarios é lido apenas para desserialização segura do payload da
                // BrasilAPI — nunca mapeado ao contrato canônico. CNAEs secundários morrem nesta
                // fronteira (B2.8, seção H de docs/audits/Arquitetura-Fornecedor-CNPJ-Decisao.md):
                // não atravessam para Application/Domain/DTO de criação/frontend/banco.
            return ComSnapshot(resultadoSucesso, corpoBruto);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            // Timeout: nunca há corpo de resposta utilizável — snapshot sempre nulo.
            return SemSnapshot(ConsultaCnpjResultado.CriarFalha(documento, FonteConsulta, dataConsulta, TipoErroConsultaCnpj.Timeout));
        }
        catch (HttpRequestException)
        {
            // Falha de conexão: idem, nunca há corpo de resposta.
            return SemSnapshot(ConsultaCnpjResultado.CriarFalha(documento, FonteConsulta, dataConsulta, TipoErroConsultaCnpj.FonteIndisponivel));
        }
        catch (Exception)
        {
            return SemSnapshot(ConsultaCnpjResultado.CriarFalha(documento, FonteConsulta, dataConsulta, TipoErroConsultaCnpj.ErroInterno));
        }
    }

    /// <summary>Lê o corpo da resposta como texto bruto, sem nunca lançar: um corpo ilegível para
    /// fins de snapshot nunca pode derrubar a classificação do resultado, que já foi decidida pelo
    /// status HTTP.</summary>
    private static async Task<string?> LerCorpoComSegurancaAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static CnpjConsultaProviderResposta SemSnapshot(ConsultaCnpjResultado resultado) =>
        new(resultado, null, false);

    private static CnpjConsultaProviderResposta ComSnapshot(ConsultaCnpjResultado resultado, string? corpoBruto)
    {
        var (snapshot, descartadoPorTamanho) = BrasilApiSnapshotSanitizer.Sanitizar(corpoBruto);
        return new(resultado, snapshot, descartadoPorTamanho);
    }

    private static string OnlyDigits(string value) => new(value.Where(char.IsDigit).ToArray());

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParse(value, out var date) ? date : null;

    private static string? FirstNotBlank(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    /// <summary>Traduz a descrição textual da situação cadastral retornada pela BrasilAPI
    /// (<c>descricao_situacao_cadastral</c>) para o enum canônico do +Compras. O código numérico
    /// bruto (<c>situacao_cadastral</c>) nunca é lido nem atravessa esta fronteira — a descrição
    /// textual é o campo estável documentado pela BrasilAPI/Receita, enquanto o código numérico é
    /// um detalhe de implementação da fonte. Qualquer texto não reconhecido (fonte alterou o
    /// vocabulário, campo ausente, etc.) cai em <see cref="SituacaoCadastralCnpj.Desconhecida"/> —
    /// nunca lança exceção nem interrompe a consulta.</summary>
    private static SituacaoCadastralCnpj MapSituacao(string? value) => value?.Trim().ToUpperInvariant() switch
    {
        "ATIVA" => SituacaoCadastralCnpj.Ativa,
        "BAIXADA" => SituacaoCadastralCnpj.Baixada,
        "SUSPENSA" => SituacaoCadastralCnpj.Suspensa,
        "INAPTA" => SituacaoCadastralCnpj.Inapta,
        "NULA" => SituacaoCadastralCnpj.Nula,
        _ => SituacaoCadastralCnpj.Desconhecida
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
        [property: JsonPropertyName("porte")] string? Porte,
        [property: JsonPropertyName("cnae_fiscal")] int? CnaeFiscal,
        [property: JsonPropertyName("cnae_fiscal_descricao")] string? CnaeFiscalDescricao,
        // Desserializado apenas para não falhar/perder o restante do payload — nunca lido nem mapeado
        // ao contrato canônico (B2.8). CNAEs secundários são descartados nesta fronteira.
        [property: JsonPropertyName("cnaes_secundarios")] IReadOnlyList<BrasilApiCnaeSecundarioItem>? CnaesSecundarios);

    private sealed record BrasilApiCnaeSecundarioItem(
        [property: JsonPropertyName("codigo")] int? Codigo,
        [property: JsonPropertyName("descricao")] string? Descricao);
}
