# AgentLearningV1 — Politicas Canonicas De Aprendizado E Evolucao + Caso Linx PROG/OP/PED

Status: accepted
Data: 2026-08-27
Escopo: `agents/`, `backend/src/BlueprintOS.Core/AI/Governance/`, `tools/agents/agent-factory-v2.js`, testes .NET e Node associados.

## 1. Resumo Executivo

Esta tarefa teve dois objetivos independentes:

1. Formalizar e implementar, com codigo e testes reais, duas politicas canonicas aplicaveis a todos os Agents e a qualquer IA/executor (Codex, Claude, ChatGPT ou futuros): **User Artifact Learning Policy** e **Capability Gap & Agent Evolution Policy**.
2. Processar o caso real de negocio "PROG/OP/PED" (ajuste de grade de producao/compra) usando o Linx Agent.

Resultado do item 1: **CONFIRMED**. As duas politicas existem como documentos canonicos, sao referenciadas por `AGENT_CONTRACT.md`/`EXECUTION_POLICY.md`, sao verificadas pela Agent Factory v2 em `AUDIT`, e sao implementadas com codigo real em `backend/src/BlueprintOS.Core/AI/Governance/` com 15 testes automatizados cobrindo as 12 regras exigidas.

Resultado do item 2 (atualizado nesta sessao): os dois artefatos reais (planilha `New Way - Size 34 - layout programacao_v2 (1).xlsx` e SQL historico `AJUSTA GRADE OP-PROG-COMPRAS.sql`) foram localizados em `downloads/showcase_produtos/`, lidos integralmente e preservados sem alteracao. A planilha foi analisada programaticamente (estrutura, grade, duplicidades) e o SQL historico foi auditado linha a linha (ver secao 7). O resultado, porem, e **KNOWLEDGE_GAP bloqueante** e nao uma solucao final: a classificacao `PROG` no mecanismo historico e um fallback por exclusao (nao uma regra positiva), a grade da planilha atual tem 6 posicoes contra 7 esperadas pelo SQL historico, e nao ha conexao Linx/SOMA_DESENV disponivel nesta sessao para validar schema/procedures atuais nem calcular o Delta real. Por instrucao explicita da tarefa (nunca escolher silenciosamente diante de ambiguidade funcional), a analise **parou** neste ponto: nenhuma solucao SQL foi gerada, nenhum dado foi inventado, e tres perguntas objetivas foram registradas para o Product Owner (secao 7.9).

## 2. Baseline (Inspecao Real Do Repositorio)

| # | Item | Expectativa do usuario | Valor real confirmado | Divergencia |
|---|------|------------------------|------------------------|-------------|
| 1 | Numero de Agents | 8 | **8** (`agents/*/agent.yaml`: agent-factory, echo-agent, knowledge-agent, linx-database-specialist-agent, linx-erp-specialist-agent, security-lgpd-agent, showcase-agent, wise-agent) | Nenhuma |
| 2 | Total de capabilities | 15 | **15** (`capability_ownership` somado por agente: agent-factory=7, echo-agent=1, knowledge-agent=1, linx-database-specialist-agent=2, linx-erp-specialist-agent=1, security-lgpd-agent=1, showcase-agent=1, wise-agent=1) | Nenhuma |
| 3 | WARN na ultima auditoria Agent Factory v2 conhecida | 12 | **Divergente conforme a fonte.** O snapshot estatico mais recente em `docs/audits/AgentFactoryV2-AuditResults.json` (timestamp 2026-08-27T16:41:11Z, presente no repo antes desta tarefa) registra `warn: 8` agentes com status WARN e `warning: 18` findings totais (AFV2-GOV-001, AFV2-TEST-001, AFV2-OBS-001, AFV2-GATEWAY-001). Ao **executar `node tools/agents/agent-factory-cli.js AUDIT` agora** (antes e depois das mudancas desta tarefa), o resultado real e `status: WARN`, **8 agentes com status WARN**, e **12 findings WARNING totais** (apenas AFV2-GOV-001 e AFV2-GATEWAY-001; AFV2-TEST-001/AFV2-OBS-001 nao dispararam porque os manifests atuais ja declaram safety tests e observability suficientes). Isto sugere que a Factory evoluiu entre o snapshot estatico e agora, ou que o snapshot estatico ja estava desatualizado antes desta tarefa comecar. **Nenhum finding foi maquiado ou suprimido por esta tarefa** — antes e depois das mudancas desta tarefa o resultado do audit e identico (12 findings, ver secao 9). | Numero "12" bate com o audit ao vivo hoje, mas nao bate com o snapshot estatico mais recente do repo (18). Reportado sem forcar consistencia. |
| 4 | `soma-database-write-proposal` existe em `linx-database-specialist-agent` | Sim | **CONFIRMED** — `agents/linx-database-specialist-agent/agent.yaml`, `capability_ownership.soma-database-write-proposal.responsible_agent_id: linx-database-specialist-agent` | Nenhuma |
| 5 | `can_execute_write: false` nesse agente | Sim | **CONFIRMED** — `agents/linx-database-specialist-agent/agent.yaml:139` `can_execute_write: false` | Nenhuma |
| 6 | `LIVE_EXECUTION` desabilitado no codigo | Sim | **CONFIRMED** — `backend/src/BlueprintOS.Core/AI/Governance/ToolGateway.cs:34`: `if (request.ExecutionMode == GovernedExecutionMode.LiveExecution) reasons.Add("LIVE_EXECUTION_DISABLED");` — qualquer requisicao em modo Live e sempre bloqueada. | Nenhuma |
| 7 | Tool Gateway e dry-run only | Sim | **CONFIRMED** — `ToolGateway.InvokeAsync` sempre chama `adapter.DryRunAsync(...)` (linha 26) e nunca um caminho de execucao real; retorno inclui sempre `DRY_RUN_ONLY`/`NO_EXTERNAL_EXECUTION`. | Nenhuma |
| 8 | Nenhum arquivo do fluxo diario Linx/WISE deveria ser alterado por esta tarefa | Sim | **Nao alterado por esta tarefa.** Porem, `git status` no momento desta auditoria mostra `.ai/context/linx-wise-daily-integration.md`, `docs/operations/LinxWiseDailyIntegrationRunbook.md` e `scripts/linx_wise_daily_integration.py` **ja modificados no worktree por trabalho nao relacionado a esta tarefa** (mudancas funcionais no script de conciliacao WISE, nao geradas por esta sessao de trabalho desta politica). Esses arquivos **nao foram tocados, nao foram adicionados ao stage e nao fazem parte do(s) commit(s) desta tarefa**. | Divergencia de estado do worktree preexistente, fora do escopo desta tarefa; reportada sem reverter (evitar destruir trabalho de terceiros fora do escopo declarado). |

