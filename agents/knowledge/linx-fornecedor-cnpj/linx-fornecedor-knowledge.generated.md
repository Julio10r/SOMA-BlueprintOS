# Conhecimento Linx Persistido — Fornecedor / CNPJ

> **ARQUIVO GERADO — NÃO EDITAR À MÃO.** Gerado deterministicamente por `tools/agents/generate-linx-fornecedor-knowledge.js` a partir de `agents/knowledge/linx-fornecedor-cnpj/linx-fornecedor-knowledge.source.json`. Para atualizar o conhecimento, edite o JSON de origem e rode o gerador novamente (`node tools/agents/generate-linx-fornecedor-knowledge.js`) — a regeneração é idempotente: mesma fonte produz o mesmo arquivo.

Domínio: `linx-fornecedor-cnpj`. Descoberto em: 2026-08-12.

Consumido por (`agent.yaml` `implementation.context_paths`): `linx-erp-specialist-agent`, `linx-database-specialist-agent`.

## Proveniência das fontes originais

Cada unidade abaixo referencia sua fonte original — este arquivo NUNCA é a fonte
primária, é uma consolidação recuperável. As fontes primárias completas são:

- `docs/audits/Discovery-Fornecedor-CNPJ-Linx-Compras.md` (`discovery`)
- `docs/audits/Arquitetura-Fornecedor-CNPJ-Decisao.md` (`arquitetura`)

## Rótulos de Proveniência (mesma convenção de `.ai/context/linx-wise-daily-integration.md`)

- `Descoberto`: fato lido diretamente do schema/procedure/banco (Linx Database Specialist).
- `Inferido`: interpretação funcional ainda não confirmada por especialista humano Visual Linx (Linx ERP Specialist).
- `Validado`/`Aprovado`: promoção formal, exclusiva do fluxo `LinxKnowledgeEntry.Promover` com RBAC dedicado — nenhuma unidade deste arquivo foi promovida além de Descoberto/Inferido.

## Unidades de Conhecimento

<!-- linx-knowledge-unit: linx-tabela-mestre-cadastro-cli-for -->
### Tabela mestre do cadastro de Fornecedor/CliFor no Visual Linx

- **Chave**: `linx-tabela-mestre-cadastro-cli-for`
- **Especialista**: LinxDatabaseSpecialist
- **Categoria**: Schema
- **Entidade Linx**: `CADASTRO_CLI_FOR`
- **Tabela**: `CADASTRO_CLI_FOR`
- **Campos**: `NOME_CLIFOR`, `CLIFOR`, `COD_CLIFOR`, `CGC_CPF`, `PJ_PF`, `INATIVO`, `CNAE`
- **Proveniência**: Descoberto
- **Confiança**: ALTA

A tabela real do cadastro de Fornecedor/CliFor no Visual Linx e `dbo.CADASTRO_CLI_FOR` (nao existe tabela chamada literalmente 'CliFor' — esse nome so aparece em tabelas satelite como CLIFOR_INTERCOMPANY, EVENTOS_CLIFOR). Chave primaria: `NOME_CLIFOR` (varchar(25), nao nulo, indice XPKCADASTRO_CLI_FOR). Existem tambem `CLIFOR` (char(6)) e `COD_CLIFOR` (char(6)) usados como identificadores alternativos em FKs de tabelas satelite, mas NENHUMA dessas colunas e IDENTITY (is_identity = 0 em todas) — o valor precisa ser fornecido por quem insere. Documento fiscal: `CGC_CPF` (varchar(19), nao nulo) — coluna unica para CNPJ e CPF, com o tipo distinguido pelo campo `PJ_PF` (bit). Situacao: `INATIVO` (bit) e o unico campo de status na tabela mestre — binario, sem enum rico como o da BrasilAPI. CNAE: coluna unica `CNAE varchar(7)` — apenas o CNAE principal e armazenado, sem estrutura para CNAEs secundarios. Sem coluna de QSA/socios na tabela mestre.

- **Fonte**: docs/audits/Discovery-Fornecedor-CNPJ-Linx-Compras.md#secao-10-a (linhas 236-250)
- **Restrições/observações**: Leitura real do schema via sys.tables/sys.columns/sys.indexes no SOMA_DESENV, sessao READ-ONLY autorizada pelo PO em 2026-08-12. Nunca validado/aprovado formalmente no fluxo LinxKnowledgeEntry.Promover.
- **Tags**: `cadastro_cli_for`, `schema`, `fornecedor`, `cnpj`

<!-- linx-knowledge-unit: linx-enderecos-triplicados-cadastro-cli-for -->
### Endereco nao normalizado — triplicado em tres blocos paralelos

- **Chave**: `linx-enderecos-triplicados-cadastro-cli-for`
- **Especialista**: LinxDatabaseSpecialist
- **Categoria**: Schema
- **Entidade Linx**: `CADASTRO_CLI_FOR`
- **Tabela**: `CADASTRO_CLI_FOR`
- **Campos**: `ENDERECO`, `COBRANCA_ENDERECO`, `ENTREGA_ENDERECO`, `COD_MUNICIPIO_IBGE`
- **Proveniência**: Descoberto
- **Confiança**: ALTA

