# AgentLearningV1 — Politicas Canonicas De Aprendizado E Evolucao + Caso PROG/OP/PED

Status: accepted
Data: 2026-08-27
Escopo: Politicas canonicas de aprendizado de artefato de usuario e de evolucao de Agents (Parte 1), avaliadas com o caso real Linx PROG/OP/PED (Parte 2), que permanece em `WAITING_FOR_EVIDENCE`.

## Resumo Executivo

Esta tarefa formalizou duas politicas canonicas provider-agnostic aplicaveis a todos os Agents do SOMA BlueprintOS e a qualquer executor de IA:

1. **User Artifact Learning Policy** (`agents/USER_ARTIFACT_LEARNING_POLICY.md`): artefato de usuario e sempre evidencia/fonte de conhecimento, nunca instrucao executavel automatica.
2. **Capability Gap And Agent Evolution Policy** (`agents/CAPABILITY_GAP_AND_AGENT_EVOLUTION_POLICY.md`): ausencia de conhecimento ou capability interrompe o fluxo; evolucao de Agent e criacao de novo Agent exigem autorizacao humana explicita, nunca bypass.

Ambas foram implementadas com codigo real (`backend/src/BlueprintOS.Core/AI/Governance/UserArtifactLearningPolicy.cs` e `CapabilityGapAndAgentEvolutionPolicy.cs`), cobertas por 12 regras testadas (`UserArtifactLearningAndCapabilityGapPolicyTests.cs`), e passam a ser herdadas automaticamente por todo Agent atual e futuro por referencia documental (precedencia 1-2 da `EXECUTION_POLICY.md`/`AGENT_CONTRACT.md`), sem exigir mudanca em `agent.schema.json`.

O caso real PROG/OP/PED **nao foi processado** — os 3 artefatos necessarios (planilha real, SQL historico, explicacao funcional do Product Owner) nao estao disponiveis nesta sessao. O caso para formalmente em `WAITING_FOR_EVIDENCE`. Nada foi inventado.

## Baseline (Confirmado Por Inspecao Real Do Repositorio)

| Item | Esperado pelo usuario | Real (inspecionado) | Divergencia |
|---|---|---|---|
| Numero de Agents | 8 | **8** (`agents/echo-agent`, `agents/knowledge-agent`, `agents/agent-factory`, `agents/wise-agent`, `agents/security-lgpd-agent`, `agents/linx-database-specialist-agent`, `agents/showcase-agent`, `agents/linx-erp-specialist-agent`) | Nenhuma |
| Numero de capabilities (unicas, `capability_ownership` de todos os agent.yaml) | 15 | **15** (`agent-catalog-management`, `agent-compliance-audit`, `agent-contract-validation`, `agent-lifecycle-management`, `agent-registration`, `agent-security-compliance-check`, `agent-test-coordination`, `ai-runtime-echo`, `linx-database-analysis`, `linx-erp-functional-analysis`, `organizational-knowledge-query`, `security-privacy-review`, `showcase-read-only-collection`, `soma-database-write-proposal`, `wise-operational-analysis`) | Nenhuma |
| WARN na ultima auditoria Agent Factory v2 | 12 | **12 findings WARNING** ao executar `node tools/agents/agent-factory-cli.js AUDIT` agora (8 agentes, todos em status `WARN`, total de 12 achados de severidade WARNING somando os findings por agente), tanto antes quanto depois das mudancas desta tarefa | `docs/audits/AgentFactoryV2-Implementacao-e-Auditoria.md` (documento historico, linha ~140) registra **18 WARNING** de uma execucao anterior do repositorio, nao 12. Isso e uma divergencia real entre o documento historico e o estado atual do codigo/manifestos — nao foi maquiada. O comando real, executado nesta tarefa, confirma 12, batendo com a expectativa do usuario; o doc antigo ficou desatualizado (provavelmente o numero de checks aumentou ou os manifests evoluiram desde aquela auditoria). |

Itens adicionais confirmados por inspecao:

- `soma-database-write-proposal` existe em `agents/linx-database-specialist-agent/agent.yaml` (`capability_ownership.soma-database-write-proposal`). **CONFIRMED**.
- `can_execute_write: false` no `linx-database-specialist-agent` (`governance.can_execute_write`). **CONFIRMED**.
- `LIVE_EXECUTION` desabilitado no codigo: `backend/src/BlueprintOS.Core/AI/Governance/Models/GovernedWriteModels.cs` define `LiveExecutionEnabled = false` por padrao; `ToolGateway.cs` rejeita qualquer `ExecutionMode.LiveExecution` com o motivo `LIVE_EXECUTION_DISABLED`. **CONFIRMED**.
- Tool Gateway e dry-run only: `ToolGateway.cs` so expoe `DryRunAsync`/preview, auditando `gateway.dry-run.requested`/`gateway.dry-run.completed` com `DRY_RUN_ONLY`/`NO_EXTERNAL_EXECUTION`. **CONFIRMED**.

**Nenhum CONTRACT GAP foi encontrado.** `agent.schema.json` ja fixa estruturalmente (`const: false`) `gap_policy.direct_bypass_allowed`, `delegation.bypass_allowed`, e exige (`const: true`) `gap_policy.explicit_human_approval_required_for_new_agent` e `gap_policy.material_capability_change_requires_human_approval` para todo manifesto valido. As novas politicas formalizam e detalham essas regras ja existentes; nao exigem novo campo, novo enum ou mudanca de semantica no schema/contrato. `agent.schema.json` **nao foi alterado**.

## Politicas Canonicas — Onde Vivem

- `agents/USER_ARTIFACT_LEARNING_POLICY.md` (novo)
- `agents/CAPABILITY_GAP_AND_AGENT_EVOLUTION_POLICY.md` (novo)
- Referenciadas em `agents/AGENT_CONTRACT.md` (secao "Politicas Canonicas Relacionadas", apos "Documentos legados...") e em `agents/EXECUTION_POLICY.md` (secao "Politicas Canonicas Complementares", antes de "Regra Global"). Ambas as edicoes sao prosa aditiva; nenhum campo estrutural do contrato foi redefinido.

Este local segue o padrao ja existente do repositorio (`agents/EXECUTION_POLICY.md`, `agents/AGENT_CONTRACT.md` ao lado do `agent.schema.json`), evitando criar uma nova hierarquia de pastas ou mover arquivos.

## Mudancas Estruturais Feitas

Nenhuma mudanca estrutural em `agent.schema.json`. Nenhum campo obrigatorio novo. Nenhum `agent.yaml` foi alterado. A unica mudanca "estrutural" e a adicao de um novo check de auditoria, read-only, na Agent Factory v2 (ver abaixo).

## Impacto Na Agent Factory

`tools/agents/agent-factory-v2.js` ganhou um metodo `canonicalPolicyFindings()`, chamado dentro de `audit()` (`repositoryFindings`). Ele verifica que os dois arquivos de politica existem em `agents/` e que `agents/AGENT_CONTRACT.md` referencia seus caminhos textualmente; caso contrario, gera um finding `AFV2-POLICY-001` de severidade `WARNING`, categoria `GOVERNANCE`, sem exigir aprovacao humana (e um alerta informativo, nao uma trava de seguranca ja que a Factory permanece read-only em AUDIT). A mudanca e puramente aditiva: nao altera `VALIDATE`, `CREATE`, `UPDATE`, `REGISTER`, `CATALOG`, `TEST` ou `SECURITY_CHECK`, e nao modifica a assinatura de nenhuma funcao existente.

Resultado: rodando `node tools/agents/agent-factory-cli.js AUDIT` antes e depois desta mudanca, o total de findings permanece **12** (as politicas ja existem e ja estao referenciadas, entao o novo check nao adiciona nenhum finding). Ver `AgentLearningV1-LinxProgOpPed-Results.json` para os numeros machine-readable.

## Comportamento Provider-Agnostic (Confirmacao)

Nenhuma classe nova (`UserArtifactLearningPolicy`, `CapabilityGapAndAgentEvolutionPolicy`, e seus modelos) recebe parametro de provider, nem contem branch condicionado a "Codex"/"Claude"/"ChatGPT"/nome de modelo. O teste `Rule11_Behavior_IsProviderAgnostic` verifica isso de duas formas: (a) executando o classificador duas vezes com instancias diferentes e comparando resultado; (b) reflexao sobre os membros publicos das duas classes garantindo ausencia de qualquer nome relacionado a provider. As politicas em Markdown tambem declaram explicitamente "Escopo: qualquer IA, modelo, executor ou Agent... independente de provider".

## Testes Das 12 Regras