## 3. Politica A — User Artifact Learning Policy

Vive em `agents/USER_ARTIFACT_LEARNING_POLICY.md`. Define: artefato de usuario (SQL, codigo, script, planilha, procedure, query, shell, Python, JS, C#, documento, exemplo, config, implementacao historica, codigo gerado por outra IA) e **evidencia**, nunca comando executavel; fornecer artefato **nunca constitui approval**; fluxo obrigatorio de 14 etapas (estudar -> extrair regras -> comparar -> validar -> identificar lacunas -> perguntar -> aprender -> projetar solucao propria -> validar -> governar -> propor execucao); rotulos de proveniencia (`USER_PROVIDED_ARTIFACT`, `DATABASE_SCHEMA_VALIDATION`, `RUNBOOK`, `CODE_INSPECTION`, `PRODUCT_OWNER_CLARIFICATION`, `EMPIRICAL_VALIDATION`); niveis de confianca (`Confirmed`, `Inferred`, `HistoricalReference`, `NeedsValidation`, `Unknown`), com regra explicita de que inferencia nunca vira `Confirmed` automaticamente; regra de que segredo nunca entra no knowledge store.

Implementacao de codigo: `backend/src/BlueprintOS.Core/AI/Governance/UserArtifactLearningPolicy.cs`, com modelos em `backend/src/BlueprintOS.Core/AI/Governance/Models/UserArtifactLearningModels.cs`. Metodos: `Classify(UserArtifact)` (sempre Evidence ou HistoricalReference, nunca comando, nunca approval), `EvaluatePersistence(LearnedKnowledgeItem)` (recusa segredo, recusa item nao-reutilizavel, recusa item incompleto), `PromoteConfidence(...)` (so promove `Inferred` -> `Confirmed` com proveniencia direta nova).

## 4. Politica B — Capability Gap & Agent Evolution Policy

Vive em `agents/CAPABILITY_GAP_AND_AGENT_EVOLUTION_POLICY.md`. Formaliza (sem contradizer) a secao "Capability Gap" ja existente em `EXECUTION_POLICY.md`. Fluxo `REQUEST -> REGISTRY -> AGENT OWNER? -> CAPABILITY COBERTA? -> KNOWLEDGE SUFICIENTE?`; Knowledge Gap e Capability Gap sempre interrompem o fluxo; ausencia de owner natural gera **proposta** de novo Agent, nunca criacao automatica; ordem de preferencia aprender > evoluir > criar; proibicao explicita de autoexpansao de capabilities sensiveis/escrita/destruicao/bypass por qualquer Agent, incluindo `agent-factory`.

Implementacao de codigo: `backend/src/BlueprintOS.Core/AI/Governance/CapabilityGapAndAgentEvolutionPolicy.cs`, com modelos em `backend/src/BlueprintOS.Core/AI/Governance/Models/CapabilityGapModels.cs`. Metodos: `Resolve(CapabilityRequest)` (retorna `KnowledgeGap`, `CapabilityGap`, `NoNaturalOwnerProposeNewAgent` ou `Covered`, sempre com `AutomaticExecutionAllowed = false`), `EvaluateEvolution(AgentEvolutionProposal)` (mudanca material exige `HumanApprovalGranted` + `ApprovedBy`), `EvaluateNewAgentProposal(NewAgentProposal)` (exige evidencia do gap, lista de Agents avaliados/rejeitados, e aprovacao humana antes de permitir `CanCreate = true`).

## 5. Onde Vivem E Como Sao Herdadas Por Agents Futuros

Decisao: dois documentos canonicos novos ao lado de `AGENT_CONTRACT.md`/`EXECUTION_POLICY.md` (`agents/USER_ARTIFACT_LEARNING_POLICY.md`, `agents/CAPABILITY_GAP_AND_AGENT_EVOLUTION_POLICY.md`), referenciados por ambos os documentos de precedencia superior:

- `agents/AGENT_CONTRACT.md` ganhou a secao **"Politicas Canonicas Relacionadas"** (linhas 35-40), citando os dois arquivos e resumindo seu conteudo.
- `agents/EXECUTION_POLICY.md` ganhou uma frase na abertura (linha 9) referenciando os dois arquivos como detalhamento, sem contradizer, das secoes "Capability Gap" e "Evolucao E Criacao De Agents" ja existentes.
- `tools/agents/agent-factory-v2.js` ganhou o metodo `canonicalPolicyFindings()`, chamado dentro de `audit()`, que gera o finding `AFV2-POLICY-001` (WARNING) caso qualquer um dos dois arquivos esteja ausente **ou** nao esteja referenciado em `AGENT_CONTRACT.md`. Isso torna a existencia/referencia das politicas **auditavel automaticamente** a cada `AUDIT`.

**Nenhuma mudanca foi feita em `agents/agent.schema.json`.** O schema ja fixava estruturalmente, antes desta tarefa: `gap_policy.direct_bypass_allowed` (`const: false`), `gap_policy.explicit_human_approval_required_for_new_agent` (`const: true`), `gap_policy.material_capability_change_requires_human_approval` (`const: true`), `delegation.bypass_allowed` (`const: false`), e um `knowledge.provenance_labels` de lista de strings livre o suficiente para os seis rotulos de proveniencia da Politica A. Ou seja, **todo `agent.yaml` valido ja herda estruturalmente as garantias centrais das duas politicas**, sem necessidade de campo novo. A heranca ocorre por (a) referencia documental obrigatoria na cadeia de precedencia e (b) checagem automatizada `AFV2-POLICY-001` da Agent Factory v2 — nenhuma mudanca de schema foi necessaria, portanto **nao houve CONTRACT GAP** nesta tarefa.

## 6. Comportamento Provider-Agnostic

**CONFIRMED.** Nenhuma classe, metodo ou campo em `UserArtifactLearningPolicy.cs` ou `CapabilityGapAndAgentEvolutionPolicy.cs` recebe ou usa um parametro de "provider"/"executor". O teste `Rule11_Behavior_IsProviderAgnostic` (em `backend/tests/BlueprintOS.UnitTests/Core/AI/Governance/UserArtifactLearningAndCapabilityGapPolicyTests.cs`) verifica isso estruturalmente por reflexao (nenhum membro publico contem "provider", "codex", "claude", "chatgpt", "openai" ou "anthropic" no nome) e comportamentalmente (mesma entrada produz mesma saida em duas instancias independentes). O texto das duas politicas tambem declara explicitamente escopo "qualquer IA, modelo, executor ou Agent... independente de provider".

## 7. Caso Real PROG/OP/PED — KNOWLEDGE_GAP (bloqueante, ver 7.9)

Status anterior (`WAITING_FOR_EVIDENCE`) superado: os dois artefatos foram localizados nesta sessao em `downloads/showcase_produtos/`:

- `New Way - Size 34 - layout programacao_v2 (1).xlsx`
- `AJUSTA GRADE OP-PROG-COMPRAS.sql`

Ambos foram lidos programaticamente e integralmente, sem alteracao. Classificacao conforme `USER_ARTIFACT_LEARNING_POLICY.md`: **EVIDENCE / KNOWLEDGE SOURCE / USER_PROVIDED_ARTIFACT / HISTORICAL_REFERENCE** — nenhum dos dois constitui approval, runbook ou verdade canonica.

### 7.1 Estrutura real da planilha — CONFIRMED (leitura direta)

Duas abas: `_dados` (15 colunas, `A1:O78`) e `Layout` (10 colunas, `A1:J78`, subconjunto derivado de `_dados`). 77 linhas de dados (sem cabecalho).

Colunas de `_dados`: `PRODUTO`, `DESC_PRODUTO`, `PROGRAMACAO`, `PO_PEDIDO_COMPRA`, `CANAL_ARQUIVO` (`WHOLESALE`/`RETAIL`), `CANAL_TEMP_COD` (`ATACADO`/`VAREJO`), `COR_PRODUTO`, `GRADE_ATUAL`, `Q_34`, `Q_36`, `Q_38`, `Q_40`, `Q_42`, `Q_44`, `TOTAL_ORIGEM`.

Grade detectada nesta execucao: **6 posicoes** (`Q_34..Q_44`, tamanhos 34/36/38/40/42/44), todas com `GRADE_ATUAL = '36-44'` (unico valor distinto na coluna). `TOTAL_ORIGEM` bate exatamente com a soma de `Q_34..Q_44` em 100% das linhas com dado presente (0 divergencias).

Produtos unicos: **39**. Combinacoes produto+cor unicas: **42**. Valores de `PROGRAMACAO`: 5 (`PA_ATC_MF_INV27_IMP_JAS`, `PA_VRJ_MF_INV27_IMP_JAS`, `PA_VRJ_MF_INV27_IMP_JAS_F`, `PA_ATC_MF_INV27_IMP_FE`, `PA_VRJ_MF_INV27_IMP_FE`). Nulos em `PRODUTO`: 0.

**Inconsistencia de dados encontrada (ERRO_DE_DADOS candidato):** `PO_PEDIDO_COMPRA` tem 77 valores nao-nulos mas apenas 71 unicos — 6 numeros de pedido aparecem em 2 linhas cada, sempre para o mesmo produto/programacao mas cores diferentes. Em 5 desses 6 casos as quantidades por tamanho sao identicas nas duas linhas (padrao compativel com "mesmo pedido, linhas de cor diferentes"); em 1 caso (`PO 1741979`, produto `15.29765`, cores `09204` e `5465`) as quantidades **divergem** entre as duas linhas do mesmo PO. Isso e evidencia, nao correcao automatica (regra da secao 7).

### 7.2 SQL historico — leitura integral, classificado `HISTORICAL_REFERENCE` + `USER_PROVIDED_ARTIFACT`

O script tem 3 blocos logicos, todos operando sobre uma tabela `#TEMP_AJUSTE` fisica/temporaria montada a partir de `[192.168.9.98].SOMA_DESENV.DBO.TEMP_AJUSTES_PROG_0710` (nome tratado apenas como referencia historica, **nao reutilizado**):

1. **Montagem de `#TEMP_AJUSTE`**: `JOIN` da tabela de import da planilha com `PRODUCAO_PROG_PROD` (por `PROGRAMACAO+PRODUTO+COR_PRODUTO`) e `LEFT JOIN` com uma uniao `UNION ALL` de duas subconsultas — uma sobre `PRODUCAO_ORDEM`/`PRODUCAO_ORDEM_COR` (rotulada `'OP  '`) e outra sobre `COMPRAS`/`COMPRAS_PRODUTO` (rotulada `'PED'`). O campo `TIPO` do resultado final e `ISNULL(B.TIPO,'PROG')`.
2. **Bloco "RODAR PARA OP E PROGRAMACAO"**: cursor sobre `#TEMP_AJUSTE` filtrando `TIPO IN ('OP','PROG')` e delta != 0 contra `PRODUCAO_ORDEM_COR`/`PRODUCAO_PROG_PROD`; chama `EXEC LX_ANM_GERA_OS_ALTERACAO_PCP` (quando `TIPO='OP'`) ou `EXEC LX_ANM_AJUSTA_PROGRAMACAO_PROD` (quando `TIPO='PROG'`), ambos dentro de `TRY/CATCH`, atualizando uma coluna de status `AJUSTADO` na propria `#TEMP_AJUSTE`.
3. **Bloco "RODAR PARA COMPRAS"**: cursor sobre `#TEMP_AJUSTE` filtrando `TIPO='PED'` e delta != 0 contra `COMPRAS_PRODUTO`; faz `UPDATE` direto em `COMPRAS_PRODUTO` (colunas `CO1..CO7`, `CE1..CE7`, `QTDE_ORIGINAL`, `QTDE_ENTREGAR`, `VALOR_ORIGINAL`, `VALOR_ENTREGAR`) e depois `EXEC LX_MOVIMENTA_COMPRAS_PA` + `EXEC LX_RECALCULO_RESERVA_MATERIAIS`, tambem em `TRY/CATCH`.

### 7.3 Auditoria do SQL historico — riscos e padroes suspeitos (achado, nao corrigido automaticamente)

- **`TIPO='PROG'` e um DEFAULT por exclusao, nao uma regra positiva.** `ISNULL(B.TIPO,'PROG')` classifica como `PROG` qualquer linha da planilha que nao encontrou correspondencia em OP nem em Pedido de Compras — nao existe condicao propria que afirme "isto e Programacao". Ver Knowledge Gap 7.9.a.
- **Grade de 7 posicoes no SQL (`TAM_1..TAM_7`, `CO1..CO7`) vs 6 posicoes na planilha atual (`Q_34..Q_44`).** O `Layout`/`_dados` desta execucao nao possui uma 7a posicao. Ver Knowledge Gap 7.9.b.
- **`JOIN` (nao `LEFT JOIN`) com `PRODUCAO_PROG_PROD` no passo 1** descarta silenciosamente qualquer linha da planilha sem programacao correspondente ja cadastrada — sem log de "nao encontrado".
- **`UPDATE` amplo sem transacao explicita nem pre-visualizacao obrigatoria** no bloco 3 (`UPDATE COMPRAS_PRODUTO ... WHERE PEDIDO = @ORDEM_PRODUCAO AND PRODUTO = @PRODUTO AND COR_PRODUTO = @COR_PRODUTO`, dentro de `TRY/CATCH` mas sem `BEGIN TRAN`/`COMMIT`/`ROLLBACK` explicitos, nem `SET XACT_ABORT ON`).
- **`(NOLOCK)`** nos `JOIN`s de descoberta de OP/PED — leitura suja, risco de dado transitorio incorreto na classificacao (fora de escopo desta tarefa alterar).
- **Hardcode de tabela de staging** (`TEMP_AJUSTES_PROG_0710`) e de nome anterior (`TEMP_AJUSTES_PROG_MF_0701`, no bloco comentado) — nomenclatura amarrada a uma execucao especifica, sem padrao reutilizavel.
- **Cursor + `RAISERROR ... WITH NOWAIT` por linha**: mecanismo funcional mas serial, sem lote; aceitavel para volumes pequenos (esta planilha tem 77 linhas) mas nao teve evidencia de limite superior testado.
- **`CASE WHEN A.TIPO_PROCESSO <> 1 THEN 0 ELSE QTDE_EM_PRODUCAO END`** na subconsulta de OP: regra de negocio implicita (o que e `TIPO_PROCESSO = 1`?) nao documentada no proprio SQL — tratado como `HISTORICAL_REFERENCE`, nao validado.
- Nao ha `SELECT` de pre-visualizacao nem pos-validacao de cardinalidade explicitos fora dos comentarios (`--select * from #TEMP_AJUSTE ...`), que estao desativados.

### 7.4 Conhecimento atual do Linx Agent — comparacao

Busca em todo o repositorio (`.md`, `.cs`, `.json`, `.ai/context/knowledge.md`) por `PRODUCAO_PROG_PROD`, `PRODUCAO_ORDEM`, `PRODUCAO_ORDEM_COR`, `COMPRAS_PRODUTO`, `LX_ANM_GERA_OS_ALTERACAO_PCP`, `LX_ANM_AJUSTA_PROGRAMACAO_PROD`, `LX_MOVIMENTA_COMPRAS_PA`, `LX_RECALCULO_RESERVA_MATERIAIS`: **zero ocorrencias**. Nao ha conhecimento previo persistido no `linx-database-specialist-agent` sobre nenhuma dessas tabelas/procedures. Classificacao: **UNKNOWN** (nao apenas `NEEDS_VALIDATION`) para todo o schema/procedures citados no SQL historico.

### 7.5 Validacao no Linx (secao 9 da tarefa) — RESOLVIDO (atualizado em etapa posterior, ver `docs/audits/AgentContractV1.1-ExecutionPolicy-Credenciais.md` secao 25)

**Atualizacao:** o gap descrito abaixo era de descoberta/uso dentro desta sessao de chat, nao de arquitetura. Uma etapa dedicada investigou `ConnectionStrings:ErpConnection` e confirmou que o mecanismo local (`dotnet user-secrets`, `B1ConnectivityValidator`, comando `dotnet run --project backend/src/BlueprintOS.Api -- validate-b1-connectivity`) ja existia e ja estava configurado nesta maquina. Resultado real, read-only (`SELECT 1` + `SELECT SUSER_SNAME()`, sem nenhuma escrita): **CONNECTION STATUS = READY** para `ErpConnection`/`SOMA_DESENV`. Detalhes completos, comando exato para um novo desenvolvedor configurar sua propria credencial, e o que fica versionado vs. local: `docs/audits/AgentContractV1.1-ExecutionPolicy-Credenciais.md`, secao 25. **Isto NAO reabre nem resolve os gaps 7.9.a e 7.9.b** (regra PROG por exclusao e grade de 6 vs 7 posicoes) — apenas o 7.9.c (acesso ao banco) deixa de ser um bloqueio. A analise PROG/OP/PED em si permanece parada aguardando resposta as perguntas 7.9.a e 7.9.b antes de calcular o Delta real ou propor solucao.

Descricao original do gap (mantida para historico): `agents/linx-database-specialist-agent/agent.yaml` declara `connections.profiles.linx-erp-read-only` com `configuration_reference: ConnectionStrings:ErpConnection` e `credential_policy.prompt_for_secret_allowed: false`. Na sessao original **nao houve tentativa de usar o mecanismo local ja existente antes de declarar o gap** — nenhuma credencial foi solicitada no chat em nenhum momento, entao a regra de seguranca foi respeitada, mas a investigacao ficou incompleta.

### 7.6 Procedures citadas — tratadas como evidencia, nao validadas

`LX_ANM_GERA_OS_ALTERACAO_PCP`, `LX_ANM_AJUSTA_PROGRAMACAO_PROD`, `LX_MOVIMENTA_COMPRAS_PA`, `LX_RECALCULO_RESERVA_MATERIAIS`: conhecidas apenas pela assinatura de chamada visivel no SQL historico (parametros posicionais + `@EXECUTADO OUTPUT`/`@RETORNO OUTPUT` nas duas primeiras). Definicao atual, efeitos colaterais, comportamento transacional e se continuam sendo o mecanismo correto: **UNKNOWN**, por falta de acesso ao schema (7.5). Nenhuma foi executada.

### 7.7 PROG / OP / PED — regras extraidas (com proveniencia) e ambiguidade bloqueante

| Tipo | Regra de identificacao (conforme SQL historico) | Fonte | Tabelas relacionadas | Mecanismo de ajuste aparente |
|---|---|---|---|---|
| OP | Existe `PRODUCAO_ORDEM`/`PRODUCAO_ORDEM_COR` cujo `PROGRAMACAO+PRODUTO+COR_PRODUTO` bate com a linha da planilha | `HISTORICAL_REFERENCE` (linhas 22-25 do SQL) | `PRODUCAO_ORDEM`, `PRODUCAO_ORDEM_COR` | `EXEC LX_ANM_GERA_OS_ALTERACAO_PCP` |
| PED | Existe `COMPRAS`/`COMPRAS_PRODUTO` cujo `PROGRAMACAO+PRODUTO+COR_PRODUTO` bate com a linha da planilha | `HISTORICAL_REFERENCE` (linhas 27-29 do SQL) | `COMPRAS`, `COMPRAS_PRODUTO` | `UPDATE COMPRAS_PRODUTO` direto + `EXEC LX_MOVIMENTA_COMPRAS_PA` + `EXEC LX_RECALCULO_RESERVA_MATERIAIS` |
| PROG | **Nao existe regra positiva propria no SQL historico** — e o valor `ISNULL(...,'PROG')` quando a linha nao bateu nem com OP nem com PED | `HISTORICAL_REFERENCE`, `INFERRED` (por exclusao) | `PRODUCAO_PROG_PROD` | `EXEC LX_ANM_AJUSTA_PROGRAMACAO_PROD` |

**Esta classificacao permanece AMBIGUA para PROG** por definicao da propria tarefa (secao 11: "se alguma classificacao continuar ambigua, PARE e pergunte ao Product Owner"). Ver pergunta objetiva em 7.9.a.

### 7.8 Dataset funcional (modelo conceitual, sem SQL) — CONFIRMED por leitura direta

Cada linha da planilha pode ser modelada como: `Produto`, `Cor`, `Programacao`, `Identificador funcional` (`OP` / `Pedido de Compra` / nenhum -> `PROG`), `Grade atual detectada` (6 posicoes: `Q_34..Q_44`), `Grade solicitada` (mesma planilha — nao ha, nesta execucao, uma "grade atual do sistema" capturada localmente; ela so existe no banco Linx, inacessivel por 7.5), `Delta` (so calculavel apos 7.5), `Mecanismo Linx aplicavel` (conforme 7.7), `Status`, `Observacao`. **Sem acesso ao banco (7.5), o Delta real nao pode ser calculado nesta sessao** — apenas a estrutura da planilha (lado "solicitado") esta confirmada; o lado "atual" (banco) e `UNKNOWN`.

### 7.9 Knowledge Gaps bloqueantes — PARADO aqui, aguardando resposta do Product Owner

**a) Classificacao PROG.** O que sabemos: no SQL historico, `TIPO='PROG'` e o resultado de `ISNULL(B.TIPO,'PROG')` — ou seja, e um fallback aplicado a qualquer linha da planilha sem OP nem Pedido de Compras correspondente, e nao uma regra positiva. O que nao sabemos: se isso e intencional (toda linha sem OP/Pedido aberto e, por definicao de negocio, apenas um ajuste de programacao futura) ou se deveria existir uma condicao propria (ex.: existir registro em `PRODUCAO_PROG_PROD` sem OP emitida) que hoje so funciona "por acaso" porque cobre os casos restantes. Evidencia: linhas 17-32 do SQL. Impacto: se a suposicao estiver errada, uma linha que na verdade e "nao encontrado" (produto/programacao sem cadastro correspondente em nenhuma das 3 tabelas) seria classificada erroneamente como `PROG` em vez de `NAO_ENCONTRADO`. **Pergunta objetiva:** confirma que "sem OP e sem Pedido correspondente" deve sempre significar "PROG", mesmo quando `PRODUCAO_PROG_PROD` tambem nao tiver o registro (hoje a JOIN nao-LEFT com `PRODUCAO_PROG_PROD` no passo 1 do SQL historico ja filtraria esse caso antes de chegar em `TIPO`, mas isso nao esta confirmado como intencional)?