O endereco em CADASTRO_CLI_FOR nao e uma FK para tabela de endereco separada — e um conjunto de colunas de texto direto na propria tabela, triplicado em tres blocos: endereco principal (ENDERECO, NUMERO, COMPLEMENTO, BAIRRO, CIDADE, UF, CEP, PAIS), endereco de cobranca (prefixo COBRANCA_) e endereco de entrega (prefixo ENTREGA_). Cada bloco tem seu proprio CGC/IE (CGC_CPF, COBRANCA_CGC, ENTREGA_CGC, RG_IE, COBRANCA_IE, ENTREGA_IE) e FK propria para UNIDADES_FEDERACAO/PAISES por bloco. As colunas COD_MUNICIPIO_IBGE (principal/cobranca/entrega) sao preenchidas automaticamente por trigger a partir de cidade+UF via lookup em LCF_LX_MUNICIPIO/LCF_LX_UF (ver unidade linx-trigger-lxi-cadastro-cli-for).

- **Fonte**: docs/audits/Discovery-Fornecedor-CNPJ-Linx-Compras.md#secao-10-a (linhas 245-246)
- **Restrições/observações**: Decisao de arquitetura +Compras (docs/audits/Arquitetura-Fornecedor-CNPJ-Decisao.md secao F) e NAO replicar essa triplicacao no dominio +Compras — apenas o Adapter Linx futuro traduziria o endereco unico do +Compras para os 3 blocos, se necessario.
- **Tags**: `cadastro_cli_for`, `endereco`, `schema`

<!-- linx-knowledge-unit: linx-triggers-cadastro-cli-for -->
### 11 triggers ativas em CADASTRO_CLI_FOR — efeitos colaterais reais de INSERT/UPDATE/DELETE

- **Chave**: `linx-triggers-cadastro-cli-for`
- **Especialista**: LinxDatabaseSpecialist
- **Categoria**: Trigger
- **Entidade Linx**: `CADASTRO_CLI_FOR`
- **Tabela**: `CADASTRO_CLI_FOR`
- **Proveniência**: Descoberto
- **Confiança**: ALTA para as 5 triggers lidas em detalhe; BAIXA (INFERIDO) para as 6 restantes (so nome/evento)

CADASTRO_CLI_FOR tem 11 triggers, todas is_disabled = 0 (ativas). `LXI_CADASTRO_CLI_FOR` (INSERT): preenche COD_MUNICIPIO_IBGE via lookup LCF_LX_MUNICIPIO/LCF_LX_UF; se o parametro `VALIDA_COD_IBGE_CADASTROS='.T.'`, BLOQUEIA o INSERT com RAISERROR+ROLLBACK quando o codigo IBGE nao resolve; ao final, se existir a procedure `LX_ATUALIZA_PAF_ECF_ERP`, executa-a com ('CADASTRO_CLI_FOR','I','') — hook de integracao fiscal PAF-ECF (INFERIDO). `LXU_CADASTRO_CLI_FOR` (UPDATE): mesmo lookup de IBGE quando cidade/UF mudam; sincroniza CGC_CPF para tabelas satelite (FORNECEDORES, REPRESENTANTES); inativa em cascata STG_FILIAIS_OMS quando INATIVO muda e o registro e uma filial (INFERIDO: 'inativar CliFor = inativar filial no OMS'). `GSI_/GSU_/GSD_CADASTRO_CLI_FOR_LOG` (INSERT/UPDATE/DELETE): auditoria incondicional, grava snapshot antes/depois de ~35 colunas em GS_CADASTRO_CLI_FOR_LOG com USUARIO_ALTERACAO=SYSTEM_USER, OPERACAO, APLICACAO=APP_NAME(). As 6 triggers restantes (LXI_ETL_*, LXU_ETL_*, LXI_ANM_*, LXU_ANM_*, GSU_SAP_CADASTRO_CLI_FOR, GSUI_WETL_CADASTRO_CLI_FOR) tiveram apenas nome/evento lido, nao o conteudo SQL completo — DESCONHECIDO/INFERIDO fraco (sugerem ETL, modulo ANM, integracao SAP e webhook, respectivamente, mas sem confirmacao).

- **Fonte**: docs/audits/Discovery-Fornecedor-CNPJ-Linx-Compras.md#secao-10-a (linhas 252-284, tabela de triggers e mapa de efeitos colaterais)
- **Restrições/observações**: Definicao SQL lida via OBJECT_DEFINITION, nunca executada. Regra registrada para os Agents Linx: toda investigacao futura de entidade Linx com finalidade de escrita deve cobrir triggers, procedures/functions chamadas, views e sequenciais — nao so schema de colunas.
- **Tags**: `cadastro_cli_for`, `trigger`, `efeito_colateral`, `ibge`, `paf_ecf`

