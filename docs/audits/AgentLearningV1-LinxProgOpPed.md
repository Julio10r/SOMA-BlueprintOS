# AgentLearningV1 — Politicas Canonicas De Aprendizado E Evolucao + Caso Linx PROG/OP/PED

Status: accepted
Data: 2026-08-27
Escopo: `agents/`, `backend/src/BlueprintOS.Core/AI/Governance/`, `tools/agents/agent-factory-v2.js`, testes .NET e Node associados.

> **Reclassificacao (Database Connection Policy v1.1, ver `docs/audits/DatabaseConnectionPolicyV1.md` e `agents/DATABASE_CONNECTION_POLICY.md` secoes 17-22):**
> todo achado deste documento rotulado `CONFIRMED_BY_SCHEMA`, `CONFIRMED_BY_DATA_VALIDATION` ou `CONFIRMED_BY_CODE_INSPECTION` na secao 7 foi obtido **exclusivamente em `SOMA_DESENV`**, nunca em Producao (`SOMA`). Sob a politica v1.1, Producao e a fonte authoritative para o estado atual do ERP; `SOMA_DESENV` e laboratorio de desenvolvimento/teste, que pode estar desatualizado em relacao a Producao. Portanto, nenhum desses achados representa mais "o estado atual confirmado do Linx" isoladamente — cada um deve ser lido como `CONFIRMED_IN_DEVELOPMENT` (schema/procedure/dado validos e lidos em `SOMA_DESENV` nesta data), com o status adicional `NEEDS_PRODUCTION_VALIDATION` ate ser confrontado com Producao. Isto vale em especial para o gap do tamanho 34 (secao 7.6.3/7.9), que deixa de ser tratado como problema funcional confirmado e passa a `DEVELOPMENT_PRODUCTION_DRIFT_SUSPECTED` + `PENDING_PRODUCTION_VALIDATION` (ver nota atualizada na secao 7.9). O aprendizado do Agent (ter detectado a lacuna) permanece valido; a conclusao de que ela reflete a realidade de producao e o que foi reclassificado. Nenhum conteudo desta secao 7 foi apagado.
>
> **Atualizacao (rodada de investigacao authoritative em producao):** a rodada que tentaria confrontar estes achados com `linx-production` foi inicialmente **bloqueada por indisponibilidade de conectividade** ao endpoint entao configurado (`192.168.0.200`). `SOMA_DESENV` **nao foi usado como substituto**. Nenhum item desta secao 7 foi promovido a `CONFIRMED_IN_PRODUCTION` nesta rodada. Relatorio da tentativa: `docs/audits/LinxProgOpPed-ProductionInvestigation.md`.
>
> **Correcao (mesma data, etapa seguinte):** o endpoint `192.168.0.200` **nunca foi o servidor SQL real de producao** — nao era bloqueio de firewall/VPN, era configuracao incorreta desde a origem. O endpoint corrigido, com evidencia real de conexao (`@@SERVERNAME`=`SRV-SOMADB`), e `192.168.9.200:1433`. Ver `docs/audits/LinxProductionEndpointCorrectionV1.md`.
>
> **CONCLUSAO (mesma data, Rodada 2 da investigacao em producao — ver `docs/audits/LinxProgOpPed-ProductionInvestigation.md`):** apos a correcao do endpoint, `linx-production` retornou `READY` e toda a secao 7 abaixo foi confrontada com `SOMA` real. **Schema e as 4 procedures sao IDENTICOS entre DEV e PROD** (nenhum drift). O gap do tamanho 34 (7.6.3) foi **RESOLVIDO**: producao confirma a mesma grade `36-44` sem o tamanho 34 (nao e drift de DEV desatualizado), mas revelou grades alternativas ja cadastradas que incluem o tamanho 34 (ex. `"36 - 44 - 34"`), e uma prova quantitativa em 77/77 linhas de que a operacao e um **rebalanceamento de grade** (total de pecas inalterado — o delta de -829 nos tamanhos 36-44 e exatamente compensado pelas 829 unidades do tamanho 34). A regra de classificacao PROG/OP/PED foi refinada com dados reais: **77 PED, 0 OP, 0 PROG, 0 NAO_ENCONTRADO** (prioridade OP > PED > PROG; existir em `PRODUCAO_PROG_PROD` isoladamente nao basta para PROG). O caso `PO 1741979` foi confirmado `CONFIRMED_VALID` (multiplas cores na mesma PO, estrutura normal). **Unico Knowledge Gap residual: qual codigo de grade cadastrado deve substituir `PRODUTOS.GRADE` destes 39 produtos** — decisao de catalogacao, nao tecnica. Nenhuma escrita foi proposta ainda.

> **NOVA ETAPA (mesma tarefa, investigacao adicional em Producao + conhecimento funcional do Product Owner):** o Product Owner forneceu a regra funcional completa de ajuste de grade PROG/OP/PED (chave `PROGRAMACAO+PRODUTO+COR`, ciclo `REVENDA` -> PED/OP, classificacao por evidencia transacional, grade posicional vs rotulo visual, delta vs quantidade final por mecanismo). Uma nova rodada de investigacao read-only em `linx-production` confirmou: (1) `PRODUCAO_PROG_PROD.P1..P48`, `PRODUCAO_ORDEM_COR.O1..O48/P1..P48` e `COMPRAS_PRODUTO.CO1..CO48/CE1..CE48` — **48 posicoes reais**, nao 6/7 como a secao 7.6.2 registrou antes (7.6.2 ja apontava "ate 48" corretamente, mas o SQL historico e a analise anterior continuaram ancoradas em 6/7 nos calculos); (2) as procedures `LX_ANM_GERA_OS_ALTERACAO_PCP` e `LX_ANM_AJUSTA_PROGRAMACAO_PROD` **hoje aceitam `@S1..@S10`** (nao apenas `@S1..@S7` como o SQL historico usava) e ambas fazem `UPDATE ... SET Px = Px + @Sx` — **delta por posicao, confirmado por leitura direta do corpo da procedure**, nao apenas inferido; (3) `LX_MOVIMENTA_COMPRAS_PA` e `LX_RECALCULO_RESERVA_MATERIAIS` nao recebem delta — a primeira apenas recalcula `QTDE_ENTREGAR`/`VALOR_ENTREGAR`/`VALOR_ENTREGUE` a partir do que ja foi gravado em `CE1..CE48`/`CO1..CO48` (que e alterado por `UPDATE` direto do chamador, com **quantidade final**, nao delta), e a segunda recalcula reserva de materiais; (4) `PRODUTOS.REVENDA` (bit) tem correlacao forte, nao absoluta, com o mecanismo real: de 162936 produtos com pelo menos uma OP ou um Pedido, `REVENDA=0` tem OP em 89,6% dos casos (145984/162936) e `REVENDA=1` tem Pedido em 99,6% dos casos (85222/85536) — **confirma o papel auxiliar de `REVENDA`, nao como fonte de verdade sozinha** (muitos produtos `REVENDA=0` tambem tem Pedido de compra na sua historia, 83495 casos). Ver secao 7.10 para o detalhamento completo e a secao "PRODUCT OWNER FUNCTIONAL KNOWLEDGE" (7.11) para a regra ensinada pelo PO, separada explicitamente do que foi confirmado tecnicamente nesta rodada.
+
+## 1. Resumo Executivo