**b) Grade de 6 vs 7 posicoes.** O que sabemos: a planilha desta execucao tem exatamente 6 posicoes de grade (`Q_34..Q_44` / `TAM_1..TAM_6` na aba `Layout`). O SQL historico e escrito para 7 posicoes (`TAM_1..TAM_7`, `CO1..CO7`). O que nao sabemos: se a 7a posicao deve ser tratada como zero/inexistente nesta execucao especifica, ou se existe um tamanho adicional (ex. 46) que deveria constar da planilha e nao consta. Evidencia: aba `Layout`, linha 1 (`TAM_1..TAM_6`, sem `TAM_7`) vs SQL linhas 10-16 (`TAM_1..TAM_7`). Impacto: qualquer solucao nova que reaproveite a mesma convencao posicional do SQL historico pode gravar/comparar a posicao errada de tamanho se a correspondencia `TAM_1..TAM_6 -> quais tamanhos reais` nao for a mesma assumida pelo mecanismo Linx atual. **Pergunta objetiva:** a grade `36-44` desta planilha (6 tamanhos) deve mapear para `TAM_1..TAM_6` deixando `TAM_7=0`, ou existe uma tabela de correspondencia tamanho->posicao vigente no Linx que precisa ser consultada (e nesse caso, onde)?

**c) Acesso ao banco — RESOLVIDO.** ~~Nao ha conexao Linx/SOMA_DESENV autorizada disponivel nesta sessao (7.5).~~ Investigado e resolvido em etapa posterior: o mecanismo local (`dotnet user-secrets` + `B1ConnectivityValidator`) ja existia e ja estava configurado. **CONNECTION STATUS = READY** (read-only, `SELECT 1` + `SELECT SUSER_SNAME()`, confirmado nesta maquina). Ver `docs/audits/AgentContractV1.1-ExecutionPolicy-Credenciais.md` secao 25 para o relato completo. Isto remove o bloqueio de *acesso*, mas nao substitui a validacao real de schema/procedures/Delta especifica do caso PROG/OP/PED, que continua nao feita nesta etapa (fora de escopo desta investigacao de conexao) e depende ainda de (a) e (b) abaixo para ser conduzida com seguranca.

