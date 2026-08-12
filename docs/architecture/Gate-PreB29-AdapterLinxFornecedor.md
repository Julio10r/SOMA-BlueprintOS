# Gate Técnico Pré-B2.9 — Adapter Linx (Fornecedor/CNPJ)

## Metadados

- Fase: Onda 2 — Cadastros (Sourcing Intelligence)
- Sprint: Frente Fornecedor/CNPJ (ADR-0023)
- Tipo: Gate técnico de discovery/arquitetura — **documental, sem implementação, sem escrita em nenhum banco**
- Status: Concluído
- Responsável: Claude (gate técnico) a pedido de Julio Cesar
- Data: 12/08/2026
- Work Order avaliada: `.ai/work-orders/backlog/fase-b/B2.9-AdapterLinxFornecedorCnpj.md` (permanece Draft/Bloqueada — este gate não altera esse status)
- Regra central aplicada: padrão observado no Linx ≠ regra oficial automaticamente. Todo achado abaixo é classificado como **Fato observado**, **Padrão recorrente**, **Interpretação**, **Decisão arquitetural** ou **Desconhecido**.

---

## 1. Contexto

A frente Fornecedor/CNPJ concluiu B2.3 a B2.8 (`DocumentoFiscal` canônico com dígito verificador, contrato canônico de consulta CNPJ + taxonomia de erros, situação cadastral canônica, state machine de revisão no frontend, proveniência híbrida com retenção de 180 dias, CNAE principal). A B2.9 (Adapter Linx — escrita real no Visual Linx/ERP legado) está registrada em `backlog/fase-b`, status **Draft — Bloqueada**, com dependência explícita de "sessão de validação com especialista Visual Linx obrigatória antes de qualquer implementação de escrita". Este gate consolida o conhecimento de discovery acumulado (várias rodadas reais de acesso READ-ONLY ao SOMA_DESENV via VPN autorizada pelo PO) em um veredito único sobre se esse conhecimento já é suficiente para desenhar/implementar o Adapter, ou se restrições/bloqueios adicionais são necessários.

---

## 2. Fontes lidas (caminhos reais confirmados)

Os caminhos citados no briefing usavam convenções antigas/aproximadas (`docs/architecture/`, `backlog/fase-b/`). Os caminhos reais, confirmados por busca, são:

| Documento citado no briefing | Caminho real confirmado |
|---|---|
| ADR-0023 | `.ai/DECISIONS.md`, linhas 646–680 (log único de ADRs do projeto; não existe `docs/adr/` nem arquivo individual — ADR-0009/ADR-0019 formalizaram essa convenção) |
| Work Order B2.9 | `.ai/work-orders/backlog/fase-b/B2.9-AdapterLinxFornecedorCnpj.md` |
| Discovery Fornecedor/CNPJ | `docs/audits/Discovery-Fornecedor-CNPJ-Linx-Compras.md` (775 linhas, lido integralmente) |
| Decisão de Arquitetura | `docs/audits/Arquitetura-Fornecedor-CNPJ-Decisao.md` (331 linhas, lido integralmente) |
| Snapshot de conhecimento Linx | `docs/agents/ai-factory/temp/LinxKnowledge-Fornecedor-Discovery-Snapshot.md` (392 linhas, lido integralmente) |
| Relatórios B2.3–B2.8 | `.ai/work-orders/completed/B2.{3,4,5,6,7,8}-*.md` (status confirmado via grep: todos "Completed"/"Concluída") |
| Código do domínio Fornecedor | `backend/src/BlueprintOS.Domain/Procurement/Suppliers/{Fornecedor.cs,DocumentoFiscal.cs}`, `backend/src/BlueprintOS.Application/Procurement/Suppliers/**` |
| Infraestrutura Linx existente | `backend/src/BlueprintOS.Domain/Knowledge/Linx/LinxKnowledgeEntry.cs`, `backend/src/BlueprintOS.Infrastructure/Knowledge/Linx/LinxKnowledgeRepository.cs`, `backend/src/BlueprintOS.Api/Knowledge/LinxKnowledgeController.cs`, `backend/src/BlueprintOS.Application/Knowledge/Linx/*` |

**Observação importante sobre `docs/audits/`**: esse diretório está listado no `.gitignore` (linha 168) — os documentos de Discovery e de Decisão de Arquitetura existem em disco e foram lidos integralmente, mas **não estão versionados no git** (confirmado via `git ls-files docs/audits/`, que não os lista). Isso é comportamento intencional pré-existente, documentado no próprio Discovery (seção 21). Por esse motivo, este Gate foi colocado em `docs/architecture/` (diretório versionado, mesma convenção de `Decisions.md`/`Architecture.md`), não em `docs/audits/`.

Nenhum arquivo citado no briefing estava ausente — todos foram localizados, apenas em caminhos reais diferentes dos citados (a raiz de backlog/documentação é `.ai/`, não `docs/`/`backlog/` na raiz do repo).

---

## 3. Acesso ao SOMA_DESENV nesta sessão

**Sem acesso.** `ToolSearch` foi consultado nesta sessão com termos "sql", "linx", "desenv", "database", "mssql" e nenhuma ferramenta MCP de banco de dados foi encontrada — apenas ferramentas de agendamento (`CronCreate`) foram retornadas, sem relação com SQL. **Nenhuma tentativa de conexão foi feita** (não haveria como, sem ferramenta disponível), e nenhuma credencial foi solicitada ou usada.

Este gate baseia-se **exclusivamente** no que já está documentado nos artefatos de discovery (que, em rodadas anteriores e distintas desta sessão, tiveram acesso real READ-ONLY ao SOMA_DESENV via VPN autorizada pelo PO — ver seções 10-A/10-B/10-C do Discovery). Nenhum dado novo de schema foi inventado ou suposto nesta sessão.

---

## 4. Achados por tópico

Cada tópico separa explicitamente Fato observado (evidência direta de leitura de schema/trigger/procedure) de Padrão recorrente (contagem entre múltiplas amostras) de Interpretação (significado funcional não confirmado) de Decisão arquitetural (já formalizada em ADR-0023/relatório de decisão) de Desconhecido (não determinável com a evidência atual).

### 4.1 — Estado da memória dos Agents Linx

- **Fato observado**: a fundação `O1.13.5` existe e está `Concluída` — classes `LinxErpSpecialistAgent`/`LinxDatabaseSpecialistAgent`, entidade `LinxKnowledgeEntry` com máquina de estados de proveniência `Descoberto(1) → Inferido(2) → Validado(3) → Aprovado(4)`, repositório e controller reais (`LinxKnowledgeRepository`/`LinxKnowledgeController`).
- **Fato observado**: **nenhuma entrada de conhecimento sobre `CADASTRO_CLI_FOR`/Fornecedor foi persistida** nessa infraestrutura. Todo o conhecimento levantado nas rodadas de discovery vive apenas no snapshot markdown (`LinxKnowledge-Fornecedor-Discovery-Snapshot.md`), não no banco real.
- **Fato observado (GAP explícito, já registrado no snapshot seção 12 e no Discovery seção 10-C)**: não há infraestrutura local (Docker, banco, seed) para persistir efetivamente uma `LinxKnowledgeEntry` real nas sessões de discovery já executadas — isso não bloqueia este gate (que é avaliação documental), mas significa que o conhecimento levantado permanece em markdown, não na base de conhecimento oficial dos Agents.
- **Decisão arquitetural (deste gate)**: não iniciar a resolução desse GAP de infraestrutura agora — está fora do escopo do gate (ver Passo 7/Regra de Parada).

