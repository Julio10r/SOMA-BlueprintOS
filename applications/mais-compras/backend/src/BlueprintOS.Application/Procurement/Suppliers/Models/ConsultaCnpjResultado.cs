using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.Application.Procurement.Suppliers.Models;

/// <summary>Situação cadastral canônica do CNPJ no +Compras, desacoplada de qualquer
/// provider externo. <see cref="Desconhecida"/> é o estado seguro para texto retornado
/// pela fonte externa que não corresponda a nenhum valor conhecido — nunca lança exceção
/// de apresentação. Não confundir com <c>Fornecedor.Status</c> (estado operacional do
/// fornecedor no +Compras) nem com o indicador binário INATIVO do cadastro Linx: são
/// conceitos diferentes (estado cadastral/fiscal da fonte de consulta).</summary>
public enum SituacaoCadastralCnpj { Ativa, Baixada, Suspensa, Inapta, Nula, Desconhecida }
public enum StatusConsultaCnpj { Sucesso, Falha }

public enum TipoErroConsultaCnpj
{
    CnpjInvalido,
    NaoEncontrado,
    FonteIndisponivel,
    Timeout,
    LimiteDeConsultas,
    ErroDeAutenticacaoDoProvider,
    RespostaInvalida,
    ErroInterno
}

public static class TipoErroConsultaCnpjExtensions
{
    public static int HttpStatusSugerido(this TipoErroConsultaCnpj tipoErro) => tipoErro switch
    {
        TipoErroConsultaCnpj.CnpjInvalido => 400,
        TipoErroConsultaCnpj.NaoEncontrado => 404,
        TipoErroConsultaCnpj.FonteIndisponivel => 503,
        TipoErroConsultaCnpj.Timeout => 504,
        TipoErroConsultaCnpj.LimiteDeConsultas => 429,
        TipoErroConsultaCnpj.ErroDeAutenticacaoDoProvider => 502,
        TipoErroConsultaCnpj.RespostaInvalida => 502,
        TipoErroConsultaCnpj.ErroInterno => 500,
        _ => 500
    };

    public static bool PermiteRetry(this TipoErroConsultaCnpj tipoErro) => tipoErro switch
    {
        TipoErroConsultaCnpj.CnpjInvalido => false,
        TipoErroConsultaCnpj.NaoEncontrado => false,
        TipoErroConsultaCnpj.FonteIndisponivel => true,
        TipoErroConsultaCnpj.Timeout => true,
        TipoErroConsultaCnpj.LimiteDeConsultas => true,
        TipoErroConsultaCnpj.ErroDeAutenticacaoDoProvider => false,
        TipoErroConsultaCnpj.RespostaInvalida => true,
        TipoErroConsultaCnpj.ErroInterno => true,
        _ => false
    };
}

