using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

/// <summary>O1.13 — Leitura das execuções em lote de sincronização de fornecedores (Monitor de
/// Integrações / Auditoria). Reaproveita as tabelas já existentes de B2.1.3; não persiste nada novo.</summary>
public interface ISincronizacaoFornecedorMonitorRepository
{
    Task<(IReadOnlyList<SincronizacaoFornecedor> Itens, int TotalRegistros)> ListarAsync(
        ListarSincronizacoesFornecedoresFiltro filtro, CancellationToken ct);

    Task<SincronizacaoFornecedor?> ObterPorIdComErrosAsync(Guid id, CancellationToken ct);
}
