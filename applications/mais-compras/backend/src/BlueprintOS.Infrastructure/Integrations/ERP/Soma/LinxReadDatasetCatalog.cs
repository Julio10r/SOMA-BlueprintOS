using BlueprintOS.Core.AI.Governance.Contracts;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Soma;

/// <summary>
/// B3 — Bloco 5A.9, Gate A / revisão pré-Gate B: o catálogo fixo, code-reviewed, de datasets que o LiveRead
/// governado pode resolver. Deliberadamente fechado — não há API de registro em runtime; um novo dataset só
/// existe mudando esta classe e passando por revisão de código, nunca por um valor vindo do chamador (ver
/// <see cref="IReadDatasetCatalog"/>).
///
/// A query de "linx.fornecedores.snapshot" espelha, para o subconjunto de colunas já usado pelas regras de
/// negócio homologadas do Fornecedor (ver SincronizarFornecedoresErpUseCase — Ativo, fonte cadastral,
/// CNPJ/vínculo), o mesmo join FORNECEDORES/CADASTRO_CLI_FOR já usado por <see cref="SomaFornecedorReader"/>,
/// incluindo <c>UltimaAlteracao</c> (COALESCE das duas colunas DATA_PARA_TRANSFERENCIA, confirmadas por
/// Discovery real em 03/09/2026 — ver <see cref="EstrategiaNormalRecomendadaFornecedores"/>), necessária para
/// REFINED aplicar LWW cadastral e seleção de Principal sem reconsultar o Linx.
/// </summary>
public sealed class LinxReadDatasetCatalog : IReadDatasetCatalog
{
    public const string FornecedoresSnapshot = "linx.fornecedores.snapshot";
    public const string ContasContabeisSnapshot = "linx.contas-contabeis.snapshot";
    public const string UnidadesMedidaSnapshot = "linx.unidades-medida.snapshot";
    public const string CentrosCustoSnapshot = "linx.centros-custo.snapshot";
    public const string FiliaisSnapshot = "linx.filiais.snapshot";
    public const string FornecedorDominiosSnapshot = "linx.fornecedor-dominios.snapshot";
    public const string ItensFiscaisSnapshot = "linx.itens-fiscais.snapshot";
    public const string ItensFiscaisReferenciasFornecedorSnapshot = "linx.itens-fiscais-referencias-fornecedor.snapshot";

    /// <summary>
    /// RECOMENDAÇÃO TÉCNICA (Discovery real, governado, read-only, contra SOMA_DESENV em 03/09/2026 —
    /// não é suposição): INCREMENTAL. Evidência:
    /// <list type="bullet">
    /// <item>FORNECEDORES.DATA_PARA_TRANSFERENCIA (datetime, precisão de milissegundo, 78.374 linhas, ZERO
    /// nulos) é estampado com GETDATE() de forma incondicional pela trigger ativa LXU_FORNECEDORES em
    /// QUALQUER UPDATE que a própria instrução não tenha explicitamente definido essa coluna — inclui,
    /// portanto, uma inativação (mudança isolada de INATIVO). Confirmado lendo o texto real da trigger
    /// (sys.sql_modules), não inferido.</item>
    /// <item>CADASTRO_CLI_FOR.DATA_PARA_TRANSFERENCIA (mesma definição de coluna, 96.699 linhas, ZERO nulos)
    /// tem o MESMO comportamento — a trigger LXU_CADASTRO_CLI_FOR tinha esse bloco de estampagem comentado
    /// (mudança #8#, 2017) numa posição antiga, mas ele foi restaurado e MOVIDO para o final da trigger
    /// (único ponto de saída normal antes do tratamento de erro) — ativo hoje, confirmado no texto real.</item>
    /// <item>RISCO REAL, não hipotético: as duas colunas são independentes — uma alteração só em
    /// CADASTRO_CLI_FOR não necessariamente toca FORNECEDORES e vice-versa. Por isso o watermark abaixo é
    /// HÍBRIDO (as duas colunas, com OR), nunca uma única coluna isolada.</item>
    /// <item>DELETE físico é fisicamente possível (exercido em teste, nunca no caminho real do Adapter, que
    /// só inativa logicamente) e não é impeditivo para INCREMENTAL — recarga Full administrativa (sempre
    /// disponível, ver <see cref="ReadDatasetDefinition.FullCommandTextFactory"/>) cobre reconciliação/
    /// recuperação extraordinária.</item>
    /// <item>Nenhum índice existe sobre DATA_PARA_TRANSFERENCIA em nenhuma das duas tabelas (confirmado via
    /// sys.indexes) — o filtro incremental faz Clustered Index Scan completo, mas o custo é aceitável dado o
    /// volume real (78k/97k linhas, não milhões) e NÃO justifica criar índice no Linx (proibido).</item>
    /// </list>
    /// Decisão final da estratégia permanece do Product Owner — este catálogo já reflete a recomendação para
    /// que o contrato do dataset seja concreto e testável, não para pré-decidir silenciosamente por ele.
    /// </summary>
    public const DatasetLoadKind EstrategiaNormalRecomendadaFornecedores = DatasetLoadKind.Incremental;