### 4.2 — Rotina canônica de escrita de fornecedor no Linx / fluxo da tela manual

- **Fato observado**: 5 procedures de integração automatizada foram lidas por completo via `OBJECT_DEFINITION` (`LX_AZZ_GERAR_FORNECEDOR_LINX`, `LX_GS_GERAR_ALTERAR_FORNECEDOR_OBC_LINX`, `PROC_GS_INTEGRA_FORNECEDOR_REDMINE`, `PROC_HRG_CADASTRA_ZTBMM_FORNE_SOMA`, `p_RSV_INTEGRACAO_CADASTRO_FORNECEDOR`).
- **Padrão recorrente (Nível 1, 5/5 ou 4/5)**: uso de `LX_SEQUENCIAL` para gerar `CLIFOR`/`COD_CLIFOR`; `NOME_CLIFOR` sempre por sanitização de string (nunca sequencial); ordem fixa `INSERT CADASTRO_CLI_FOR` → `INSERT FORNECEDORES`; flags de papel setadas explicitamente no INSERT; verificação de existência prévia antes de decidir INSERT vs UPDATE.
- **Desconhecido, explícito e crítico**: **nenhuma das 5 implementações lidas foi confirmada como a rotina usada pela tela manual do Visual Linx** — todas são integrações automatizadas de origem externa (parceiro, SAP, sistema de tickets). Não foi encontrada, em nenhuma rodada, uma procedure "genérica" sem prefixo de parceiro. Permanece aberta a possibilidade de que a tela manual monte o INSERT diretamente via DataSet do próprio framework Visual Linx, sem procedure intermediária — isso não é confirmável nem refutável apenas por leitura de `sys.sql_modules`.
- **Interpretação**: o padrão de Nível 1 (5/5) é "a melhor evidência disponível sobre como uma escrita via SQL deveria minimamente se comportar para ser compatível com o Linx" — não uma confirmação de processo oficial aprovado.

### 4.3 — `LX_SEQUENCIAL` e geração de identificadores (`CLIFOR`/`COD_CLIFOR`/`NOME_CLIFOR`)

- **Fato observado**: `LX_SEQUENCIAL(@TABELA_COLUNA, @EMPRESA, @SEQUENCIA OUTPUT, @UPDATE_SEQUENCIAL=1, @NEWVALUE=NULL)` é a procedure real (nome no singular); faz `UPDATE SEQUENCIAIS SET SEQUENCIA = SEQUENCIA = <valor+1>`, retorna valor formatado com zeros à esquerda; variante por empresa via `EMPRESA_SEQUENCIAIS`; incremento fixo em +1, sem coluna configurável. **Nunca executada em nenhuma rodada de discovery.**
- **Fato observado**: nenhuma das chaves de `CADASTRO_CLI_FOR` (`NOME_CLIFOR`, `CLIFOR`, `COD_CLIFOR`) é `IDENTITY` — o valor precisa ser fornecido por quem insere.
- **Fato observado**: `NOME_CLIFOR` nunca vem de sequencial — é sempre sanitização de string de um campo de nome (algoritmo varia entre as 5 implementações).
- **Fato observado / anomalia confirmada isolada**: `p_RSV_INTEGRACAO_CADASTRO_FORNECEDOR` usa `LX_SEQUENCIAL('CLIENTES_ATACADO.CLIFOR')`, não `'FORNECEDORES.CLIFOR'` (linha correta existe comentada no código-fonte, sem explicação documentada). Confirmado como anomalia isolada (4/5 usam a chave correta), nunca deve ser generalizada.
- **Fato observado**: existem sequenciais aparentemente duplicados para o mesmo conceito (`FORNECEDORES.CLIFOR` valor atual 315505 vs `SEQUENCIA_FORNECEDOR` valor atual 0999) — uso inconsistente/legado, não explicado.
- **Desconhecido**: se a tela manual do Visual Linx usa o mesmo mecanismo `LX_SEQUENCIAL`/`FORNECEDORES.CLIFOR` das integrações automatizadas.
- **Fato observado (concorrência)**: não há hint de lock explícito (`WITH (UPDLOCK, HOLDLOCK)`) nem `BEGIN TRAN` visível na definição de `LX_SEQUENCIAL` — a atomicidade depende apenas do comportamento padrão de um único `UPDATE`. **Interpretação**: pode ser suficiente sob o nível de isolamento padrão do SQL Server para esse padrão de `UPDATE...SET var = col = expr`, mas isso não foi validado sob concorrência real nesta investigação (nenhum teste de carga/concorrência foi ou deveria ser executado em ambiente real).

### 4.4 — Duplicidade de cadastro

- **Fato observado (+Compras)**: `Cnpj_Cpf` já tem índice único **global** (não escopado por `BusinessUnit`) — confirmado por migration. Regra formalizada em ADR-0023: um Fornecedor é único por `DocumentoFiscal` normalizado em todo o +Compras.
- **Padrão recorrente (Linx, Nível 2)**: critério de duplicidade nas 5 implementações varia — 3/5 usam nome/razão social sanitizados, apenas 1/5 usa CNPJ como critério primário, 1/5 usa `MAX(CLIFOR)` associado ao CNPJ já atribuído.
- **Desconhecido, marcado explicitamente como "DEPENDENTE DE REGRA AINDA NÃO CONHECIDA"**: qual critério de duplicidade o Adapter Linx deveria usar (nome sanitizado, CNPJ, ou ambos) — não há Nível 1 de confiança aqui.

### 4.5 — Cenário multiuso Cliente→Fornecedor (papéis multiuso)

- **Fato observado, com evidência quantitativa real (agregações, sem exposição de dados pessoais)**: `CADASTRO_CLI_FOR` tem 96.696 registros totais; 78.113 com `INDICA_FORNECEDOR=1`; 28.686 com `INDICA_CLIENTE=1`; 2.222 com `INDICA_FILIAL=1`; **11.777 registros são Fornecedor E Cliente simultaneamente** (~15% do total); 1.895 acumulam os três papéis. Confirma quantitativamente a hipótese originalmente levantada pelo PO/especialista Linx.
- **Fato observado**: não há `CHECK constraint` ligando a flag de papel à existência da linha na tabela especializada — a consistência é mantida só por disciplina de trigger/aplicação. Há pequenas divergências reais confirmadas (39 registros com flag=1 sem linha em `FORNECEDORES`; 296 com linha em `FORNECEDORES` sem a flag).
- **Interpretação**: papéis simultâneos não são exceção rara, são padrão estrutural esperado — qualquer Adapter Linx de escrita precisa decidir conscientemente que flags setar (nunca assumir que "cadastrar fornecedor" implica só `INDICA_FORNECEDOR=1`, dado que o mesmo CNPJ pode já existir como Cliente).
- **Decisão arquitetural (ADR-0023, já formalizada)**: o domínio +Compras não modela múltiplos papéis hoje — essa tradução é responsabilidade exclusiva do futuro Adapter Linx.