Arquivo: `backend/tests/BlueprintOS.UnitTests/Core/AI/Governance/UserArtifactLearningAndCapabilityGapPolicyTests.cs` (13 `[Fact]`/`[Theory]`, cobrindo as 12 regras solicitadas — a regra 10 tem cobertura dupla via `[Theory]` + `[Fact]` adicional para o flag explicito `ContainsSecret`).

1. SQL do usuario nao e automaticamente executavel — `Rule01`.
2. Artefato historico e evidence, nunca comando — `Rule02`.
3. Artefato nao constitui approval — `Rule03`.
4. Knowledge gap interrompe o fluxo — `Rule04`.
5. Capability gap interrompe o fluxo — `Rule05`.
6. Ausencia de owner propoe Agent, nao cria automaticamente — `Rule06`.
7. Evolucao material exige autorizacao explicita — `Rule07`.
8. Conhecimento validado pode ser persistido — `Rule08`.
9. Inferencia nao vira CONFIRMED automaticamente — `Rule09`.
10. Segredo nao entra no knowledge store — `Rule10` (Theory + Fact).
11. Comportamento identico independente de provider — `Rule11`.
12. Bypass/LIVE_EXECUTION continuam false — `Rule12` (nesta camada, nenhuma resolucao de gap concede execucao automatica; os invariantes de `bypass_allowed=false` e `LIVE_EXECUTION_DISABLED` continuam cobertos, sem regressao, pelos testes ja existentes do Governed Write Stack e da Agent Factory v2/Runtime Registry/Showcase/WISE safety suites).

## Artefatos Recebidos (Caso Real)

**Nenhum.** Nem planilha, nem SQL historico, nem explicacao funcional foram fornecidos ou encontrados em `downloads/` ou em qualquer outro lugar do repositorio nesta sessao. Confirmado por busca — nao existe planilha de ajuste de grade PROG/OP/PED nem SQL historico correspondente no repositorio.

## O Que Seria Aprendido Do SQL Historico

N/A — sem evidencia. Nao ha SQL historico disponivel para estudo. Qualquer afirmacao sobre o que ele revelaria seria invencao e foi deliberadamente evitada.

## O Que Seria Aprendido Da Planilha

N/A — sem evidencia. Nao ha planilha disponivel para estudo.

## Inconsistencias

N/A — sem dados para comparar.

## Schema Validado

Nao aplicavel ao caso real: nenhuma inspecao de schema de banco de producao/homologacao foi realizada nesta tarefa (fora de escopo — somente leitura/planejamento, sem conexao real). `NEEDS_VALIDATION` para qualquer schema de tabela associada a PROG/OP/PED.

## Procedures Estudadas

N/A — nenhuma procedure foi fornecida nem estudada.

## Modelo Funcional PROG/OP/PED (Conceitual — Nao E Analise Real)

**Rotulo: HISTORICAL_REFERENCE / INFERRED conceitual, nao CONFIRMED.** O fluxo abaixo descreve como o `linx-database-specialist-agent` processaria o caso **quando** os 3 artefatos chegarem — e um modelo do processo, nao um resultado de analise:

```text
ARTEFATO (planilha + SQL historico + explicacao PO)
  -> ESTUDAR (ler os 3 artefatos por completo)
  -> INTENCAO (o que o ajuste de grade PROG/OP/PED pretende resolver)
  -> REGRAS DE NEGOCIO (extrair do SQL/planilha quais campos/condicoes definem um ajuste valido)
  -> HIPOTESES (o que parece verdadeiro mas nao foi confirmado por schema/PO)
  -> COMPARACAO com conhecimento atual do linx-database-specialist-agent (knowledge.memory_paths)
  -> VALIDACAO contra schema real do banco Linx (via SELECT/metadata read-only, nunca escrita)
  -> LACUNAS (o que nao fecha: Knowledge Gap ou Capability Gap)
  -> PERGUNTAS ao Product Owner quando a lacuna exigir esclarecimento humano
  -> APRENDIZADO (persistir so o validado, com proveniencia, sem segredo)
  -> SOLUCAO PROPRIA (design de ActionProposal, nunca reexecucao literal do SQL historico)
  -> VALIDACAO da solucao
  -> GOVERNANCA: ActionProposal -> AIGovernancePolicyEngine -> ApprovalPolicy
  -> PROPOSTA DE EXECUCAO via Governed Write Stack, sempre em dry-run enquanto LIVE_EXECUTION=false
```

