using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Persistência dos metadados locais +Compras de Centro de Custo (O1.7). Toda leitura/escrita por
/// Unidade de Negócio é escopada; <see cref="ObterPorCodigoErpGlobalAsync"/> é a única consulta sem esse
/// escopo, usada exclusivamente pela validação de vínculo Usuário×Centro de Custo (resolução da dívida
/// O1.6-L2) para detectar código já ancorado a OUTRA Unidade de Negócio.</summary>
public interface ICentroCustoMetadadoRepository
{
    Task<IReadOnlyDictionary<string, CentroCustoMetadado>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct);

    Task<CentroCustoMetadado?> ObterPorCodigoErpAsync(string codigoErp, Guid unidadeNegocioId, CancellationToken ct);

    /// <summary>Busca sem filtro de Unidade de Negócio — usada apenas para verificar a qual Unidade de
    /// Negócio um código ERP já está ancorado, antes de permitir um vínculo Usuário×Centro de Custo.</summary>
    Task<CentroCustoMetadado?> ObterPorCodigoErpGlobalAsync(string codigoErp, CancellationToken ct);

    /// <summary>O1.12 — usado para validar FKs de Centro de Custo em Alçadas de Aprovação/Regras
    /// Orçamentárias: um Id existente porém de outra Unidade de Negócio nunca é aceito.</summary>
    Task<CentroCustoMetadado?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct);

    Task AdicionarAsync(CentroCustoMetadado metadado, CancellationToken ct);

    Task SalvarAlteracoesAsync(CancellationToken ct);
}
