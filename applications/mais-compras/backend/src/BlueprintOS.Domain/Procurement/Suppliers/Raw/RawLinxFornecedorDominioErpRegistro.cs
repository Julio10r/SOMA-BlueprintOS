namespace BlueprintOS.Domain.Procurement.Suppliers.Raw;

/// <summary>
/// B3 — Bloco 5A (preparação de certificação final): linha crua (staging) do snapshot unificado dos 3
/// catálogos Linx que alimentam <see cref="FornecedorDominioErp"/> — <c>FORNECEDOR_TIPOS</c>,
/// <c>FORNECEDOR_SUBTIPO</c> e <c>COND_ENT_PGTOS</c>, descobertos via FK real de <c>FORNECEDORES</c>
/// (sys.foreign_keys), não suposição. <see cref="TipoDominio"/> é o discriminador ("TipoFornecedor",
/// "SubtipoFornecedor", "CondicaoPagamento" — mesmos literais usados em <see cref="FornecedorDominioErp.Tipo"/>).
/// Para Subtipo, cuja chave real no Linx é composta (SUBTIPO_FORNECEDOR + TIPO — FK confirmada), o RAW
/// codifica a composição em <see cref="CodigoErp"/> como <c>"{TIPO}:{SUBTIPO_FORNECEDOR}"</c>, decisão de
/// design documentada aqui para nunca ser reinventada por engano.
/// </summary>
public sealed class RawLinxFornecedorDominioErpRegistro
{
    public int Id { get; private set; }
    public string TipoDominio { get; private set; } = string.Empty;
    public string CodigoErp { get; private set; } = string.Empty;
    public string? Descricao { get; private set; }
    public DateTime? UltimaAlteracao { get; private set; }

    private RawLinxFornecedorDominioErpRegistro()
    {
    }

    public static RawLinxFornecedorDominioErpRegistro ParaTeste(string tipoDominio, string codigoErp, string? descricao, DateTime? ultimaAlteracao) => new()
    {
        TipoDominio = tipoDominio,
        CodigoErp = codigoErp,
        Descricao = descricao,
        UltimaAlteracao = ultimaAlteracao,
    };
}