Esta tarefa teve dois objetivos independentes:

1. Formalizar e implementar, com codigo e testes reais, duas politicas canonicas aplicaveis a todos os Agents e a qualquer IA/executor (Codex, Claude, ChatGPT ou futuros): **User Artifact Learning Policy** e **Capability Gap & Agent Evolution Policy**.
2. Processar o caso real de negocio "PROG/OP/PED" (ajuste de grade de producao/compra) usando o Linx Agent.

Resultado do item 1: **CONFIRMED**. As duas politicas existem como documentos canonicos, sao referenciadas por `AGENT_CONTRACT.md`/`EXECUTION_POLICY.md`, sao verificadas pela Agent Factory v2 em `AUDIT`, e sao implementadas com codigo real em `backend/src/BlueprintOS.Core/AI/Governance/` com 15 testes automatizados cobrindo as 12 regras exigidas.

Resultado do item 2 (atualizado nesta 3a etapa): os dois artefatos reais (planilha `New Way - Size 34 - layout programacao_v2 (1).xlsx` e SQL historico `AJUSTA GRADE OP-PROG-COMPRAS.sql`) foram localizados, lidos integralmente e preservados sem alteracao. Uma etapa dedicada resolveu a conexao read-only ao Linx/`SOMA_DESENV` (`docs/audits/AgentContractV1.1-ExecutionPolicy-Credenciais.md` secao 25). Com a conexao pronta, esta 3a etapa investigou **read-only** o schema real (5 tabelas), a definicao das 4 procedures citadas, o mecanismo real de grade (`PRODUTOS`/`PRODUTOS_TAMANHOS`) e cruzou os dados da planilha contra `SOMA_DESENV` (ver secao 7.6). Resultado: **2 dos 3 Knowledge Gaps antigos foram resolvidos com evidencia real do banco/codigo** (regra positiva de classificacao PROG confirmada por inspecao de procedure — 7.6.4; mecanismo generico de grade de 1 a 48 posicoes via `PRODUTOS.GRADE`+`PRODUTOS_TAMANHOS` confirmado por schema e validado com os dados reais da planilha — 7.6.2). Um **novo Knowledge Gap bloqueante, mais preciso, substituiu o antigo "6 vs 7 posicoes"**: a grade cadastrada para estes produtos (`36-44`) nao inclui o tamanho `34` em nenhuma posicao, apesar da planilha ter uma coluna `Q_34` com quantidades reais (secao 7.6.3) — contradicao confirmada por leitura direta do banco, nao suposicao. Nenhuma solucao SQL foi gerada, nenhum dado foi inventado, nenhuma escrita ou migration foi executada, e uma pergunta objetiva final foi registrada para o Product Owner (secao 7.9).

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

### 7.6 Procedures — lidas integralmente do SOMA_DESENV (read-only, `OBJECT_DEFINITION`), CONFIRMED_BY_CODE_INSPECTION

Investigacao read-only real via `dotnet run --project backend/src/BlueprintOS.Api -- investigate-linx-prog-op-ped schema`, profile `linx-development` (`SOMA_DESENV`, `192.168.9.98`). Nenhuma procedure foi executada (`EXEC`); apenas `sys.parameters` e `OBJECT_DEFINITION`.

| Procedure | Parametros reais | Status | Achado |
|---|---|---|---|
| `LX_ANM_GERA_OS_ALTERACAO_PCP` | `@XORDEM_PRODUCAO, @COR_PRODUTO, @S1..@S10, @RECALCULA_RESERVA, @EXECUTADO OUTPUT, @RETORNO OUTPUT` | **CONFIRMED_BY_CODE_INSPECTION — atual, ativa** (comentario de manutencao 29/03/2023) | Pre-valida saldo: se `S1+@S1 < 0 OR ... OR S10+@S10 < 0` contra `PRODUCAO_TAREFAS_SALDO`, retorna `@EXECUTADO=0, @RETORNO='GRADE A SER ALTERADA MAIOR QUE O SALDO NA OP'` sem alterar nada. Se a OP nao tem tarefa em processo (`@SEQUENCIA_ANTERIOR IS NULL`), atualiza `PRODUCAO_ORDEM_COR` diretamente (`O1..O10`, `P1..P10`, PK de 3 colunas — seguro); senao gera uma nova Ordem de Servico. Chama `LX_RECALCULO_RESERVA_MATERIAIS` ao final. O SQL historico so passa 7 dos 10 parametros `@S1..@S7`; os demais assumem `= 0` por default — **chamada historica compativel**, nao incorreta. |
| `LX_ANM_AJUSTA_PROGRAMACAO_PROD` | `@PROGRAMACAO, @PRODUTO, @COR_PRODUTO, @S1..@S10, @EXECUTADO OUTPUT, @RETORNO OUTPUT` | **CONFIRMED_BY_CODE_INSPECTION — atual, ativa** | `UPDATE PRODUCAO_PROG_PROD SET QTDE_PROGRAMADA += (soma), P1..P10 += @S1..@S10, S1..S10 += @S1..@S10 WHERE PROGRAMACAO=@P AND PRODUTO=@P AND COR_PRODUTO=@C`. **Risco confirmado (ver 7.6.1): a clausula `WHERE` NAO inclui `ENTREGA_INICIAL`**, que faz parte da chave primaria real da tabela (4 colunas, ver 7.6.2). **Risco confirmado adicional: a procedure NUNCA verifica quantas linhas o `UPDATE` afetou** — se 0 linhas baterem (produto/programacao/cor sem registro em `PRODUCAO_PROG_PROD`), o `UPDATE` e um no-op silencioso e a procedure retorna `@EXECUTADO=1, @RETORNO='EXECUTADO COM SUCESSO'` do mesmo jeito. Isso tem uma consequencia direta e util para o Gap (a) — ver 7.7. |
| `LX_MOVIMENTA_COMPRAS_PA` | `@PEDIDO` | **CONFIRMED_BY_CODE_INSPECTION — atual, ativa** (change log ate 09/10/2025) | Recalcula `QTDE_ENTREGAR`/`VALOR_ENTREGAR` de `COMPRAS_PRODUTO` somando `CE1..CE48` (48 posicoes reais, nao 7) e aplicando `PONTEIRO_PRECO_TAM` para escolher o custo correto por posicao. Nao e a procedure que grava o delta de grade em si (isso e feito pelo `UPDATE COMPRAS_PRODUTO` do proprio SQL historico, antes de chamar esta procedure) — apenas recalcula totais/valores a partir do que ja foi gravado. |
| `LX_RECALCULO_RESERVA_MATERIAIS` | `@PRODUTO, @MOSTRA, @XORDEM_PRODUCAO, @XTIPO_PROCESSO, @MOSTRA_BLOCO_K` | **CONFIRMED_BY_CODE_INSPECTION — atual, ativa** (change log ate 01/07/2024) | Recalculo de reserva de materiais/ficha tecnica; tipos de reserva documentados no proprio cabecalho (`TIPO_RESERVA 1..7`). Nao mexe em grade/quantidade de produto acabado diretamente. |

**Nenhuma das 4 procedures parece obsoleta** — todas tem historico de manutencao recente (2023-2025), contradizendo a hipotese inicial (secao 7.3) de que poderiam estar desatualizadas.

#### 7.6.1 Risco confirmado: chave primaria de 4 colunas nao totalmente respeitada nas escritas de grade

