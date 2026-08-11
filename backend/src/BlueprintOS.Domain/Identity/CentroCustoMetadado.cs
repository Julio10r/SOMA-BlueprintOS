namespace BlueprintOS.Domain.Identity;

/// <summary>Metadados locais do +Compras para um Centro de Custo (O1.7, ADR-0020 item 3/D3, ADR-0021).
/// Centro de Custo é dado mestre integrado do ERP (`SOMA_DESENV`) — imutável no +Compras:
/// <see cref="CodigoErp"/> é apenas a chave de correlação com o registro real, nunca criado/editado/excluído
/// por aqui. Este registro guarda SOMENTE o que o +Compras tem autoridade sobre:
/// <see cref="DescricaoMaisCompras"/> (opcional) e <see cref="AtivoNoMaisCompras"/> (ativação/inativação
/// local, sem qualquer efeito no ERP).
///
/// <see cref="UnidadeNegocioId"/> ancora o registro à Unidade de Negócio de quem o criou/editou primeiro —
/// usado também como resolução da dívida O1.6-L2 (ver <c>UsuarioUseCases</c>): o vínculo Usuário×Centro de
/// Custo passa a exigir que o código ERP exista de fato no ERP e, quando já houver um metadado local para
/// aquele código, que ele pertença à mesma Unidade de Negócio do usuário — impedindo vínculo cruzado entre
/// Unidades de Negócio.</summary>
public sealed class CentroCustoMetadado
{
    public Guid Id { get; private set; }
    public string CodigoErp { get; private set; }
    public string? DescricaoMaisCompras { get; private set; }
    public bool AtivoNoMaisCompras { get; private set; }
    public Guid UnidadeNegocioId { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    private CentroCustoMetadado() { CodigoErp = string.Empty; }

    public CentroCustoMetadado(string codigoErp, Guid unidadeNegocioId, DateTimeOffset agora, string? descricaoMaisCompras = null, bool ativoNoMaisCompras = true)
    {
        if (string.IsNullOrWhiteSpace(codigoErp)) throw new ArgumentException("Código ERP do Centro de Custo é obrigatório.", nameof(codigoErp));

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
