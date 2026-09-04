namespace BlueprintOS.Domain.Procurement.Suppliers;

/// <summary>B3 — Bloco 5A.9 (modelo 1 CNPJ/CPF = 1 Fornecedor, N vínculos Linx — decisão do Product Owner,
/// GAPs KALUNGA/PLATINUM). Representa individualmente um cadastro Linx (`FORNECEDORES` + `CADASTRO_CLI_FOR`)
/// associado a um <see cref="Fornecedor"/> — vários vínculos podem coexistir para o mesmo Fornecedor quando
/// o Linx tem múltiplos `COD_FORNECEDOR` para o mesmo CNPJ (cadastro legado duplicado, comprovado real:
/// 1.856 CNPJs/1.302 já sincronizados localmente).
///
/// Identidade ERP do vínculo é <see cref="UnidadeNegocioId"/> + <see cref="ErpSistema"/> + <see cref="CodigoErp"/>
/// (CLIFOR/COD_FORNECEDOR) — nunca o CNPJ, que identifica o <see cref="Fornecedor"/>, não o vínculo. Onda 2
/// (Multi-BU/Multi-ERP, 03/09/2026, decisão do Product Owner): a Business Unit passa a compor a identidade
/// porque instâncias Linx de BUs diferentes podem, em tese, atribuir o mesmo `CLIFOR`/`COD_FORNECEDOR` a
/// registros distintos — dois vínculos de BUs diferentes nunca colidem por esse motivo.
///
/// <see cref="Ativo"/> exige as DUAS tabelas Linx concordarem (decisão do Product Owner: CADASTRO_CLI_FOR é
/// master do cadastro de pessoa/fornecedor — um vínculo cujo CADASTRO_CLI_FOR.INATIVO=1 é inativo
/// independentemente de FORNECEDORES.INATIVO). Computado, nunca persistido diretamente, para nunca divergir
/// dos dois bits de origem.
///
/// <see cref="Principal"/> é um flag histórico, não um estado voltado a ser "limpo" quando o vínculo fica
/// inativo — um vínculo Principal que se torna inativo PERMANECE com Principal=true (preserva a informação
/// de que já foi/é o Principal), só deixa de ser operacionalmente utilizável porque deixou de ser
/// <see cref="Ativo"/>. "Principal operacional" é a combinação (Principal AND Ativo), nunca um terceiro
/// campo — evita estado contraditório sem inventar uma segunda regra funcional. A unicidade de Principal
/// ativo por Fornecedor é garantida por índice único filtrado
/// (`IX_FornecedorLinxVinculos_FornecedorId_PrincipalAtivo`), nunca por lógica aplicada isoladamente.</summary>
public sealed class FornecedorLinxVinculo
{
    private FornecedorLinxVinculo() { }

    public FornecedorLinxVinculo(
        Guid fornecedorId, Guid unidadeNegocioId, string erpSistema, string codigoErp, string nomeClifor,
        bool inativoFornecedores, bool inativoCadastroCliFor, DateTimeOffset? dataParaTransferencia,
        bool principal, DateTimeOffset agora)
    {
        if (fornecedorId == Guid.Empty) throw new ArgumentException("FornecedorId é obrigatório.", nameof(fornecedorId));
        if (unidadeNegocioId == Guid.Empty) throw new ArgumentException("UnidadeNegocioId é obrigatória (Onda 2, Multi-BU).", nameof(unidadeNegocioId));
        if (string.IsNullOrWhiteSpace(erpSistema)) throw new ArgumentException("ErpSistema é obrigatório.", nameof(erpSistema));
        if (string.IsNullOrWhiteSpace(codigoErp)) throw new ArgumentException("CodigoErp (CLIFOR/COD_FORNECEDOR) é obrigatório.", nameof(codigoErp));
        if (principal && (inativoFornecedores || inativoCadastroCliFor)) throw new ArgumentException("Um vínculo inativo não pode nascer Principal.", nameof(principal));

        Id = Guid.NewGuid();
        FornecedorId = fornecedorId;
        UnidadeNegocioId = unidadeNegocioId;
        ErpSistema = erpSistema.Trim();
        CodigoErp = codigoErp.Trim();
        NomeClifor = nomeClifor?.Trim() ?? string.Empty;
        InativoFornecedores = inativoFornecedores;
        InativoCadastroCliFor = inativoCadastroCliFor;
        DataParaTransferencia = dataParaTransferencia;
        Principal = principal;
        CriadoEm = agora;
        AtualizadoEm = agora;
    }

