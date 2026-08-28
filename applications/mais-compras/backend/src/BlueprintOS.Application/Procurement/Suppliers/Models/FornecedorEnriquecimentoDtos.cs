namespace BlueprintOS.Application.Procurement.Suppliers.Models;

public enum FornecedorCampoDecisao { Pendente, Aceito, Rejeitado }

public sealed record FornecedorCampoDivergencia(
    string Campo,
    string? ValorAtual,
    string? ValorSugerido,
    string Origem,
    FornecedorCampoDecisao StatusDecisao);

public sealed record FornecedorEnriquecimentoAnaliseDto(
    Guid FornecedorId,
    string Cnpj_Cpf,
    Guid? ConsultaId,
    string FonteConsulta,
    string CorrelationId,
    IReadOnlyList<FornecedorCampoDivergencia> Divergencias,
    IReadOnlyList<string> Alertas);

public sealed record AnalisarEnriquecimentoFornecedorDto(
    ConsultaCnpjResultado Consulta,
    Guid? ConsultaId,
    string BusinessUnit,
    string? ErpSistema,
    string? CorrelationId);

public sealed record DecidirEnriquecimentoFornecedorDto(
    ConsultaCnpjResultado Consulta,
    Guid? ConsultaId,
    IReadOnlyList<string> Campos,
    string BusinessUnit,
    string? ErpSistema,
    string? CorrelationId);
