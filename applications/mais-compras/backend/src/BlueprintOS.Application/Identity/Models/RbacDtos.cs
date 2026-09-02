namespace BlueprintOS.Application.Identity.Models;

/// <summary>Uma permissão do catálogo global, como devolvida à interface.</summary>
public sealed record PermissaoCatalogoDto(string Codigo, string Recurso, string Acao, string Descricao);

/// <summary>Projeção de leitura de um Perfil. <c>Permissoes</c> traz os códigos canônicos do catálogo;
/// <c>UsuariosVinculados</c> é contado no banco (nunca mantido como contador denormalizado).</summary>
public sealed record PerfilDto(
    Guid Id,
    string Nome,
    string Descricao,
    Guid UnidadeNegocioId,
    bool Ativo,
    IReadOnlyList<string> Permissoes,
    int UsuariosVinculados,
    DateTimeOffset CriadoEm,
    DateTimeOffset AtualizadoEm);

/// <summary>Entrada de criação/edição de Perfil. Note a ausência deliberada de <c>UnidadeNegocioId</c>:
/// a Unidade de Negócio é sempre a da identidade autenticada, nunca um valor escolhido pelo cliente
/// (evita atribuição cruzada de Perfis entre Unidades de Negócio).</summary>
public sealed record PerfilInput(string Nome, string Descricao, IReadOnlyList<string> Permissoes);

/// <summary>Um Perfil vinculado a um Usuário, na projeção de leitura de <see cref="UsuarioDto"/>.</summary>
public sealed record UsuarioPerfilResumoDto(Guid Id, string Nome, bool Ativo);

/// <summary>Projeção de leitura de um Usuário (O1.6 — Gestão de Usuários). <c>CentrosCusto</c> traz os
/// códigos ERP explicitamente vinculados; quando <c>TodosCentrosCusto</c> é verdadeiro, o vínculo explícito
/// é irrelevante para efeito de acesso (escopo declarado da Work Order — sem integração ERP nesta sprint).</summary>
public sealed record UsuarioDto(
    Guid Id,
    string Nome,
    string Email,
    Guid UnidadeNegocioId,
    bool Ativo,
    IReadOnlyList<UsuarioPerfilResumoDto> Perfis,
    IReadOnlyList<string> CentrosCusto,
    bool TodosCentrosCusto,
    DateTimeOffset CriadoEm,
    DateTimeOffset AtualizadoEm);

/// <summary>Entrada de criação/edição de Usuário. <c>UnidadeNegocioId</c> é deliberadamente ausente — vem
/// sempre da identidade autenticada, nunca do cliente (mesmo cuidado de <see cref="PerfilInput"/>).</summary>
public sealed record UsuarioInput(
    string Nome,
    string Email,
    IReadOnlyList<Guid> Perfis,
    IReadOnlyList<string> CentrosCusto,
    bool TodosCentrosCusto);

public enum RbacFalha
{
    Nenhuma = 0,
    NomeObrigatorio,
    NomeDuplicado,
    PermissaoDesconhecida,
    PerfilNaoEncontrado,
    UltimoPerfilAdministrativo,
    EscalonamentoDePrivilegio,

    /// <summary>Gate Final da Onda 1 (ADR-0022) — "Administrador Sênior" é nome reservado: nenhum Perfil
    /// criado ou renomeado pela Gestão de Perfis comum pode assumir esse nome. Sem esta barreira, um
    /// Administrador de BU com <c>Perfil.Gerenciar</c> poderia criar/renomear um Perfil para esse nome na
    /// própria BU e, ao se vincular a ele, ganhar <c>EscopoAdministrativo.Produto</c> (cross-BU) sem nunca
    /// ter recebido essa concessão — escalonamento de ESCOPO, não de permissão (a checagem existente de
    /// não-escalonamento de permissão não cobre este caminho). O Perfil "Administrador Sênior" real só é
    /// criado pelo Bootstrap (<c>ConcluirBootstrapUseCase</c>), que nunca passa por este caso de uso.</summary>
    NomeReservado,
    EmailObrigatorio,
    EmailInvalido,
    EmailDuplicado,
    UsuarioNaoEncontrado,
    PerfilInvalido,
    UltimoAdministradorSeniorAtivo,

    /// <summary>Resolução da dívida O1.6-L2: um código ERP de Centro de Custo informado no vínculo
    /// Usuário×Centro de Custo não existe no ERP, ou já está ancorado (via <c>CentroCustoMetadado</c>) a
    /// outra Unidade de Negócio — nenhum dos dois casos é aceito.</summary>
    CentroCustoInvalido,

    /// <summary>O1.8 — Unidade de Alocação não encontrada (Id inexistente, ou de outra Unidade de
    /// Negócio — nunca revelado como distinção, sempre tratado como "não encontrada").</summary>
    UnidadeAlocacaoNaoEncontrada,

    // ---- O1.11 — Administração de Unidades de Negócio e Configuração Técnica ----
    UnidadeNegocioNaoEncontrada,
    SlugObrigatorio,
    SlugInvalido,
    SlugDuplicado,
    TipoObrigatorio,
    IdentityProviderNaoEncontrado,
    SistemaErpObrigatorio,
    ConfiguracaoErpNaoEncontrada,
    ConfiguracaoErpJaConfigurada,
    ChaveObrigatoria,
    ParametroNaoEncontrado,
    ParametroDuplicado,
    FeatureFlagNaoEncontrada,
    FeatureFlagDuplicada,