Nenhum passo deste modelo foi executado com dados reais nesta tarefa.

## Grade Detectada

N/A — `UNKNOWN`. Sem planilha, nao ha grade para detectar.

## Estrategia De Staging (Conceitual, Nao Implementada)

Proposta conceitual, sujeita a revisao quando os artefatos chegarem: uma tabela/area de staging read-only receberia os dados extraidos da planilha real, isolada dos dados de producao Linx/WISE, permitindo comparacao (`Compare`) contra o estado atual antes de qualquer `ActionProposal` de escrita. Nenhuma tabela de staging foi criada; isto e apenas uma proposta de desenho, sem implementacao.

## Estrategia De Normalizacao (Conceitual)

Proposta conceitual: os campos PROG/OP/PED extraidos da planilha seriam normalizados (tipo, precisao, trim, mapeamento de codigo) segundo o schema real confirmado por inspecao, antes de qualquer comparacao. Sem os artefatos, nao ha regra concreta de normalizacao a declarar.

## Dataset De Diferencas

N/A — sem dados reais para comparar.

## Impact Analysis

`UNKNOWN` — sem planilha/SQL reais, o numero de registros afetados, tabelas envolvidas e risco de reversibilidade permanecem desconhecidos. Nenhuma estimativa foi inventada.

## Duvidas Em Aberto

1. Qual e a definicao exata de PROG, OP e PED no dominio Linx (Product Owner deve esclarecer)?
2. A planilha de ajuste de grade representa o estado alvo completo ou apenas o delta a aplicar?
3. O SQL historico e um exemplo de consulta valida ou de correcao ja aplicada no passado (proveniencia `HISTORICAL_REFERENCE` vs `RUNBOOK`)?
4. Existe uma tabela/procedure Linx ja mapeada em `backend/src/BlueprintOS.Infrastructure/Integrations/ERP/Soma/*` relacionada a PROG/OP/PED, ou o dominio e inteiramente novo para o `linx-database-specialist-agent`?

## Conhecimento Incorporado Ao Linx Agent

Apenas o generico e valido, sem depender do caso real:

- As duas politicas canonicas (User Artifact Learning / Capability Gap and Agent Evolution) agora fazem parte do bootstrap de todo Agent, incluindo `linx-database-specialist-agent`, por referencia em `AGENT_CONTRACT.md`/`EXECUTION_POLICY.md` (precedencia 1-2, herdada por todos).
- Nenhum conhecimento especifico de PROG/OP/PED foi incorporado ao knowledge store do `linx-database-specialist-agent` — nao ha `knowledge.memory_paths` alterado, pois nao ha conhecimento validado a persistir.

## Knowledge Gaps Restantes

Ver secao "Duvidas Em Aberto" e `AgentLearningV1-LinxProgOpPed-Results.json.knowledge_gaps`. Resumo: semantica funcional PROG/OP/PED, estrutura real da grade, SQL de referencia, e schema real das tabelas envolvidas — todos pendentes dos 3 artefatos.

## Capability Gaps

Nenhum capability gap tecnico foi identificado que bloqueie o fluxo conceitual: `linx-database-analysis` (leitura/analise) e `soma-database-write-proposal` (proposta de escrita governada) do `linx-database-specialist-agent` ja cobrem, em principio, leitura e proposta de escrita para este dominio. A questao em aberto e se a granularidade dessas duas capabilities e suficiente ou se merece uma capability mais especifica — ver secao seguinte.

## Avaliacao Da Capability `linx-production-purchase-grade-adjustment` (PROPOSTA — Nao Criada)

**Isto e analise/proposta apenas. Nenhuma capability ou Agent foi criado.**

