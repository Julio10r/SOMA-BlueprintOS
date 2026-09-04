namespace BlueprintOS.Domain.Identity.Raw;

/// <summary>
/// B3 — Bloco 5A (preparação de certificação final): linha crua (staging) do snapshot Linx de Itens Fiscais
/// (<c>CADASTRO_ITEM_FISCAL</c>, colunas já confirmadas por Discovery real: <c>CODIGO_ITEM</c>,
/// <c>ITEM_DESCRICAO</c>, <c>UNIDADE</c>, <c>CONTA_CONTABIL</c>, <c>INATIVO</c>, <c>DATA_PARA_TRANSFERENCIA</c>).
/// <see cref="UnidadeErp"/>/<see cref="ContaContabilErp"/> podem ser nulos/vazios — o Linx permite Item
/// Fiscal sem Unidade/Conta Contábil (comprovado: 144 itens ativos sem Conta Contábil, 2 sem Unidade) — RAW
/// nunca inventa nem descarta, apenas espelha.
/// </summary>
public sealed class RawLinxItemFiscalRegistro
{
    public int Id { get; private set; }
    public string CodigoErp { get; private set; } = string.Empty;
    public string Descricao { get; private set; } = string.Empty;
    public string? UnidadeErp { get; private set; }
    public string? ContaContabilErp { get; private set; }
    public bool InativoErp { get; private set; }
    public DateTime? UltimaAlteracao { get; private set; }

    private RawLinxItemFiscalRegistro()
    {
    }

    public static RawLinxItemFiscalRegistro ParaTeste(string codigoErp, string descricao, string? unidadeErp, string? contaContabilErp, bool inativoErp, DateTime? ultimaAlteracao) => new()
    {
        CodigoErp = codigoErp,
        Descricao = descricao,
        UnidadeErp = unidadeErp,
        ContaContabilErp = contaContabilErp,
        InativoErp = inativoErp,
        UltimaAlteracao = ultimaAlteracao,
    };
}