<!-- linx-knowledge-unit: linx-fks-cadastro-cli-for -->
### CADASTRO_CLI_FOR e referenciada por mais de 90 tabelas satelite

- **Chave**: `linx-fks-cadastro-cli-for`
- **Especialista**: LinxDatabaseSpecialist
- **Categoria**: Schema
- **Entidade Linx**: `CADASTRO_CLI_FOR`
- **Tabela**: `CADASTRO_CLI_FOR`
- **Campos**: `NOME_CLIFOR`, `CLIFOR`, `COD_CLIFOR`
- **Proveniência**: Descoberto
- **Confiança**: ALTA

CADASTRO_CLI_FOR e referenciada por mais de 90 tabelas satelite via NOME_CLIFOR/CLIFOR/COD_CLIFOR, incluindo FORNECEDORES, REPRESENTANTES, CLIENTES_ATACADO, CONTRATO, FATURAMENTO, ENTRADAS, VENDAS, CTB_A_PAGAR_FATURA, CTB_A_RECEBER_FATURA, entre outras. A propria tabela tem FKs de saida para UNIDADES_FEDERACAO, PAISES, BANCOS, CARTEIRAS_COBRANCA, CONTATO, CTB_CONTA_PLANO, CTB_EXCECAO_GRUPO, CTB_LX_INDICADOR_FISCAL_TERCEIRO. Essa amplitude de FKs de entrada e evidencia forte de que CADASTRO_CLI_FOR e uma tabela central e amplamente acoplada — qualquer escrita direta precisa considerar esse acoplamento.

- **Fonte**: docs/audits/Discovery-Fornecedor-CNPJ-Linx-Compras.md#secao-10-a (linha 268)
- **Restrições/observações**: Contagem aproximada ('mais de 90'), lista de exemplos nao exaustiva.
- **Tags**: `cadastro_cli_for`, `foreign_key`, `acoplamento`

<!-- linx-knowledge-unit: linx-procedure-lx-sequencial -->
### LX_SEQUENCIAL — mecanismo real de geracao de codigo sequencial no Linx

- **Chave**: `linx-procedure-lx-sequencial`
- **Especialista**: LinxDatabaseSpecialist
- **Categoria**: Procedure
- **Entidade Linx**: `SEQUENCIAIS`
- **Tabela**: `SEQUENCIAIS`
- **Procedure**: `LX_SEQUENCIAL`
- **Campos**: `TABELA_COLUNA`, `SEQUENCIA`, `TAMANHO`, `EMPRESA_SEQUENCIAIS`
- **Proveniência**: Descoberto
- **Confiança**: ALTA

`LX_SEQUENCIAL` (nome real no singular — nao 'lx_sequenciais' no plural, que era hipotese anterior do PO) e a procedure real de geracao/consulta de codigo sequencial no Linx. Assinatura: `LX_SEQUENCIAL @TABELA_COLUNA VARCHAR(37), @EMPRESA INT = NULL, @SEQUENCIA VARCHAR(20) OUTPUT, @UPDATE_SEQUENCIAL BIT = 1, @NEWVALUE VARCHAR(20) = NULL`. Quando @UPDATE_SEQUENCIAL=1 (padrao), faz UPDATE incremental (+1 fixo, sem coluna de incremento configuravel) na tabela `SEQUENCIAIS` (SET SEQUENCIA = SEQUENCIA + 1 WHERE TABELA_COLUNA=@TABELA_COLUNA) e retorna o novo valor formatado com zeros a esquerda conforme TAMANHO da tabela. Tem variante por empresa via EMPRESA_SEQUENCIAIS quando o parametro CTRL_MULTI_EMPRESA esta ativo. Quando @UPDATE_SEQUENCIAL=0, apenas le o proximo valor sem consumir. NAO ha hint de lock/transacao explicita (WITH (UPDLOCK, HOLDLOCK)) na definicao lida — atomicidade depende so do UPDATE padrao.

- **Fonte**: docs/audits/Discovery-Fornecedor-CNPJ-Linx-Compras.md#secao-10-b (linhas 404-409, 452)
- **Restrições/observações**: Definicao lida via OBJECT_DEFINITION, NUNCA executada em nenhuma sessao (regra READ-ONLY absoluta respeitada). Nao ha evidencia de controle de concorrencia explicito.
- **Tags**: `lx_sequencial`, `sequenciais`, `geracao_codigo`, `clifor`

<!-- linx-knowledge-unit: linx-sequenciais-concorrentes-fornecedor -->
### Dois sequenciais concorrentes aparentemente com o mesmo proposito

- **Chave**: `linx-sequenciais-concorrentes-fornecedor`
- **Especialista**: LinxDatabaseSpecialist
- **Categoria**: Regra
- **Entidade Linx**: `SEQUENCIAIS`
- **Tabela**: `SEQUENCIAIS`
- **Campos**: `FORNECEDORES.CLIFOR`, `SEQUENCIA_FORNECEDOR`
- **Proveniência**: Descoberto
- **Confiança**: MEDIA — confirmado como anomalia desta procedure especifica, NAO validado como regra geral do Linx