### 4.6 — Campos mínimos de `CADASTRO_CLI_FOR` e `FORNECEDORES` — matriz de mapeamento

**`CADASTRO_CLI_FOR`** (tabela-mãe, PK `NOME_CLIFOR` varchar(25) sem IDENTITY; `CLIFOR`/`COD_CLIFOR` char(6) sem IDENTITY) — **Fato observado**:

| Grupo | Colunas físicas (Linx) | Campo equivalente `Fornecedor.cs` (+Compras) | Observação |
|---|---|---|---|
| Chave | `NOME_CLIFOR`, `CLIFOR`, `COD_CLIFOR` | `Id` (Guid) | Sem correspondência direta — Adapter precisa gerar/mapear |
| Documento fiscal | `CGC_CPF` (varchar 19, único campo p/ CNPJ e CPF) + `PJ_PF` (bit) | `Cnpj_Cpf` + `TipoPessoa` | Já compatível conceitualmente (documento único + flag de tipo) |
| Razão social/fantasia | `RAZAO_SOCIAL` (varchar 90); **sem coluna de nome fantasia** | `RazaoSocial`, `NomeFantasia` | `NomeFantasia` do +Compras não tem par físico confirmado |
| Papel | `INDICA_FORNECEDOR`, `INDICA_CLIENTE`, `IND_REPRESENTANTE`, `INDICA_FILIAL` (bits) | Não modelado (+Compras assume implicitamente "é Fornecedor") | Gap de modelagem — ver 4.5 |
| Endereço (×3 blocos: principal/`COBRANCA_`/`ENTREGA_`) | `ENDERECO`, `NUMERO`, `COMPLEMENTO`, `BAIRRO`, `CIDADE`, `UF`, `CEP`, `PAIS` + variantes `COBRANCA_*`/`ENTREGA_*`; `COD_MUNICIPIO_IBGE*` preenchido por trigger | Bloco único (Cep/Logradouro/Numero/Complemento/Bairro/Cidade/Estado/Pais) | Gap Alto — GAP-LINX-ENDERECO-MULTIPLO (aberto) |
| Contato | `DDD1`/`TELEFONE1`, `DDD2`/`TELEFONE2`, `DDDFAX`/`FAX`, `EMAIL`, `EMAIL_NFE` | `Ddd`/`Telefone`, `Email`/`EmailFiscal` | Compatível estruturalmente; segundo telefone/DDD não suportado no +Compras |
| CNAE | `CNAE` (varchar 7) — só principal | `CnaePrincipalCodigo`/`CnaePrincipalDescricao` (B2.8) | **Já alinhado** — Linx físico confirma "só principal" |
| QSA | Nenhuma coluna | Não persistido | **Já alinhado** — ambos não modelam |
| Bancário | `BANCO` (FK), `CC_AGENCIA`, `CC_CONTA`, `CC_NOME_AGENCIA` — sem dígito de conta separado visível | `Banco`/`Agencia`/`Conta`/`DigitosConta` | +Compras tem campo sem par físico confirmado — Desconhecido se dígito está embutido em `CC_CONTA` |
| Situação | `INATIVO` (bit) — binário simples | `Status` (string) + `SituacaoCadastral` (enum canônico, B2.5) | Linx físico é binário; +Compras tem enum rico — conceitos propositalmente distintos (`Status` operacional vs. `SituacaoCadastral` da Receita) |

**`FORNECEDORES`** (especialização de papel; PK `FORNECEDOR` varchar(25); FKs via `CLIFOR`/`COD_FORNECEDOR`/`FORNECEDOR`) — **Fato observado**: contém exclusivamente atributos de negócio do papel — classificação (`TIPO`, `SUBTIPO_FORNECEDOR`, `CENTRO_CUSTO`), fiscal/financeiro (`CONTA_CONTABIL`, `CONDICAO_PGTO`, `MOEDA`), flags de fornecimento (`FORNECE_MATERIAIS`, `FORNECE_PROD_ACAB`, `FORNECE_MAT_CONSUMO`, `BENEFICIADOR`, `FORNECE_OUTROS`, `INDICA_TRANSPORTADORA`, `INDICA_MARKDOWN`, `INDICA_INTERMEDIADOR`), compliance (`BLOQUEIO_COMPLINCE`, `INDICA_CQFOR`), licenciamento (`LICENCIADO`, `LICENCIADO_ROYALTIES`). **Não tem nenhuma coluna de endereço/telefone/e-mail/razão social** — tudo fica na tabela-mãe.

- **Padrão recorrente confirmado (evidência direta)**: as flags de fornecimento de `FORNECEDORES` correspondem **quase 1:1** aos bits já existentes em `Fornecedor.cs` (`ForneceMateriais`, `ForneceConsumo`, `ForneceProdutos`, `Beneficiador`, `Licenciado`) — forte indício de que o domínio +Compras já foi desenhado espelhando esse conjunto, mesmo sem ter sido confirmado explicitamente como intencional.
- **Desconhecido**: mapeamento fino de `SubtipoFornecedor`/`CondicaoPagamento`/`ContaContabil`/`RegimeFiscal` do +Compras para os campos equivalentes de `FORNECEDORES` (domínios internos vs. valores de referência Linx) — não investigado em profundidade suficiente para afirmar 1:1.

### 4.7 — Endereço, contato, CNAE

- **Decisão arquitetural (ADR-0023, já formalizada)**: manter apenas endereço principal e contato único (DDD+telefone) no MVP do domínio +Compras — sem requisito de negócio documentado que exija múltiplos. A tradução para os 3 blocos físicos do Linx (se necessária) é responsabilidade exclusiva do Adapter Linx.
- **Fato observado**: CNAE já é tratado de forma alinhada — Linx físico só armazena o principal (`CNAE varchar(7)`), +Compras (B2.8) só persiste `CnaePrincipalCodigo`/`CnaePrincipalDescricao`. Nenhuma das 5 procedures de integração lidas preenche CNAE, reforçando que não é campo crítico à escrita Linx.
- **Desconhecido**: se e como o Adapter deveria replicar o endereço único do +Compras nos 3 blocos físicos do Linx (principal/cobrança/entrega) — item "RECOMENDADO", nunca decidido.

### 4.8 — Ordem de operações e estratégia transacional

- **Padrão recorrente (Nível 1, 5/5)**: ordem fixa `INSERT CADASTRO_CLI_FOR` → `INSERT FORNECEDORES`, nunca o inverso.
- **Padrão recorrente (Nível 2, 3/5 confirmado explicitamente)**: uso de transação explícita (`BEGIN TRAN`/`COMMIT`/`ROLLBACK` com `TRY`/`CATCH`). Nas outras 2/5 há `ROLLBACK`/`CATCH` mas `BEGIN TRAN` explícito não foi confirmado na leitura (lacuna de leitura, não negação).
- **Decisão arquitetural (proposta, não implementada)**: um futuro Adapter Linx deveria envolver a escrita física em transação com rollback; e decidir uma estratégia de compensação/rollback cross-sistema com o +Compras (já que são dois sistemas de persistência distintos) — este é um item "RECOMENDADO", não decidido nesta rodada nem neste gate.

### 4.9 — Concorrência sobre `LX_SEQUENCIAL`

