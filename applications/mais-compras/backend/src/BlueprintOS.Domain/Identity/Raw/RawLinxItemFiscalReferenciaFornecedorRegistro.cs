namespace BlueprintOS.Domain.Identity.Raw;

/// <summary>
/// B3 — Bloco 5A (preparação de certificação final): linha crua (staging) do snapshot Linx de Item Fiscal
/// Referência por Fornecedor (<c>ITEM_FISCAL_REF_FORNECEDOR</c>). A resolução de identidade do Fornecedor
/// (FORNECEDOR = NOME_CLIFOR, texto livre -&gt; CADASTRO_CLI_FOR.NOME_CLIFOR, igualdade exata com trim -&gt;
/// CLIFOR -&gt; FORNECEDORES.COD_FORNECEDOR) já acontece na própria query RAW — mesma cadeia já homologada em
/// <c>SomaItemFiscalReferenciaFornecedorReader</c>, nunca reinventada aqui. <see cref="FornecedoresResolvidos"/>
/// é a contagem de CLIFOR distintos que casaram por nome — o REFINED nunca confia em
/// <see cref="ErpFornecedorId"/> quando esse número é diferente de 1 (nunca escolhe arbitrariamente em
/// ambiguidade, nunca usa CNPJ como fallback).
/// </summary>
public sealed class RawLinxItemFiscalReferenciaFornecedorRegistro
{
    public int Id { get; private set; }
    public string CodigoItem { get; private set; } = string.Empty;
    public string CodigoItemFornecedor { get; private set; } = string.Empty;
    public string? ErpFornecedorId { get; private set; }
    public int FornecedoresResolvidos { get; private set; }

    private RawLinxItemFiscalReferenciaFornecedorRegistro()
    {
    }

    public static RawLinxItemFiscalReferenciaFornecedorRegistro ParaTeste(string codigoItem, string codigoItemFornecedor, string? erpFornecedorId, int fornecedoresResolvidos) => new()
    {
        CodigoItem = codigoItem,
        CodigoItemFornecedor = codigoItemFornecedor,
        ErpFornecedorId = erpFornecedorId,
        FornecedoresResolvidos = fornecedoresResolvidos,
    };
}