Existem dois sequenciais concorrentes e nao usados de forma consistente para o mesmo proposito: `FORNECEDORES.CLIFOR` e `SEQUENCIA_FORNECEDOR`. A procedure de integracao `p_RSV_INTEGRACAO_CADASTRO_FORNECEDOR` usa `CLIENTES_ATACADO.CLIFOR` (nao `FORNECEDORES.CLIFOR`, que aparece comentado/desativado no codigo) mesmo quando o registro sera marcado INDICA_FORNECEDOR=1 — ou seja, nesta integracao especifica, o codigo do CliFor vem do sequencial de CLIENTE, nao do papel real do registro. Das 5 implementacoes de escrita lidas em profundidade, 4 de 5 usam corretamente LX_SEQUENCIAL(FORNECEDORES.CLIFOR); apenas p_RSV_INTEGRACAO_CADASTRO_FORNECEDOR usa a chave divergente.

- **Fonte**: docs/audits/Discovery-Fornecedor-CNPJ-Linx-Compras.md#secao-10-b (linha 429), #secao-10-c (linhas 530, 585)
- **Restrições/observações**: GAP-LINX-SEQUENCIAL-INCONSISTENTE (aberto): se uma futura integracao de escrita do +Compras replicar esse padrao sem entender por que, pode gerar codigos incoerentes com o papel do registro. Nao decidir geracao de codigo sem confirmar com especialista Linx qual sequencial usar por papel.
- **Tags**: `lx_sequencial`, `clifor`, `anomalia`, `p_rsv_integracao_cadastro_fornecedor`

<!-- linx-knowledge-unit: linx-nome-clifor-nao-vem-de-sequencial -->
### NOME_CLIFOR e construido por sanitizacao de string, nunca por sequencial

- **Chave**: `linx-nome-clifor-nao-vem-de-sequencial`
- **Especialista**: LinxDatabaseSpecialist
- **Categoria**: Regra
- **Entidade Linx**: `CADASTRO_CLI_FOR`
- **Tabela**: `CADASTRO_CLI_FOR`
- **Campos**: `NOME_CLIFOR`
- **Proveniência**: Descoberto
- **Confiança**: ALTA para 'vem de sanitizacao de string, nunca de sequencial'; MEDIA para o algoritmo exato (varia por implementacao)

Em 4 das 5 implementacoes de escrita lidas em profundidade, CLIFOR/COD_CLIFOR/COD_FORNECEDOR vem de uma unica chamada a LX_SEQUENCIAL(FORNECEDORES.CLIFOR), com o mesmo valor reaproveitado nas tres colunas. NOME_CLIFOR NUNCA vem de sequencial — e sempre construido por sanitizacao de string de um campo de nome de origem (razao social, nome fantasia, ou campo especifico da integracao), removendo espaco inicial e caracteres especiais (reforcado por uma trigger real que bloqueia inserts com nome mal formatado). O campo de origem exato e o algoritmo de sanitizacao variam entre implementacoes (nivel 2 de recorrencia, nao nivel 1). Na procedure `p_RSV_INTEGRACAO_CADASTRO_FORNECEDOR` (a anomalia), NOME_CLIFOR e construido como 'AZCB-' + substring(RAZAO_SOCIAL,1,18) sem virgulas/pontos + ' ' + ultimos 6 digitos do CGC/CPF, truncado para varchar(25) — prefixo fixo de marca especifico dessa integracao de parceiro, nao convencao geral do Linx.

- **Fonte**: docs/audits/Discovery-Fornecedor-CNPJ-Linx-Compras.md#secao-10-b (linha 399), #secao-10-c
- **Restrições/observações**: Confirmado que o algoritmo exato de sanitizacao nao e uniforme entre as 5 implementacoes lidas.
- **Tags**: `nome_clifor`, `sanitizacao`, `clifor`

<!-- linx-knowledge-unit: linx-procedure-p-rsv-integracao-cadastro-fornecedor -->
### p_RSV_INTEGRACAO_CADASTRO_FORNECEDOR — integracao de marketplace/parceiro com vocabulario SAP

- **Chave**: `linx-procedure-p-rsv-integracao-cadastro-fornecedor`
- **Especialista**: LinxDatabaseSpecialist
- **Categoria**: Procedure
- **Entidade Linx**: `CADASTRO_CLI_FOR`
- **Tabela**: `CADASTRO_CLI_FOR`
- **Procedure**: `p_RSV_INTEGRACAO_CADASTRO_FORNECEDOR`
- **Proveniência**: Descoberto
- **Confiança**: ALTA para o conteudo lido; a extrapolacao de que seja 'o mecanismo oficial generico' e explicitamente rejeitada (Inferido fraco/Desconhecido)