Enquanto (a) e (b) nao forem respondidas, **nao e seguro produzir a solucao propria (secao 19-21 da tarefa)** nem calcular o impact analysis quantitativo completo (secao 16) — os numeros que dependem do banco (Delta, zero-delta, nao encontrados, ambiguos, bloqueados, unidades atuais) ficam **UNKNOWN**, nao inventados. A conexao pronta (c) significa que, assim que (a) e (b) forem esclarecidos, o calculo do Delta real podera ser feito sem novo bloqueio de acesso.

Artefatos necessarios para desbloquear a analise (todos ausentes — `UNKNOWN`):

- **(A) Planilha real de ajustes de grade** (formato .xlsx/.csv com as colunas/linhas reais usadas pelo Product Owner para o ajuste de PROG/OP/PED).
- **(B) SQL historico/modelo de referencia** (script(s) ou procedure(s) historicamente usados para aplicar este tipo de ajuste no banco Linx/SOMA, fornecido como evidencia, nunca como comando a executar).
- **(C) Explicacao funcional do Product Owner** sobre o que PROG/OP/PED representa no dominio de producao/compra e qual e a regra de negocio esperada para o ajuste de grade.

Ate a chegada desses tres artefatos, o Linx Agent permanece formalmente em `WAITING_FOR_EVIDENCE` para este caso. Nenhuma das perguntas abaixo pode ser respondida de outra forma sem violar a User Artifact Learning Policy:

- O que seria aprendido do SQL historico: **N/A — UNKNOWN** (sem evidencia).
- O que seria aprendido da planilha: **N/A — UNKNOWN**.
- Inconsistencias detectadas: **N/A — UNKNOWN**.
- Schema validado: **NEEDS_VALIDATION** — apenas a existencia estrutural do `linx-database-specialist-agent` e de sua capability `linx-database-analysis`/`soma-database-write-proposal` foi confirmada por leitura de manifesto (`CODE_INSPECTION`); nenhuma tabela `PROG_OP_PED` real foi inspecionada nesta sessao (nao ha acesso a banco real autorizado neste escopo).
- Procedures estudadas: **N/A — UNKNOWN**.
- Grade detectada: **N/A — UNKNOWN**.
- Dataset de diferencas: **N/A — UNKNOWN**.
- Impact analysis: **N/A — UNKNOWN**.

### 7.1 Modelo Conceitual (NAO E analise real — apenas estrutura de processo)

Quando os tres artefatos chegarem, o fluxo que o Linx Agent seguira, sob a User Artifact Learning Policy, e:

```text
ARTEFATO (planilha + SQL historico + explicacao PO)
  -> ESTUDAR (linx-database-specialist-agent / linx-erp-specialist-agent)
  -> IDENTIFICAR INTENCAO
  -> EXTRAIR REGRAS DE NEGOCIO
  -> FORMULAR HIPOTESES
  -> COMPARAR COM CONHECIMENTO ATUAL (knowledge store dos Agents Linx)
  -> VALIDAR CONTRA SCHEMA REAL (leitura, nunca escrita)
  -> IDENTIFICAR LACUNAS (Knowledge Gap / Capability Gap se aplicavel)
  -> PERGUNTAR AO PRODUCT OWNER QUANDO NECESSARIO
  -> APRENDER (persistir conhecimento validado, com proveniencia)
  -> PROJETAR SOLUCAO PROPRIA (nunca reexecutar o SQL historico literal)
  -> VALIDAR A SOLUCAO
  -> GOVERNAR: gerar ActionProposal
  -> SUBMETER a AIGovernancePolicyEngine + ApprovalPolicy
  -> PROPOR EXECUCAO via ToolGateway em modo DRY-RUN (LIVE_EXECUTION permanece false)
```

