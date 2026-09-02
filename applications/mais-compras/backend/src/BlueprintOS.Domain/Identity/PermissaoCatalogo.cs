namespace BlueprintOS.Domain.Identity;

/// <summary>Uma entrada do catálogo global de permissões atômicas. <paramref name="Recurso"/> e
/// <paramref name="Acao"/> são derivados do próprio <paramref name="Codigo"/> (<c>Recurso.Acao</c>) e
/// existem apenas para agrupamento/apresentação — o <paramref name="Codigo"/> é a única identidade
/// semântica da permissão.</summary>
public sealed record PermissaoDefinicao(Guid Id, string Codigo, string Recurso, string Acao, string Descricao);

/// <summary>Fonte central única do catálogo de permissões do +Compras (O1.5 — RBAC Real).
///
/// Existe para eliminar a duplicação de "nomes mágicos" de permissão que a ADR-0020 (item 8) e a
/// Work Order O1.5 proíbem: as policies de ASP.NET Core (<c>RbacPolicies</c>), o seed do banco
/// (<c>PermissaoConfiguration.HasData</c>), os endpoints protegidos e o catálogo devolvido ao frontend
/// derivam TODOS deste mesmo arranjo. Nenhuma outra parte do código declara um código de permissão
/// literalmente.
///
/// Os <see cref="Guid"/> abaixo são **estáveis e imutáveis** — são a chave primária das linhas de
/// <c>Permissoes</c> semeadas pela migration <c>AddRbacPerfilPermissaoCatalogo</c>. Alterá-los quebraria
/// os vínculos <c>PerfisPermissoes</c> já persistidos.
///
/// Conteúdo: derivado exclusivamente dos códigos já documentados em `docs/product/ComprasFuncional.md`
/// (seções de Administração e "Gestão de Perfis"). O catálogo definitivo de **Perfis** (quais perfis
/// existem e o que cada um contém) permanece pendência de produto registrada na ADR-0020 e em
/// `PROJECT_STATE.md` — esta classe entrega a mecânica e as permissões já documentadas, nunca perfis
/// inventados.</summary>
public static class PermissaoCatalogo
{
    public const string UnidadeNegocioGerenciar = "UnidadeNegocio.Gerenciar";
    public const string UsuarioGerenciar = "Usuario.Gerenciar";
    public const string PerfilGerenciar = "Perfil.Gerenciar";
    public const string FilialGerenciar = "Filial.Gerenciar";
    public const string CentroCustoGerenciar = "CentroCusto.Gerenciar";
    public const string UnidadeAlocacaoGerenciar = "UnidadeAlocacao.Gerenciar";
    public const string ConfiguracaoErpGerenciar = "ConfiguracaoErp.Gerenciar";
    public const string SistemaGerenciar = "Sistema.Gerenciar";
    public const string FornecedorCriar = "Fornecedor.Criar";
    public const string FornecedorEditar = "Fornecedor.Editar";
    public const string FornecedorAprovar = "Fornecedor.Aprovar";
    public const string PedidoCriar = "Pedido.Criar";
    public const string PedidoAprovar = "Pedido.Aprovar";
    public const string PedidoCancelar = "Pedido.Cancelar";

    /// <summary>O1.12 — Fundação de Administração (Workflow, Alçadas, Controle Orçamentário), ADR-0020
    /// (revisão R1.1/ADR-0020). Apenas o cadastro administrativo destas 3 estruturas; nenhuma delas
    /// autoriza a execução do motor operacional correspondente (fora de escopo desta sprint).</summary>
    public const string WorkflowGerenciar = "Workflow.Gerenciar";
    public const string AlcadaGerenciar = "Alcada.Gerenciar";
    public const string OrcamentoGerenciar = "Orcamento.Gerenciar";

    /// <summary>O1.13.5 — Fundação dos Agents Especialistas Linx. <see cref="ConhecimentoLinxGerenciar"/>
    /// cobre registrar descobertas/inferências e promover até "Validado". <see cref="ConhecimentoLinxAprovar"/>
    /// é a permissão dedicada exigida pela Work Order (seção 9/18) especificamente para a promoção final e
    /// sensível a "Aprovado" — nunca concedida junto do catálogo básico por padrão implícito.</summary>
    public const string ConhecimentoLinxGerenciar = "ConhecimentoLinx.Gerenciar";
    public const string ConhecimentoLinxAprovar = "ConhecimentoLinx.Aprovar";