    private static readonly IReadOnlyList<string> FornecedoresSnapshotColumns =
    [
        "CodigoFornecedor", "Clifor", "CnpjCpf", "RazaoSocial", "NomeFantasia", "TipoPessoa",
        "InativoFornecedores", "InativoCadastroCliFor", "UltimaAlteracao",
    ];

    private readonly IReadOnlyDictionary<string, ReadDatasetDefinition> _datasets;

    public LinxReadDatasetCatalog()
    {
        _datasets = new Dictionary<string, ReadDatasetDefinition>(StringComparer.Ordinal)
        {
            [FornecedoresSnapshot] = new ReadDatasetDefinition(
                Name: FornecedoresSnapshot,
                Description: "Snapshot bruto (RAW) de FORNECEDORES + CADASTRO_CLI_FOR do ERP Linx/SOMA, sem interpretação de regra de negócio.",
                SourceConnectionProfileKey: "linx-development",
                DestinationConnectionProfileKey: "mais-compras-development",
                DestinationTable: "RAW_LinxFornecedoresSnapshot",
                Columns: FornecedoresSnapshotColumns,
                EstrategiaNormal: EstrategiaNormalRecomendadaFornecedores,
                FullCommandTextFactory: BuildFullCommandText,
                IncrementalCommandTextFactory: BuildIncrementalCommandText,
                Watermark: new WatermarkDefinition(
                    QualifiedColumns: ["FORNECEDORES.DATA_PARA_TRANSFERENCIA", "CADASTRO_CLI_FOR.DATA_PARA_TRANSFERENCIA"],
                    SqlType: "datetime",
                    // Janela de sobreposição de segurança: o pipeline é idempotente, então reler alguns
                    // minutos de sobreposição é sempre preferível a arriscar perder um registro na fronteira
                    // temporal (GETDATE() dentro de uma transação que confirma um pouco depois do instante em
                    // que a leitura anterior capturou seu próprio watermark). 5 minutos é conservador dado que
                    // a cadência esperada é diária, não sub-segundo, e o full-scan de ~80-97k linhas é barato.
                    OverlapWindow: TimeSpan.FromMinutes(5),
                    Description: "FORNECEDORES.DATA_PARA_TRANSFERENCIA OR CADASTRO_CLI_FOR.DATA_PARA_TRANSFERENCIA — watermark híbrido, confirmado por leitura real do texto das triggers LXU_FORNECEDORES/LXU_CADASTRO_CLI_FOR em 03/09/2026."),
                CommandTimeoutSeconds: 300),

            [ContasContabeisSnapshot] = new ReadDatasetDefinition(
                Name: ContasContabeisSnapshot,
                Description: "Snapshot bruto (RAW) de CTB_CONTA_PLANO do ERP Linx/SOMA — cadastro de apoio de Conta Contábil, sem interpretação de regra de negócio.",
                SourceConnectionProfileKey: "linx-development",
                DestinationConnectionProfileKey: "mais-compras-development",
                DestinationTable: "RAW_LinxContasContabeisSnapshot",
                Columns: ["CodigoErp", "DescricaoErp", "InativoErp", "UltimaAlteracao"],
                // Discovery real 03/09/2026: DATA_PARA_TRANSFERENCIA 0% NULL (3.264 linhas), trigger ativa
                // LXUDT_CTB_CONTA_PLANO confirmada por leitura de sys.sql_modules. Tabela irmã CONTAS_PLANO é
                // legado morto (parado em 2004) — deliberadamente NÃO usada aqui.
                EstrategiaNormal: DatasetLoadKind.Incremental,
                FullCommandTextFactory: () => """
                    SELECT [CONTA_CONTABIL] AS CodigoErp, [DESC_CONTA] AS DescricaoErp, [INATIVA] AS InativoErp, [DATA_PARA_TRANSFERENCIA] AS UltimaAlteracao
                    FROM [dbo].[CTB_CONTA_PLANO]
                    """,
                IncrementalCommandTextFactory: () => """
                    SELECT [CONTA_CONTABIL] AS CodigoErp, [DESC_CONTA] AS DescricaoErp, [INATIVA] AS InativoErp, [DATA_PARA_TRANSFERENCIA] AS UltimaAlteracao
                    FROM [dbo].[CTB_CONTA_PLANO]
                    WHERE [DATA_PARA_TRANSFERENCIA] >= @watermark
                    """,
                Watermark: new WatermarkDefinition(
                    QualifiedColumns: ["CTB_CONTA_PLANO.DATA_PARA_TRANSFERENCIA"],
                    SqlType: "datetime",
                    OverlapWindow: TimeSpan.FromMinutes(5),
                    Description: "CTB_CONTA_PLANO.DATA_PARA_TRANSFERENCIA — coluna única, confirmada por Discovery real em 03/09/2026 (0% NULL, trigger ativa)."),
                CommandTimeoutSeconds: 60),

            [UnidadesMedidaSnapshot] = new ReadDatasetDefinition(
                Name: UnidadesMedidaSnapshot,
                Description: "Snapshot bruto (RAW) de UNIDADES do ERP Linx/SOMA — cadastro de apoio de Unidade de Medida, sem interpretação de regra de negócio.",
                SourceConnectionProfileKey: "linx-development",
                DestinationConnectionProfileKey: "mais-compras-development",
                DestinationTable: "RAW_LinxUnidadesMedidaSnapshot",
                Columns: ["CodigoErp", "DescricaoErp", "InativoErp", "UltimaAlteracao"],
                // Decisão definitiva do PO: FULL — volume trivial (~75 linhas) e UNIDADES não tem NENHUMA
                // coluna de status ativo/inativo no Linx (confirmado por Discovery — daí InativoErp sempre
                // NULL nesta query). Sem incremental necessário; catálogo não expõe IncrementalCommandTextFactory.
                EstrategiaNormal: DatasetLoadKind.Full,
                FullCommandTextFactory: () => """
                    SELECT [UNIDADE] AS CodigoErp, [DESC_UNIDADE] AS DescricaoErp, CAST(NULL AS bit) AS InativoErp, [Data_para_transferencia] AS UltimaAlteracao
                    FROM [dbo].[UNIDADES]
                    """,
                IncrementalCommandTextFactory: null,
                Watermark: null,
                CommandTimeoutSeconds: 30),

            [CentrosCustoSnapshot] = new ReadDatasetDefinition(
                Name: CentrosCustoSnapshot,
                Description: "Snapshot bruto (RAW) de CTB_CENTRO_CUSTO do ERP Linx/SOMA — cadastro de apoio de Centro de Custo, sem interpretação de regra de negócio.",
                SourceConnectionProfileKey: "linx-development",
                DestinationConnectionProfileKey: "mais-compras-development",
                DestinationTable: "RAW_LinxCentrosCustoSnapshot",
                Columns: ["CodigoErp", "DescricaoErp", "InativoErp", "UltimaAlteracao"],
                // Discovery real 03/09/2026: 1.800/2.138 linhas (84%) têm DATA_PARA_TRANSFERENCIA NULL —
                // legado anterior à criação do trigger LXU_GS_CTB_CENTRO_CUSTO em 07/06/2024 (confirmado no
                // próprio texto do trigger). Por isso este dataset é Incremental mas exige bootstrap FULL
                // obrigatório (regra geral já válida para todo Incremental) tratando o legado sem watermark.
                EstrategiaNormal: DatasetLoadKind.Incremental,
                FullCommandTextFactory: () => """
                    SELECT [CENTRO_CUSTO] AS CodigoErp, [DESC_CENTRO_CUSTO] AS DescricaoErp, [INATIVA] AS InativoErp, [DATA_PARA_TRANSFERENCIA] AS UltimaAlteracao
                    FROM [dbo].[CTB_CENTRO_CUSTO]
                    """,
                IncrementalCommandTextFactory: () => """
                    SELECT [CENTRO_CUSTO] AS CodigoErp, [DESC_CENTRO_CUSTO] AS DescricaoErp, [INATIVA] AS InativoErp, [DATA_PARA_TRANSFERENCIA] AS UltimaAlteracao
                    FROM [dbo].[CTB_CENTRO_CUSTO]
                    WHERE [DATA_PARA_TRANSFERENCIA] >= @watermark
                    """,
                Watermark: new WatermarkDefinition(
                    QualifiedColumns: ["CTB_CENTRO_CUSTO.DATA_PARA_TRANSFERENCIA"],
                    SqlType: "datetime",
                    OverlapWindow: TimeSpan.FromMinutes(5),
                    Description: "CTB_CENTRO_CUSTO.DATA_PARA_TRANSFERENCIA — confiável só a partir de jun/2024 (trigger criada em 07/06/2024); bootstrap FULL cobre o legado sem watermark (84% das linhas)."),
                CommandTimeoutSeconds: 60),

            [FiliaisSnapshot] = new ReadDatasetDefinition(
                Name: FiliaisSnapshot,
                Description: "Snapshot bruto (RAW) de FILIAIS + CADASTRO_CLI_FOR do ERP Linx/SOMA — origem principal FILIAIS, CADASTRO_CLI_FOR quando necessário ao contrato (decisão definitiva do PO), sem interpretação de regra de negócio.",
                SourceConnectionProfileKey: "linx-development",
                DestinationConnectionProfileKey: "mais-compras-development",
                DestinationTable: "RAW_LinxFiliaisSnapshot",
                Columns: ["CodigoErp", "DescricaoErp", "InativoErp", "UltimaAlteracao"],
                // Decisão definitiva do PO: INCREMENTAL HIBRIDO. Discovery real 03/09/2026 (prova empírica,
                // não precaução): zero dos 2.144 pares FILIAIS×CADASTRO_CLI_FOR casados por CLIFOR têm
                // DATA_PARA_TRANSFERENCIA idêntica; 217 (~10%) têm CADASTRO_CLI_FOR mais recente — mudança
                // real que só tocou essa tabela. Triggers LXU_FILIAIS/LXU_CADASTRO_CLI_FOR (lidas via
                // sys.sql_modules) confirmam que cada tabela estampa seu próprio watermark independentemente,
                // sem propagação cruzada. Bootstrap FULL obrigatório (regra geral de todo Incremental).
                EstrategiaNormal: DatasetLoadKind.Incremental,
                FullCommandTextFactory: () => """
                    SELECT
                        f.[COD_FILIAL]                               AS CodigoErp,
                        f.[FILIAL]                                     AS DescricaoErp,
                        c.[INATIVO]                                    AS InativoErp,
                        COALESCE(c.[DATA_PARA_TRANSFERENCIA], f.[DATA_PARA_TRANSFERENCIA]) AS UltimaAlteracao
                    FROM [dbo].[FILIAIS] f
                    LEFT JOIN [dbo].[CADASTRO_CLI_FOR] c ON c.[COD_CLIFOR] = f.[CLIFOR]
                    """,
                IncrementalCommandTextFactory: () => """
                    SELECT
                        f.[COD_FILIAL]                               AS CodigoErp,
                        f.[FILIAL]                                     AS DescricaoErp,
                        c.[INATIVO]                                    AS InativoErp,
                        COALESCE(c.[DATA_PARA_TRANSFERENCIA], f.[DATA_PARA_TRANSFERENCIA]) AS UltimaAlteracao
                    FROM [dbo].[FILIAIS] f
                    LEFT JOIN [dbo].[CADASTRO_CLI_FOR] c ON c.[COD_CLIFOR] = f.[CLIFOR]
                    WHERE f.[DATA_PARA_TRANSFERENCIA] >= @watermark OR c.[DATA_PARA_TRANSFERENCIA] >= @watermark
                    """,
                Watermark: new WatermarkDefinition(
                    QualifiedColumns: ["FILIAIS.DATA_PARA_TRANSFERENCIA", "CADASTRO_CLI_FOR.DATA_PARA_TRANSFERENCIA"],
                    SqlType: "datetime",
                    OverlapWindow: TimeSpan.FromMinutes(5),
                    Description: "FILIAIS.DATA_PARA_TRANSFERENCIA OR CADASTRO_CLI_FOR.DATA_PARA_TRANSFERENCIA — watermark híbrido, comprovado por evidência real (217/2.144 pares com CADASTRO_CLI_FOR mais recente que FILIAIS) em 03/09/2026, mesmo padrão homologado para linx.fornecedores.snapshot."),
                CommandTimeoutSeconds: 60),

            [FornecedorDominiosSnapshot] = new ReadDatasetDefinition(
                Name: FornecedorDominiosSnapshot,
                Description: "Snapshot bruto (RAW) unificado de FORNECEDOR_TIPOS + FORNECEDOR_SUBTIPO + COND_ENT_PGTOS do ERP Linx/SOMA — os 3 catálogos que alimentam FornecedorDominioErp, descobertos via FK real de FORNECEDORES (sys.foreign_keys). FULL apenas (decisão do PO: volume pequeno, sem necessidade de incremental).",
                SourceConnectionProfileKey: "linx-development",
                DestinationConnectionProfileKey: "mais-compras-development",
                DestinationTable: "RAW_LinxFornecedorDominiosSnapshot",
                Columns: ["TipoDominio", "CodigoErp", "Descricao", "UltimaAlteracao"],
                EstrategiaNormal: DatasetLoadKind.Full,
                // FORNECEDOR_TIPOS/FORNECEDOR_SUBTIPO não têm coluna de descrição própria no Linx (só
                // TIPO/SUBTIPO_FORNECEDOR, comprovado por sys.columns) — mas os códigos já são texto legível
                // (ex.: "ADMINISTRATIVO", "13 SALARIO"), então o próprio código também serve como descrição.
                // SUBTIPO_FORNECEDOR tem chave composta real (FK confirmada: SUBTIPO_FORNECEDOR+TIPO) — o
                // CodigoErp aqui codifica essa composição como "TIPO:SUBTIPO", decisão de design documentada
                // em RawLinxFornecedorDominioErpRegistro para nunca ser reinventada por engano.
                // Correção real (achado durante execução, mesma classe do padding char(6) de Fornecedor e
                // dos espaços inconsistentes de Centro de Custo): TIPO/SUBTIPO_FORNECEDOR chegam com padding
                // fixo do Linx — sem LTRIM/RTRIM aqui, a chave composta "TIPO:SUBTIPO" ultrapassa o tamanho
                // físico da coluna de destino. Aparar em TODAS as colunas de texto, não só nas usadas na
                // composição, evita reintroduzir o mesmo problema em Descricao.
                FullCommandTextFactory: () => """
                    SELECT 'TipoFornecedor' AS TipoDominio, LTRIM(RTRIM([TIPO])) AS CodigoErp, LTRIM(RTRIM([TIPO])) AS Descricao, [DATA_PARA_TRANSFERENCIA] AS UltimaAlteracao
                    FROM [dbo].[FORNECEDOR_TIPOS]
                    UNION ALL
                    SELECT 'SubtipoFornecedor' AS TipoDominio, LTRIM(RTRIM([TIPO])) + ':' + LTRIM(RTRIM([SUBTIPO_FORNECEDOR])) AS CodigoErp, LTRIM(RTRIM([SUBTIPO_FORNECEDOR])) AS Descricao, [DATA_PARA_TRANSFERENCIA] AS UltimaAlteracao
                    FROM [dbo].[FORNECEDOR_SUBTIPO]
                    UNION ALL
                    SELECT 'CondicaoPagamento' AS TipoDominio, LTRIM(RTRIM([CONDICAO_PGTO])) AS CodigoErp, LTRIM(RTRIM([DESC_COND_PGTO])) AS Descricao, [DATA_PARA_TRANSFERENCIA] AS UltimaAlteracao
                    FROM [dbo].[COND_ENT_PGTOS]
                    """,
                IncrementalCommandTextFactory: null,
                Watermark: null,
                CommandTimeoutSeconds: 60),

            [ItensFiscaisSnapshot] = new ReadDatasetDefinition(
                Name: ItensFiscaisSnapshot,
                Description: "Snapshot bruto (RAW) de CADASTRO_ITEM_FISCAL do ERP Linx/SOMA, sem interpretação de regra de negócio — regras LWW já homologadas (SincronizarItensFiscaisErpUseCase) são reproduzidas pelo REFINED, nunca aqui.",
                SourceConnectionProfileKey: "linx-development",
                DestinationConnectionProfileKey: "mais-compras-development",
                DestinationTable: "RAW_LinxItensFiscaisSnapshot",
                Columns: ["CodigoErp", "Descricao", "UnidadeErp", "ContaContabilErp", "InativoErp", "UltimaAlteracao"],
                // Discovery real (docs/audits/B3-Bloco5A-PreValidacaoLinxProducao.md): DATA_PARA_TRANSFERENCIA
                // com 100% de cobertura (0 nulos) em CADASTRO_ITEM_FISCAL — LWW já homologado no fluxo direto
                // depende dessa coluna, confirmando viabilidade de INCREMENTAL. Watermark único (não híbrido):
                // Item Fiscal não compartilha tabela fonte com nenhum outro dataset.
                EstrategiaNormal: DatasetLoadKind.Incremental,
                FullCommandTextFactory: () => """
                    SELECT [CODIGO_ITEM] AS CodigoErp, [ITEM_DESCRICAO] AS Descricao, [UNIDADE] AS UnidadeErp, [CONTA_CONTABIL] AS ContaContabilErp, [INATIVO] AS InativoErp, [DATA_PARA_TRANSFERENCIA] AS UltimaAlteracao
                    FROM [dbo].[CADASTRO_ITEM_FISCAL]
                    """,
                IncrementalCommandTextFactory: () => """
                    SELECT [CODIGO_ITEM] AS CodigoErp, [ITEM_DESCRICAO] AS Descricao, [UNIDADE] AS UnidadeErp, [CONTA_CONTABIL] AS ContaContabilErp, [INATIVO] AS InativoErp, [DATA_PARA_TRANSFERENCIA] AS UltimaAlteracao
                    FROM [dbo].[CADASTRO_ITEM_FISCAL]
                    WHERE [DATA_PARA_TRANSFERENCIA] >= @watermark
                    """,
                Watermark: new WatermarkDefinition(
                    QualifiedColumns: ["CADASTRO_ITEM_FISCAL.DATA_PARA_TRANSFERENCIA"],
                    SqlType: "datetime",
                    OverlapWindow: TimeSpan.FromMinutes(5),
                    Description: "CADASTRO_ITEM_FISCAL.DATA_PARA_TRANSFERENCIA — 100% de cobertura confirmada por Discovery real, mesmo padrão de overlap de 5 minutos já homologado para os demais datasets."),
                CommandTimeoutSeconds: 120),

            [ItensFiscaisReferenciasFornecedorSnapshot] = new ReadDatasetDefinition(
                Name: ItensFiscaisReferenciasFornecedorSnapshot,
                Description: "Snapshot bruto (RAW) de ITEM_FISCAL_REF_FORNECEDOR do ERP Linx/SOMA — a resolução de identidade do Fornecedor (NOME_CLIFOR -> CLIFOR -> COD_FORNECEDOR) já acontece nesta query, mesma cadeia já homologada em SomaItemFiscalReferenciaFornecedorReader. FULL apenas (sem watermark/trigger disponíveis nesta tabela — comprovado por Discovery).",
                SourceConnectionProfileKey: "linx-development",
                DestinationConnectionProfileKey: "mais-compras-development",
                DestinationTable: "RAW_LinxItensFiscaisReferenciasFornecedorSnapshot",
                Columns: ["CodigoItem", "CodigoItemFornecedor", "ErpFornecedorId", "FornecedoresResolvidos"],
                EstrategiaNormal: DatasetLoadKind.Full,
                // COD_FORNECEDOR é char(6) no Linx (mesmo achado do padding de Fornecedor) — LTRIM/RTRIM
                // aqui é obrigatório para casar com FornecedorLinxVinculo.CodigoErp, que já é persistido
                // aparado. Sem isso, a resolução SEMPRE falharia silenciosamente (comparação nunca bateria).
                FullCommandTextFactory: () => """
                    SELECT
                        r.[CODIGO_ITEM]                                                       AS CodigoItem,
                        r.[CODIGO_ITEM_FORNECEDOR]                                             AS CodigoItemFornecedor,
                        (SELECT COUNT(DISTINCT c.[CLIFOR])
                         FROM [dbo].[CADASTRO_CLI_FOR] c
                         WHERE LTRIM(RTRIM(c.[NOME_CLIFOR])) = LTRIM(RTRIM(r.[FORNECEDOR])))     AS FornecedoresResolvidos,
                        (SELECT TOP (1) LTRIM(RTRIM(f.[COD_FORNECEDOR]))
                         FROM [dbo].[CADASTRO_CLI_FOR] c
                         JOIN [dbo].[FORNECEDORES] f ON f.[CLIFOR] = c.[CLIFOR]
                         WHERE LTRIM(RTRIM(c.[NOME_CLIFOR])) = LTRIM(RTRIM(r.[FORNECEDOR])))     AS ErpFornecedorId
                    FROM [dbo].[ITEM_FISCAL_REF_FORNECEDOR] r
                    """,
                IncrementalCommandTextFactory: null,
                Watermark: null,
                CommandTimeoutSeconds: 60),
        };
    }