Este e um esqueleto de processo, rotulado explicitamente como **modelo conceitual**, nao como resultado de analise do caso real.

### 7.2 Avaliacao Arquitetural — Capability `linx-production-purchase-grade-adjustment` (PROPOSTA, nao implementada)

Pergunta do usuario: vale a pena uma capability especifica `linx-production-purchase-grade-adjustment`, ou e melhor reaproveitar `linx-database-analysis` + `soma-database-write-proposal` (ambas ja existentes em `linx-database-specialist-agent`)?

Analise (apenas proposta/opiniao arquitetural, **nao implementada, nao criada, aguardando autorizacao**):

- **Necessaria?** Nao ha evidencia suficiente para afirmar que sim ou que nao — depende inteiramente da regra de negocio real de PROG/OP/PED, ainda desconhecida (`UNKNOWN`).
- **Util?** Potencialmente, **se e somente se** o ajuste de grade envolver uma logica de validacao/transformacao especifica o bastante (ex.: regras de arredondamento de grade, dependencias entre OP e PED, casos especiais de producao) que nao se encaixa limpamente em `linx-database-analysis` (leitura/analise generica) nem em `soma-database-write-proposal` (proposta de escrita generica).
- **Redundante?** Ha risco real de redundancia: `soma-database-write-proposal` ja cobre "propor escrita governada no banco SOMA/Linx" de forma generica. Se o ajuste de grade for apenas "mais um tipo" de proposta de escrita (mesmo padrao de ActionProposal, mesma tabela-alvo, mesma governanca), criar uma capability nova apenas fragmenta o dominio sem beneficio funcional.
- **Responsabilidade correta?** Se criada, a responsabilidade natural seria do `linx-database-specialist-agent` (evolucao do Agent existente), nunca de um Agent novo — o dominio de dados/banco Linx ja e dele.
- **Risco de granularidade excessiva?** Sim, real. `EXECUTION_POLICY.md`/`CAPABILITY_GAP_AND_AGENT_EVOLUTION_POLICY.md` alertam explicitamente contra transformar um Agent em "faz tudo" **e** contra fragmentar capabilities alem do necessario. Uma capability por caso de negocio especifico tende a essa fragmentacao.
- **Recomendacao preliminar (PROPOSTA, aguardando autorizacao humana explicita e, sobretudo, aguardando os 3 artefatos):** nao criar a capability agora. Reavaliar apos os artefatos chegarem: se a regra de negocio for genuinamente uma variacao de "propor escrita governada" (mesmo padrao de ActionProposal/Update), reaproveitar `soma-database-write-proposal` como Capability Gap **resolvido por evolucao de conhecimento**, nao por nova capability. Se a regra envolver validacao/transformacao de dominio suficientemente distinta (p.ex. calculo de grade multi-tabela com regras proprias), entao evoluir `linx-database-specialist-agent` com a nova capability seria justificavel — mas isso exige `Agent Factory UPDATE` com autorizacao humana explicita e reauditoria, nunca criacao automatica.