    /// <summary>B3 — Bloco 1 (Discovery homologado, `ContratoFuncionalPreliminar-B3-ItemFiscal.md` §2/§8):
    /// Conta Contábil é cadastro de apoio originado do Linx (`CTB_CONTA_PLANO`) — mesma semântica de
    /// <see cref="FilialGerenciar"/>/<see cref="CentroCustoGerenciar"/> (ativar/inativar localmente e manter
    /// a Descrição +Compras, nunca criar/editar o dado mestre).</summary>
    public const string ContaContabilGerenciar = "ContaContabil.Gerenciar";

    /// <summary>B3 — Bloco 2 (Discovery homologado): Unidade de Medida é cadastro de apoio originado do
    /// Linx (`UNIDADES`) — mesma semântica de <see cref="ContaContabilGerenciar"/>.</summary>
    public const string UnidadeMedidaGerenciar = "UnidadeMedida.Gerenciar";

    /// <summary>B3 — Bloco 3 (Discovery homologado, `ContratoFuncionalPreliminar-B3-ItemFiscal.md` §7):
    /// cadastrar/editar/inativar Item Fiscal dependem de permissões separadas — não presumir que todo
    /// usuário com acesso ao cadastro pode executar as três operações (mesmo padrão de granularidade de
    /// <see cref="FornecedorCriar"/>/<see cref="FornecedorEditar"/>, em vez do "Gerenciar" único usado nos
    /// cadastros de apoio somente-leitura do ERP).</summary>
    public const string ItemFiscalVisualizar = "ItemFiscal.Visualizar";
    public const string ItemFiscalCriar = "ItemFiscal.Criar";
    public const string ItemFiscalEditar = "ItemFiscal.Editar";
    public const string ItemFiscalInativar = "ItemFiscal.Inativar";

    private static readonly PermissaoDefinicao[] Definicoes =
    [
        Definir(new Guid("b1a5c4e0-0001-4a10-9f01-000000000001"), UnidadeNegocioGerenciar, "Criar, editar e inativar Unidades de Negócio"),
        Definir(new Guid("b1a5c4e0-0001-4a10-9f01-000000000002"), UsuarioGerenciar, "Criar, editar, ativar/inativar usuários e vincular Perfis e Centros de Custo"),
        Definir(new Guid("b1a5c4e0-0001-4a10-9f01-000000000003"), PerfilGerenciar, "Criar, editar e ativar/inativar Perfis e suas permissões"),
        Definir(new Guid("b1a5c4e0-0001-4a10-9f01-000000000004"), FilialGerenciar, "Ativar/inativar Filiais no +Compras e manter a Descrição +Compras"),
        Definir(new Guid("b1a5c4e0-0001-4a10-9f01-000000000005"), CentroCustoGerenciar, "Ativar/inativar Centros de Custo no +Compras e manter a Descrição +Compras"),
        Definir(new Guid("b1a5c4e0-0001-4a10-9f01-000000000006"), UnidadeAlocacaoGerenciar, "Criar, editar e ativar/inativar Unidades de Alocação"),
        Definir(new Guid("b1a5c4e0-0001-4a10-9f01-000000000007"), ConfiguracaoErpGerenciar, "Configurar a integração de ERP por Unidade de Negócio"),
        Definir(new Guid("b1a5c4e0-0001-4a10-9f01-000000000008"), SistemaGerenciar, "Acessar Administração do Sistema (integrações, monitor, filas, logs, saúde)"),
        Definir(new Guid("b1a5c4e0-0001-4a10-9f01-000000000009"), FornecedorCriar, "Cadastrar novo fornecedor"),
        Definir(new Guid("b1a5c4e0-0001-4a10-9f01-00000000000a"), FornecedorEditar, "Atualizar dados de fornecedor"),
        Definir(new Guid("b1a5c4e0-0001-4a10-9f01-00000000000b"), FornecedorAprovar, "Aprovar divergências de enriquecimento de fornecedor"),
        Definir(new Guid("b1a5c4e0-0001-4a10-9f01-00000000000c"), PedidoCriar, "Criar pedido de compra"),
        Definir(new Guid("b1a5c4e0-0001-4a10-9f01-00000000000d"), PedidoAprovar, "Aprovar pedido de compra"),
        Definir(new Guid("b1a5c4e0-0001-4a10-9f01-00000000000e"), PedidoCancelar, "Cancelar pedido de compra"),
        Definir(new Guid("b1a5c4e0-0001-4a10-9f01-00000000000f"), WorkflowGerenciar, "Criar, editar e ativar/inativar Regras de Workflow por Unidade de Negócio"),
        Definir(new Guid("b1a5c4e0-0001-4a10-9f01-000000000010"), AlcadaGerenciar, "Criar, editar e ativar/inativar Alçadas de Aprovação por Unidade de Negócio"),
        Definir(new Guid("b1a5c4e0-0001-4a10-9f01-000000000011"), OrcamentoGerenciar, "Criar, editar e ativar/inativar Regras Orçamentárias por Unidade de Negócio"),
        Definir(new Guid("b1a5c4e0-0001-4a10-9f01-000000000012"), ConhecimentoLinxGerenciar, "Registrar descobertas/inferências e validar conhecimento dos Agents Especialistas Linx"),
        Definir(new Guid("b1a5c4e0-0001-4a10-9f01-000000000013"), ConhecimentoLinxAprovar, "Promover conhecimento dos Agents Especialistas Linx a 'Aprovado'"),
        Definir(new Guid("b1a5c4e0-0001-4a10-9f01-000000000014"), ContaContabilGerenciar, "Ativar/inativar Contas Contábeis no +Compras e manter a Descrição +Compras"),
        Definir(new Guid("b1a5c4e0-0001-4a10-9f01-000000000015"), UnidadeMedidaGerenciar, "Ativar/inativar Unidades de Medida no +Compras e manter a Descrição +Compras"),
        Definir(new Guid("b1a5c4e0-0001-4a10-9f01-000000000016"), ItemFiscalVisualizar, "Consultar o cadastro de Item Fiscal"),
        Definir(new Guid("b1a5c4e0-0001-4a10-9f01-000000000017"), ItemFiscalCriar, "Cadastrar novo Item Fiscal"),
        Definir(new Guid("b1a5c4e0-0001-4a10-9f01-000000000018"), ItemFiscalEditar, "Editar Item Fiscal existente"),
        Definir(new Guid("b1a5c4e0-0001-4a10-9f01-000000000019"), ItemFiscalInativar, "Ativar/inativar Item Fiscal no +Compras"),
    ];