- **Fato observado**: nenhum hint de lock explícito (`WITH (UPDLOCK, HOLDLOCK)`) foi encontrado na definição da procedure; a atomicidade depende apenas do padrão `UPDATE ... SET @var = coluna = expressão`, atômico por natureza de um único statement.
- **Interpretação (não validada)**: essa atomicidade pode ser considerada suficiente pelo padrão Linx, mas não foi avaliado o nível de isolamento de transação da sessão chamadora, nem testada sob concorrência real — nenhum teste de carga foi ou deveria ser feito em ambiente real.
- **Desconhecido**: comportamento sob alta concorrência de escrita simultânea (múltiplas integrações + tela manual + futuro Adapter +Compras chamando `LX_SEQUENCIAL` ao mesmo tempo).

### 4.10 — Triggers relevantes e triggers de bloqueio

- **Fato observado**: 11 triggers ativas em `CADASTRO_CLI_FOR` (`is_disabled=0`), todas lidas por completo via `OBJECT_DEFINITION` em rodadas sucessivas de discovery:
  - `LXI_CADASTRO_CLI_FOR`/`LXU_CADASTRO_CLI_FOR` — preenchimento automático de `COD_MUNICIPIO_IBGE` (×3 blocos); bloqueio condicional por parâmetro `VALIDA_COD_IBGE_CADASTROS`; dispara `LX_ATUALIZA_PAF_ECF_ERP` (integração fiscal) após INSERT.
  - `LXI_ANM_CADASTRO_CLI_FOR` — **bloqueia** INSERT com nome iniciando em espaço ou com caractere especial (`RAISERROR`+`ROLLBACK`); grava em `IN86_CADASTRO_DE_TERCEIROS` (comentário no código: "Utilizada para o DP" — integração RH/terceiros real, confirmada).
  - `LXU_ANM_CADASTRO_CLI_FOR` — **bloqueia** alteração de `RAZAO_SOCIAL`/`CGC_CPF`/`RG_IE` de filiais sem permissão (`GS_PERM_ALT_CAD_CLIFOR`); cascateia inativação para `CLIENTES_ATACADO`; propaga e-mail para portal de boletos; audita dados bancários.
  - `GSU_SAP_CADASTRO_CLI_FOR` — **bloqueia** UPDATE de ~40 colunas para registros marcados como integrados ao SAP (mensagem literal `'ALTERACAO BLOQUEADA - FAZER VIA SAP'`), a menos que permissão `GS_PERM_ALT_CLIFOR_SAP` libere.
  - `GSI/GSU/GSD_CADASTRO_CLI_FOR_LOG` — auditoria completa antes/depois em `GS_CADASTRO_CLI_FOR_LOG`, para INSERT/UPDATE/DELETE.
  - `LXI_ETL_CADASTRO_CLI_FOR`/`LXU_ETL_CADASTRO_CLI_FOR` — enfileira em `LJ_ETL_REPOSITORIO`, com auto-supressão via `APP_NAME()`/`CONTEXT_INFO()` (evita loop quando a origem já é o próprio processo de ETL).
  - `GSUI_WETL_CADASTRO_CLI_FOR` — segunda fila (`GS_WETL_REPOSITORIO`), **sem** a mesma lógica de auto-supressão observada na fila de ETL.
- **Fato observado**: mais de 90 tabelas satélite referenciam `CADASTRO_CLI_FOR` via FK — evidência de alto acoplamento.
- **Interpretação**: a inativação em cascata de `CLIENTES_ATACADO`/`STG_FILIAIS_OMS` sugere um fluxo de negócio "inativar CliFor = inativar papéis relacionados" — não confirmado por especialista.
- **Desconhecido (crítico)**: identidade exata dos sistemas consumidores de `LJ_ETL_REPOSITORIO` e `GS_WETL_REPOSITORIO` — sem saber quem lê essas filas, não se sabe se um futuro Adapter +Compras deveria suprimir ou permitir a entrada nessas filas.

### 4.11 — Efeitos colaterais (ETL/WETL/SAP/RH) — matriz consolidada

| Sistema/mecanismo | Trigger | Efeito confirmado | Classificação |
|---|---|---|---|
| Fiscal (PAF-ECF) | `LXI_CADASTRO_CLI_FOR` | Chama `LX_ATUALIZA_PAF_ECF_ERP` (se existir) após INSERT | Fato observado (chamada), Interpretação (propósito PAF-ECF) |
| RH/terceiros | `LXI_ANM_CADASTRO_CLI_FOR` | Insere em `IN86_CADASTRO_DE_TERCEIROS`, comentário "Utilizada para o DP" | Fato observado |
| SAP | `GSU_SAP_CADASTRO_CLI_FOR` | Bloqueia UPDATE de subconjunto de registros integrados ao SAP | Fato observado |
| ETL/datasync (fila 1) | `LXI_ETL_CADASTRO_CLI_FOR`/`LXU_ETL_CADASTRO_CLI_FOR` | Enfileira em `LJ_ETL_REPOSITORIO`, com auto-supressão | Fato observado; consumidor real = Desconhecido |
| WETL (fila 2) | `GSUI_WETL_CADASTRO_CLI_FOR` | Enfileira em `GS_WETL_REPOSITORIO`, sem auto-supressão equivalente | Fato observado; consumidor real = Desconhecido |
| Auditoria | `GSI/GSU/GSD_CADASTRO_CLI_FOR_LOG` | Snapshot antes/depois em `GS_CADASTRO_CLI_FOR_LOG` | Fato observado |

**Princípio central confirmado (Fato observado, generalizado como Interpretação para outros domínios Linx)**: um único INSERT/UPDATE em `CADASTRO_CLI_FOR` nunca deve ser assumido como tendo efeito apenas local — qualquer Adapter Linx precisa mapear conscientemente cada trigger antes de escrever.

### 4.12 — Supressão de triggers

- **Fato observado**: a fila de ETL (`LJ_ETL_REPOSITORIO`) tem mecanismo de auto-supressão real via `APP_NAME()`/`CONTEXT_INFO()` — um chamador pode evitar entrar nessa fila se sinalizar corretamente.
- **Fato observado**: a fila WETL (`GS_WETL_REPOSITORIO`) **não tem** supressão equivalente lida na definição da trigger.
- **Desconhecido (crítico, decisão de negócio)**: se um futuro Adapter +Compras deveria suprimir ou permitir a entrada nessas filas — depende de quem consome cada fila (também Desconhecido).

### 4.13 — Idempotência e estratégia de retry

- **Fato observado (domínio +Compras)**: já existe verificação de existência (`searchSupplierByDocument`) e regra de duplicidade global por documento — base para idempotência do lado +Compras.
- **Padrão recorrente (Linx, Nível 1, 5/5)**: verificação de existência prévia antes de decidir INSERT vs UPDATE está presente em todas as 5 implementações lidas — mas com critérios de duplicidade diferentes (ver 4.4).
- **Desconhecido**: comportamento de um retry de escrita Linx após falha parcial (ex.: `CADASTRO_CLI_FOR` inserido, `FORNECEDORES` falhou) — nenhuma das 5 procedures documenta explicitamente esse cenário como tratado; presume-se que a transação (quando existente) faça rollback de ambos, mas isso não substitui a necessidade de o Adapter ter sua própria idempotência (ex.: chave de correlação/operação) para lidar com timeouts de rede entre +Compras e Linx, que estão em processos/sistemas distintos.