Esta secao e apenas analise/proposta. Nenhuma capability e nenhum Agent foram criados ou alterados por esta tarefa em decorrencia dela.

## 8. Governed Write Stack, Security/LGPD, Policy Decision, Approval Requirement

- **Governed Write Stack**: intacto e reutilizado, nao redesenhado. As novas politicas vivem ao lado de `ActionProposal`, `AIGovernancePolicyEngine`, `ApprovalPolicy`, `ToolGateway` sem substitui-los.
- **Dry-run**: conceitual nesta tarefa — nenhuma proposta real (`ActionProposal`) foi gerada para o caso PROG/OP/PED, pois nao ha dado real para preencher `Resource`, `Operation`, `ExpectedAffectedRows` etc. sem inventar.
- **Security/LGPD**: nao aplicavel neste momento ao caso real (nenhum dado, nenhuma tabela, nenhuma classificacao real avaliada). Para as politicas canonicas (Parte 1), a revisao de seguranca se resume a: nenhum segredo em nenhum arquivo criado/alterado (ver secao 10), nenhuma capacidade de bypass adicionada, nenhuma reducao de `policy_engine_required`/`approval_required_for` em nenhum manifesto.
- **Policy Decision**: nenhuma decisao de policy foi emitida para o caso real (nao ha `ActionProposal` para avaliar). Para as politicas canonicas, a "decisao" e estrutural: os testes automatizados (secao 9) sao a evidencia de conformidade.
- **Approval Requirement**: nao aplicavel ao caso real (`WAITING_FOR_EVIDENCE`, nada a aprovar). Para as politicas canonicas, nenhuma mudanca desta tarefa e uma "mudanca material de capability" de um Agent existente (nenhum `agent.yaml` teve `capability_ownership`, `governance` ou `gap_policy` alterados); portanto nao se aplicava o requisito de aprovacao humana explicita para *esta* tarefa especificamente. Qualquer evolucao futura de `linx-database-specialist-agent` (ex.: a capability da secao 7.2) exigira essa aprovacao.

## 9. Testes

### 9.1 Testes .NET

Comando: `dotnet build BlueprintOS.sln` — **build succeeded, 0 warnings, 0 errors**.

Comando: `dotnet test tests/BlueprintOS.UnitTests/BlueprintOS.UnitTests.csproj` — **892 passed, 0 failed, 0 skipped** (execucao apos remocao de um arquivo de teste duplicado criado por engano durante esta sessao — ver nota abaixo).

Subconjunto relevante — `dotnet test ... --filter "FullyQualifiedName~Governance"` — **58 passed, 0 failed**, incluindo:

- Todos os testes preexistentes do Governed Write Stack (`ActionProposal`/`ProposalHash`, expired approval, revoked approval, changed proposal, UPDATE sem filtro, TRUNCATE, `SecretCredential`, PII export, identity permission, privilege escalation, `LIVE_EXECUTION_DISABLED`) — **preservados, nenhum quebrado**.
- Os 15 casos de `UserArtifactLearningAndCapabilityGapPolicyTests` (`backend/tests/BlueprintOS.UnitTests/Core/AI/Governance/UserArtifactLearningAndCapabilityGapPolicyTests.cs`), cobrindo as 12 regras exigidas (Regra 10 tem 3 casos de teoria de secret patterns + 1 caso de flag explicita = 4 testes para a regra 10).

Nao foram executados testes de integracao (`BlueprintOS.IntegrationTests`) nem qualquer comando `dotnet ef` conectado a banco real, conforme exigido.

**Nota de processo**: durante a execucao desta tarefa, um agente em background foi iniciado e interrompido; ele havia produzido, de forma independente e antes da interrupcao, uma implementacao completa e bem estruturada das duas politicas (documentos, modelos, servicos e um arquivo de testes cobrindo as 12 regras). Essa implementacao foi inspecionada, validada e adotada nesta tarefa em vez de recriada do zero, para evitar duplicacao. Um arquivo de teste duplicado que esta sessao havia escrito antes de descobrir o trabalho do agente em background foi removido.

### 9.2 Testes Node/JS (Agent Factory v2 e afins)

Comando: `node --test tools/agents/*.test.js` — **6 arquivos, 6 passed, 0 failed** (agent-factory-v2, governed-orchestrator, runtime-registry, showcase-agent-safety, validate-agent-manifests, wise-agent-safety).

## 10. Secret Scan