Leitura real de `INFORMATION_SCHEMA`/`sys.indexes` (read-only) mostrou que **3 das 4 tabelas envolvidas tem chave primaria mais ampla do que o `JOIN`/`WHERE` usado tanto pelo SQL historico quanto pela procedure atual `LX_ANM_AJUSTA_PROGRAMACAO_PROD`**:

| Tabela | PK real (CONFIRMED_BY_SCHEMA) | `JOIN`/`WHERE` usado (SQL historico e/ou procedure atual) | Coluna da PK omitida |
|---|---|---|---|
| `PRODUCAO_PROG_PROD` | `PROGRAMACAO, PRODUTO, COR_PRODUTO, ENTREGA_INICIAL` (4 col.) | `PROGRAMACAO, PRODUTO, COR_PRODUTO` (3 col.) | `ENTREGA_INICIAL` |
| `PRODUCAO_ORDEM_COR` | `ORDEM_PRODUCAO, PRODUTO, COR_PRODUTO` (3 col.) | `ORDEM_PRODUCAO, PRODUTO, COR_PRODUTO` (3 col.) | Nenhuma — **seguro** |
| `COMPRAS_PRODUTO` | `PRODUTO, PEDIDO, COR_PRODUTO, ENTREGA` (4 col.) | `PEDIDO, PRODUTO, COR_PRODUTO` (3 col., no `UPDATE` do SQL historico) | `ENTREGA` |

**Implicacao (CONFIRMED_BY_SCHEMA, risco real, nao apenas teorico):** se um `PROGRAMACAO+PRODUTO+COR_PRODUTO` tiver mais de uma linha em `PRODUCAO_PROG_PROD` com `ENTREGA_INICIAL` diferentes (ex.: duas entregas programadas para datas distintas do mesmo produto/cor), tanto o SQL historico quanto a procedure atual `LX_ANM_AJUSTA_PROGRAMACAO_PROD` aplicariam o **mesmo delta a todas elas simultaneamente** — nao apenas a uma. O mesmo vale para `COMPRAS_PRODUTO`/`ENTREGA` no bloco PED do SQL historico. Nao foi possivel confirmar nesta etapa (SOMA_DESENV nao tem os dados operacionais desta planilha — ver 7.6.3) se essa multiplicidade realmente ocorre na pratica para os produtos/programacoes desta planilha. **Qualquer solucao nova deve filtrar explicitamente por `ENTREGA_INICIAL`/`ENTREGA` (nao apenas herdar a omissao historica) ou validar antes que a cardinalidade e 1.**

#### 7.6.2 Grade: mecanismo real descoberto (resolve a maior parte do Gap b)

Busca read-only em `INFORMATION_SCHEMA.COLUMNS` por colunas de grade/tamanho encontrou a tabela mestre de produto (`PRODUTOS`, nao `PRODUTO`) e a tabela de definicao de grade (`PRODUTOS_TAMANHOS`):

- `PRODUTOS.GRADE` (varchar): codigo de grade do produto. **CONFIRMED_BY_DATA_VALIDATION**: para os 30 produtos da planilha encontrados em `PRODUTOS` (de 39 distintos — 9 nao encontrados, ver 7.6.3), `PRODUTOS.GRADE = '36-44'` em 100% dos casos, **identico ao valor da coluna `GRADE_ATUAL` da planilha**. Isso confirma que a planilha usa o mesmo codigo de grade do cadastro de produto.
- `PRODUTOS_TAMANHOS` (chave `GRADE`, nao `PRODUTO`): tem `TAMANHO_1..TAMANHO_48` (varchar — o tamanho fisico real, ex. `'36'`, `'38'`) e `NUMERO_TAMANHOS`/`TAMANHOS_DIGITADOS`. As tabelas operacionais (`PRODUCAO_PROG_PROD.P1..P48`/`S1..S48`, `PRODUCAO_ORDEM_COR.O1..O48`/`P1..P48`, `COMPRAS_PRODUTO.CO1..CO48`/`CE1..CE48`) usam **ate 48 posicoes genericas**, nao 6 ou 7 fixas — o numero de posicoes e uma particularidade de cada grade, resolvida via `PRODUTOS_TAMANHOS`, nao um limite fixo do schema. Isso **substitui** a suposicao anterior de que "6 vs 7" era a pergunta certa: a pergunta certa e "qual posicao (1..48) corresponde a qual tamanho fisico, para a grade `36-44`".
- **Resultado real, lido do banco, para `GRADE = '36-44'`:** `NUMERO_TAMANHOS=16`; `TAMANHO_1='36'`, `TAMANHO_2='38'`, `TAMANHO_3='40'`, `TAMANHO_4='42'`, `TAMANHO_5='44'`, `TAMANHO_6..TAMANHO_8` em branco (nao populados).

#### 7.6.3 NOVO Knowledge Gap bloqueante (mais preciso que o Gap b original): tamanho 34 ausente da grade `36-44` em SOMA_DESENV — `DEVELOPMENT_PRODUCTION_DRIFT_SUSPECTED` + `PENDING_PRODUCTION_VALIDATION`

> **Correcao de aprendizado (nova etapa, ver secao 7.11.6):** a conclusao que este gap levou a, mais tarde (`docs/audits/LinxProgOpPed-ProductionInvestigation.md` R2.11 — trocar `PRODUTOS.GRADE` destes produtos por um codigo que inclua o rotulo "34"), foi construida pela interpretacao do **rotulo visual do tamanho**, nao pela semantica **posicional** da grade Linx ensinada pelo Product Owner nesta etapa. Essa conclusao **nao deve mais ser tratada como a regra geral do modelo**. O texto original abaixo e de R2.4/R2.11 e de 7.6.3 fica preservado sem alteracao para historico; a reclassificacao completa, incluindo por que a aplicacao estrita da regra posicional tambem nao resolve trivialmente este caso especifico, esta em 7.11.6.

**Status reclassificado (Database Connection Policy v1.1):** este achado foi obtido inteiramente em `SOMA_DESENV`, nunca confrontado com Producao (`SOMA`). Conforme esclarecido pelo Product Owner/especialista Linx, `SOMA_DESENV` nao e espelho garantido de producao — cadastros podem estar desatualizados. Portanto **este NAO deve mais ser tratado como problema funcional confirmado do Linx**; o status correto e `DEVELOPMENT_PRODUCTION_DRIFT_SUSPECTED` + `PENDING_PRODUCTION_VALIDATION`: uma divergencia real observada entre a planilha e o cadastro de `SOMA_DESENV`, cuja causa raiz (drift Development/Production real vs. regra de negocio) so pode ser determinada lendo `PRODUTOS.GRADE`/`PRODUTOS_TAMANHOS` em Producao, read-only (`agents/DATABASE_CONNECTION_POLICY.md` secao 18). O bloqueio para propor escrita permanece — apenas a interpretacao do porque muda.

**O que sabemos (CONFIRMED_IN_DEVELOPMENT, leitura direta do SOMA_DESENV — ainda NAO confirmado em Producao):** para a grade `36-44` — a mesma grade cadastrada nos produtos desta planilha — as posicoes 1 a 5 mapeiam para os tamanhos fisicos `36, 38, 40, 42, 44`. **Nao existe tamanho `34` em nenhuma posicao dessa grade.**