- **Necessaria?** Nao no sentido estrito: `linx-database-analysis` (leitura/analise de schema e dados Linx) somada a `soma-database-write-proposal` (proposta de escrita governada, ja `can_execute_write: false`, exigindo `ActionProposal`/Policy Engine/Approval) ja cobrem tecnicamente o ciclo leitura -> proposta de escrita necessario para o ajuste de grade PROG/OP/PED.
- **Util?** Potencialmente, se o dominio PROG/OP/PED tiver regras de negocio complexas e recorrentes o suficiente para justificar conhecimento dedicado (ex.: uma rotina periodica de ajuste de grade, distinta de outras analises Linx ad hoc). Isso so pode ser avaliado com os 3 artefatos em maos.
- **Redundante?** Ha risco real de redundancia: criar uma capability por caso de uso especifico dentro do mesmo dominio (Linx/ERP Soma) pode fragmentar responsabilidade que hoje pertence coerentemente ao `linx-database-specialist-agent` via suas duas capabilities gerais.
- **Responsabilidade correta?** Se criada, o owner natural seria o proprio `linx-database-specialist-agent` (evolucao, nao novo Agent) — o dominio (dados Linx/ERP Soma) e o mesmo.
- **Risco de granularidade excessiva?** Sim — capabilities excessivamente especificas por caso de uso tendem a inflar o catalogo sem ganho de seguranca ou clareza, contrariando a diretriz "nao transformar um Agent em faz tudo" e tambem "nao fragmentar capability desnecessariamente".
- **Recomendacao (PROPOSTA, aguardando autorizacao):** reaproveitar `linx-database-analysis` + `soma-database-write-proposal` como capabilities gerais, documentando o caso PROG/OP/PED como um *runbook*/knowledge especifico dentro do `linx-database-specialist-agent` (ex.: um arquivo em `knowledge.memory_paths` referenciando o caso), em vez de criar uma capability nova. Uma capability dedicada so deveria ser proposta se, apos os artefatos chegarem, ficar evidente que o caso exige uma politica de aprovacao/risco distinta da generica de `soma-database-write-proposal` (ex.: reversibilidade ou volume de linhas muito diferentes do padrao). **Nenhuma capability foi criada; isto e apenas a analise solicitada, aguardando decisao humana.**

## Governed Write Stack — Confirmacao

Dry-run confirmado (`ToolGateway.DryRunAsync`), `LIVE_EXECUTION_DISABLED` confirmado, nenhuma escrita real proposta ou executada para o caso PROG/OP/PED (nao ha `ActionProposal` gerado, pois nao ha dados reais para basea-lo).

## Security/LGPD

Nenhum dado real foi processado; nao ha PII/dado sensivel envolvido nesta tarefa. Quando os artefatos chegarem, o `security-lgpd-agent` (transversal, consultivo) devera participar da avaliacao antes de qualquer `ActionProposal` real, conforme `delegation.cross_cutting` do seu manifesto.

## Policy Decision

`BLOCKED_PENDING_EVIDENCE` — nao ha proposta de acao real a avaliar pelo `AIGovernancePolicyEngine` nesta tarefa.

## Approval Requirement

Qualquer execucao futura real exigira `ActionProposal` valido, avaliacao do `AIGovernancePolicyEngine`, `ApprovalPolicy` quando aplicavel, verificacao de identidade/permissao efetiva, e `Tool/Adapter` governado — com `LIVE_EXECUTION` permanecendo desabilitado ate decisao humana explicita separada desta tarefa.

## SQL Proposto

Nenhum. **Confirmado: nenhum SQL foi executado ou proposto para o caso real.**

## Testes Executados E Resultado

- `dotnet test tests/BlueprintOS.UnitTests --filter FullyQualifiedName~Governance`: **46 passed, 0 failed** (inclui os testes preexistentes do Governed Write Stack — ProposalHash, expired approval, revoked approval, changed proposal, UPDATE sem filtro, TRUNCATE, SecretCredential, PII export, identity permission, privilege escalation, LIVE disabled — mais os 13 novos testes das 12 regras).
- `dotnet test tests/BlueprintOS.UnitTests` (suite completa): **904 passed, 0 failed**.
- `node tools/agents/agent-factory-v2.test.js`: PASS.
- `node tools/agents/validate-agent-manifests.test.js`: PASS.
- `node tools/agents/governed-orchestrator.test.js`: PASS.
- `node tools/agents/runtime-registry.test.js`: PASS.
- `node tools/agents/showcase-agent-safety.test.js`: PASS.
- `node tools/agents/wise-agent-safety.test.js`: PASS.

Nenhum teste de integracao que exija banco real foi executado. Nenhum `dotnet ef` conectado a banco real foi usado.

## Agent Factory Audit — Antes/Depois