    public Guid Id { get; private set; }
    public Guid FornecedorId { get; private set; }

    /// <summary>Onda 2 (Multi-BU/Multi-ERP, 03/09/2026): compõe a identidade ERP deste vínculo junto com
    /// <see cref="ErpSistema"/> + <see cref="CodigoErp"/>. Sempre a mesma BU do <see cref="Fornecedor"/> pai.</summary>
    public Guid UnidadeNegocioId { get; private set; }
    public string ErpSistema { get; private set; } = null!;

    /// <summary>CLIFOR / COD_FORNECEDOR — código ERP do vínculo, imutável após criação (identidade).</summary>
    public string CodigoErp { get; private set; } = null!;

    /// <summary>`CADASTRO_CLI_FOR.NOME_CLIFOR` preservado com a grafia original (trim apenas) — nunca
    /// maiusculizado aqui: maiusculização é regra de apresentação/persistência exclusiva de
    /// `Fornecedor.NomeFantasia` (fonte cadastral consolidada), nunca da identidade do vínculo, que segue as
    /// regras de match já homologadas (exato + trim, nunca case-fold).</summary>
    public string NomeClifor { get; private set; } = null!;

    public bool InativoFornecedores { get; private set; }
    public bool InativoCadastroCliFor { get; private set; }

    /// <summary>`FORNECEDORES.DATA_PARA_TRANSFERENCIA` — validado empiricamente contra `ANM_FORNECEDORES_LOG`
    /// (Bloco 5A.8) como representação confiável da última alteração real do cadastro. Único critério
    /// homologado de "mais recente" entre vínculos ativos do mesmo CNPJ — nunca CLIFOR, ordem de SELECT ou
    /// ordem de sincronização.</summary>
    public DateTimeOffset? DataParaTransferencia { get; private set; }

    public bool Principal { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    /// <summary>Decisão do Product Owner (Bloco 5A.9): CADASTRO_CLI_FOR é master — um vínculo só é Ativo
    /// quando NENHUMA das duas tabelas o marca inativo.</summary>
    public bool Ativo => !InativoFornecedores && !InativoCadastroCliFor;

    /// <summary>Atualiza os metadados vindos do Linx nesta sincronização — nunca toca
    /// <see cref="Principal"/> (escolha do comprador ou atribuição automática inicial, sempre via
    /// <see cref="DefinirComoPrincipal"/>/<see cref="RemoverComoPrincipal"/>).</summary>
    public void AtualizarDadosErp(string nomeClifor, bool inativoFornecedores, bool inativoCadastroCliFor,
        DateTimeOffset? dataParaTransferencia, DateTimeOffset agora)
    {
        NomeClifor = nomeClifor?.Trim() ?? string.Empty;
        InativoFornecedores = inativoFornecedores;
        InativoCadastroCliFor = inativoCadastroCliFor;
        DataParaTransferencia = dataParaTransferencia;
        AtualizadoEm = agora;
    }

    /// <summary>Só deve ser chamado pelo caso de uso após confirmar a invariante "no máximo um vínculo
    /// ATIVO Principal por Fornecedor" (o índice único filtrado é a rede de segurança final, não a
    /// primeira linha de defesa).</summary>
    public void DefinirComoPrincipal(DateTimeOffset agora)
    {
        if (!Ativo) throw new InvalidOperationException("Um vínculo inativo não pode ser definido como Principal.");
        Principal = true;
        AtualizadoEm = agora;
    }

    /// <summary>Usado exclusivamente quando o comprador troca explicitamente o Principal para outro
    /// vínculo ATIVO (nunca automaticamente por recência — decisão do Product Owner) ou pela defesa contra
    /// o caso-limite de reativação de um Principal histórico que colidiria com o Principal ativo atual
    /// (ver <c>SincronizarFornecedoresErpUseCase</c>). Nunca chamado só porque o vínculo ficou inativo —
    /// isso preservaria a informação histórica perdida, o que a decisão do Product Owner proíbe.</summary>
    public void RemoverComoPrincipal(DateTimeOffset agora)
    {
        Principal = false;
        AtualizadoEm = agora;
    }
}
