using BlueprintOS.Application.Identity.Models;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>B3 — Bloco 5A/5A.7: sincronização de leitura/importação Linx -> +Compras de Item Fiscal
/// (`CADASTRO_ITEM_FISCAL`). Um Item Fiscal ainda não existente localmente é sempre criado; um já existente
/// cujo conteúdo diverge do Linx tem sua resolução decidida pelo algoritmo de Last Write Wins (Bloco 5A.7),
/// nunca por escolha manual — ver <c>SincronizarItensFiscaisErpUseCase</c> para os casos A-F homologados.</summary>
public interface ISincronizarItensFiscaisErpUseCase
{
    Task<SincronizacaoItensFiscaisErpResumo> ExecuteAsync(SincronizarItensFiscaisErpDto dto, CancellationToken cancellationToken = default);
}