Procedure de integracao real, definicao completa lida via OBJECT_DEFINITION. Obtem novo codigo via EXEC LX_SEQUENCIAL @TABELA_COLUNA='CLIENTES_ATACADO.CLIFOR', @EMPRESA=1, @SEQUENCIA=@p4 OUTPUT (a linha equivalente para 'FORNECEDORES.CLIFOR' existe no codigo mas esta comentada/desativada). Sequencia real do INSERT confirmada: (1) EXEC LX_SEQUENCIAL (obtem codigo); (2) INSERT INTO CADASTRO_CLI_FOR (cria a entidade-mae, com INDICA_FORNECEDOR/INDICA_CLIENTE explicitamente setados); (3) INSERT INTO FORNECEDORES (cria a especializacao, mesmo CLIFOR, TIPO derivado de codigo de grupo de conta @grupo_conta vindo de fora — vocabulario grupo_conta/ORG_COMPRA/codigo_sap evidencia integracao tipo SAP/marketplace); (4) chamadas subsequentes a LX_AZZ_API_RETORNO_SAP_FORNECEDORES e GS_CADASTRO_CLI_FOR_CONSULTA — confirma integracao especifica com SAP/API externa. E uma integracao customizada para um parceiro/marketplace especifico ('RSV', prefixo AZCB-), NAO necessariamente a rotina que o operador humano usa na tela do Visual Linx.

- **Fonte**: docs/audits/Discovery-Fornecedor-CNPJ-Linx-Compras.md#secao-10-b (linhas 394-402)
- **Restrições/observações**: Nao encontrada nesta rodada uma procedure claramente 'generica'/sem prefixo de parceiro que faca o mesmo INSERT a partir de tela de cadastro manual — permanece DESCONHECIDO.
- **Tags**: `p_rsv_integracao_cadastro_fornecedor`, `procedure`, `sap`, `marketplace`, `lx_sequencial`

<!-- linx-knowledge-unit: linx-padrao-nivel-1-cinco-implementacoes-cadastro -->
### Padrao recorrente entre 5 implementacoes reais de cadastro de Fornecedor no Linx

- **Chave**: `linx-padrao-nivel-1-cinco-implementacoes-cadastro`
- **Especialista**: LinxErpSpecialist
- **Categoria**: Regra
- **Entidade Linx**: `CADASTRO_CLI_FOR`
- **Tabela**: `CADASTRO_CLI_FOR`
- **Proveniência**: Descoberto
- **Confiança**: ALTA para o padrao de 5/5 usar LX_SEQUENCIAL; nenhuma promocao a Validado/Aprovado ocorreu nesta rodada

5 implementacoes de escrita foram lidas em profundidade via sys.sql_modules: LX_AZZ_GERAR_FORNECEDOR_LINX, LX_GS_GERAR_ALTERAR_FORNECEDOR_OBC_LINX, PROC_GS_INTEGRA_FORNECEDOR_REDMINE (usa LX_SEQUENCIAL('LOG_INTEG_FORNECEDOR_REDMINE.LOTE') alem de FORNECEDORES.CLIFOR — unico caso de dois sequenciais na mesma procedure), PROC_HRG_CADASTRA_ZTBMM_FORNE_SOMA e p_RSV_INTEGRACAO_CADASTRO_FORNECEDOR. Padrao Nivel 1 (5/5, ALTA confianca): todas usam LX_SEQUENCIAL para gerar o codigo do CliFor; CLIFOR/COD_CLIFOR nunca vem de IDENTITY/geracao propria em SQL puro. Padrao Nivel 2/3 (variavel): campo de origem e algoritmo de sanitizacao do NOME_CLIFOR variam por implementacao. Anomalia confirmada (so 1/5): uso do sequencial CLIENTES_ATACADO.CLIFOR (p_RSV) em vez de FORNECEDORES.CLIFOR. Outras ~15 procedures candidatas foram apenas listadas por nome (LX_AZZ_GERAR_CLIENTE_ATAC_LINX, MIT_INTEGRA_ORO, MIT_INTEGRA_TRUNK, mit_integra_vintage, PROC_GS_INTEGRA_CLIENTES_ATACADO_REDMINE, LX_LGPD_PROC_CLIENTE, GS_CRIA_FILIAIS, etc.) — nao lidas em detalhe, pendencia explicita.

- **Fonte**: docs/audits/Discovery-Fornecedor-CNPJ-Linx-Compras.md#secao-10-c (linhas 495-585)
- **Restrições/observações**: Metodologia explicita do PO: nenhuma procedure isolada deve ser tratada como 'o mecanismo oficial do Visual Linx' — tratar cada uma como amostra independente e buscar padrao recorrente.
- **Tags**: `padrao`, `lx_sequencial`, `clifor`, `metodologia`

<!-- linx-knowledge-unit: linx-ferramentas-discovery-schema-readonly -->
### LX_CADE, LX_CADE_COLUNA e ANM_BUSCA_INSTRUCAO — wrappers read-only de descoberta de schema

- **Chave**: `linx-ferramentas-discovery-schema-readonly`
- **Especialista**: LinxDatabaseSpecialist
- **Categoria**: Procedure
- **Procedure**: `LX_CADE`
- **Proveniência**: Descoberto
- **Confiança**: ALTA