    public bool TryGet(string datasetName, out ReadDatasetDefinition? definition)
    {
        if (string.IsNullOrWhiteSpace(datasetName))
        {
            definition = null;
            return false;
        }

        return _datasets.TryGetValue(datasetName, out definition);
    }

    private static string BuildFullCommandText() => """
        SELECT
            f.[COD_FORNECEDOR]                          AS CodigoFornecedor,
            f.[CLIFOR]                                    AS Clifor,
            COALESCE(c.[CGC_CPF], f.[CGC_CPF])          AS CnpjCpf,
            COALESCE(c.[RAZAO_SOCIAL], f.[FORNECEDOR])  AS RazaoSocial,
            c.[NOME_CLIFOR]                              AS NomeFantasia,
            CASE WHEN c.[PJ_PF] = 1 THEN 'PJ' ELSE 'PF' END AS TipoPessoa,
            f.[INATIVO]                                  AS InativoFornecedores,
            c.[INATIVO]                                  AS InativoCadastroCliFor,
            COALESCE(c.[DATA_PARA_TRANSFERENCIA], f.[DATA_PARA_TRANSFERENCIA]) AS UltimaAlteracao
        FROM [dbo].[FORNECEDORES] f
        LEFT JOIN [dbo].[CADASTRO_CLI_FOR] c ON c.[COD_CLIFOR] = f.[CLIFOR]
        """;

