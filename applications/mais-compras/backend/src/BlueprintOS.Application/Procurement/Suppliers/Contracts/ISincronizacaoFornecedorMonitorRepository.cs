using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

/// <summary>O1.13 — Leitura das execuções em lote de sincronização de fornecedores (Monitor de
/// Integrações / Auditoria). Reaproveita as tabelas já existentes de B2.1.3; não persiste nada novo.</summary>
public interface ISincronizacaoFornecedorMonitorRepository
{
    Task<(IReadOnlyList<SincronizacaoFornecedor> Itens, int TotalRegistros)> ListarAsync(
        Guid unidadeNegocioId, ListarSincronizacoesFornecedoresFiltro filtro, CancellationToken ct);

    Task<SincronizacaoFornecedor?> ObterPorIdComErrosAsync(Guid unidadeNegocioId, Guid id, CancellationToken ct);

    /// <summary>Guarda de segurança 4c (execução concorrente): existe alguma <see cref="SincronizacaoFornecedor"/>
    /// para a mesma Unidade de Negócio + BusinessUnit com Status "EmAndamento"? Usado pelo
    /// SincronizarFornecedoresErpUseCase antes de iniciar uma execução real (nunca em dry-run) para
    /// rejeitar disparos concorrentes da mesma sincronização.</summary>
    Task<bool> ExisteEmAndamentoAsync(Guid unidadeNegocioId, string businessUnit, CancellationToken ct);
}