Tres procedures auxiliares no Linx, comprovadamente READ-ONLY (wrappers de SELECT sobre catalogos do SQL Server): `LX_CADE @TEXTO` — SELECT sobre sys.objects+sys.schemas, filtro name LIKE '%texto%' para tipos U/V/FN/TF/P (tabelas, views, functions, procedures); wrapper de busca de objetos por nome. `LX_CADE_COLUNA @texto` — SELECT sobre sys.columns+sys.types, retorna tabela/nome da coluna/tipo formatado, filtro a.name LIKE @texto; wrapper de busca de tabelas por nome de coluna. `ANM_BUSCA_INSTRUCAO(@INSTRUCAO)` — busca texto literal dentro da definicao SQL de procedures/functions/triggers/views (via syscomments/sysobjects, equivalente funcional a sys.sql_modules). Quando disponivel, sys.sql_modules/sys.objects/sys.columns do proprio SQL Server produz o mesmo resultado com mais controle de filtro — essas tres ferramentas sao conveniencias do Linx, nao a unica via de discovery.

- **Fonte**: docs/audits/Discovery-Fornecedor-CNPJ-Linx-Compras.md#secao-10-c (linhas 335-336)
- **Restrições/observações**: Nenhuma dessas procedures foi executada com efeito de escrita — sao SELECTs sobre catalogos.
- **Tags**: `lx_cade`, `lx_cade_coluna`, `anm_busca_instrucao`, `discovery`, `metodologia`

<!-- linx-knowledge-unit: linx-metodologia-investigacao-escrita-futura -->
### Regra reutilizavel: descoberta de schema fisico nao basta para integracao de escrita

- **Chave**: `linx-metodologia-investigacao-escrita-futura`
- **Especialista**: LinxDatabaseSpecialist
- **Categoria**: Regra
- **Proveniência**: Descoberto
- **Confiança**: ALTA — regra de processo, nao de dado especifico

Regra de metodologia registrada para os Agents Linx (LinxErpSpecialistAgent/LinxDatabaseSpecialistAgent), aplicavel a toda investigacao futura de entidades Linx com finalidade de integracao de escrita: a descoberta de schema fisico (tabela/coluna/tipo/FK) NAO e suficiente. A investigacao deve cobrir tambem: triggers (eventos tratados, colunas lidas/alteradas, validacoes, bloqueios, geracao automatica de codigos/timestamps, auditoria, tabelas secundarias afetadas, cascata de outras triggers), stored procedures e functions chamadas, views relevantes, e sequencias/geradores de chave. Todo achado deve ser registrado com proveniencia apropriada (LinxConhecimentoProveniencia: Descoberto/Inferido/Validado/Aprovado). Vale para qualquer dominio Linx futuro (Itens, Pedidos, Notas Fiscais etc.), nao so Fornecedor/CliFor. Confirmada na pratica: CADASTRO_CLI_FOR tem 11 triggers ativas e mais de 90 FKs de entrada — uma integracao desenhada so a partir do schema de colunas teria ignorado o preenchimento automatico de IBGE, o bloqueio condicional por parametro, a integracao fiscal automatica e a inativacao em cascata de filial.

- **Fonte**: docs/audits/Discovery-Fornecedor-CNPJ-Linx-Compras.md#secao-10-a (linhas 292-296)
- **Restrições/observações**: Separacao de responsabilidades reforcada: LinxDatabaseSpecialist descobre o fato fisico (proveniencia inicial Descoberto); LinxErpSpecialist interpreta o significado funcional (proveniencia inicial Inferido). Uma interpretacao Inferida nunca deve ser promovida a Validado/Aprovado sem confirmacao humana.
- **Tags**: `metodologia`, `trigger`, `procedure`, `proveniencia`, `regra_geral`

<!-- linx-knowledge-unit: arquitetura-fronteira-adapter-linx -->
### Fronteira do futuro Adapter Linx — o que nunca entra no dominio +Compras

- **Chave**: `arquitetura-fronteira-adapter-linx`
- **Especialista**: LinxErpSpecialist
- **Categoria**: Regra
- **Entidade Linx**: `CADASTRO_CLI_FOR`
- **Tabela**: `CADASTRO_CLI_FOR`
- **Proveniência**: Inferido
- **Confiança**: ALTA como decisao de arquitetura; os itens 'AINDA DESCONHECIDO' permanecem nao confirmados