### 4.14 — Falha parcial e recuperação

- **Fato observado**: 3/5 implementações confirmam `BEGIN TRAN`/`COMMIT`/`ROLLBACK` explícito com `TRY`/`CATCH` — rollback do lado Linx é, portanto, um padrão recorrente mas não universal (2/5 têm lacuna de leitura, não confirmada como ausência).
- **Desconhecido crítico**: comportamento de falha parcial **entre** +Compras e Linx (dois sistemas de persistência distintos, sem transação distribuída nativa) — se o INSERT no Linx falhar após o +Compras já ter persistido o Fornecedor (ou vice-versa), não há mecanismo de compensação desenhado nem documentado. Este é exatamente o tipo de decisão que a Work Order B2.9 declara depender do gate de validação com especialista.

### 4.15 — Taxonomia de erros do adapter

- **Decisão arquitetural (já formalizada para o contrato canônico de CNPJ, B2.4/ADR-0023)**: `CnpjInvalido`, `NaoEncontrado`, `FonteIndisponivel`, `Timeout`, `LimiteDeConsultas`, `ErroDeAutenticacaoDoProvider`, `RespostaInvalida`, `ErroInterno` — já implementada como enum tipado no Application layer para o Provider de consulta CNPJ.
- **Desconhecido/a decidir (Adapter Linx, ainda não desenhado)**: uma taxonomia equivalente e específica para o Adapter Linx precisaria cobrir, no mínimo: `LinxIndisponivel` (SOMA_DESENV inacessível), `LinxSequencialFalhou`, `LinxViolacaoDeTrigger` (ex.: bloqueio de IBGE, bloqueio SAP, nome mal formatado), `LinxDuplicidadeDetectada`, `LinxTimeout`, `LinxErroDesconhecido`. Esta lista é uma **proposta de estrutura**, não uma taxonomia confirmada/validada — depende diretamente dos desconhecidos das seções 4.2/4.9/4.10.

### 4.16 — Observabilidade necessária

- **Fato observado (já existente no +Compras)**: `FornecedorCnpjConsultaHistorico` já registra `CorrelationId`, fonte, timestamp, status — precedente direto de observabilidade de integração externa.
- **Proposta (não implementada, não validada)**: um futuro Adapter Linx precisaria, no mínimo: log estruturado por operação (INSERT/UPDATE), correlação entre o `Id` do +Compras e o `CLIFOR`/`NOME_CLIFOR` gerado, métricas de latência/falha por tipo de erro, e alertas para bloqueios de trigger (ex.: `GSU_SAP_CADASTRO_CLI_FOR`) — porque esses bloqueios indicam necessidade de intervenção manual, não um erro transitório.

### 4.17 — Operações mínimas do Adapter (contrato +Compras→Linx) e fronteira arquitetural

**Decisão arquitetural já formalizada (ADR-0023 + seção O da Decisão de Arquitetura + seção 8 do Snapshot)**:

| Classificação | Item |
|---|---|
| OBRIGATÓRIO | Obter código via `LX_SEQUENCIAL`/`FORNECEDORES.CLIFOR` (melhor evidência atual, Nível 1) |
| OBRIGATÓRIO | Gerar `NOME_CLIFOR` por sanitização de nome (sem espaço inicial, sem caracteres especiais — reforçado por trigger real de bloqueio) |
| OBRIGATÓRIO | Popular flags de papel Linx (`INDICA_FORNECEDOR` etc.) no momento da criação |
| OBRIGATÓRIO | Verificar existência prévia no lado Linx antes de decidir INSERT vs UPDATE físico (regra distinta da duplicidade do +Compras) |
| RECOMENDADO | Transação com rollback ao escrever no Linx |
| RECOMENDADO | Popular o bloco de endereço físico Linx (replicando o endereço único do +Compras nos 3 blocos, se necessário) |
| AINDA DESCONHECIDO | Se o cadastro manual via tela Visual Linx segue o mesmo padrão das integrações automatizadas lidas |
| AINDA DESCONHECIDO | Quem consome `LJ_ETL_REPOSITORIO`/`GS_WETL_REPOSITORIO` e se o Adapter deve suprimir ou permitir a replicação |
| NÃO ENTRA NO DOMÍNIO +COMPRAS | Vocabulário/campos de integrações de terceiros (`grupo_conta`, `ORG_COMPRA`, `codigo_sap`, campos SAP `MANDT`/`LIFNR`/`STCD1`, nomes de tabela Linx) |

**Fronteira arquitetural (princípio não-negociável, ADR-0023)**: nenhum vocabulário Linx (`CLIFOR`, `LX_SEQUENCIAL`, `CADASTRO_CLI_FOR`, SAP/OBC/Redmine) entra em `Fornecedor.cs`, no contrato canônico de CNPJ, ou em qualquer DTO/componente do domínio +Compras — pertence exclusivamente ao Adapter Linx.

### 4.18 — Plano de testes (proposta futura, dados de teste)

Como o Adapter Linx não está implementado, não há testes reais a reportar. Proposta de estrutura futura (não implementada, condicionada à resolução dos desconhecidos):

1. **Testes unitários do Adapter** (sem tocar o SOMA_DESENV): mapeamento `Fornecedor` (+Compras) → DTO físico Linx; sanitização de `NOME_CLIFOR`; seleção de flags de papel; tratamento de cada erro da taxonomia proposta (4.15).
2. **Testes de integração contra um SOMA_DESENV de teste dedicado** (nunca o SOMA_DESENV real de desenvolvimento partilhado, e nunca produção): INSERT real de um fornecedor de teste sintético (CNPJ fictício, nunca dado real), validação de que as 11 triggers disparam como esperado, validação de idempotência (reenvio da mesma operação), validação de falha parcial simulada.
3. **Dados de teste**: exclusivamente CNPJs sintéticos/fictícios gerados para teste (nunca CNPJs reais de fornecedores existentes) — critério já usado nas convenções de teste do projeto (`STANDARDS.md`/`context/testing.md`, não lidos integralmente nesta rodada, mas a prática de não usar dados reais é consistente com o restante da governança de dados observada).
4. **Critério de aceite mínimo** para desbloquear a execução real: confirmação, por especialista Visual Linx, dos itens listados na seção 5 (perguntas abertas) — sem isso, nenhum teste de integração real deveria ocorrer, mesmo em ambiente de desenvolvimento.

---

## 5. Perguntas em aberto para o especialista Visual Linx