**O que a planilha tem:** uma coluna `Q_34` com quantidades reais e nao-zero (ex.: primeira linha, produto `15.29433`, `Q_34=8`). O proprio nome do arquivo (`New Way - Size 34 - layout programacao_v2`) sugere que o tamanho 34 e central a esta execucao especifica.

**Isto e uma contradicao real entre a planilha e o cadastro de grade do produto no SOMA_DESENV, nao uma suposicao nossa.** Nao adivinhamos uma resposta. Possibilidades genuinamente plausiveis, todas exigindo confirmacao do Product Owner:

1. A grade correta para estes produtos deveria ser `34-44` (6 tamanhos), e o cadastro `PRODUTOS.GRADE='36-44'` no SOMA_DESENV esta desatualizado/incompleto para este drop/colecao — comum quando um tamanho e adicionado tardiamente a uma colecao ja cadastrada.
2. O tamanho 34 e tratado por um mecanismo separado (ex.: uma "grade estendida" ou "quebra" — a tabela `PRODUTOS_TAMANHOS` tem colunas `QUEBRA_1..QUEBRA_5` nao investigadas em profundidade nesta etapa) que nao aparece no `TAMANHO_1..8` simples.
3. `Q_34` na planilha e residual/legado e nao deveria gerar ajuste real (ja que o produto formalmente so vende `36-44`) — o que mudaria a classificacao dessas quantidades para `ERRO_DE_DADOS` ou `BLOQUEADO`, nao para um Delta valido.
4. O cadastro em `SOMA_DESENV` diverge do cadastro em producao (`SOMA`) para estes produtos especificos — nao pode ser descartado sem acesso a producao (fora de escopo desta etapa, ver 7.6.5).

**Pergunta objetiva ao Product Owner:** para os produtos desta planilha (grade cadastrada `36-44`), o tamanho 34 deveria: (i) fazer parte de uma grade `34-44` que precisa ser corrigida no cadastro; (ii) ser tratado por outro mecanismo de grade/quebra que ainda nao identificamos; ou (iii) ser ignorado/reportado como inconsistencia de dados da planilha? Enquanto isso nao for esclarecido, **nenhuma proposta de escrita pode mapear `Q_34` com seguranca para uma posicao real do Linx.**

#### 7.6.4 PROG / OP / PED — regra POSITIVA confirmada para PROG (resolve o Gap a sem precisar perguntar ao PO)

A leitura da procedure atual (7.6, `LX_ANM_AJUSTA_PROGRAMACAO_PROD`) da a resposta que faltava: a propria Linx **nao verifica se a `UPDATE` afetou alguma linha** — ou seja, o unico jeito seguro de saber se uma linha e legitimamente `PROG` (em vez de `NAO_ENCONTRADO`) e verificar **antes** de qualquer proposta de escrita se existe uma linha em `PRODUCAO_PROG_PROD` para aquele `PROGRAMACAO+PRODUTO+COR_PRODUTO` (idealmente tambem `ENTREGA_INICIAL`, ver 7.6.1). Isso da uma regra positiva, nao mais um fallback por exclusao:

| Tipo | Regra de identificacao (positiva) | Fonte | Tabelas relacionadas | Mecanismo de ajuste |
|---|---|---|---|---|
| **OP** | Existe linha em `PRODUCAO_ORDEM`+`PRODUCAO_ORDEM_COR` para `PROGRAMACAO+PRODUTO+COR_PRODUTO` | `CONFIRMED_BY_SCHEMA` (PK de 3 colunas, join seguro) | `PRODUCAO_ORDEM`, `PRODUCAO_ORDEM_COR` | `EXEC LX_ANM_GERA_OS_ALTERACAO_PCP` (pre-valida saldo antes de aplicar) |
| **PED** | Existe linha em `COMPRAS`+`COMPRAS_PRODUTO` para `PROGRAMACAO+PRODUTO+COR_PRODUTO` | `CONFIRMED_BY_CODE_INSPECTION` do SQL historico; risco de multiplicidade por `ENTREGA` (7.6.1) | `COMPRAS`, `COMPRAS_PRODUTO` | `UPDATE COMPRAS_PRODUTO` (grade) + `EXEC LX_MOVIMENTA_COMPRAS_PA` (recalculo) + `EXEC LX_RECALCULO_RESERVA_MATERIAIS` |
| **PROG** | Existe linha em `PRODUCAO_PROG_PROD` para `PROGRAMACAO+PRODUTO+COR_PRODUTO`, **verificado explicitamente antes de chamar a procedure** (a procedure nao valida isso sozinha) | `CONFIRMED_BY_CODE_INSPECTION` (`LX_ANM_AJUSTA_PROGRAMACAO_PROD` nao checa rowcount do `UPDATE`) | `PRODUCAO_PROG_PROD` | `EXEC LX_ANM_AJUSTA_PROGRAMACAO_PROD` |
| **NAO_ENCONTRADO** | Nao existe linha em nenhuma das 3 tabelas acima para aquele `PROGRAMACAO+PRODUTO+COR_PRODUTO` | `CONFIRMED_BY_CODE_INSPECTION` | — | Nenhum — nao deve gerar proposta de escrita |

Isso **resolve o antigo Gap (a)** sem precisar de resposta do Product Owner: a ambiguidade nao era do dominio de negocio, era de uma checagem ausente na propria procedure — e ja sabemos como contornar isso na solucao propria (checar `PRODUCAO_PROG_PROD` antes, nunca confiar no retorno da procedure para diferenciar PROG de NAO_ENCONTRADO).

#### 7.6.5 Validacao dos dados desta planilha no SOMA_DESENV — nenhuma correspondencia encontrada (esperado, `PENDING_PRODUCTION_READ_ONLY_VALIDATION`)

Cruzamento read-only real (`investigate-linx-prog-op-ped crossref`) das 77 linhas da planilha (`PROGRAMACAO+PRODUTO+COR_PRODUTO` e `PO_PEDIDO_COMPRA`) contra `PRODUCAO_PROG_PROD`, `PRODUCAO_ORDEM`+`PRODUCAO_ORDEM_COR` e `COMPRAS`+`COMPRAS_PRODUTO` no `SOMA_DESENV`:

**Resultado: 77 de 77 linhas — zero correspondencias em qualquer uma das 3 tabelas.** Nenhuma linha da planilha tem hoje um registro correspondente de `PRODUCAO_PROG_PROD`, OP ou Pedido de Compras no banco de desenvolvimento — incluindo o caso `PO 1741979`/produto `15.29765` (secao 7.1), que tambem nao foi encontrado.

**Interpretacao correta (nao e um erro nem uma reprovacao dos dados):** por instrucao explicita da tarefa (secao 13: "NAO usar producao... marcar `PENDING_PRODUCTION_READ_ONLY_VALIDATION`"), este resultado **nao** significa que as 77 linhas sejam `NAO_ENCONTRADO` de verdade — significa que o `SOMA_DESENV` (banco de desenvolvimento/homologacao) simplesmente nao contem a operacao real desta planilha, que muito provavelmente so existe em producao (`SOMA`). O `SOMA_DESENV` serviu para o que podia servir nesta etapa: **validar schema, procedures e o mecanismo de grade** (7.6-7.6.4) — nao para validar dados transacionais desta execucao especifica. Classificacao de cada uma das 77 linhas (PROG/OP/PED/ZERO_DELTA/etc.) fica **`PENDING_PRODUCTION_READ_ONLY_VALIDATION`**, nao inventada.