    /// <summary>Watermark híbrido (item de gate do PO: "não invente solução antes da evidência" — as duas
    /// colunas são independentes, confirmado lendo o texto real das duas triggers). <c>@watermark</c> é
    /// sempre um parâmetro (<see cref="System.Data.SqlDbType.DateTime"/>) fornecido pelo chamador — nunca um
    /// valor literal concatenado nesta string.</summary>
    private static string BuildIncrementalCommandText() => """
        SELECT
            f.[COD_FORNECEDOR]                          AS CodigoFornecedor,
            f.[CLIFOR]                                    AS Clifor,
            COALESCE(c.[CGC_CPF], f.[CGC_CPF])          AS CnpjCpf,
            COALESCE(c.[RAZAO_SOCIAL], f.[FORNECEDOR])  AS RazaoSocial,
            c.[NOME_CLIFOR]                              AS NomeFantasia,
            CASE WHEN c.[PJ_PF] = 1 THEN 'PJ' ELSE 'PF' END AS TipoPessoa,
            f.[INATIVO]                                  AS InativoFornecedores,
            c.[INATIVO]                                  AS InativoCadastroCliFor,
            COALESCE(c.[DATA_PARA_TRANSFERENCIA], f.[DATA_PARA_TRANSFERENCIA]) AS UltimaAlteracao
        FROM [dbo].[FORNECEDORES] f
        LEFT JOIN [dbo].[CADASTRO_CLI_FOR] c ON c.[COD_CLIFOR] = f.[CLIFOR]
        WHERE f.[DATA_PARA_TRANSFERENCIA] >= @watermark OR c.[DATA_PARA_TRANSFERENCIA] >= @watermark
        """;
}
