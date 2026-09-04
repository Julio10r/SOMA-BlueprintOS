using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Persistência dos metadados locais +Compras de Centro de Custo (O1.7). Toda leitura/escrita é
/// escopada por Unidade de Negócio — inclusive a ancoragem do vínculo Usuário×Centro de Custo (dívida
/// O1.6-L2): desde a Onda 2 (Multi-BU/Multi-ERP, 03/09/2026), o índice físico de <see cref="CentroCustoMetadado.CodigoErp"/>
/// deixou de ser único globalmente — duas Unidades de Negócio podem ancorar o mesmo código ERP como
/// metadados independentes (decisão do Product Owner, applications/mais-compras/docs/cadernos/Onda-2.md).
/// Por isso não existe mais nenhuma consulta "global" sem escopo de BU neste contrato.</summary>
public interface ICentroCustoMetadadoRepository
{
    Task<IReadOnlyDictionary<string, CentroCustoMetadado>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct);

    Task<CentroCustoMetadado?> ObterPorCodigoErpAsync(string codigoErp, Guid unidadeNegocioId, CancellationToken ct);

    /// <summary>O1.12 — usado para validar FKs de Centro de Custo em Alçadas de Aprovação/Regras
    /// Orçamentárias: um Id existente porém de outra Unidade de Negócio nunca é aceito.</summary>
    Task<CentroCustoMetadado?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct);

    Task AdicionarAsync(CentroCustoMetadado metadado, CancellationToken ct);

    Task SalvarAlteracoesAsync(CancellationToken ct);
}