### 7.7 Dataset funcional (modelo conceitual) — estrutura CONFIRMED, Delta real `PENDING_PRODUCTION_READ_ONLY_VALIDATION`

Cada linha da planilha pode ser modelada como: `Produto`, `Cor`, `Programacao`, `Tipo` (`OP`/`PED`/`PROG`/`NAO_ENCONTRADO`, regra de 7.6.4), `Grade atual no Linx` (posicoes 1..N conforme `PRODUTOS_TAMANHOS` da grade do produto — mecanismo confirmado em 7.6.2, mas com o Gap do tamanho 34 em aberto, 7.6.3), `Grade solicitada` (`Q_34..Q_44` da planilha), `Delta`, `Mecanismo Linx aplicavel`, `Status`, `Observacao`. **Estrutura e mecanismo: CONFIRMED.** Delta numerico real por linha: `PENDING_PRODUCTION_READ_ONLY_VALIDATION` (7.6.5) — nao calculavel com seguranca no SOMA_DESENV para esta planilha, e bloqueado adicionalmente pelo Gap do tamanho 34 (7.6.3) ate segunda ordem.

### 7.8 Conhecimento persistido no Linx Agent apos esta etapa

Ver `docs/audits/AgentLearningV1-LinxProgOpPed-Results.json`, campo `persisted_knowledge`, para a lista completa com proveniencia. Resumo: schema das 5 tabelas, definicoes das 4 procedures, mecanismo `PRODUTOS`/`PRODUTOS_TAMANHOS` de grade, e a regra positiva de classificacao PROG/OP/PED/NAO_ENCONTRADO (7.6.4) — todos com base `CONFIRMED_BY_SCHEMA`/`CONFIRMED_BY_CODE_INSPECTION`, especificos desta execucao (nomes de tabela/coluna reais do SOMA_DESENV), sem generalizar numeros especificos da planilha (39 produtos, 6 posicoes de `Q_`) como regra universal do Linx.

### 7.9 Knowledge Gaps — status apos esta etapa

| Gap | Status | Resolucao |
|---|---|---|
| (a) Regra de classificacao PROG | **RESOLVIDO, refinado com dados reais de producao** | 7.6.4 propos a regra positiva; `docs/audits/LinxProgOpPed-ProductionInvestigation.md` R2.5 provou, com dados reais, que a regra correta e uma prioridade **OP > PED > PROG** (existir isoladamente em `PRODUCAO_PROG_PROD` nao basta) |
| (b) Grade 6 vs 7 posicoes / tamanho 34 | **RESOLVIDO** | `docs/audits/LinxProgOpPed-ProductionInvestigation.md` R2.4: producao confirma a mesma grade `36-44` sem tamanho 34 (nao e drift de DEV), mas existem grades alternativas cadastradas que incluem o 34 (ex. `"36 - 44 - 34"`), e a operacao e provadamente um rebalanceamento de grade (total inalterado, 77/77 linhas). Residual: qual grade cadastrada usar — decisao de catalogacao, nao tecnica |
| (c) Acesso ao banco (conexao) | **RESOLVIDO** | `docs/audits/AgentContractV1.1-ExecutionPolicy-Credenciais.md` secao 25 (Development) + `docs/audits/LinxProductionEndpointCorrectionV1.md` (Production, apos correcao do endpoint) |
| **7.6.1** Multiplicidade `ENTREGA_INICIAL`/`ENTREGA` nao filtrada | **CONFIRMED_BY_PRODUCTION_SCHEMA** (schema identico em PROD) | A solucao propria deve tratar isso explicitamente (filtrar por `ENTREGA_INICIAL`/`ENTREGA` ou validar cardinalidade 1 antes de propor escrita) |
| **7.6.5** Dados desta planilha ausentes do SOMA_DESENV | **Confirmado — dados existem em Producao** | `docs/audits/LinxProgOpPed-ProductionInvestigation.md` R2.5: 77/77 linhas encontradas em producao (77 PED) |

**Knowledge Gap residual (estreito, nao mais bloqueante para o entendimento, apenas para a proposta final): qual codigo de grade cadastrado (`"36 - 44 - 34"`, `34-44`, ou outro) deve substituir `PRODUTOS.GRADE` destes 39 produtos.** Ver `docs/audits/LinxProgOpPed-ProductionInvestigation.md` R2.11 para a pergunta objetiva completa ao Product Owner. Assim que respondida, a solucao tecnica propria (secao 19-21 da tarefa original) pode ser projetada com seguranca — schema, procedures, regra de classificacao e Delta ja estao todos `CONFIRMED_BY_PRODUCTION_*`.

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

### 7.10 Nova rodada de investigacao em Producao (`linx-production`, read-only) — confirma e amplia 7.6

Comando usado: `dotnet run --project backend/src/BlueprintOS.Api -- investigate-linx-prog-op-ped schema --env=production` (mesma ferramenta ja existente, `--env=production`, apenas `SELECT`/`INFORMATION_SCHEMA`/`OBJECT_DEFINITION`, nenhuma escrita, nenhum `EXEC` de procedure mutavel). Uma consulta ad hoc adicional (`REVENDA`, ver 7.10.4) foi feita adicionando temporariamente um modo `adhoc-revenda` ao mesmo `Program.cs`, executando, e revertendo o arquivo com `git checkout` imediatamente apos — **nenhuma mudanca permanente foi feita no codigo do investigador**.

#### 7.10.1 Numero real de posicoes — CONFIRMED_IN_PRODUCTION: 48, nao 6/7

| Tabela | Colunas de posicao confirmadas em Producao |
|---|---|
| `PRODUCAO_PROG_PROD` | `P1..P48` (delta/estado programado), `S1..S48`, `PO1..PO16` |
| `PRODUCAO_ORDEM_COR` | `O1..O48`, `P1..P48` |
| `COMPRAS_PRODUTO` | `CO1..CO48`, `CE1..CE48` |
| `PRODUTOS_TAMANHOS` | `TAMANHO_1..TAMANHO_48` (varchar, tamanho fisico real por posicao), `NUMERO_TAMANHOS`, `TAMANHOS_DIGITADOS`, `GRADE_BASE`, `GRADE_CODIGO`, `QUEBRA_1..QUEBRA_5` |

Isto **confirma e reforca** 7.6.2 (que ja afirmava "ate 48 posicoes genericas"): o numero real de posicoes suportadas pelo schema e **48**, para as 4 estruturas (grade, PROG, OP, PED). O "6 vs 7" do SQL historico era uma particularidade de uma execucao antiga (parametros `@S1..@S7`), nunca um limite do schema.

`PRODUTOS_TAMANHOS.GRADE` e a chave que relaciona a definicao de grade ao cadastro do produto — **nao ha FK declarada** (`INFORMATION_SCHEMA.TABLE_CONSTRAINTS`/`REFERENTIAL_CONSTRAINTS` nao foi consultada nesta rodada especificamente para FK, mas o join usado em toda a base de codigo e por igualdade de valor `PRODUTOS.GRADE = PRODUTOS_TAMANHOS.GRADE`, ambos `varchar`); tratar como relacionamento logico confirmado por uso consistente no schema e nos dados, nao como FK fisica verificada.

#### 7.10.2 As 4 procedures — definicao completa lida em Producao, `CONFIRMED_IN_PRODUCTION`

