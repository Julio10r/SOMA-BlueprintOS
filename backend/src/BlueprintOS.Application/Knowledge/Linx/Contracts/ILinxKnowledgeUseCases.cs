using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Knowledge.Linx.Models;
using BlueprintOS.Domain.Knowledge.Linx;

namespace BlueprintOS.Application.Knowledge.Linx.Contracts;

/// <summary>Registra uma descoberta/inferência nova, ou uma nova versão de uma entrada existente. Nunca
/// aceita <see cref="LinxConhecimentoProveniencia.Aprovado"/> como proveniência de entrada — RBAC dedicado
/// de promoção é o único caminho para "Aprovado" (Work Order, seção 18).</summary>
public interface IRegistrarConhecimentoUseCase
{
    Task<RbacResultado<LinxKnowledgeDto>> ExecuteAsync(RegistrarConhecimentoInput input, string ator, CancellationToken ct);
}

/// <summary>Promove a proveniência de uma entrada existente (Descoberto/Inferido → Validado, ou
/// Validado → Aprovado). A checagem de permissão dedicada para alcançar "Aprovado" acontece na Api
/// (endpoint próprio, protegido por <c>ConhecimentoLinx.Aprovar</c>) — este caso de uso apenas executa a
/// transição já autorizada.</summary>
public interface IPromoverConhecimentoUseCase
{
    Task<RbacResultado<LinxKnowledgeDto>> ExecuteAsync(Guid id, LinxConhecimentoProveniencia novaProveniencia, string ator, CancellationToken ct);
}

/// <summary>Recuperação de conhecimento — MVP de busca textual/estruturada (Work Order, seção 13).</summary>
public interface IBuscarConhecimentoUseCase
{
    Task<IReadOnlyList<LinxKnowledgeDto>> ExecuteAsync(LinxKnowledgeFiltro filtro, CancellationToken ct);
}

/// <summary>Histórico completo de versões de uma cadeia de conhecimento — nunca perdido.</summary>
public interface IObterHistoricoConhecimentoUseCase
{
    Task<RbacResultado<IReadOnlyList<LinxKnowledgeDto>>> ExecuteAsync(Guid versaoRaizId, CancellationToken ct);
}
