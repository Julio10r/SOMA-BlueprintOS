namespace BlueprintOS.Infrastructure.Integrations.ERP.Contracts;

/// <summary>Leitura real (somente leitura) de Referências de Item Fiscal por Fornecedor do ERP
/// `SOMA_DESENV`/`SOMA` (B3 — Bloco 5A, `ITEM_FISCAL_REF_FORNECEDOR`). A resolução de identidade do
/// Fornecedor (comprovada real, `docs/audits/B3-Bloco5A-ValidacaoIdentidadeFornecedor.md`) já acontece
/// dentro da consulta: `ITEM_FISCAL_REF_FORNECEDOR.FORNECEDOR` (que contém `NOME_CLIFOR`, não código) é
/// comparado por igualdade EXATA com trim apenas para padding acidental — nunca `LIKE`/`contains`/
/// case-folding — contra `CADASTRO_CLI_FOR.NOME_CLIFOR`, encadeando `CLIFOR -> FORNECEDORES.COD_FORNECEDOR`.
/// Todos os 9 registros reais de Produção resolvem hoje para exatamente 1 fornecedor; nada aqui força uma
/// resolução ambígua — <see cref="ItemFiscalReferenciaFornecedorErpDto.FornecedoresResolvidos"/> permite ao
/// chamador nunca aplicar um valor de <see cref="ItemFiscalReferenciaFornecedorErpDto.ErpFornecedorId"/>
/// obtido de uma resolução que não seja exatamente 1.</summary>
public interface IItemFiscalReferenciaFornecedorErpReader
{
    Task<IReadOnlyList<ItemFiscalReferenciaFornecedorErpDto>> BuscarReferenciasAsync(int skip, int take, CancellationToken cancellationToken = default);
}

/// <summary><c>ErpFornecedorId</c> só deve ser considerado confiável quando <c>FornecedoresResolvidos</c>
/// for exatamente 1 — 0 (nenhum `NOME_CLIFOR` bate) ou mais de 1 (nome ambíguo, colisão real comprovada em
/// Produção sob normalização) são ambos conflito de sincronização, nunca resolvidos por aproximação ou
/// escolha arbitrária.</summary>
public sealed record ItemFiscalReferenciaFornecedorErpDto(
    string CodigoItem,
    string CodigoItemFornecedor,
    string? ErpFornecedorId,
    int FornecedoresResolvidos);
