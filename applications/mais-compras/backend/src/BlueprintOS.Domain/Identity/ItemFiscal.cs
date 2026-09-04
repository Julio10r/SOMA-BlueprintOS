namespace BlueprintOS.Domain.Identity;

/// <summary>Origem do dado de um <see cref="ItemFiscal"/> — mesmo padrão de <c>Fornecedor.OrigemInformacao</c>.
/// Determina qual regra de obrigatoriedade de Conta Contábil/Unidade de Medida se aplica: cadastro local
/// (+Compras) sempre exige ambos (Bloco 3, decisão de negócio já homologada); origem Linx (Bloco 5A) pode
/// vir sem um ou outro, porque o Linx aceita <c>CONTA_CONTABIL</c> nula e a pré-validação real (02/09/2026)
/// comprovou 144+2+2 itens ativos reais nessa condição — o Domain nunca inventa/descarta esses dados,
/// apenas os representa como cadastralmente Ativos e operacionalmente inaptos (ver
/// <c>ItemFiscalProjection.CalcularAptidaoOperacional</c>, Application layer).</summary>
public enum OrigemInformacaoItemFiscal
{
    MaisCompras,
    Linx,
}

/// <summary>Item Fiscal (B3 — Bloco 3, Discovery homologado `ContratoFuncionalPreliminar-B3-ItemFiscal.md`;
/// Bloco 5A, `docs/audits/B3-Bloco5A-*.md`). Cadastro único do +Compras — não existem cadastros mestres
/// separados de "Material" e "Serviço" (Discovery B3, seção Material×Serviço).
///
/// <see cref="Codigo"/> é imutável após a criação (chave de negócio, único globalmente) — corresponde 1:1 a
/// `CADASTRO_ITEM_FISCAL.CODIGO_ITEM` quando de origem Linx.
///
/// <see cref="ContaContabilCodigoErp"/> e <see cref="UnidadeMedidaCodigoErp"/> são chaves de correlação em
/// texto para os cadastros de apoio dos Blocos 1/2 (mesmo padrão de <c>FilialMetadado.CodigoErp</c> —
/// nenhuma FK física). São NULÁVEIS aqui exclusivamente para representar, sem falsificar, um Item Fiscal
/// real do Linx que já existe sem uma ou ambas (situação cadastral ATIVO comprovada em Produção). Para
/// origem <see cref="OrigemInformacaoItemFiscal.MaisCompras"/> continuam obrigatórios — a obrigatoriedade
/// e a validação de existência/atividade continuam acontecendo no caso de uso
/// (<c>CriarItemFiscalUseCase</c>/<c>AtualizarItemFiscalUseCase</c>), nunca aqui.
///
/// <see cref="Ativo"/> é SOMENTE situação cadastral (Ativo/Inativo conforme o cadastro) — nunca confundir
/// com aptidão operacional (poder ser usado num Pedido futuro, que depende de Conta Contábil/Unidade
/// resolvidas e ativas). Um Item Fiscal pode ser cadastralmente Ativo e operacionalmente inapto ao mesmo
/// tempo; este Domain nunca torna um Ativo em Inativo só por estar incompleto (decisão explícita do
/// Product Owner, Bloco 5A). A aptidão operacional é computada em tempo de leitura na Application layer,
/// nunca persistida aqui.
///
/// <see cref="UltimaAlteracaoErp"/> espelha `CADASTRO_ITEM_FISCAL.DATA_PARA_TRANSFERENCIA` — o último
/// timestamp ERP efetivamente incorporado localmente (Bloco 5A.7, Last Write Wins — ver
/// <see cref="AtualizarDeErp"/>).
///
/// <see cref="UltimaAlteracaoLocalEm"/> é o timestamp de negócio LOCAL relevante para o LWW: só é tocado
/// pelas edições genuinamente locais (<see cref="Atualizar"/>/<see cref="Ativar"/>/<see cref="Inativar"/>,
/// exclusivas do cadastro +Compras) e pela criação local — nunca por <see cref="AtualizarDeErp"/> (quando o
/// Linx prevalece, isso não é uma "alteração de negócio local"). Fica <c>null</c> para um registro criado
/// via <see cref="CriarDeErp"/> que nunca sofreu edição local — essa ausência é deliberada (Bloco 5A.7,
/// decisão do Product Owner: nunca inventar um timestamp local que não existe; ver caso F do algoritmo de
/// LWW em <c>SincronizarItensFiscaisErpUseCase</c>). Distinto de <see cref="AtualizadoEm"/>, que é apenas
/// auditoria técnica genérica e é tocado por qualquer escrita (local OU aplicação de dado do Linx).</summary>
public sealed class ItemFiscal
{
    public Guid Id { get; private set; }
    public string Codigo { get; private set; }
    public string Descricao { get; private set; }
    public string? UnidadeMedidaCodigoErp { get; private set; }
    public string? ContaContabilCodigoErp { get; private set; }
    public Guid UnidadeNegocioId { get; private set; }
    public bool Ativo { get; private set; }
    public OrigemInformacaoItemFiscal OrigemInformacao { get; private set; }
    public DateTimeOffset? UltimaAlteracaoErp { get; private set; }
    public DateTimeOffset? UltimaAlteracaoLocalEm { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    private ItemFiscal()
    {
        Codigo = string.Empty;
        Descricao = string.Empty;
    }

    /// <summary>Criação LOCAL (+Compras) — Conta Contábil e Unidade de Medida continuam obrigatórias
    /// (regra de negócio homologada do Bloco 3, nunca relaxada para esta origem).</summary>
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
        OrigemInformacao = OrigemInformacaoItemFiscal.MaisCompras;
        UltimaAlteracaoLocalEm = agora;
        CriadoEm = agora;
        AtualizadoEm = agora;
    }