Decisao de arquitetura (nao implementada): nenhum detalhe fisico do Linx (nomes de tabela/coluna, LX_SEQUENCIAL, CLIFOR, filas de ETL/WETL, vocabulario SAP/OBC/Redmine como grupo_conta/ORG_COMPRA/codigo_sap/MANDT/LIFNR/STCD1) deve aparecer em Fornecedor.cs, no contrato canonico ConsultaCnpjResultado, ou em qualquer DTO/componente do dominio +Compras — pertencem exclusivamente a um futuro Adapter Linx. Itens OBRIGATORIOS do Adapter Linx quando implementado: obter codigo via LX_SEQUENCIAL/FORNECEDORES.CLIFOR; gerar NOME_CLIFOR por sanitizacao (sem espaco inicial, sem caracteres especiais); popular flags de papel Linx (INDICA_FORNECEDOR etc, ja espelhadas 1:1 pelas flags ForneceMateriais/etc do dominio +Compras); verificar existencia previa no lado Linx antes de decidir INSERT vs UPDATE fisico. Itens AINDA DESCONHECIDO que bloqueiam a implementacao real do Adapter (nao bloqueiam a modelagem): se o cadastro manual via tela Visual Linx segue o mesmo padrao das 5 integracoes automatizadas ou existe rotina distinta; quem consome as filas ETL/WETL e se o Adapter deve suprimir essa replicacao. Gate de validacao obrigatorio: nenhuma escrita real do Adapter Linx deve ir para producao sem confirmacao de um especialista Visual Linx sobre os itens AINDA DESCONHECIDO.

- **Fonte**: docs/audits/Arquitetura-Fornecedor-CNPJ-Decisao.md#secao-o (linhas 253-269), #secao-b (linhas 55-84)
- **Restrições/observações**: Nenhum Adapter Linx foi implementado nesta rodada — apenas decisao/modelagem.
- **Tags**: `adapter_linx`, `arquitetura`, `fronteira`, `gate_validacao`

<!-- linx-knowledge-unit: arquitetura-documento-fiscal-canonico -->
### DocumentoFiscal como unico Value Object canonico de CNPJ/CPF no +Compras

- **Chave**: `arquitetura-documento-fiscal-canonico`
- **Especialista**: LinxErpSpecialist
- **Categoria**: Regra
- **Entidade Linx**: `CGC_CPF`
- **Tabela**: `CADASTRO_CLI_FOR`
- **Campos**: `CGC_CPF`
- **Proveniência**: Inferido
- **Confiança**: ALTA — resolvida por evidencia de uso real no codigo, nao pede decisao do PO

Decisao formalizada: manter um unico Value Object canonico, DocumentoFiscal, eliminando Cnpj.cs como classe publica ativa (fora do escopo desta rodada remover fisicamente). Regras: (1) normalizacao unica — remover tudo que nao e digito ao construir (mesma regra que Cnpj.Create ja usa), corrigindo o comportamento permissivo atual de DocumentoFiscal.Create (so Trim()), causa raiz do BUG-4 do discovery original; (2) a compatibilidade com o campo Linx CGC_CPF (que pode conter valores nao numericos legados) pertence a fronteira do Adapter Linx, nunca a normalizacao do Value Object canonico — se necessario, um campo bruto adicional (ex.: CodigoLegadoErp) seria responsabilidade do Adapter; (3) validar digito verificador de CNPJ (modulo 11) e CPF na fronteira de entrada — elimina a lacuna que causa o BUG-3 (string com 14 digitos mas DV invalido passa a validacao e so falha na fonte externa, classificada erroneamente como 'fonte indisponivel'); (4) composicao, nao especializacao por tipo — um unico DocumentoFiscal com propriedade TipoPessoa (PJ/PF), nao subtipos Cnpj/Cpf separados; (5) mascara e so apresentacao, nunca parte do valor armazenado ou da comparacao de igualdade.

- **Fonte**: docs/audits/Arquitetura-Fornecedor-CNPJ-Decisao.md#secao-c (linhas 88-97), #secao-a1 (linhas 15-20)
- **Restrições/observações**: Correspondencia com o campo fisico Linx CGC_CPF (varchar(19), coluna unica para CNPJ/CPF distinguida por PJ_PF) e responsabilidade do futuro Adapter Linx, nao do dominio +Compras.
- **Tags**: `documento_fiscal`, `cnpj`, `cgc_cpf`, `arquitetura`, `bug_4`

<!-- linx-knowledge-unit: arquitetura-taxonomia-erros-cnpj -->
### Taxonomia de erros tipada para consulta de CNPJ — corrige mascaramento de erro

- **Chave**: `arquitetura-taxonomia-erros-cnpj`
- **Especialista**: LinxErpSpecialist
- **Categoria**: Regra
- **Proveniência**: Inferido
- **Confiança**: ALTA — decisao tecnica direta a partir do comportamento HTTP ja observado no codigo

Taxonomia formalizada de 8 erros para o fluxo de consulta CNPJ: CnpjInvalido (validacao local, DV invalido, detectado ANTES de qualquer chamada externa — 400, sem retry), NaoEncontrado (provider retornou 404 — sem retry), FonteIndisponivel (5xx do provider — retry), Timeout (retry), LimiteDeConsultas (429 — retry com backoff), ErroDeAutenticacaoDoProvider (nao se aplica hoje a BrasilAPI, mas prevista para troca futura de provider — sem retry), RespostaInvalida (2xx mas payload nao interpretavel — retry possivel), ErroInterno (qualquer erro nao classificado — retry). Decisao estrutural: essa taxonomia deve ser um enum/hierarquia de exceptions tipadas no Application layer, nunca strings livres decidindo comportamento — corrige diretamente o BUG-3 do discovery original, que hoje classifica todo erro HTTP >=400 nao-404 e toda excecao nao tratada como 'fonte indisponivel'/'falha ao consultar a fonte externa' generica.