Ambas as execucoes de `node tools/agents/agent-factory-cli.js AUDIT` (antes e depois da adicao do check `AFV2-POLICY-001`) retornam `status: WARN`, 8 agentes, **12 findings WARNING** no total, 0 findings de repositorio adicionais — a mudanca na Factory e puramente aditiva/informativa e nao altera o resultado porque as politicas ja existem e ja estao referenciadas no momento da segunda execucao. Ver `AgentLearningV1-LinxProgOpPed-Results.json` para os numeros machine-readable.

## Arquivos Criados

- `agents/USER_ARTIFACT_LEARNING_POLICY.md`
- `agents/CAPABILITY_GAP_AND_AGENT_EVOLUTION_POLICY.md`
- `backend/src/BlueprintOS.Core/AI/Governance/UserArtifactLearningPolicy.cs`
- `backend/src/BlueprintOS.Core/AI/Governance/Models/UserArtifactLearningModels.cs`
- `backend/src/BlueprintOS.Core/AI/Governance/CapabilityGapAndAgentEvolutionPolicy.cs`
- `backend/src/BlueprintOS.Core/AI/Governance/Models/CapabilityGapModels.cs`
- `backend/tests/BlueprintOS.UnitTests/Core/AI/Governance/UserArtifactLearningAndCapabilityGapPolicyTests.cs`
- `docs/audits/AgentLearningV1-LinxProgOpPed.md` (este documento)
- `docs/audits/AgentLearningV1-LinxProgOpPed-Results.json`

## Arquivos Alterados

- `agents/AGENT_CONTRACT.md` (secao de referencia as novas politicas, prosa aditiva)
- `agents/EXECUTION_POLICY.md` (secao de referencia as novas politicas, prosa aditiva)
- `tools/agents/agent-factory-v2.js` (novo check `canonicalPolicyFindings()` chamado em `audit()`, aditivo)

## Arquivos Explicitamente Nao Alterados

- `.ai/prompts/processar-planilha-integracao-linx-wise.md`, `.ai/context/linx-wise-daily-integration.md`, `docs/operations/LinxWiseDailyIntegrationRunbook.md`, `scripts/linx_wise_daily_integration.py` (fluxo diario Linx/WISE — intacto)
- Showcase agent/collector — intacto
- `agents/agent.schema.json` — intacto (nao houve Contract Gap)
- `docs/agents/AgentsCatalog.html` — intacto (nenhum Agent foi criado/removido/renomeado; documento explicitamente nao-canonico)
- Toda a area de Suppliers/Fornecedores ja modificada no worktree antes desta tarefa (`frontend/web/src/procurement/suppliers/*`, `backend/*/Suppliers/*`, `backend/*/Procurement/Suppliers/*`, `backend/*/Fornecedor*`, `frontend/web/src/core/AppRoutes.tsx`, `.ai/dashboard/DASHBOARD_STATE.md`) — nao tocada, nao commitada, nao revertida.

## Riscos

- O check `AFV2-POLICY-001` e apenas textual (`String.includes`); se o caminho for referenciado com formatacao diferente (ex.: link relativo diferente), pode gerar falso WARNING. Risco baixo, mitigado por manter o texto exato do caminho nas duas edicoes.
- A divergencia entre `docs/audits/AgentFactoryV2-Implementacao-e-Auditoria.md` (18 WARNING) e o estado atual (12 WARNING) nao foi investigada a fundo (fora de escopo desta tarefa); recomenda-se auditoria dedicada para entender a causa da mudanca (evolucao de manifests desde aquele doc).
- A capability `linx-production-purchase-grade-adjustment` permanece apenas como proposta; se o caso real chegar antes de uma decisao humana, o Agent devera reavaliar a analise acima com os dados reais em maos.

## Proximos Passos

1. Aguardar os 3 artefatos (A: planilha real; B: SQL historico; C: explicacao funcional do PO) para retomar o caso PROG/OP/PED.
2. Quando chegarem, seguir o fluxo `ARTEFATO -> ESTUDAR -> ... -> GOVERNANCA -> PROPOSTA DE EXECUCAO` descrito acima, com dry-run e sem LIVE_EXECUTION.
3. Decidir, com o Product Owner, se `linx-production-purchase-grade-adjustment` deve ser proposta formalmente como capability nova ou se o caso cabe nas capabilities existentes do `linx-database-specialist-agent`.
4. Investigar a divergencia de contagem de WARNING (18 vs 12) entre o doc historico e o estado atual, se relevante para auditoria de compliance continua.
