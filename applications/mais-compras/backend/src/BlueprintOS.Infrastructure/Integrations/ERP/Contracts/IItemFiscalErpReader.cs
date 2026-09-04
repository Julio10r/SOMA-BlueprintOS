namespace BlueprintOS.Infrastructure.Integrations.ERP.Contracts;

/// <summary>Leitura real (somente leitura) de Item Fiscal do ERP `SOMA_DESENV`/`SOMA` (B3 — Bloco 5A,
/// `CADASTRO_ITEM_FISCAL`). Mesmo padrão de <c>IContaContabilErpReader</c>/<c>IFornecedorErpReader</c>: o
/// ERP é fonte canônica — o +Compras nunca cria/edita/exclui o dado mestre no Linx, apenas lê para
/// sincronizar o espelho local (<c>ItemFiscal</c>, Bloco 3). Paginado: 14.103 registros reais confirmados
/// em Produção (`docs/audits/B3-Bloco5A-PreValidacaoLinxProducao.md`).</summary>
public interface IItemFiscalErpReader
{
    Task<IReadOnlyList<ItemFiscalErpDto>> BuscarItensFiscaisAsync(int skip, int take, CancellationToken cancellationToken = default);
}

/// <summary><c>UnidadeErp</c>/<c>ContaContabilErp</c> podem vir nulos/vazios — o Linx aceita
/// `CADASTRO_ITEM_FISCAL.CONTA_CONTABIL` nula e a pré-validação real comprovou 144 casos ativos sem Conta e
/// 2 sem Unidade preenchida (02/09/2026); nunca inventados aqui. <c>Inativo</c> é a situação cadastral real
/// do Linx (`INATIVO` bit). <c>UltimaAlteracaoEm</c> é `DATA_PARA_TRANSFERENCIA` — guardado para o futuro
/// Last Write Wins, não usado nesta rodada.</summary>
public sealed record ItemFiscalErpDto(
    string CodigoItem,
    string Descricao,
    string? UnidadeErp,
    string? ContaContabilErp,
    bool Inativo,
    DateTimeOffset? UltimaAlteracaoEm);
