namespace BlueprintOS.Domain.Identity;

/// <summary>Item Fiscal (B3 — Bloco 3, Discovery homologado `ContratoFuncionalPreliminar-B3-ItemFiscal.md`).
/// Cadastro único do +Compras — não existem cadastros mestres separados de "Material" e "Serviço"
/// (Discovery B3, seção Material×Serviço). Este bloco é exclusivamente local: nenhuma integração com o
/// Linx ainda (leitura/escrita) — a sincronização (Last Write Wins via `DATA_PARA_TRANSFERENCIA`,
/// `ADR-0024`) é escopo do Bloco 5.
///
/// <see cref="Codigo"/> é imutável após a criação (chave de negócio) — decisão de implementação desta
/// rodada, sem contradizer a granularidade livre decidida pelo Product Owner (o formato/nível de detalhe
/// do código e da descrição é escolha da área de Compras, não imposto aqui).
///
/// <see cref="ContaContabilCodigoErp"/> e <see cref="UnidadeMedidaCodigoErp"/> são chaves de correlação em
/// texto para os cadastros de apoio dos Blocos 1/2 (mesmo padrão de <c>FilialMetadado.CodigoErp</c> —
/// nenhuma FK física, pois um código de apoio pode ser válido no ERP sem nunca ter tido um metadado local
/// criado). A obrigatoriedade e a validação de existência/atividade acontecem no caso de uso
/// (<c>CriarItemFiscalUseCase</c>/<c>AtualizarItemFiscalUseCase</c>), nunca aqui — o Domain só garante que
/// os campos não estão vazios.</summary>
public sealed class ItemFiscal
{
    public Guid Id { get; private set; }
    public string Codigo { get; private set; }
    public string Descricao { get; private set; }
    public string UnidadeMedidaCodigoErp { get; private set; }
    public string ContaContabilCodigoErp { get; private set; }
    public Guid UnidadeNegocioId { get; private set; }
    public bool Ativo { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    private ItemFiscal()
    {
        Codigo = string.Empty;
        Descricao = string.Empty;
        UnidadeMedidaCodigoErp = string.Empty;
        ContaContabilCodigoErp = string.Empty;
    }

    public ItemFiscal(
        string codigo, string descricao, string unidadeMedidaCodigoErp, string contaContabilCodigoErp,
        Guid unidadeNegocioId, DateTimeOffset agora)
    {
        if (string.IsNullOrWhiteSpace(codigo)) throw new ArgumentException("Código do Item Fiscal é obrigatório.", nameof(codigo));
        if (string.IsNullOrWhiteSpace(descricao)) throw new ArgumentException("Descrição do Item Fiscal é obrigatória.", nameof(descricao));
        if (string.IsNullOrWhiteSpace(unidadeMedidaCodigoErp)) throw new ArgumentException("Unidade de Medida é obrigatória.", nameof(unidadeMedidaCodigoErp));
        if (string.IsNullOrWhiteSpace(contaContabilCodigoErp)) throw new ArgumentException("Conta Contábil é obrigatória.", nameof(contaContabilCodigoErp));

        Id = Guid.NewGuid();
        Codigo = codigo.Trim();
        Descricao = descricao.Trim();
        UnidadeMedidaCodigoErp = unidadeMedidaCodigoErp.Trim();
        ContaContabilCodigoErp = contaContabilCodigoErp.Trim();
        UnidadeNegocioId = unidadeNegocioId;
        Ativo = true;
        CriadoEm = agora;
        AtualizadoEm = agora;
    }

    /// <summary>Não altera <see cref="Codigo"/> — imutável após a criação.</summary>
    public void Atualizar(string descricao, string unidadeMedidaCodigoErp, string contaContabilCodigoErp, DateTimeOffset agora)
    {
        if (string.IsNullOrWhiteSpace(descricao)) throw new ArgumentException("Descrição do Item Fiscal é obrigatória.", nameof(descricao));
        if (string.IsNullOrWhiteSpace(unidadeMedidaCodigoErp)) throw new ArgumentException("Unidade de Medida é obrigatória.", nameof(unidadeMedidaCodigoErp));
        if (string.IsNullOrWhiteSpace(contaContabilCodigoErp)) throw new ArgumentException("Conta Contábil é obrigatória.", nameof(contaContabilCodigoErp));

        Descricao = descricao.Trim();
        UnidadeMedidaCodigoErp = unidadeMedidaCodigoErp.Trim();
        ContaContabilCodigoErp = contaContabilCodigoErp.Trim();
        AtualizadoEm = agora;
    }

    public void Ativar(DateTimeOffset agora)
    {
        if (Ativo) return;
        Ativo = true;
        AtualizadoEm = agora;
    }

    public void Inativar(DateTimeOffset agora)
    {
        if (!Ativo) return;
        Ativo = false;
        AtualizadoEm = agora;
    }
}
