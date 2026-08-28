using BlueprintOS.Application.Identity.Models;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Resolução da dívida O1.6-L2 (`.ai/BACKLOG.md`): antes desta sprint, o vínculo Usuário×Centro de
/// Custo (<c>UsuarioCentroCusto.CentroCustoCodigoErp</c>) aceitava qualquer texto livre, sem validar contra
/// o ERP nem contra a Unidade de Negócio da sessão. A abstração fica em <c>Application</c> (sem depender de
/// <c>Infrastructure</c>) e é implementada em <c>Infrastructure</c> consultando <c>ICentroCustoErpReader</c>
/// e <c>ICentroCustoMetadadoRepository</c> — ver <c>CentroCustoVinculoValidator</c> para a lógica completa e
/// o relatório final da O1.7 para a justificativa da abordagem escolhida.</summary>
public interface ICentroCustoVinculoValidator
{
    /// <summary>Valida os códigos ERP informados e devolve a lista normalizada (trim, sem duplicados) em
    /// caso de sucesso. Falha com <see cref="RbacFalha.CentroCustoInvalido"/> se algum código não existir no
    /// ERP, ou já estiver ancorado a outra Unidade de Negócio.</summary>
    Task<RbacResultado<IReadOnlyList<string>>> ValidarEAncorarAsync(
        IReadOnlyList<string>? codigosErp, Guid unidadeNegocioId, CancellationToken ct);
}
