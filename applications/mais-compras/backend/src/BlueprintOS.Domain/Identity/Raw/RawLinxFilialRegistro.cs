namespace BlueprintOS.Domain.Identity.Raw;

/// <summary>
/// B3 — Bloco 5A (preparação de certificação final): linha crua (staging) do snapshot Linx de Filiais.
/// Decisão definitiva do PO: origem principal <c>FILIAIS</c>, com <c>CADASTRO_CLI_FOR</c> "quando necessário
/// ao contrato". <see cref="InativoErp"/> usa <c>CADASTRO_CLI_FOR.INATIVO</c> (flag booleana real) quando
/// existe um CLIFOR correspondente — <c>FILIAIS</c> não tem coluna de status explícita (comprovado por
/// Discovery; a Discovery sugeriu <c>DATA_FECHAMENTO</c> como proxy, mas essa é uma decisão de negócio ainda
/// não homologada pelo PO nesta rodada, por isso não foi adotada sem confirmação — ver relatório). Quando
/// não há CLIFOR correspondente, <see cref="InativoErp"/> é <c>null</c> (nunca inventa inatividade).
/// </summary>
public sealed class RawLinxFilialRegistro
{
    public int Id { get; private set; }
    public string CodigoErp { get; private set; } = string.Empty;
    public string? DescricaoErp { get; private set; }
    public bool? InativoErp { get; private set; }
    public DateTime? UltimaAlteracao { get; private set; }

    private RawLinxFilialRegistro()
    {
    }

    public static RawLinxFilialRegistro ParaTeste(string codigoErp, string? descricaoErp, bool? inativoErp, DateTime? ultimaAlteracao) => new()
    {
        CodigoErp = codigoErp,
        DescricaoErp = descricaoErp,
        InativoErp = inativoErp,
        UltimaAlteracao = ultimaAlteracao,
    };
}
