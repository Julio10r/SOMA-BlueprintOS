using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.Application.Procurement.Suppliers.Models;

public enum SituacaoCadastralCnpj { Ativa, Baixada, Suspensa, Inapta, NaoEncontrada }
public enum StatusConsultaCnpj { Sucesso, Falha }

public sealed record ConsultaCnpjResultado(
    string Cnpj_Cpf,
    string? RazaoSocial,
    string? NomeFantasia,
    string? TipoPessoa,
    SituacaoCadastralCnpj SituacaoCadastral,
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
    string FonteConsulta,
    DateTimeOffset DataConsulta,
    StatusConsultaCnpj StatusConsulta,
    string? MensagemErro)
{
    public bool Sucesso => StatusConsulta == StatusConsultaCnpj.Sucesso;

    public static ConsultaCnpjResultado CriarSucesso(string cnpjCpf, string fonteConsulta,
        SituacaoCadastralCnpj situacaoCadastral, DateTimeOffset dataConsulta,
        string? razaoSocial = null, string? nomeFantasia = null, string? tipoPessoa = null,
        DateOnly? dataSituacaoCadastral = null, DateOnly? dataAbertura = null,
        string? cep = null, string? logradouro = null, string? numero = null, string? complemento = null,
        string? bairro = null, string? cidade = null, string? estado = null, string? pais = null,
        string? email = null, string? telefone = null, string? naturezaJuridica = null, string? porteEmpresa = null)
    {
        var documento = DocumentoFiscal.Create(cnpjCpf).Value;
        if (string.IsNullOrWhiteSpace(fonteConsulta)) throw new ArgumentException("FonteConsulta is required.", nameof(fonteConsulta));
        return new(documento, razaoSocial?.Trim(), nomeFantasia?.Trim(), tipoPessoa?.Trim(), situacaoCadastral,
            dataSituacaoCadastral, dataAbertura, cep?.Trim(), logradouro?.Trim(), numero?.Trim(), complemento?.Trim(),
            bairro?.Trim(), cidade?.Trim(), estado?.Trim(), pais?.Trim(), email?.Trim(), telefone?.Trim(),
            naturezaJuridica?.Trim(), porteEmpresa?.Trim(), fonteConsulta.Trim(), dataConsulta, StatusConsultaCnpj.Sucesso, null);
    }

    public static ConsultaCnpjResultado CriarFalha(string cnpjCpf, string fonteConsulta, DateTimeOffset dataConsulta, string mensagemErro)
    {
        var documento = DocumentoFiscal.Create(cnpjCpf).Value;
        if (string.IsNullOrWhiteSpace(fonteConsulta)) throw new ArgumentException("FonteConsulta is required.", nameof(fonteConsulta));
        if (string.IsNullOrWhiteSpace(mensagemErro)) throw new ArgumentException("MensagemErro is required.", nameof(mensagemErro));
        return new(documento, null, null, null, SituacaoCadastralCnpj.NaoEncontrada, null, null, null, null, null,
            null, null, null, null, null, null, null, null, null, fonteConsulta.Trim(), dataConsulta,
            StatusConsultaCnpj.Falha, mensagemErro.Trim());
    }
}

public sealed record ConsultarCnpjFornecedorDto(string Cnpj_Cpf, string BusinessUnit, string? ErpSistema, string? CorrelationId);