public sealed record ConsultaCnpjResultado(
    string Cnpj_Cpf,
    string? RazaoSocial,
    string? NomeFantasia,
    string? TipoPessoa,
    SituacaoCadastralCnpj? SituacaoCadastral,
    DateOnly? DataSituacaoCadastral,
    DateOnly? DataAbertura,
    string? Cep,
    string? Logradouro,
    string? Numero,
    string? Complemento,
    string? Bairro,
    string? Cidade,
    string? Estado,
    string? Pais,
    string? Email,
    string? Telefone,
    string? NaturezaJuridica,
    string? PorteEmpresa,
    string? CnaePrincipalCodigo,
    string? CnaePrincipalDescricao,
    string FonteConsulta,
    DateTimeOffset DataConsulta,
    StatusConsultaCnpj StatusConsulta,
    string? MensagemErro,
    TipoErroConsultaCnpj? TipoErro = null)
{
    public bool Sucesso => StatusConsulta == StatusConsultaCnpj.Sucesso;
    public bool PermiteRetry => TipoErro?.PermiteRetry() ?? false;
    public int? HttpStatusSugerido => TipoErro?.HttpStatusSugerido();

    public static ConsultaCnpjResultado CriarSucesso(string cnpjCpf, string fonteConsulta,
        SituacaoCadastralCnpj situacaoCadastral, DateTimeOffset dataConsulta,
        string? razaoSocial = null, string? nomeFantasia = null, string? tipoPessoa = null,
        DateOnly? dataSituacaoCadastral = null, DateOnly? dataAbertura = null,
        string? cep = null, string? logradouro = null, string? numero = null, string? complemento = null,
        string? bairro = null, string? cidade = null, string? estado = null, string? pais = null,
        string? email = null, string? telefone = null, string? naturezaJuridica = null, string? porteEmpresa = null,
        string? cnaePrincipalCodigo = null, string? cnaePrincipalDescricao = null)
    {
        var documento = DocumentoFiscal.Create(cnpjCpf).Value;
        if (string.IsNullOrWhiteSpace(fonteConsulta)) throw new ArgumentException("FonteConsulta is required.", nameof(fonteConsulta));
        return new(documento, razaoSocial?.Trim(), nomeFantasia?.Trim(), tipoPessoa?.Trim(), situacaoCadastral,
            dataSituacaoCadastral, dataAbertura, cep?.Trim(), logradouro?.Trim(), numero?.Trim(), complemento?.Trim(),
            bairro?.Trim(), cidade?.Trim(), estado?.Trim(), pais?.Trim(), email?.Trim(), telefone?.Trim(),
            naturezaJuridica?.Trim(), porteEmpresa?.Trim(),
            Fornecedor.NormalizarCnaeCodigo(cnaePrincipalCodigo),
            string.IsNullOrWhiteSpace(cnaePrincipalDescricao) ? null : cnaePrincipalDescricao.Trim(),
            fonteConsulta.Trim(), dataConsulta, StatusConsultaCnpj.Sucesso, null);
    }

    public static ConsultaCnpjResultado CriarFalha(string cnpjCpf, string fonteConsulta, DateTimeOffset dataConsulta,
        TipoErroConsultaCnpj tipoErro, string? mensagemErro = null)
    {
        // Documento não é validado aqui de propósito: CriarFalha também representa o caso CnpjInvalido
        // (seção K do relatório de arquitetura, ADR-0023) — exigir um documento válido para registrar
        // a própria falha de invalidez recriaria o BUG-3 (documento inválido nunca deve lançar exceção
        // antes de produzir uma resposta de erro classificada).
        var documento = new string((cnpjCpf ?? string.Empty).Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(fonteConsulta)) throw new ArgumentException("FonteConsulta is required.", nameof(fonteConsulta));
        var mensagem = string.IsNullOrWhiteSpace(mensagemErro) ? MensagemPadrao(tipoErro) : mensagemErro.Trim();
        // SituacaoCadastral so e preenchida em consultas bem-sucedidas (nunca em falha) —
        // separa semanticamente "consulta falhou" de "situacao cadastral desconhecida/nao encontrada".
        return new(documento,
            RazaoSocial: null, NomeFantasia: null, TipoPessoa: null, SituacaoCadastral: null,
            DataSituacaoCadastral: null, DataAbertura: null, Cep: null, Logradouro: null, Numero: null,
            Complemento: null, Bairro: null, Cidade: null, Estado: null, Pais: null, Email: null, Telefone: null,
            NaturezaJuridica: null, PorteEmpresa: null, CnaePrincipalCodigo: null, CnaePrincipalDescricao: null,
            FonteConsulta: fonteConsulta.Trim(), DataConsulta: dataConsulta,
            StatusConsulta: StatusConsultaCnpj.Falha, MensagemErro: mensagem, TipoErro: tipoErro);
    }

    private static string MensagemPadrao(TipoErroConsultaCnpj tipoErro) => tipoErro switch
    {
        TipoErroConsultaCnpj.CnpjInvalido => "CNPJ informado é inválido. Verifique os dígitos.",
        TipoErroConsultaCnpj.NaoEncontrado => "CNPJ não encontrado na base da Receita Federal.",
        TipoErroConsultaCnpj.FonteIndisponivel => "Não foi possível consultar o CNPJ agora. Tente novamente em alguns minutos.",
        TipoErroConsultaCnpj.Timeout => "A consulta demorou demais. Tente novamente.",
        TipoErroConsultaCnpj.LimiteDeConsultas => "Limite de consultas excedido. Tente novamente em breve.",
        TipoErroConsultaCnpj.ErroDeAutenticacaoDoProvider => "Erro de configuração da integração. Contate o suporte.",
        TipoErroConsultaCnpj.RespostaInvalida => "Resposta inesperada da fonte externa. Contate o suporte.",
        TipoErroConsultaCnpj.ErroInterno => "Erro interno. Tente novamente ou contate o suporte.",
        _ => "Erro interno. Tente novamente ou contate o suporte."
    };
}

public sealed record ConsultarCnpjFornecedorDto(string Cnpj_Cpf, string BusinessUnit, string? ErpSistema, string? CorrelationId);
