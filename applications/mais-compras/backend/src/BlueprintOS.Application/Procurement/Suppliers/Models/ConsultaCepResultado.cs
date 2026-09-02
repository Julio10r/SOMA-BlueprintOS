namespace BlueprintOS.Application.Procurement.Suppliers.Models;

public enum StatusConsultaCep { Sucesso, Falha }

public enum TipoErroConsultaCep
{
    CepInvalido,
    NaoEncontrado,
    FonteIndisponivel,
    Timeout,
    RespostaInvalida,
    ErroInterno
}

public static class TipoErroConsultaCepExtensions
{
    public static int HttpStatusSugerido(this TipoErroConsultaCep tipoErro) => tipoErro switch
    {
        TipoErroConsultaCep.CepInvalido => 400,
        TipoErroConsultaCep.NaoEncontrado => 404,
        TipoErroConsultaCep.FonteIndisponivel => 503,
        TipoErroConsultaCep.Timeout => 504,
        TipoErroConsultaCep.RespostaInvalida => 502,
        TipoErroConsultaCep.ErroInterno => 500,
        _ => 500
    };
}

/// <summary>Resultado canônico da consulta de CEP (Gate de homologação de Fornecedores,
/// 2026-09-01, item 6). Espelha a forma de <c>ConsultaCnpjResultado</c> — Sucesso/Falha explícito,
/// nunca lança para um CEP não encontrado/mal formatado — mas deliberadamente mais simples: não há
/// histórico de auditoria por consulta de CEP (diferente de CNPJ, que tem obrigação regulatória de
/// proveniência via ADR-0023/B2.7); se essa necessidade surgir, é uma decisão de produto separada,
/// não assumida aqui.</summary>
public sealed record ConsultaCepResultado(
    string Cep,
    string? Logradouro,
    string? Bairro,
    string? Complemento,
    string? Cidade,
    string? Estado,
    string FonteConsulta,
    DateTimeOffset DataConsulta,
    StatusConsultaCep StatusConsulta,
    string? MensagemErro,
    TipoErroConsultaCep? TipoErro = null)
{
    public bool Sucesso => StatusConsulta == StatusConsultaCep.Sucesso;
    public int? HttpStatusSugerido => TipoErro?.HttpStatusSugerido();

    public static ConsultaCepResultado CriarSucesso(string cep, string fonteConsulta, DateTimeOffset dataConsulta,
        string? logradouro, string? bairro, string? complemento, string? cidade, string? estado)
    {
        if (string.IsNullOrWhiteSpace(fonteConsulta)) throw new ArgumentException("FonteConsulta is required.", nameof(fonteConsulta));
        return new(cep, logradouro?.Trim(), bairro?.Trim(), complemento?.Trim(), cidade?.Trim(), estado?.Trim(),
            fonteConsulta.Trim(), dataConsulta, StatusConsultaCep.Sucesso, null);
    }

    public static ConsultaCepResultado CriarFalha(string cep, string fonteConsulta, DateTimeOffset dataConsulta,
        TipoErroConsultaCep tipoErro, string? mensagemErro = null)
    {
        if (string.IsNullOrWhiteSpace(fonteConsulta)) throw new ArgumentException("FonteConsulta is required.", nameof(fonteConsulta));
        var mensagem = string.IsNullOrWhiteSpace(mensagemErro) ? MensagemPadrao(tipoErro) : mensagemErro.Trim();
        return new(new string((cep ?? string.Empty).Where(char.IsDigit).ToArray()), null, null, null, null, null,
            fonteConsulta.Trim(), dataConsulta, StatusConsultaCep.Falha, mensagem, tipoErro);
    }

    private static string MensagemPadrao(TipoErroConsultaCep tipoErro) => tipoErro switch
    {
        TipoErroConsultaCep.CepInvalido => "CEP informado é inválido. Verifique os dígitos.",
        TipoErroConsultaCep.NaoEncontrado => "CEP não encontrado.",
        TipoErroConsultaCep.FonteIndisponivel => "Não foi possível consultar o CEP agora. Tente novamente em alguns minutos.",
        TipoErroConsultaCep.Timeout => "A consulta demorou demais. Tente novamente.",
        // Retest do Gate de Fornecedores (2026-09-01), item 7: uma resposta em formato inesperado da
        // fonte externa (ou uma falha interna) não é culpa do usuário nem algo que ele possa corrigir —
        // a mensagem nunca deve soar como um erro técnico assustador ("contate o suporte" sozinho lê
        // como beco sem saída). O caminho real sempre disponível é preencher manualmente; o detalhe
        // técnico (TipoErro, aqui preservado) continua indo para logs/auditoria, nunca escondido — só
        // não aparece cru para quem está preenchendo o formulário.
        TipoErroConsultaCep.RespostaInvalida => "Não foi possível consultar o CEP automaticamente. Você pode preencher o endereço manualmente.",
        TipoErroConsultaCep.ErroInterno => "Não foi possível consultar o CEP agora. Você pode preencher o endereço manualmente.",
        _ => "Erro interno. Tente novamente ou contate o suporte."
    };
}

public sealed record ConsultarCepFornecedorDto(string Cep);
