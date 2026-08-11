namespace BlueprintOS.Domain.Identity;

/// <summary>Metadados locais do +Compras para uma Filial (O1.7, ADR-0020 item 3/D3, ADR-0021). Filial é
/// dado mestre integrado do ERP (`SOMA_DESENV`) — imutável no +Compras: <see cref="CodigoErp"/> é apenas
/// a chave de correlação com o registro real, nunca criado/editado/excluído por aqui. Este registro guarda
/// SOMENTE o que o +Compras tem autoridade sobre: <see cref="DescricaoMaisCompras"/> (opcional) e
/// <see cref="AtivoNoMaisCompras"/> (ativação/inativação local, sem qualquer efeito no ERP).
///
/// Não existe linha aqui até a primeira edição/ativação local (ver <c>AtualizarMetadadoFilialUseCase</c>):
/// uma Filial retornada pelo ERP sem metadado local é considerada Ativa por padrão na listagem.</summary>
public sealed class FilialMetadado
{
    public Guid Id { get; private set; }
    public string CodigoErp { get; private set; }
    public string? DescricaoMaisCompras { get; private set; }
    public bool AtivoNoMaisCompras { get; private set; }
    public Guid UnidadeNegocioId { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    private FilialMetadado() { CodigoErp = string.Empty; }

    public FilialMetadado(string codigoErp, Guid unidadeNegocioId, DateTimeOffset agora, string? descricaoMaisCompras = null, bool ativoNoMaisCompras = true)
    {
        if (string.IsNullOrWhiteSpace(codigoErp)) throw new ArgumentException("Código ERP da Filial é obrigatório.", nameof(codigoErp));

        Id = Guid.NewGuid();
        CodigoErp = codigoErp.Trim();
        UnidadeNegocioId = unidadeNegocioId;
        DescricaoMaisCompras = NormalizarDescricao(descricaoMaisCompras);
        AtivoNoMaisCompras = ativoNoMaisCompras;
        CriadoEm = agora;
        AtualizadoEm = agora;
    }

    public void AtualizarDescricao(string? descricaoMaisCompras, DateTimeOffset agora)
    {
        DescricaoMaisCompras = NormalizarDescricao(descricaoMaisCompras);
        AtualizadoEm = agora;
    }

    public void Ativar(DateTimeOffset agora)
    {
        if (AtivoNoMaisCompras) return;
        AtivoNoMaisCompras = true;
        AtualizadoEm = agora;
    }

    public void Inativar(DateTimeOffset agora)
    {
        if (!AtivoNoMaisCompras) return;
        AtivoNoMaisCompras = false;
        AtualizadoEm = agora;
    }

    private static string? NormalizarDescricao(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
