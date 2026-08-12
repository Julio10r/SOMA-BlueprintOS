using BlueprintOS.Application.Procurement.Suppliers.Models;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

public interface ICnpjConsultaProvider
{
    string FonteConsulta { get; }
    Task<ConsultaCnpjResultado> ConsultarAsync(string cnpjCpf, CancellationToken cancellationToken = default);
}

/// <summary>Interface opcional (B2.7/ADR-0023) implementada por Providers capazes de fornecer, além
/// do contrato canônico, um snapshot bruto sanitizado da resposta original para fins exclusivos de
/// auditoria/proveniência. Deliberadamente separada de <see cref="ICnpjConsultaProvider"/> para que o
/// contrato canônico (<see cref="ConsultaCnpjResultado"/>) nunca dependa de JSON externo, e para que
/// providers futuros que não tenham (ou não queiram expor) um snapshot bruto continuem satisfazendo
/// apenas a interface mínima sem qualquer alteração de domínio.</summary>
public interface ICnpjConsultaProviderComSnapshot : ICnpjConsultaProvider
{
    /// <summary>Executa a mesma consulta de <see cref="ICnpjConsultaProvider.ConsultarAsync"/>, mas
    /// também retorna o snapshot bruto já sanitizado pelo próprio Provider (responsável por conhecer
    /// e remover QSA/segredos/dados pessoais de sócios do seu contrato específico) e sinalizado quanto
    /// a descarte por tamanho. O snapshot nunca deve ser deserializado pelo chamador para uso de
    /// domínio — é opaco por design.</summary>
    Task<CnpjConsultaProviderResposta> ConsultarComSnapshotAsync(string cnpjCpf, CancellationToken cancellationToken = default);
}

/// <summary>Envelope de resposta do Provider com snapshot bruto opcional. <see cref="Resultado"/> é o
/// único dado que atravessa para o domínio/Application; <see cref="SnapshotBrutoSanitizado"/> e
/// <see cref="SnapshotDescartadoPorTamanho"/> destinam-se exclusivamente ao registro de proveniência
/// em <c>FornecedorCnpjConsultaHistorico</c>.</summary>
public sealed record CnpjConsultaProviderResposta(
    ConsultaCnpjResultado Resultado,
    string? SnapshotBrutoSanitizado,
    bool SnapshotDescartadoPorTamanho);