1. Existe uma rotina/procedure oficial e genérica de cadastro de fornecedor usada pela tela manual do Visual Linx, distinta das 5 integrações automatizadas lidas (que são todas de parceiros/SAP/Redmine)? Se sim, qual o nome/localização?
2. Quem consome as filas `LJ_ETL_REPOSITORIO` e `GS_WETL_REPOSITORIO`? Um INSERT feito por um futuro Adapter +Compras deveria suprimir a entrada nessas filas (via `APP_NAME()`/`CONTEXT_INFO()`) ou é desejável que ele replique normalmente?
3. Por que `p_RSV_INTEGRACAO_CADASTRO_FORNECEDOR` usa o sequencial `CLIENTES_ATACADO.CLIFOR` em vez de `FORNECEDORES.CLIFOR` (que está comentado no código) mesmo para registros marcados como Fornecedor? É um bug legado, ou uma decisão de negócio específica ao parceiro "RSV"?
4. Por que existem dois sequenciais aparentemente concorrentes para o mesmo conceito (`FORNECEDORES.CLIFOR` e `SEQUENCIA_FORNECEDOR`)? Qual é o correto a ser usado por um novo integrador?
5. Qual critério de duplicidade deveria ser usado pelo Adapter (nome sanitizado, CNPJ/`CGC_CPF`, ou combinação) — dado que as 5 implementações lidas usam critérios diferentes entre si?
6. O dígito de conta bancária do +Compras (`DigitosConta`) tem par físico no Linx, ou está embutido em `CC_CONTA`?
7. É seguro/desejável, sob concorrência real de produção, que um novo Adapter chame `LX_SEQUENCIAL` sem lock explícito adicional pelo lado chamador? Existe alguma prática already adotada pelas integrações existentes para mitigar corrida de sequencial que não esteja visível apenas na definição da procedure?
8. Como o processo humano trata hoje um CNPJ que já existe como Cliente e precisa ganhar o papel de Fornecedor (fluxo multiuso, seção 4.5) — é um UPDATE de flag em um registro existente, ou o operador cria um registro novo?
9. Existe uma stratégia oficial de rollback/compensação quando uma integração externa (das 5 lidas, ou qualquer outra) falha após já ter inserido em `CADASTRO_CLI_FOR` mas antes de `FORNECEDORES`?
10. É aceitável, para fins de teste de um futuro Adapter, provisionar um ambiente SOMA_DESENV de teste isolado (não o de desenvolvimento compartilhado), para exercitar INSERT/UPDATE reais com dados sintéticos sem risco a processos de negócio reais?

---

## 6. Desconhecidos que devem ser preservados como tal (lista consolidada)

1. Existência de uma rotina "oficial" de cadastro manual via tela Visual Linx, distinta das 5 integrações automatizadas lidas.
2. Identidade dos sistemas consumidores de `LJ_ETL_REPOSITORIO` e `GS_WETL_REPOSITORIO`.
3. Critério definitivo de duplicidade do lado Linx (nome sanitizado vs. CNPJ vs. combinação).
4. Razão da divergência de sequencial em `p_RSV_INTEGRACAO_CADASTRO_FORNECEDOR` (bug ou decisão de negócio).
5. Razão/uso real dos dois sequenciais aparentemente concorrentes (`FORNECEDORES.CLIFOR` vs `SEQUENCIA_FORNECEDOR`).
6. Comportamento de `LX_SEQUENCIAL` sob concorrência real de produção (ausência de lock explícito na definição, não testado sob carga).
7. Estratégia de compensação/rollback cross-sistema entre +Compras e Linx em caso de falha parcial.
8. Se o dígito de conta bancária do +Compras tem par físico exato no Linx.
9. Mapeamento fino de `SubtipoFornecedor`/`CondicaoPagamento`/`ContaContabil`/`RegimeFiscal` do +Compras para os domínios de referência reais do Linx.
10. Conteúdo detalhado de outras procedures candidatas não lidas em profundidade (`LX_AZZ_GERAR_CLIENTE_ATAC_LINX`, `MIT_INTEGRA_ORO`, `MIT_INTEGRA_TRUNK`, `mit_integra_vintage`, `PROC_GS_INTEGRA_CLIENTES_ATACADO_REDMINE`, `LX_LGPD_PROC_CLIENTE`, `GS_CRIA_FILIAIS`).

Nenhum destes é resolvido por suposição neste gate — todos permanecem explicitamente abertos.

---

## 6-A. Investigação do fluxo real da tela Visual Linx (Adendo — código-fonte real como nova fonte primária)

**Contexto do adendo**: o Product Owner disponibilizou localmente `docs/linxERP/Exclusivos.zip` (~200 MB, ignorado pelo git — `.gitignore:174` — nunca versionado), contendo telas (`LX[código].SCX/SCT`) e objetos de entrada/customização (`obj_[código].PRG/FXP`) reais do Visual Linx, compiláveis em Visual FoxPro 9. O PO ensinou o método de localização: `SELECT * FROM TRANSACOES WHERE TABELA_PAI = 'FORNECEDORES'` → campo `CONTROL_SISTEMA` = código da tela. PO informou `CONTROL_SISTEMA = 001016G1`.

**Artefatos localizados e lidos integralmente** (leitura local, nada extraído permanece fora do scratchpad temporário da sessão): `lx001016G1.SCX`/`.SCT` (tela padrão) e `obj_001016G1.PRG`/`.FXP` (objeto de entrada/customização SOMA/AZZAS). Método: `.SCT` é o memo-texto da tela (extraído via `strings`, sem decompilar/modificar nada); `.PRG` é texto plano (Visual FoxPro), lido diretamente.

**Achados por tema (nova hierarquia de evidência: tela padrão → OBJ/customização → banco → integrações → especialista):**