Varredura manual (gitleaks/trufflehog nao instalados) por regex de senha/token/API key/connection string/credencial sobre todos os arquivos criados/alterados por esta tarefa (`agents/USER_ARTIFACT_LEARNING_POLICY.md`, `agents/CAPABILITY_GAP_AND_AGENT_EVOLUTION_POLICY.md`, `agents/AGENT_CONTRACT.md`, `agents/EXECUTION_POLICY.md`, `tools/agents/agent-factory-v2.js`, os 4 arquivos C# novos em `backend/src/BlueprintOS.Core/AI/Governance/`, e o arquivo de teste novo). **Nenhum segredo real encontrado.** O unico hit do padrao de varredura foi `pwd=abc123` dentro de um `[InlineData]` de teste unitario, que e um valor sintetico de fixture para provar que o classificador de segredo funciona (Regra 10) — nao e uma credencial real, nao aponta para nenhum sistema, e e o proprio objeto do teste.

## 11. Agent Factory Audit — Antes E Depois

Executado via `node tools/agents/agent-factory-cli.js AUDIT`, capturado antes e depois de todas as mudancas de codigo desta tarefa:

| Momento | status geral | agentes WARN | findings totais | ids de finding |
|---|---|---|---|---|
| Antes | WARN | 8/8 | 12 | AFV2-GOV-001, AFV2-GATEWAY-001 |
| Depois | WARN | 8/8 | 12 | AFV2-GOV-001, AFV2-GATEWAY-001 |

**Nenhuma mudanca de finding count.** O finding `AFV2-POLICY-001` (que verificaria a ausencia/nao-referencia das duas politicas) **nao aparece em nenhum dos dois momentos** porque, no momento em que esta auditoria foi executada nesta sessao, os arquivos de politica e suas referencias ja existiam (produzidos pelo agente em background mencionado na secao 9.1, antes da captura do "antes"). Portanto esta comparacao antes/depois nao demonstra a transicao "sem-politica -> com-politica" via o audit; essa transicao foi verificada de outra forma: **por inspecao direta do codigo-fonte de `canonicalPolicyFindings()`** (secao 5), confirmando que a condicao `!exists || !referenced` dispararia `AFV2-POLICY-001` caso qualquer um dos artefatos fosse removido — o que foi validado lendo a logica, nao reexecutando um estado historico do repositorio. Nenhum finding foi maquiado, suprimido ou promovido artificialmente a `ENFORCED`.

## 12. Arquivos Criados

- `agents/USER_ARTIFACT_LEARNING_POLICY.md`
- `agents/CAPABILITY_GAP_AND_AGENT_EVOLUTION_POLICY.md`
- `backend/src/BlueprintOS.Core/AI/Governance/UserArtifactLearningPolicy.cs`
- `backend/src/BlueprintOS.Core/AI/Governance/CapabilityGapAndAgentEvolutionPolicy.cs`
- `backend/src/BlueprintOS.Core/AI/Governance/Models/UserArtifactLearningModels.cs`
- `backend/src/BlueprintOS.Core/AI/Governance/Models/CapabilityGapModels.cs`
- `backend/tests/BlueprintOS.UnitTests/Core/AI/Governance/UserArtifactLearningAndCapabilityGapPolicyTests.cs`
- `docs/audits/AgentLearningV1-LinxProgOpPed.md` (este arquivo)
- `docs/audits/AgentLearningV1-LinxProgOpPed-Results.json`

## 13. Arquivos Alterados (Minimos, Aditivos, Nao-Breaking)

- `agents/AGENT_CONTRACT.md` (+9 linhas: secao "Politicas Canonicas Relacionadas")
- `agents/EXECUTION_POLICY.md` (+4 linhas: referencia de abertura as duas politicas)
- `tools/agents/agent-factory-v2.js` (+19 linhas: metodo `canonicalPolicyFindings()` e sua chamada em `audit()`)

Nenhum outro arquivo do escopo desta tarefa foi alterado. `agents/agent.schema.json` **nao foi tocado** (nenhum CONTRACT GAP identificado — ver secao 5).

## 14. Riscos

- O finding `AFV2-GATEWAY-001` (WARNING, presente antes e depois) documenta honestamente que o Tool Gateway ainda nao medeia universalmente todo acesso externo — risco preexistente, nao introduzido nem agravado por esta tarefa.
- O caso real PROG/OP/PED permanece bloqueado; ha risco de o usuario interpretar o "modelo conceitual" da secao 7.1 como analise real caso o rotulo seja removido em uma edicao futura deste documento — reforcar sempre o rotulo ao reutilizar este conteudo.
- Os 3 arquivos do fluxo diario Linx/WISE ja estavam modificados no worktree por trabalho alheio a esta tarefa; existe risco de confusao em um futuro `git add -A` acidental misturando esse trabalho com o desta tarefa — por isso o commit desta tarefa usa `git add <arquivo>` explicito por arquivo.

## 15. Proximos Passos

1. **Bloqueante:** obter resposta do Product Owner as 3 perguntas objetivas da secao 7.9 (classificacao PROG por exclusao, grade de 6 vs 7 posicoes, acesso read-only ao banco Linx/SOMA_DESENV).
2. Somente apos 7.9 resolvido: calcular o Delta real (planilha vs banco), completar o impact analysis quantitativo da secao 16 da tarefa, e projetar a solucao propria do Agent (SQL novo, `PROPOSED — NOT EXECUTED`) para submissao ao Governed Write Stack (`ActionProposal -> Policy Engine -> Approval -> Tool Gateway -> DRY_RUN`).
3. Apos a solucao propria existir, reavaliar a necessidade da capability `linx-production-purchase-grade-adjustment` com evidencia real, e submeter a decisao (evoluir `linx-database-specialist-agent` ou reaproveitar `soma-database-write-proposal`) para autorizacao humana explicita antes de qualquer `Agent Factory UPDATE`.
4. Investigar a divergencia de duplicidade de `PO_PEDIDO_COMPRA` na planilha (secao 7.1) com quem gerou o arquivo — em especial o caso `PO 1741979` com quantidades divergentes entre as duas linhas do mesmo pedido.
5. Investigar por que o snapshot estatico `docs/audits/AgentFactoryV2-AuditResults.json` (18 findings) diverge do resultado ao vivo atual do mesmo comando (12 findings) — fora do escopo desta tarefa, mas registrado como divergencia a esclarecer.
6. Confirmar com o dono do trabalho nao relacionado (`.ai/context/linx-wise-daily-integration.md`, `docs/operations/LinxWiseDailyIntegrationRunbook.md`, `scripts/linx_wise_daily_integration.py`) se essas mudancas devem ser commitadas separadamente — esta tarefa deliberadamente as deixou intocadas e fora do commit.
