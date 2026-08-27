# AgentLearningV1 — Politicas Canonicas De Aprendizado E Evolucao + Caso Linx PROG/OP/PED

Status: accepted
Data: 2026-08-27
Escopo: `agents/`, `backend/src/BlueprintOS.Core/AI/Governance/`, `tools/agents/agent-factory-v2.js`, testes .NET e Node associados.

## 1. Resumo Executivo

Esta tarefa teve dois objetivos independentes:

1. Formalizar e implementar, com codigo e testes reais, duas politicas canonicas aplicaveis a todos os Agents e a qualquer IA/executor (Codex, Claude, ChatGPT ou futuros): **User Artifact Learning Policy** e **Capability Gap & Agent Evolution Policy**.
2. Processar o caso real de negocio "PROG/OP/PED" (ajuste de grade de producao/compra) usando o Linx Agent.

Resultado do item 1: **CONFIRMED**. As duas politicas existem como documentos canonicos, sao referenciadas por `AGENT_CONTRACT.md`/`EXECUTION_POLICY.md`, sao verificadas pela Agent Factory v2 em `AUDIT`, e sao implementadas com codigo real em `backend/src/BlueprintOS.Core/AI/Governance/` com 15 testes automatizados cobrindo as 12 regras exigidas.

Resultado do item 2: **WAITING_FOR_EVIDENCE**. Os artefatos reais (planilha de ajuste de grade, SQL historico/modelo, explicacao funcional do Product Owner) nao estao disponiveis nesta sessao e nao foram inventados. Nenhuma analise real do caso PROG/OP/PED foi produzida; apenas um fluxo conceitual, claramente rotulado, e uma avaliacao arquitetural sobre a necessidade de uma capability especifica.

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

## 7. Caso Real PROG/OP/PED — WAITING_FOR_EVIDENCE

**CONFIRMED**: nenhum artefato do caso real foi encontrado nesta sessao, em `downloads/` ou em qualquer outro local do repositorio. Nenhum dado de planilha ou SQL historico foi inventado.

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

1. Obter os 3 artefatos do caso PROG/OP/PED (planilha real, SQL historico/modelo, explicacao funcional do PO) para sair de `WAITING_FOR_EVIDENCE`.
2. Apos os artefatos chegarem, reavaliar a necessidade da capability `linx-production-purchase-grade-adjustment` (secao 7.2) com evidencia real, e submeter a decisao (evoluir `linx-database-specialist-agent` ou reaproveitar `soma-database-write-proposal`) para autorizacao humana explicita antes de qualquer `Agent Factory UPDATE`.
3. Investigar por que o snapshot estatico `docs/audits/AgentFactoryV2-AuditResults.json` (18 findings) diverge do resultado ao vivo atual do mesmo comando (12 findings) — fora do escopo desta tarefa, mas registrado como divergencia a esclarecer.
4. Confirmar com o dono do trabalho nao relacionado (`.ai/context/linx-wise-daily-integration.md`, `docs/operations/LinxWiseDailyIntegrationRunbook.md`, `scripts/linx_wise_daily_integration.py`) se essas mudancas devem ser commitadas separadamente — esta tarefa deliberadamente as deixou intocadas e fora do commit.
