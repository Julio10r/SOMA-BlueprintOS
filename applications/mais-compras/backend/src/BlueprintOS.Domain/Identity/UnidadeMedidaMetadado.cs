namespace BlueprintOS.Domain.Identity;

/// <summary>Metadados locais do +Compras para uma Unidade de Medida (B3 — Bloco 2, Discovery homologado).
/// Unidade é cadastro de apoio originado do Linx (`UNIDADES`) — imutável no +Compras:
/// <see cref="CodigoErp"/> é apenas a chave de correlação com o registro real, nunca criado/editado/excluído
/// por aqui. Diferente de <see cref="ContaContabilMetadado"/>: `UNIDADES` não possui nenhuma coluna de
/// status/ativo/inativo no Linx (comprovado por schema discovery dedicado) — por isso
/// <see cref="AtivoNoMaisCompras"/> aqui é a ÚNICA fonte de ativo/inativo para Unidade, sem nenhuma
/// restrição vinda do lado ERP (ao contrário de Conta Contábil).</summary>
public sealed class UnidadeMedidaMetadado
{
    public Guid Id { get; private set; }
    public string CodigoErp { get; private set; }
    public string? DescricaoMaisCompras { get; private set; }
    public bool AtivoNoMaisCompras { get; private set; }
    public Guid UnidadeNegocioId { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    private UnidadeMedidaMetadado() { CodigoErp = string.Empty; }

    public UnidadeMedidaMetadado(string codigoErp, Guid unidadeNegocioId, DateTimeOffset agora, string? descricaoMaisCompras = null, bool ativoNoMaisCompras = true)
    {
        if (string.IsNullOrWhiteSpace(codigoErp)) throw new ArgumentException("Código ERP da Unidade de Medida é obrigatório.", nameof(codigoErp));

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
