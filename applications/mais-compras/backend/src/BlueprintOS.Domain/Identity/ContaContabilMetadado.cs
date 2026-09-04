namespace BlueprintOS.Domain.Identity;

/// <summary>Metadados locais do +Compras para uma Conta Contábil (B3 — Bloco 1, Discovery homologado
/// `ContratoFuncionalPreliminar-B3-ItemFiscal.md` §2/§8). Conta Contábil é cadastro de apoio originado do
/// Linx (`CTB_CONTA_PLANO`) — imutável no +Compras: <see cref="CodigoErp"/> é apenas a chave de correlação
/// com o registro real, nunca criado/editado/excluído por aqui. Este registro guarda SOMENTE o que o
/// +Compras tem autoridade sobre: <see cref="DescricaoMaisCompras"/> (opcional) e
/// <see cref="AtivoNoMaisCompras"/> (uma restrição adicional só do lado +Compras — nunca pode reativar uma
/// conta que o Linx marcou como inativa, conforme `ADR-0024`: em ambiguidade, Linx prevalece).</summary>
public sealed class ContaContabilMetadado : ICadastroApoioMetadado
{
    public Guid Id { get; private set; }
    public string CodigoErp { get; private set; }
    public string? DescricaoMaisCompras { get; private set; }
    public bool AtivoNoMaisCompras { get; private set; }
    public Guid UnidadeNegocioId { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    private ContaContabilMetadado() { CodigoErp = string.Empty; }

    public ContaContabilMetadado(string codigoErp, Guid unidadeNegocioId, DateTimeOffset agora, string? descricaoMaisCompras = null, bool ativoNoMaisCompras = true)
    {
        if (string.IsNullOrWhiteSpace(codigoErp)) throw new ArgumentException("Código ERP da Conta Contábil é obrigatório.", nameof(codigoErp));

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
