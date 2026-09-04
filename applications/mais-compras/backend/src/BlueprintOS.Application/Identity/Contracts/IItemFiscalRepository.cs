using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Repositório do cadastro local de Item Fiscal (B3 — Bloco 3). Leitura escopada por Unidade de
/// Negócio; unicidade de <see cref="ItemFiscal.Codigo"/> é GLOBAL (mesma decisão de
/// <see cref="ExisteComCodigoAsync"/> — ver <c>ItemFiscalConfiguration</c>).</summary>
public interface IItemFiscalRepository
{
    Task<IReadOnlyList<ItemFiscal>> ListarPorUnidadeNegocioAsync(Guid unidadeNegocioId, CancellationToken ct);

    Task<ItemFiscal?> ObterPorIdEUnidadeNegocioAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct);

    Task<bool> ExisteComCodigoAsync(string codigo, Guid? excluirId, CancellationToken ct);

    Task AdicionarAsync(ItemFiscal itemFiscal, CancellationToken ct);

    Task SalvarAlteracoesAsync(CancellationToken ct);

    /// <summary>B3 — Bloco 5A: leitura GLOBAL (sem escopo de Unidade de Negócio — <see cref="ItemFiscal.Codigo"/>
    /// é único globalmente) e sem rastreamento (mesmo motivo de <c>IFornecedorRepository.ObterPorCnpjSemRastreamentoAsync</c>:
    /// a sincronização só decide se vai escrever depois de classificar o registro).</summary>
    Task<ItemFiscal?> ObterPorCodigoSemRastreamentoAsync(string codigo, CancellationToken ct);

    /// <summary>B3 — Bloco 5A.7: leitura GLOBAL RASTREADA por código — usada exclusivamente quando o
    /// algoritmo de Last Write Wins decide que o Linx prevalece e o registro precisa ser efetivamente
    /// atualizado (<c>ItemFiscal.AtualizarDeErp</c>). Nunca usada para a classificação inicial (essa
    /// continua via <see cref="ObterPorCodigoSemRastreamentoAsync"/>, sem custo de tracking).</summary>
    Task<ItemFiscal?> ObterPorCodigoAsync(string codigo, CancellationToken ct);

    /// <summary>B3 — Bloco 5A: total de Itens Fiscais já existentes (qualquer origem), usado apenas para
    /// diagnóstico/relatório da sincronização — nunca para decisão de guarda (o Bloco 5A não inativa
    /// automaticamente, então não há percentual de inativação a proteger, diferente de Fornecedor).</summary>
    Task<int> ContarAsync(CancellationToken ct);
}