| Procedure | Parametros reais em Producao | Delta ou quantidade final? | Efeito colateral confirmado |
|---|---|---|---|
| `LX_ANM_GERA_OS_ALTERACAO_PCP` | `@XORDEM_PRODUCAO, @COR_PRODUTO, @S1..@S10, @RECALCULA_RESERVA=1, @EXECUTADO OUTPUT, @RETORNO OUTPUT` | **DELTA** — `UPDATE PRODUCAO_ORDEM_COR SET O1=O1+@S1,...,P1=P1+@S1,...` (quando a OP nao tem tarefa em processo) | Pre-valida saldo (`S{n}+@S{n} < 0` bloqueia com `'GRADE A SER ALTERADA MAIOR QUE O SALDO NA OP'`); se ha tarefa em processo, gera nova Ordem de Servico em vez de UPDATE direto; chama `LX_RECALCULO_RESERVA_MATERIAIS` se `@RECALCULA_RESERVA=1` |
| `LX_ANM_AJUSTA_PROGRAMACAO_PROD` | `@PROGRAMACAO, @PRODUTO, @COR_PRODUTO, @S1..@S10, @EXECUTADO OUTPUT, @RETORNO OUTPUT` | **DELTA** — `UPDATE PRODUCAO_PROG_PROD SET QTDE_PROGRAMADA += soma, P1..P10 += @S1..@S10, S1..S10 += @S1..@S10 WHERE PROGRAMACAO=@P AND PRODUTO=@P AND COR_PRODUTO=@C` | Nao verifica rowcount do UPDATE (risco ja documentado em 7.6, 7.6.4); tambem grava um log textual em `PRODUCAO_PROGRAMA.OBS` |
| `LX_MOVIMENTA_COMPRAS_PA` | `@PEDIDO` (unico parametro) | **QUANTIDADE FINAL** — nao recebe delta; recalcula `QTDE_ENTREGUE/QTDE_ENTREGAR/VALOR_ENTREGAR/VALOR_ENTREGUE` a partir da soma de `CE1..CE48`/e correspondentes `E1..E48` ja gravados em `COMPRAS_PRODUTO`, usando `PONTEIRO_PRECO_TAM` para escolher `CUSTO1..CUSTO4` por posicao | Le o que ja foi escrito por um `UPDATE` anterior (do chamador) — nao grava grade, so recalcula totais/valores dependentes |
| `LX_RECALCULO_RESERVA_MATERIAIS` | `@PRODUTO, @MOSTRA=NULL, @XORDEM_PRODUCAO=NULL, @XTIPO_PROCESSO=NULL, @MOSTRA_BLOCO_K=NULL` | N/A (nao mexe em grade de produto acabado) | Recalculo de reserva de materiais/ficha tecnica (`TIPO_RESERVA 1..7` documentados no cabecalho da procedure) |

**Confirma a semantica do script historico (secao 7.6 do documento, revalidada em Producao)**: PED usa `UPDATE COMPRAS_PRODUTO` com **quantidade final** feito pelo chamador (nao pelas procedures em si) e depois `LX_MOVIMENTA_COMPRAS_PA` + `LX_RECALCULO_RESERVA_MATERIAIS` para recalculo; OP e PROG usam as duas procedures de **delta** (`LX_ANM_GERA_OS_ALTERACAO_PCP`, `LX_ANM_AJUSTA_PROGRAMACAO_PROD`). Nenhuma das 4 procedures foi executada (`EXEC`) nesta investigacao — apenas `OBJECT_DEFINITION`/`sys.parameters`.

#### 7.10.3 Validacao de posicao invalida — regra aplicavel confirmada estruturalmente

A grade `36-44` (a mesma dos 39 produtos da planilha, confirmada identica em Producao por R2.4 de `LinxProgOpPed-ProductionInvestigation.md`) tem **5 posicoes reais** (`TAMANHO_1..5 = 36,38,40,42,44`; `TAMANHO_6..48` em branco). Isso significa que, para esta grade especifica, qualquer posicao 6 em diante com quantidade diferente de zero e `INVALID_GRADE_POSITION` por definicao estrutural (a posicao nao existe na grade cadastrada do produto) — e qualquer posicao 6+ com quantidade zero e apenas ausencia normal, nao erro. Esta e a regra generica (secao 7.11.5); a aplicacao concreta aos dados desta planilha depende de saber **qual coluna da planilha corresponde a qual posicao numerica** (ver 7.11.6 — permanece `NEEDS_VALIDATION`, nao resolvido nesta rodada).

#### 7.10.4 `PRODUTOS.REVENDA` — papel confirmado como evidencia auxiliar forte, nao regra absoluta

Consulta agregada real em Producao (`PRODUTOS` JOIN `EXISTS` em `PRODUCAO_ORDEM` e `COMPRAS_PRODUTO`, sobre os 248.472 produtos que tem pelo menos uma OP ou um Pedido registrado historicamente):

| `REVENDA` | Produtos com OP | Produtos com Pedido | Total com pelo menos um dos dois |
|---|---|---|---|
| `False` (fabricado, segundo o PO) | 145.984 (89,6%) | 83.495 (51,2%) | 162.936 |
| `True` (comprado, segundo o PO) | 8.214 (9,6%) | 85.222 (99,6%) | 85.536 |

**Interpretacao (`CONFIRMED_IN_PRODUCTION` para a correlacao estatistica, `INFERRED` para a causalidade):** `REVENDA=1` e um preditor quase perfeito de "tem Pedido de compra" (99,6%), confirmando fortemente a metade "comprado -> Pedido" da regra do PO. `REVENDA=0` e um preditor forte mas nao exclusivo de "tem OP" (89,6%) — mais de metade dos produtos `REVENDA=0` **tambem** tem historico de Pedido de compra (51,2%), o que e esperado (um produto fabricado pode ter itens/materia-prima comprados, ou ter mudado de estrategia de sourcing ao longo do tempo) e **confirma exatamente a instrucao do PO de tratar `REVENDA` como evidencia auxiliar, nunca como fonte de verdade sozinha** — o estado real de uma linha especifica (`PROGRAMACAO+PRODUTO+COR`) deve sempre ser determinado pela existencia transacional real em `PRODUCAO_ORDEM_COR`/`COMPRAS_PRODUTO`, nao apenas por `REVENDA`.

### 7.11 PRODUCT OWNER FUNCTIONAL KNOWLEDGE — GRADE ADJUSTMENT (ensinamento do PO, separado explicitamente da evidencia tecnica)

Esta secao registra a regra de negocio completa **como ensinada pelo Product Owner**, com cada item marcado quanto ao seu status de confirmacao tecnica nesta tarefa. Nenhum numero especifico deste caso (77 linhas, PO 1741979, tamanho 34) e generalizado como regra do modelo — esses numeros ficam apenas como evidencia do caso, aqui e em `LinxProgOpPed-ProductionInvestigation.md`.