- **Fonte**: docs/audits/Arquitetura-Fornecedor-CNPJ-Decisao.md#secao-k (linhas 190-203)
- **Restrições/observações**: Nao implementada nesta rodada — e decisao de arquitetura para Work Order futura (WO-2).
- **Tags**: `taxonomia_erro`, `cnpj`, `bug_3`, `arquitetura`

<!-- linx-knowledge-unit: arquitetura-proveniencia-hibrida-consulta-cnpj -->
### Modelo de proveniencia hibrido: camada normalizada + snapshot bruto

- **Chave**: `arquitetura-proveniencia-hibrida-consulta-cnpj`
- **Especialista**: LinxErpSpecialist
- **Categoria**: Regra
- **Tabela**: `FornecedoresCnpjConsultas`
- **Campos**: `PayloadBrutoJson`
- **Proveniência**: Inferido
- **Confiança**: MEDIA — decisao tecnica formalizada, mas politica de retencao ainda pendente de decisao do PO/juridico

FornecedorCnpjConsultaHistorico ja grava fonte, timestamp, documento, status e correlation id, mas NAO grava o payload bruto (snapshot JSON) do provider — apenas um Resultado (string curta, ate 1000 caracteres, .ToString() do resultado tipado). Decisao — modelo hibrido definitivo: (1) camada normalizada, ja existente, mantida — campos individuais gravados em Fornecedor apos decisao humana de aceitar o enriquecimento, complementada por UltimaConsultaCnpjEm/UltimaConsultaCnpjFonte se o PO aprovar; (2) camada de snapshot/evidencia nova — coluna adicional PayloadBrutoJson (nullable, tamanho grande) na tabela ja existente FornecedoresCnpjConsultas, com o JSON completo do provider antes da traducao para o contrato canonico; esse snapshot e evidencia/auditoria, nunca fonte de leitura para o dominio; (3) politica minima contra armazenamento desnecessario — so gravar snapshot em consulta bem-sucedida que resulta em criacao/atualizacao real de Fornecedor, com periodo de retencao a definir (pendencia de compliance).

- **Fonte**: docs/audits/Arquitetura-Fornecedor-CNPJ-Decisao.md#secao-l (linhas 207-214), #secao-a3 (linhas 30-33)
- **Restrições/observações**: Nao implementada nesta rodada. Pendencia explicita: politica de retencao do snapshot bruto (item 2 da secao Q).
- **Tags**: `proveniencia`, `snapshot`, `cnpj`, `lgpd`, `auditoria`

<!-- linx-knowledge-unit: arquitetura-nao-persistir-qsa-cnae-secundario -->
### QSA nunca persistido; apenas CNAE principal — minimizacao de dados

- **Chave**: `arquitetura-nao-persistir-qsa-cnae-secundario`
- **Especialista**: LinxErpSpecialist
- **Categoria**: Regra
- **Entidade Linx**: `CADASTRO_CLI_FOR`
- **Tabela**: `CADASTRO_CLI_FOR`
- **Campos**: `CNAE`
- **Proveniência**: Inferido
- **Confiança**: ALTA — resolvida por evidencia do Linx fisico + principio de minimizacao de dados; confirmacao formal de politica ainda pendente do PO/juridico

Decisao confirmada: nao persistir QSA (quadro societario) em nenhuma hipotese no dominio +Compras — justificativa: minimizacao de dados (LGPD art. 6 III), ausencia de requisito de compliance documentado, e confirmacao direta no Linx fisico de que CADASTRO_CLI_FOR/FORNECEDORES nao tem nenhuma coluna de QSA. Mecanismo de consulta pontual proposto para o futuro (nao implementar agora): se compliance exigir verificar socios (PEP, sancoes), deveria ser consulta sob demanda, nao armazenada, disparada explicitamente e descartada apos exibicao — nunca parte do cadastro permanente. Para CNAE: persistir apenas codigo+descricao do CNAE principal; CNAEs secundarios NAO persistem (podem aparecer so como dado transitorio na tela de revisao) — reforcado por evidencia de que nenhuma das 5 implementacoes de cadastro Linx lidas preenche CNAE, e o Linx fisico so tem uma coluna CNAE varchar(7) (sem estrutura para secundarios).

- **Fonte**: docs/audits/Arquitetura-Fornecedor-CNPJ-Decisao.md#secao-h (linhas 162-164), #secao-i (linhas 168-170)
- **Restrições/observações**: Pendencia explicita: confirmacao formal de nao persistir QSA e recomendacao tecnica forte (LGPD), mas decisao final de politica de dados pessoais deve ser ratificada pelo PO/juridico (secao Q, item 1).
- **Tags**: `qsa`, `cnae`, `lgpd`, `minimizacao_dados`, `arquitetura`
