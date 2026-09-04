using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

/// <summary>B3 — Bloco 5A.9: vínculos Linx de um Fornecedor (1 CNPJ = 1 Fornecedor, N vínculos). Onda 2
/// (Multi-BU/Multi-ERP, 03/09/2026, decisão do Product Owner): identidade ERP é
/// `UnidadeNegocioId + ErpSistema + CodigoErp` — a busca por código é sempre escopada pela Business Unit,
/// nunca mais global.</summary>
public interface IFornecedorLinxVinculoRepository
{
    Task AdicionarAsync(FornecedorLinxVinculo vinculo, CancellationToken cancellationToken = default);

    Task<FornecedorLinxVinculo?> ObterPorErpSistemaECodigoAsync(string erpSistema, string codigoErp, Guid unidadeNegocioId, CancellationToken cancellationToken = default);

    Task<FornecedorLinxVinculo?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Todos os vínculos de um Fornecedor — usado pela sincronização para recomputar, a cada
    /// linha do Linx processada, qual vínculo ATIVO tem a maior `DataParaTransferencia` (fonte cadastral
    /// mais recente) e se a invariante de Principal único-ativo seria violada. Volume real sempre pequeno
    /// (pior caso conhecido: 135 vínculos para 1 CNPJ) — nunca paginado.</summary>
    Task<IReadOnlyList<FornecedorLinxVinculo>> ListarPorFornecedorAsync(Guid fornecedorId, CancellationToken cancellationToken = default);

    Task SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
}