1. **Chave funcional** (`PRODUCT_OWNER_KNOWLEDGE`, `CONFIRMED_BY_CODE_INSPECTION` como chave de join): `PROGRAMACAO + PRODUTO + COR` identifica uma linha de negocio. Nota tecnica adicional (7.6.1, mantida): a PK real de `PRODUCAO_PROG_PROD`/`COMPRAS_PRODUTO` tem uma 4a coluna (`ENTREGA_INICIAL`/`ENTREGA`) que a chave funcional do PO nao menciona — qualquer solucao tecnica deve tratar essa coluna extra explicitamente (filtrar ou validar cardinalidade 1), o ensinamento do PO nao contradiz isso, apenas nao entra nesse nivel de detalhe.
2. **Fluxo REVENDA -> PED/OP** (`PRODUCT_OWNER_KNOWLEDGE`, correlacao `CONFIRMED_IN_PRODUCTION` — ver 7.10.4): `PRODUTOS.REVENDA=1` (comprado) tende a Pedido de Compra; `REVENDA=0` (fabricado) tende a Ordem de Producao. **O PO e explicito, e a evidencia real confirma, que isto e evidencia auxiliar, nunca a fonte de verdade sozinha** — o estado real e sempre determinado pelas estruturas transacionais (existencia em `PRODUCAO_ORDEM_COR` ou `COMPRAS_PRODUTO`).
3. **Classificacao PROG/PED/OP por evidencia transacional, sem prioridade abstrata cega** (`PRODUCT_OWNER_KNOWLEDGE` + `CONFIRMED_BY_CODE_INSPECTION`/`CONFIRMED_IN_PRODUCTION` para a mecanica de cada tipo):
   - **PROG**: so existe em `PRODUCAO_PROG_PROD` (sem OP, sem Pedido); tipicamente `QTDE_EM_OP=0` e `QTDE_SALDO_EMITIR_OP>0` (evidencia do SQL historico, secao 7 acima). Ajuste via `LX_ANM_AJUSTA_PROGRAMACAO_PROD` (delta).
   - **PED**: chave existe em `COMPRAS`/`COMPRAS_PRODUTO`. Ajuste via `UPDATE COMPRAS_PRODUTO` (quantidade final, nao delta — 7.10.2) + `LX_MOVIMENTA_COMPRAS_PA` + `LX_RECALCULO_RESERVA_MATERIAIS`.
   - **OP**: chave existe em `PRODUCAO_ORDEM_COR`. **Nunca fazer UPDATE arbitrario na grade da OP** — precisa passar por `LX_ANM_GERA_OS_ALTERACAO_PCP` (delta, com pre-validacao de saldo) e o recalculo embutido nela.
   - **Prioridade OP > PED > PROG quando ha match em mais de uma tabela**, ja confirmada com dados reais de producao em `LinxProgOpPed-ProductionInvestigation.md` R2.5 (77/77 linhas classificadas como PED nesta rodada anterior). O SQL historico usa um `LEFT JOIN` de `UNION ALL` que **nao trata explicitamente** o caso teorico de uma chave casar em OP e em Pedido simultaneamente (produziria duplicidade na UNION ALL) — nenhuma evidencia real desse caso foi encontrada nesta tarefa nem na anterior; se encontrada no futuro, classificar como `AMBIGUOUS`/`DATA_INCONSISTENCY`, nunca escolher arbitrariamente.
   - **Estado inconsistente/impossivel**: nunca inventar — classificar `AMBIGUOUS`/`DATA_INCONSISTENCY` e investigar.
4. **Grade e posicional, nao por rotulo visual** (`PRODUCT_OWNER_KNOWLEDGE`, mecanismo de schema `CONFIRMED_IN_PRODUCTION` — 7.10.1): `TAM_1` da planilha corresponde a **posicao 1** da estrutura Linx aplicavel (`P1`/`O1`/`CO1`, conforme o tipo), independente do rotulo visual do tamanho que estiver cadastrado ali (`PRODUTOS_TAMANHOS.TAMANHO_1`) para aquele produto especifico. O rotulo visual (34, PP, GG etc.) vem de uma tabela de traducao posicao->rotulo separada (`PRODUTOS.GRADE` + `PRODUTOS_TAMANHOS`), e **nunca deve ser assumido igual entre produtos diferentes** so porque o numero da posicao e o mesmo.
5. **Validacao de posicoes** (`PRODUCT_OWNER_KNOWLEDGE`, estrutura de suporte `CONFIRMED_IN_PRODUCTION` — 7.10.1/7.10.3): se a planilha tem mais posicoes que a grade real do produto suporta (`NUMERO_TAMANHOS`/posicoes populadas em `PRODUTOS_TAMANHOS`), posicoes extras com quantidade `0` sao normais; posicoes extras com quantidade `<> 0` que nao existem na grade do produto sao **erro bloqueante** (`INVALID_GRADE_POSITION`) — nenhuma escrita para essa linha especifica.
6. **Delta vs quantidade final variam por mecanismo, nao sao a mesma semantica de input** (`CONFIRMED_IN_PRODUCTION` — 7.10.2): `LX_ANM_GERA_OS_ALTERACAO_PCP` e `LX_ANM_AJUSTA_PROGRAMACAO_PROD` esperam **delta** (quantidade desejada da planilha menos quantidade atual no Linx, por posicao); o ajuste de PED e **quantidade final** via `UPDATE` direto (nao ha delta explicito nas colunas `CO`/`CE`, o chamador grava o valor final desejado) seguido de recalculo. Delta total (soma de todas as posicoes) pode ser zero (rebalanceamento), positivo (aumento) ou negativo (reducao) — nao assumir sempre rebalanceamento so porque este caso especifico (77 linhas, ver 7.6.3/R2.4 em `LinxProgOpPed-ProductionInvestigation.md`) foi um rebalanceamento com delta total zero.

#### 7.11.6 Reclassificacao explicita do aprendizado anterior sobre "tamanho 34" / troca de `PRODUTOS.GRADE` (secao 7.6.3, `LinxProgOpPed-ProductionInvestigation.md` R2.11)

**Nao apagar o texto anterior (7.6.3, `LinxProgOpPed-ProductionInvestigation.md` R2.4/R2.11) — apenas reclassificar a conclusao.** A conclusao anterior — de que seria preciso trocar o codigo de `PRODUTOS.GRADE` destes 39 produtos para um codigo que inclua o tamanho 34 (ex. `"36 - 44 - 34"`) — foi construida **pela interpretacao do rotulo visual** ("o produto precisa vender tamanho 34, entao a grade cadastrada precisa ter o rotulo 34"), nao pela semantica posicional ensinada pelo PO nesta etapa (item 4 acima). Isso **nao deve mais ser mantido como a regra geral do modelo**.

A aplicacao estrita da regra posicional a este caso especifico, no entanto, **nao produz uma resposta trivial e permanece `NEEDS_VALIDATION`**: a planilha tem 6 colunas de quantidade na ordem `Q_34, Q_36, Q_38, Q_40, Q_42, Q_44`; se a correspondencia posicional for estritamente "1a coluna de quantidade = posicao 1", entao `Q_34` corresponderia a posicao 1 (`TAMANHO_1='36'` na grade `36-44` cadastrada) e `Q_44` corresponderia a posicao 6 — que **nao existe** na grade `36-44` (so tem 5 posicoes, 7.10.3). Isso bloquearia `Q_44` (nao-zero nas 77 linhas, soma 433 unidades) como `INVALID_GRADE_POSITION` em vez de `Q_34` (soma 829 unidades, secao anterior). Isso contradiz a premissa de que a operacao e um rebalanceamento legitimo de grade cuja intencao inclui o tamanho 34. **Portanto, a correspondencia exata entre cada coluna da planilha e a posicao numerica real na estrutura Linx para este caso especifico permanece um Knowledge Gap genuino, que so o Product Owner ou quem gerou a planilha pode confirmar** — nao e possivel inferir com seguranca, a partir do schema sozinho, se a planilha foi montada assumindo a grade atual (`36-44`, 5 posicoes) ou uma grade alternativa ja cadastrada que inclui o 34 (ex. `"36 - 44 - 34"`, que tem 6 posicoes na ordem `36,38,40,42,44,34` — tambem nao bate literalmente com a ordem `34,36,38,40,42,44` da planilha). **Nenhuma das duas hipoteses foi assumida como resposta; ambas ficam registradas como possibilidades a confirmar.**

