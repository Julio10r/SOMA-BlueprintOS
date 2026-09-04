namespace BlueprintOS.Domain.Identity.Raw;

/// <summary>
/// B3 — Bloco 5A (preparação de certificação final): linha crua (staging) do snapshot Linx de Centros de
/// Custo (<c>CTB_CENTRO_CUSTO</c>). Ver <c>RawLinxContaContabilRegistro</c> para a decisão de design de
/// staging compartilhada. Discovery real: 1.800/2.138 linhas têm <c>DATA_PARA_TRANSFERENCIA</c> NULL (legado
/// anterior à criação do trigger em jun/2024) — por isso este dataset exige bootstrap FULL antes de confiar
/// no incremental (mesma regra geral já válida para todo dataset Incremental, aqui apenas mais crítica).
/// </summary>
public sealed class RawLinxCentroCustoRegistro
{
    public int Id { get; private set; }
    public string CodigoErp { get; private set; } = string.Empty;
    public string? DescricaoErp { get; private set; }
    public bool InativoErp { get; private set; }
    public DateTime? UltimaAlteracao { get; private set; }

    private RawLinxCentroCustoRegistro()
    {
    }

    public static RawLinxCentroCustoRegistro ParaTeste(string codigoErp, string? descricaoErp, bool inativoErp, DateTime? ultimaAlteracao) => new()
    {
        CodigoErp = codigoErp,
        DescricaoErp = descricaoErp,
        InativoErp = inativoErp,
        UltimaAlteracao = ultimaAlteracao,
    };
}