    /// <summary>O1.11, item #24 — Configuração de Notificações. E-mail remetente inválido, ou ausente ao
    /// tentar ativar as notificações por e-mail.</summary>
    EmailRemetenteInvalido,
    ConfiguracaoNotificacaoNaoEncontrada,

    // ---- O1.12 — Fundação de Administração (Workflow, Alçadas, Controle Orçamentário) ----
    TipoProcessoObrigatorio,
    OrdemInvalida,
    RegraWorkflowNaoEncontrada,

    NivelInvalido,
    FaixaDeValorInvalida,
    AprovadorInvalido,
    CentroCustoObrigatorio,
    CentroCustoInvalidoNaUnidadeDeNegocio,
    AlcadaAprovacaoNaoEncontrada,

    ValorLimiteInvalido,
    RegraOrcamentariaNaoEncontrada,

    // ---- O1.13.5 — Fundação dos Agents Especialistas Linx (base de conhecimento) ----
    ConhecimentoLinxNaoEncontrado,
    AssuntoObrigatorio,
    ConteudoObrigatorio,
    FonteObrigatoria,

    /// <summary>Transição de <see cref="BlueprintOS.Domain.Knowledge.Linx.LinxConhecimentoProveniencia"/>
    /// não permitida pela máquina de estados de <c>LinxKnowledgeEntry.Promover</c> (pular etapa, rebaixar,
    /// ou reabrir uma entrada já Aprovada).</summary>
    TransicaoProvenienciaInvalida,

    /// <summary>Uma nova descoberta/inferência para o mesmo <c>VersaoRaizId</c> contradiz o conteúdo da
    /// versão mais recente já Validada/Aprovada — a Work Order (seção 12) proíbe substituição automática:
    /// o conflito é registrado e exige tratamento/validação explícita, nunca aceito silenciosamente.</summary>
    ConflitoDeConhecimentoDetectado,

    // ---- B3 — Bloco 3: Item Fiscal (Discovery homologado) ----
    CodigoObrigatorio,
    CodigoDuplicado,
    DescricaoObrigatoria,
    ItemFiscalNaoEncontrado,

    /// <summary>Conta Contábil obrigatória no Item Fiscal (decisão do Product Owner, mesmo o Linx
    /// permitindo `CADASTRO_ITEM_FISCAL.CONTA_CONTABIL` nula) — código ausente, inexistente, ou
    /// existente porém inativo (`AtivoEfetivo`, respeitando `ADR-0024`).</summary>
    ContaContabilObrigatoria,
    ContaContabilInvalidaOuInativa,

    /// <summary>Unidade de Medida obrigatória no Item Fiscal (Discovery homologado) — código ausente,
    /// inexistente, ou existente porém inativo no +Compras.</summary>
    UnidadeMedidaObrigatoria,
    UnidadeMedidaInvalidaOuInativa,

    // ---- B3 — Bloco 4: Referências de Item Fiscal por Fornecedor (Discovery homologado) ----
    ItemFiscalReferenciaFornecedorNaoEncontrada,
    FornecedorObrigatorio,
    FornecedorNaoEncontrado,

    /// <summary>Fornecedor existe, porém está inativo no +Compras — mesma regra já aplicada a Conta
    /// Contábil/Unidade de Medida: só entidades ativas podem ser selecionadas em novas referências.</summary>
    FornecedorInvalidoOuInativo,
    CodigoItemFornecedorObrigatorio,

    /// <summary>Estrutura comprovada em Linx (`ITEM_FISCAL_REF_FORNECEDOR.KeyFieldList = FORNECEDOR,
    /// CODIGO_ITEM`) — um Fornecedor já possui uma referência para este Item Fiscal.</summary>
    ReferenciaJaExistenteParaFornecedor,

    /// <summary>Decisão do Product Owner (homologação do Bloco 4): (FornecedorId, CodigoItemFornecedor) é
    /// único GLOBALMENTE — garante que o DE/PARA reverso sempre resolva para um único Item Fiscal.</summary>
    CodigoItemFornecedorDuplicadoParaFornecedor,
}

/// <summary>Projeção de leitura de uma Unidade de Alocação (O1.8 — Persistência Real). Sem vínculo com
/// Centro de Custo nesta sprint (escopo da O1.9).</summary>
public sealed record UnidadeAlocacaoDto(
    Guid Id,
    string Nome,
    string Descricao,
    Guid UnidadeNegocioId,
    bool Ativo,
    DateTimeOffset CriadoEm,
    DateTimeOffset AtualizadoEm);

/// <summary>Entrada de criação/edição de Unidade de Alocação. <c>UnidadeNegocioId</c> é deliberadamente
/// ausente — vem sempre da identidade autenticada, nunca do cliente (mesmo cuidado de
/// <see cref="UsuarioInput"/>/<see cref="PerfilInput"/>).</summary>
public sealed record UnidadeAlocacaoInput(string Nome, string Descricao);

/// <summary>Resultado de operação de escrita de Perfil. Nunca lança exceção para falha de regra de
/// negócio esperada — a camada de API traduz <see cref="Falha"/> em código HTTP.</summary>
public sealed record RbacResultado<T>(bool Sucesso, RbacFalha Falha, string? Mensagem, T? Valor)
{
    public static RbacResultado<T> Ok(T valor) => new(true, RbacFalha.Nenhuma, null, valor);
    public static RbacResultado<T> Erro(RbacFalha falha, string mensagem) => new(false, falha, mensagem, default);
}