**Pergunta objetiva revisada ao Product Owner:** para os produtos desta planilha, qual e a correspondencia exata entre cada coluna `Q_34/Q_36/Q_38/Q_40/Q_42/Q_44` e a posicao numerica (1..N) da grade Linx que deve ser usada — a posicao e definida pela ordem das colunas da planilha, pela ordem de uma grade especifica ja cadastrada (qual codigo?), ou por outro criterio (ex. mapeamento explicito por tamanho fisico, exigindo antes trocar `PRODUTOS.GRADE` para um codigo que contenha as 6 posicoes)? Enquanto isso nao for esclarecido, nenhuma proposta de escrita pode mapear as colunas da planilha com seguranca para posicoes reais do Linx — nem pelo criterio antigo (rotulo visual) nem por um novo criterio posicional assumido sem confirmacao.

#### 7.11.7 Resposta do Product Owner/compradora responsavel (2026-08-27) — gap reclassificado, execucao bloqueada por pre-requisito

A pergunta objetiva de 7.11.6 foi respondida pelo Product Owner apos consulta a compradora responsavel: **os produtos desta planilha realmente precisam ter o cadastro alterado de `PRODUTOS.GRADE = '36-44'` para `PRODUTOS.GRADE = '34-44'`.** A divergencia detectada pelo Agent nao era erro de interpretacao posicional nem confusao de rotulo visual — era um **pre-requisito cadastral real, confirmado pelo especialista funcional**.

**Reclassificacao do Knowledge Gap (7.9, 7.11.6):**

| Antes | Depois |
|---|---|
| `NEEDS_VALIDATION` / `KNOWLEDGE_GAP` (correspondencia coluna-planilha <-> posicao-Linx incerta) | `CONFIRMED_PRODUCT_GRADE_CHANGE_REQUIRED` (confirmado por decisao explicita do Product Owner/compradora, nao por inferencia tecnica) |

O gap **nao esta mais aberto como duvida de interpretacao** — a causa raiz e conhecida e confirmada. O que resta e um bloqueio de execucao, nao mais um gap de conhecimento.

**Fora de escopo deste caso (PROG/OP/PED):** a alteracao de `PRODUTOS.GRADE` de `36-44` para `34-44` **nao sera executada, proposta como SQL, nem automatizada dentro deste fluxo**. Segundo o Product Owner, essa mudanca tem processo proprio, com dependencias adicionais: autorizacoes especificas, liberacao/validacao de Auditoria, participacao do time do CD, ajuste de saldo de estoque, e demais controles operacionais. O Agent nao deve tentar contornar ou antecipar esse processo.

**Status do caso: `BLOCKED_BY_PREREQUISITE`** (motivo: `PRODUCT_GRADE_REGISTRATION_MISMATCH`) — ver "Resume Checkpoint" ao final deste documento.

## Resume Checkpoint

```
STATUS: BLOCKED_BY_PREREQUISITE

BLOCKER: Produtos ainda cadastrados com grade 36-44.

EXPECTED: Grade 34-44 regularizada pelo processo operacional apropriado
          (autorizacoes, Auditoria, time do CD, ajuste de saldo de estoque).

RESUME WHEN: Product Owner confirmar conclusao da alteracao cadastral
             (PRODUTOS.GRADE = '34-44' para os produtos desta planilha).

FIRST ACTION ON RESUME: Validar PRODUTOS.GRADE em producao READ-ONLY
                         (linx-production/SOMA, agents/DATABASE_CONNECTION_POLICY.md secao 18).
```

Ao retomar, seguir a sequencia: (1) validar `PRODUTOS.GRADE`/`PRODUTOS_TAMANHOS` read-only em producao para os produtos envolvidos; (2) confirmar que o pre-requisito foi de fato resolvido; (3) reler a planilha; (4) validar `PROGRAMACAO+PRODUTO+COR`; (5) classificar PROG/PED/OP; (6) obter quantidades atuais por posicao; (7) calcular DELTA por posicao; (8) gerar Impact Analysis; (9) so entao construir a proposta tecnica de escrita; (10) passar pela governanca normal (`ActionProposal -> Policy Engine -> Approval -> Tool Gateway`) antes de qualquer execucao. O conhecimento canonico ja confirmado (chave funcional, fluxo REVENDA, classificacao PROG/PED/OP, grade posicional, delta vs quantidade final, as 4 procedures — secao 7.11 acima) **nao precisa ser reinvestigado do zero** na retomada, apenas reaplicado ao novo estado apos a mudanca de grade.

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

1. **Bloqueante:** validar em Producao (`SOMA`, read-only, `agents/DATABASE_CONNECTION_POLICY.md` secao 18) se `PRODUTOS.GRADE`/`PRODUTOS_TAMANHOS` para estes produtos tambem carece do tamanho 34 (drift real confirmado) ou diverge de `SOMA_DESENV`; somente se o drift se confirmar em Producao a pergunta objetiva da secao 7.6.3/7.9 precisa necessariamente ir ao Product Owner.
2. Somente apos 7.6.3 resolvido, e com o resultado da validacao de producao acima: calcular o Delta real, completar o impact analysis quantitativo da secao 16 da tarefa, e projetar a solucao propria do Agent (SQL novo, `PROPOSED — NOT EXECUTED`, tratando explicitamente a multiplicidade `ENTREGA_INICIAL`/`ENTREGA` de 7.6.1) para submissao ao Governed Write Stack (`ActionProposal -> Policy Engine -> Approval -> Tool Gateway -> DRY_RUN`).
3. Apos a solucao propria existir, reavaliar a necessidade da capability `linx-production-purchase-grade-adjustment` com evidencia real, e submeter a decisao (evoluir `linx-database-specialist-agent` ou reaproveitar `soma-database-write-proposal`) para autorizacao humana explicita antes de qualquer `Agent Factory UPDATE`.
4. Investigar a divergencia de duplicidade de `PO_PEDIDO_COMPRA` na planilha (secao 7.1) com quem gerou o arquivo — em especial o caso `PO 1741979` com quantidades divergentes entre as duas linhas do mesmo pedido.
5. Investigar por que o snapshot estatico `docs/audits/AgentFactoryV2-AuditResults.json` (18 findings) diverge do resultado ao vivo atual do mesmo comando (12 findings) — fora do escopo desta tarefa, mas registrado como divergencia a esclarecer.
6. Confirmar com o dono do trabalho nao relacionado (`.ai/context/linx-wise-daily-integration.md`, `docs/operations/LinxWiseDailyIntegrationRunbook.md`, `scripts/linx_wise_daily_integration.py`) se essas mudancas devem ser commitadas separadamente — esta tarefa deliberadamente as deixou intocadas e fora do commit.
