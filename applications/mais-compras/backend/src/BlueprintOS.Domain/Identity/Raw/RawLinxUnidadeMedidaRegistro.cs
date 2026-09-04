namespace BlueprintOS.Domain.Identity.Raw;

/// <summary>
/// B3 — Bloco 5A (preparação de certificação final): linha crua (staging) do snapshot Linx de Unidades de
/// Medida (<c>UNIDADES</c>). <see cref="InativoErp"/> não existe fisicamente nesta tabela Linx (comprovado
/// por Discovery — <c>UNIDADES</c> não tem nenhuma coluna de status) e por isso é sempre <c>null</c> aqui:
/// o REFINED nunca força inativação a partir deste dataset, only registra ocorrência informativa para
/// códigos novos. Ver <c>RawLinxContaContabilRegistro</c> para a decisão de design de staging compartilhada.
/// </summary>
public sealed class RawLinxUnidadeMedidaRegistro
{
    public int Id { get; private set; }
    public string CodigoErp { get; private set; } = string.Empty;
    public string? DescricaoErp { get; private set; }
    public bool? InativoErp { get; private set; }
    public DateTime? UltimaAlteracao { get; private set; }

    private RawLinxUnidadeMedidaRegistro()
    {
    }

    public static RawLinxUnidadeMedidaRegistro ParaTeste(string codigoErp, string? descricaoErp, bool? inativoErp, DateTime? ultimaAlteracao) => new()
    {
        CodigoErp = codigoErp,
        DescricaoErp = descricaoErp,
        InativoErp = inativoErp,
        UltimaAlteracao = ultimaAlteracao,
    };
}