    /// <summary>Catálogo completo, na ordem canônica de apresentação.</summary>
    public static IReadOnlyList<PermissaoDefinicao> Todas => Definicoes;

    /// <summary>Todos os códigos válidos. Usado para registrar as policies e para rejeitar códigos
    /// desconhecidos vindos do frontend (nunca confiar no cliente).</summary>
    public static IReadOnlyCollection<string> Codigos { get; } =
        Definicoes.Select(x => x.Codigo).ToArray();

    private static readonly Dictionary<string, PermissaoDefinicao> PorCodigo =
        Definicoes.ToDictionary(x => x.Codigo, StringComparer.OrdinalIgnoreCase);

    /// <summary>Comparação case-insensitive deliberada: o código é um identificador semântico, não um
    /// dado sensível a caixa. Isto impede que "perfil.gerenciar" enviado pelo cliente seja tratado como
    /// uma permissão diferente de <c>Perfil.Gerenciar</c>.</summary>
    public static bool Existe(string codigo) =>
        !string.IsNullOrWhiteSpace(codigo) && PorCodigo.ContainsKey(codigo.Trim());

    public static PermissaoDefinicao? Obter(string codigo) =>
        !string.IsNullOrWhiteSpace(codigo) && PorCodigo.TryGetValue(codigo.Trim(), out var definicao) ? definicao : null;

    /// <summary>Normaliza para a grafia canônica do catálogo. Retorna <c>null</c> para código
    /// desconhecido — o chamador deve rejeitar, nunca aceitar o valor cru do cliente.</summary>
    public static string? Normalizar(string codigo) => Obter(codigo)?.Codigo;

    private static PermissaoDefinicao Definir(Guid id, string codigo, string descricao)
    {
        var separador = codigo.IndexOf('.');
        if (separador <= 0 || separador == codigo.Length - 1)
        {
            throw new InvalidOperationException($"Código de permissão fora do padrão 'Recurso.Acao': '{codigo}'.");
        }

        return new PermissaoDefinicao(id, codigo, codigo[..separador], codigo[(separador + 1)..], descricao);
    }
}