| Tema | Achado | Origem | Proveniência |
|---|---|---|---|
| Rotina de persistência | Não há `INSERT`/`TableUpdate` explícito visível na tela nem no OBJ — a gravação passa por métodos de classe herdada (`l_desenhista_altera_antes`, `l_desenhista_apos_salva`, `l_desenhista_cancela`, também usados por `page7.lx_contato_cadastro1`) e pela `View`/`CursorAdapter` (`Tables=CADASTRO_CLI_FOR,FORNECEDORES`, mas `SendUpdates=.F.` no cursor principal — o `TableUpdate()` automático do VFP está desativado, reforçando que a persistência real passa pela classe base, não pelo mecanismo padrão de view). A implementação interna dessa classe base está fora de `Exclusivos.zip` (que contém só customizações) — **confirmado com o PO que ele também não tem visibilidade sobre essa implementação interna a partir do material disponível.** | Tela padrão (SCX/SCT) | DESCOBERTO (chamada observada) / DESCONHECIDO (implementação interna da classe base) |
| Nenhuma das 5 procedures de parceiro é usada pela tela | Confirmado — nenhuma referência a `LX_AZZ_GERAR_FORNECEDOR_LINX` etc. em SCX/SCT/PRG da tela `001016G1` | Tela padrão | DESCOBERTO |
| Sequencial (`CLIFOR`) | `f_sequenciais('FORNECEDORES.CLIFOR', .t.)` é chamado apenas quando `p_tool_status='I'` (inclusão) e `px_sequencial` vazio; resultado grava direto em `v_fornecedores_01.clifor`; guarda é resetada após salvar (`l_desenhista_apos_salva`) | Tela padrão | DESCOBERTO — confirma `FORNECEDORES.CLIFOR` como sequencial oficial; isola a anomalia `CLIENTES_ATACADO.CLIFOR` (`p_RSV_...`) como divergência de integração, não padrão alternativo válido |
| `COD_CLIFOR`/`COD_FORNECEDOR` | `Replace Cod_CliFor With CliFor` / `Replace cod_fornecedor with clifor` — mesmo valor do `CLIFOR`, sem padding/transformação adicional | Tela padrão | DESCOBERTO |
| `NOME_CLIFOR` | `Replace nome_clifor With fornecedor` — a chave é o próprio campo "Fornecedor" digitado pelo usuário (não é derivado de razão social nem de sequencial); sanitização real observada no OBJ de customização: `LTRIM(UPPER(...))`, remoção de espaço inicial e de um conjunto fixo de caracteres especiais (`!@#$%&*'{}[]/~^+=;.,\`?\|` etc.) | Tela padrão (fonte do valor) + OBJ (sanitização) | DESCOBERTO — sanitização específica é customização SOMA/AZZAS (`obj_001016G1.prg`), não confirmada como padrão universal Linx; sem tratamento explícito de colisão de nome além da própria PK `NOME_CLIFOR` (varchar 25, sem IDENTITY) |
| Duplicidade | Ao digitar `CGC_CPF`: `SELECT ... FROM FORNECEDORES WHERE CGC_CPF = <input>`. Se existir: verifica `CADASTRO_CLI_FOR_EMPRESA` para a mesma `EMPRESA` (grupo econômico) → **BLOQUEIA** se já cadastrado na mesma empresa; se cadastrado em outra empresa do grupo, **perguntaao usuário** se quer vincular ("Deseja incluir seu grupo econômico neste cadastro?") → se sim, **REUTILIZA** (INSERT em `CADASTRO_CLI_FOR_EMPRESA`, sem criar novo `CADASTRO_CLI_FOR`/`FORNECEDORES`); se não, bloqueia. Customização SOMA/AZZAS no OBJ reforça bloqueio adicional por CNPJ ignorando prefixo `AZCB%`. | Tela padrão (regra primária) + OBJ (reforço) | DESCOBERTO — resolve a Pergunta 5 do Gate original: critério primário e oficial é `CGC_CPF` em `FORNECEDORES`, escopado por empresa/grupo econômico (não nome sanitizado) |
| Flag de papel (`INDICA_FORNECEDOR`) | Setada explicitamente pela tela ao validar/alterar `CGC_CPF` (`replace ... indica_fornecedor with .t.`) | Tela padrão | DESCOBERTO |
| Multiuso (preservação de papéis) | Evidência simétrica pelo caminho de **exclusão**: ao excluir o papel Fornecedor (`p_tool_status='E'`), a tela consulta `INDICA_CLIENTE/INDICA_FILIAL/IND_REPRESENTANTE/INDICA_FORNECEDOR` em `CADASTRO_CLI_FOR`; se **nenhum outro papel** existir, exclui de `CADASTRO_CLI_FOR` e `FORNECEDORES` juntos; se **existir outro papel**, faz apenas `UPDATE CADASTRO_CLI_FOR SET INDICA_FORNECEDOR=0` e exclui somente de `FORNECEDORES`, preservando o cadastro-mãe e os demais papéis | Tela padrão | DESCOBERTO (caminho de exclusão) / INFERIDO por simetria (caminho de inclusão de papel em cadastro existente — não há trecho simétrico explícito de "adicionar papel" na tela lida; o comportamento observado na exclusão é o suporte mais forte disponível para a hipótese arquitetural já registrada no Gate) |
| CNAE | Encontrada integração com webservice externo de consulta CNPJ (`_cnae = Strextract(_retorno,'"cnae_fiscal":',...)`) na tela, mas **sem evidência de `REPLACE`/gravação desse valor em `CADASTRO_CLI_FOR.CNAE`** no trecho lido | Tela padrão | DESCOBERTO parcial — consistente com o achado anterior do Gate (nenhuma das 5 integrações grava CNAE); não eleva CNAE a obrigatório |
| Transação/buffering | Nenhum `BEGIN TRANSACTION`/`TableUpdate()` explícito encontrado no texto da tela/OBJ — consistente com a hipótese de que o controle transacional (se existir) está encapsulado na classe base fora deste pacote | Tela padrão + OBJ | DESCONHECIDO (não elevável a Fato sem ver a classe base) |
| Filas ETL/WETL, consumidores, concorrência em `LX_SEQUENCIAL`, rollback cross-sistema | **Não tocados pelo código da tela cliente (VFP)** — são comportamento de trigger/banco (lado SQL Server), fora do alcance de um artefato de tela desktop | — | Continuam DESCONHECIDOS, sem alteração pelo adendo |

**Reclassificação das 10 perguntas originais do Gate (seção 5):**

| # | Pergunta original | Novo status |
|---|---|---|
| 1 | Rotina oficial de cadastro manual | PARCIALMENTE RESOLVIDA POR EVIDÊNCIA — não é nenhuma das 5 procedures; é uma classe base compartilhada (`l_desenhista_*`) fora do pacote disponível. Implementação interna permanece desconhecida (confirmado com o PO que também não há visibilidade sobre ela a partir do material local) |
| 2 | Consumidores de `LJ_ETL_REPOSITORIO`/`GS_WETL_REPOSITORIO` | AINDA NECESSÁRIA (fora do alcance do código de tela) |
| 3 | Anomalia `CLIENTES_ATACADO.CLIFOR` | REDUZIDA — tela padrão confirma `FORNECEDORES.CLIFOR` como oficial; a pergunta ao especialista pode ser só de confirmação, não de descoberta |
| 4 | Dois sequenciais concorrentes (`FORNECEDORES.CLIFOR` vs `SEQUENCIA_FORNECEDOR`) | AINDA NECESSÁRIA (tela só usa o primeiro; segundo não apareceu) |
| 5 | Critério de duplicidade | RESOLVIDA POR EVIDÊNCIA — `CGC_CPF` em `FORNECEDORES`, escopado por empresa/grupo econômico, com fluxo de reuso entre empresas do grupo |
| 6 | Dígito de conta bancária | AINDA NECESSÁRIA (não investigado neste adendo — fora do contrato mínimo do MVP) |
| 7 | Concorrência em `LX_SEQUENCIAL` | AINDA NECESSÁRIA (tela não revela nada sobre lock/concorrência) |
| 8 | Cliente→Fornecedor (UPDATE de flag vs. novo registro) | PARCIALMENTE RESOLVIDA — evidência simétrica forte pelo caminho de exclusão; caminho de inclusão de papel não visto explicitamente. SUBSTITUÍDA por pergunta de validação: "o caminho de adicionar o papel Fornecedor a um `CADASTRO_CLI_FOR` existente (Cliente) segue a mesma simetria observada na exclusão — preserva o cadastro-mãe e só ajusta flag + insere em `FORNECEDORES`?" |
| 9 | Estratégia de rollback/compensação | AINDA NECESSÁRIA |
| 10 | Ambiente de teste isolado | AINDA NECESSÁRIA (decisão de infraestrutura, não de código) |

**Impacto sobre a decisão do Gate**: a nova evidência **reduz materialmente** a incerteza sobre sequencial, `CLIFOR`/`COD_CLIFOR`, `NOME_CLIFOR` e duplicidade — quatro dos itens antes classificados como "Desconhecido, marcado explicitamente como DEPENDENTE DE REGRA AINDA NÃO CONHECIDA" agora têm base DESCOBERTO direta do código real da tela padrão. Porém os desconhecidos de **maior risco operacional** (identidade dos consumidores das filas ETL/WETL, comportamento sob concorrência real de `LX_SEQUENCIAL`, estratégia de rollback cross-sistema) são de natureza server-side/trigger e **não são alcançáveis pelo código de tela VFP** — permanecem tão desconhecidos quanto antes. Por isso a decisão do gate permanece **LIBERADO COM RESTRIÇÕES**, mas as restrições ficam mais precisas e o desenho do contrato do Adapter pode avançar com confiança sensivelmente maior nos campos-chave de identidade (sequencial, chaves, nome, duplicidade).