    /// <summary>Criação a partir da sincronização Linx (Bloco 5A — `SincronizarItensFiscaisErpUseCase`).
    /// Conta Contábil/Unidade podem vir nulas/vazias (Linx permite) — nunca inventadas nem bloqueadas aqui;
    /// <paramref name="ativo"/> reflete a situação cadastral real do Linx (`!INATIVO`), nunca forçada a
    /// `true`. Só usada pelo Adapter de sincronização — nunca pela API de cadastro local.</summary>
    public static ItemFiscal CriarDeErp(
        string codigo, string descricao, string? unidadeMedidaCodigoErp, string? contaContabilCodigoErp,
        bool ativo, Guid unidadeNegocioId, DateTimeOffset? ultimaAlteracaoErp, DateTimeOffset agora)
    {
        if (string.IsNullOrWhiteSpace(codigo)) throw new ArgumentException("Código do Item Fiscal é obrigatório.", nameof(codigo));
        if (string.IsNullOrWhiteSpace(descricao)) throw new ArgumentException("Descrição do Item Fiscal é obrigatória.", nameof(descricao));

        return new ItemFiscal
        {
            Id = Guid.NewGuid(),
            Codigo = codigo.Trim(),
            Descricao = descricao.Trim(),
            UnidadeMedidaCodigoErp = NormalizarCodigoErp(unidadeMedidaCodigoErp),
            ContaContabilCodigoErp = NormalizarCodigoErp(contaContabilCodigoErp),
            UnidadeNegocioId = unidadeNegocioId,
            Ativo = ativo,
            OrigemInformacao = OrigemInformacaoItemFiscal.Linx,
            UltimaAlteracaoErp = ultimaAlteracaoErp,
            CriadoEm = agora,
            AtualizadoEm = agora,
        };
    }

    /// <summary>Não altera <see cref="Codigo"/> — imutável após a criação. Exclusiva da edição LOCAL
    /// (+Compras) — Conta Contábil/Unidade continuam obrigatórias aqui, independentemente da
    /// <see cref="OrigemInformacao"/> original do registro (editar no +Compras sempre exige os dois).</summary>
    public void Atualizar(string descricao, string unidadeMedidaCodigoErp, string contaContabilCodigoErp, DateTimeOffset agora)
    {
        if (string.IsNullOrWhiteSpace(descricao)) throw new ArgumentException("Descrição do Item Fiscal é obrigatória.", nameof(descricao));
        if (string.IsNullOrWhiteSpace(unidadeMedidaCodigoErp)) throw new ArgumentException("Unidade de Medida é obrigatória.", nameof(unidadeMedidaCodigoErp));
        if (string.IsNullOrWhiteSpace(contaContabilCodigoErp)) throw new ArgumentException("Conta Contábil é obrigatória.", nameof(contaContabilCodigoErp));

        Descricao = descricao.Trim();
        UnidadeMedidaCodigoErp = unidadeMedidaCodigoErp.Trim();
        ContaContabilCodigoErp = contaContabilCodigoErp.Trim();
        UltimaAlteracaoLocalEm = agora;
        AtualizadoEm = agora;
    }

    public void Ativar(DateTimeOffset agora)
    {
        if (Ativo) return;
        Ativo = true;
        UltimaAlteracaoLocalEm = agora;
        AtualizadoEm = agora;
    }

    public void Inativar(DateTimeOffset agora)
    {
        if (!Ativo) return;
        Ativo = false;
        UltimaAlteracaoLocalEm = agora;
        AtualizadoEm = agora;
    }

    /// <summary>Aplica ao registro local o estado vindo do Linx quando o algoritmo de Last Write Wins
    /// (Bloco 5A.7, <c>SincronizarItensFiscaisErpUseCase</c>) decide que o Linx prevalece (casos B, E ou F
    /// do algoritmo homologado). Nunca chamado a partir de edição local — por isso NÃO toca
    /// <see cref="UltimaAlteracaoLocalEm"/> (preserva o timestamp de negócio local existente, ou o mantém
    /// <c>null</c> se nunca houve edição local), apenas <see cref="AtualizadoEm"/> (auditoria técnica) e
    /// <see cref="UltimaAlteracaoErp"/> (novo timestamp ERP incorporado). Conta Contábil/Unidade podem vir
    /// nulas, mesma regra de <see cref="CriarDeErp"/> — nunca inventadas nem bloqueadas aqui.</summary>
    public void AtualizarDeErp(
        string descricao, string? unidadeMedidaCodigoErp, string? contaContabilCodigoErp, bool ativo,
        DateTimeOffset? ultimaAlteracaoErp, DateTimeOffset agora)
    {
        if (string.IsNullOrWhiteSpace(descricao)) throw new ArgumentException("Descrição do Item Fiscal é obrigatória.", nameof(descricao));

        Descricao = descricao.Trim();
        UnidadeMedidaCodigoErp = NormalizarCodigoErp(unidadeMedidaCodigoErp);
        ContaContabilCodigoErp = NormalizarCodigoErp(contaContabilCodigoErp);
        Ativo = ativo;
        OrigemInformacao = OrigemInformacaoItemFiscal.Linx;
        UltimaAlteracaoErp = ultimaAlteracaoErp;
        AtualizadoEm = agora;
    }

    private static string? NormalizarCodigoErp(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
