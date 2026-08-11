namespace BlueprintOS.Domain.Identity;

/// <summary>Vínculo N:N entre Centro de Custo e Unidade de Alocação (O1.9, ADR-0020 item 6, D4/ADR-0021).
///
/// Referencia o Centro de Custo pela sua identidade canônica local já estabelecida na O1.7
/// (<see cref="CentroCustoMetadado"/>, ancorado a uma única Unidade de Negócio) — nunca pelo código ERP em
/// texto livre, e sem criar uma segunda fonte canônica local para Centro de Custo. Referencia
/// <see cref="UnidadeAlocacao"/> pelo seu Id real (O1.8).
///
/// No máximo um vínculo por <see cref="CentroCustoMetadadoId"/> pode ter <see cref="Padrao"/> verdadeiro
/// (garantido por índice único filtrado — ver <c>CentroCustoUnidadeAlocacaoConfiguration</c>), refletindo a
/// "Unidade de Alocação padrão" da ADR-0020, item 6.</summary>
public sealed class CentroCustoUnidadeAlocacao
{
    public Guid Id { get; private set; }
    public Guid CentroCustoMetadadoId { get; private set; }
    public Guid UnidadeAlocacaoId { get; private set; }
    public bool Padrao { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }

    private CentroCustoUnidadeAlocacao() { }

    public CentroCustoUnidadeAlocacao(Guid centroCustoMetadadoId, Guid unidadeAlocacaoId, bool padrao, DateTimeOffset agora)
    {
        Id = Guid.NewGuid();
        CentroCustoMetadadoId = centroCustoMetadadoId;
        UnidadeAlocacaoId = unidadeAlocacaoId;
        Padrao = padrao;
        CriadoEm = agora;
    }

    public void DefinirPadrao(bool padrao) => Padrao = padrao;
}