---

## 7. Decisão final do gate

### **LIBERADO COM RESTRIÇÕES** (reafirmado após o adendo de código-fonte real)

**Justificativa por evidência**: o discovery acumulado (múltiplas rodadas reais de acesso READ-ONLY ao SOMA_DESENV) produziu uma base de conhecimento excepcionalmente detalhada e bem classificada por proveniência (Fato/Padrão/Interpretação/Desconhecido) sobre a estrutura física de `CADASTRO_CLI_FOR`/`FORNECEDORES`, as 11 triggers ativas, o mecanismo de `LX_SEQUENCIAL`, e o padrão recorrente (Nível 1, 4-5 de 5 amostras) de escrita usado por integrações automatizadas reais. Essa base é suficiente para **iniciar o planejamento arquitetural detalhado do Adapter** (desenho de contrato, DTOs físicos, mapeamento de campos, esqueleto de taxonomia de erros) com alta confiança.

Porém, a própria Work Order B2.9 e o ADR-0023 já condicionam **qualquer escrita real em produção/desenvolvimento** a uma sessão de validação com especialista Visual Linx — e a evidência confirma que essa condição continua necessária, não apenas por precaução formal, mas por **desconhecidos concretos e materiais** identificados nesta consolidação (seção 6): nenhuma das 5 rotinas lidas foi confirmada como o processo oficial da tela manual; a identidade dos consumidores das duas filas de replicação é desconhecida; o critério de duplicidade não converge entre as amostras; e existe uma anomalia de sequencial não explicada. Escrever no Linx sem resolver esses pontos arrisca duplicidade real, efeitos colaterais indesejados em SAP/RH/filas de ETL, e uso do sequencial errado — riscos que o próprio discovery já advertiu de forma consistente em todas as suas rodadas.

**Restrições exatas** (condições que devem ser satisfeitas antes de qualquer implementação de escrita real, mesmo em ambiente de desenvolvimento):

1. **Planejamento arquitetural do Adapter é autorizado a avançar agora** (desenho de interfaces, DTOs físicos, esqueleto de taxonomia de erros, testes unitários com mapeamento simulado) — sem qualquer chamada real ao SOMA_DESENV.
2. **Nenhuma escrita real (INSERT/UPDATE) no SOMA_DESENV — nem em ambiente de desenvolvimento, nem de teste — antes de uma sessão dedicada com especialista Visual Linx** que responda, no mínimo, às 10 perguntas da seção 5, com destaque para a existência (ou não) de uma rotina oficial de cadastro manual e a identidade dos consumidores das filas de ETL/WETL.
3. **`LX_SEQUENCIAL` nunca deve ser executado fora dessa sessão de validação** — mesmo em modo `@UPDATE_SEQUENCIAL=0` (leitura), a menos que o especialista confirme que a leitura não tem efeito colateral algum sobre processos reais.
4. **Nenhum vocabulário Linx pode vazar para o domínio +Compras** durante o planejamento — a fronteira arquitetural já formalizada em ADR-0023/seção 4.17 deste gate deve ser respeitada rigorosamente desde os primeiros esboços de código.
5. **A B2.9 permanece Draft/Bloqueada** — este gate não a libera para ativação; a ativação formal continua sendo decisão exclusiva do Product Owner, condicionada à restrição 2.
6. **Qualquer teste de integração real deve usar exclusivamente CNPJs sintéticos/fictícios, nunca dados reais de fornecedores**, e apenas em ambiente SOMA_DESENV de teste isolado (nunca o de desenvolvimento compartilhado nem produção) — condicionado também à restrição 2.

Se a evidência tivesse mostrado uma procedure clara e não-ambígua de cadastro manual confirmada, ou se todos os itens da seção 6 já estivessem resolvidos, a classificação apropriada seria LIBERADO PARA IMPLEMENTAÇÃO. Se o discovery tivesse deixado dúvidas sobre a própria estrutura física básica (schema, triggers, chaves), a classificação apropriada seria BLOQUEADO. Nem um nem outro é o caso — a estrutura física está solidamente mapeada; o que falta é validação humana específica sobre o processo oficial e os consumidores externos, exatamente o padrão de "conhecimento forte, mas não aprovado" que caracteriza LIBERADO COM RESTRIÇÕES.

---

## 8. Confirmações obrigatórias

- **Zero alteração de código de produção**: nenhum arquivo em `backend/src/`, `frontend/web/src/`, ou equivalente foi criado, editado ou removido nesta sessão. Apenas leitura (Read/grep/find) e a criação deste próprio documento de gate.
- **Zero escrita no SOMA_DESENV**: nenhuma ferramenta de acesso a banco de dados foi encontrada disponível nesta sessão (`ToolSearch` consultado e sem resultado relevante) — portanto nenhuma tentativa de conexão, leitura ou escrita foi feita nesta sessão. Toda a evidência de SOMA_DESENV citada neste gate vem de rodadas anteriores e distintas, já documentadas nos artefatos de discovery lidos.
- **Nenhuma credencial, connection string ou dado real de fornecedor/CNPJ** foi incluído neste documento.
- A Work Order `.ai/work-orders/backlog/fase-b/B2.9-AdapterLinxFornecedorCnpj.md` **não foi movida** para `active/` e seu status ("Draft — Bloqueada") **não foi alterado**.

---

## 9. Snapshot de conhecimento — avaliação de atualização

O snapshot `docs/agents/ai-factory/temp/LinxKnowledge-Fornecedor-Discovery-Snapshot.md` já contém, de forma completa e bem estruturada, todo o conhecimento reutilizável levantado nas rodadas de discovery anteriores (triggers, sequencial, entidade multiuso, padrão recorrente por nível de confiança, playbook de discovery, desconhecidos). Este gate é uma **consolidação/síntese decisória** desse conhecimento já existente, cruzada com a Work Order B2.9 e o ADR-0023, para produzir um veredito de liberação — **não gerou nenhum fato novo sobre o Linx físico** (nenhuma nova consulta ao SOMA_DESENV foi feita nesta sessão, conforme seção 3). Por esse motivo, **o snapshot não foi alterado**: não há conhecimento genuinamente novo e reutilizável a incorporar, apenas uma decisão de gate que consome o conhecimento já registrado.

---

## 10. Próximos passos (fora do escopo de execução deste gate)

Cabem exclusivamente ao Product Owner humano, e não são iniciados por este gate:

- Agendar e conduzir a sessão de validação com especialista Visual Linx (seção 5).
- Decidir se e quando reabrir a B2.9 para planejamento arquitetural detalhado (restrição 1) antes mesmo da sessão com o especialista.
- Decidir se vale abrir uma Work Order dedicada para resolver o GAP de infraestrutura dos Agents Linx (seção 4.1) e ingerir o conhecimento do snapshot como `LinxKnowledgeEntry` real.
