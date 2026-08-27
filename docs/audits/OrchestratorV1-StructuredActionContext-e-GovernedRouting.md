# Orchestrator v1 - Structured Action Context e Governed Routing

Data: 2026-08-27  
Contrato: Agent Contract v1.1

## 1. Resumo Executivo

CONFIRMADO: o Orchestrator v1 foi implementado como infraestrutura provider-agnostic e read-only. Ele recebe contexto estruturado, resolve capabilities pelo Runtime Registry, determina participação cross-cutting, monta plano governado e para antes da execução.

CONFIRMADO: não executa tool, SQL, browser, API, workflow, Policy Engine ou Agent. Não concede approval, não resolve credencial e não inventa capability.

## 2. Baseline

CONFIRMADO: o baseline possui 8 Agents, 14 capabilities, 13 ownerships primários, 1 complementar e 1 Agent cross-cutting. A Agent Factory mantém 12 warnings: 8 `AFV2-GOV-001` e 4 `AFV2-GATEWAY-001`. `soma-database-write` permanece ausente e gera Capability Gap.

## 3. Arquitetura

```text
StructuredActionContext
  -> GovernedOrchestrator
     -> explicit/minimal deterministic capability mapping
     -> RuntimeRegistry
     -> deterministic cross-cutting evaluation
  -> GovernedExecutionPlan
  -> STOP
```

CONFIRMADO: o Orchestrator é infraestrutura, não Agent. Registry resolve ownership; Orchestrator coordena; AI Governance decide política; futuro Tool Gateway executará.

## 4. Structured Action Context

CONFIRMADO: `tools/agents/structured-action-context.js` normaliza e valida request ID, solicitante, ambiente, sistema, tipo/recurso, intent, capabilities, campos, filtro, impacto esperado, finalidade, classificação, indicadores de dados sensíveis, reversibilidade, runbook, workflow e connection profile.

CONFIRMADO: valores desconhecidos permanecem explícitos e geram context gaps quando impedem planejamento seguro. Texto livre adicional não é usado para inferir capability ou autorização.

## 5. ActionContext vs ActionProposal

CONFIRMADO: `StructuredActionContext` descreve intenção. `ActionProposal` representa ação concreta pronta para avaliação determinística. O Orchestrator apenas marca `action_proposal_required`; não cria proposal, não calcula risco e não chama Policy Engine.

## 6. Operation Intent

CONFIRMADO: intents suportados são `READ`, `ANALYZE`, `EXPORT`, `CREATE`, `UPDATE`, `DELETE`, `TRUNCATE`, `EXECUTE_WORKFLOW`, `CONFIGURE` e `UNKNOWN`. Esse vocabulário é próprio porque uma intenção ampla não equivale necessariamente a uma operação concreta de `ActionProposal`.

## 7. Environment

CONFIRMADO: os nomes `Unknown`, `Development`, `Homologation` e `Production` reutilizam `GovernanceEnvironment`. Produção isoladamente não inclui Security/LGPD, pois esse critério não está declarado sozinho no manifesto.

## 8. Purpose

CONFIRMADO: finalidade vazia em mutação ou exportação produz `SENSITIVE_PURPOSE_MISSING`. O Orchestrator nunca inventa finalidade.

## 9. Impact

CONFIRMADO: `expected_affected_rows` aceita inteiro não negativo ou desconhecido. UPDATE/EXPORT sem estimativa produz context gap; nenhuma contagem real é executada.

## 10. Data Classification

CONFIRMADO: `DataClassification` é reutilizado por nome: `Unknown`, `Public`, `Internal`, `Confidential`, `PersonalData`, `SensitivePersonalData` e `SecretCredential`. `Unknown` não é reclassificado e aciona revisão transversal por corresponder a sinal Yellow da política existente.

## 11. Capability Resolution

CONFIRMADO: capabilities explícitas são preferenciais. Há somente três mappings determinísticos comprováveis: Showcase READ com purpose de coleta, WISE READ/ANALYZE e SOMA/Linx ANALYZE. Sem capability explícita ou regra inequívoca, o plano produz `CAPABILITY_RESOLUTION_CONTEXT_GAP`.

CONFIRMADO: ownership, complementary, gap e conflito vêm do Runtime Registry injetado; não foram reimplementados.

## 12. Cross-Cutting Resolution

CONFIRMADO: Security/LGPD é incluído quando o contexto comprova dado pessoal/sensível, segredo, classificação desconhecida relevante, escrita/efeito externo, exportação material/sensível ou ação destrutiva. Essas razões derivam dos critérios do manifesto e sinais determinísticos da política existente.

CONFIRMADO: Showcase e WISE read-only, classificados e sem dado sensível não incluem Security/LGPD. Exportação sem impacto conhecido produz `CROSS_CUTTING_EXPORT_IMPACT_UNKNOWN`, sem escolha silenciosa.

## 13. Governed Execution Plan

CONFIRMADO: o plano contém resumo de contexto, capabilities, rotas, primary/complementary/cross-cutting, gaps, conflitos, workflows, runbook, perfis lógicos, sinais de sensibilidade, necessidade de proposal/approval, status e próximos passos. Todos os planos retornam `execution_performed: false`, `approval_granted: false` e `direct_bypass_allowed: false`.

## 14. Capability Gap

CONFIRMADO: qualquer Capability Gap bloqueia o plano após validação do contexto. Os próximos passos são avaliar evolução do Agent existente, owner alternativo e somente então proposta de novo Agent com autorização explícita.

## 15. Context Gap

CONFIRMADO: campos estruturais ausentes/desconhecidos, UPDATE sem filtro/impacto, exportação sem impacto e capability não resolvível geram `BLOCKED_CONTEXT_GAP`. Valores não são inferidos.

## 16. Routing Conflict

CONFIRMADO: conflito retornado pelo Registry produz `BLOCKED_ROUTING_CONFLICT`. Teste com Registry injetado comprova que nenhum primary é escolhido arbitrariamente.

## 17. Workflow e Runbook Awareness

CONFIRMADO: workflows das rotas e referência explícita do contexto são preservados. Runbook é propagado como referência, nunca como autorização universal. Integração Linx/WISE não foi executada nem transformada em Agent.

## 18. Connection Profiles

CONFIRMADO: o plano propaga somente IDs lógicos informados ou descobertos nas rotas.

## 19. Credential Handling

CONFIRMADO: presença de profile gera `credential_resolution_required: true` para infraestrutura futura. Nenhuma credencial é testada, solicitada ou carregada.

## 20. Observabilidade

CONFIRMADO: observer injetável emite `orchestrator.plan.started`, `orchestrator.capability.resolved`, `orchestrator.capability.gap`, `orchestrator.crosscutting.resolved`, `orchestrator.context.gap` e `orchestrator.plan.completed`. Eventos contêm IDs, categorias, status e contagens, sem payload integral ou dado sensível.

## 21. Testes

CONFIRMADO: testes cobrem read-only, UPDATE, produção, classificação, capability explícita/derivada/inexistente, gaps, bypass, Registry reutilizado, primary/complementary, cross-cutting positivo/negativo, contexto insuficiente, conflito, múltiplas capabilities, workflow, runbook, profile lógico, ActionProposal requerido, ausência de approval/tool/mutação e observabilidade redigida.

## 22. Simulacao SOMA

CONFIRMADO: UPDATE estruturado em produção resolveu `linx-database-analysis` para `linx-database-specialist-agent`; `soma-database-write` permaneceu Capability Gap. Security/LGPD foi incluído por escrita, ActionProposal foi marcado como requerido e o plano terminou `BLOCKED_CAPABILITY_GAP`, sem SQL e sem bypass.

## 23. Simulacao Showcase

CONFIRMADO: contexto read-only de coleta derivou `showcase-read-only-collection`, owner `showcase-agent`, enforcement `DOCUMENTAL`, workflows preservados e status `READ_ONLY_PLAN`. Sem critério sensível, Security/LGPD não foi incluído. Nenhuma API/browser foi chamada.

## 24. Simulacao WISE

CONFIRMADO: contexto de análise derivou `wise-operational-analysis`, owner `wise-agent`, enforcement `DOCUMENTAL` e workflows Linx/WISE e Showcase preservados. Security/LGPD não foi incluído. Nenhuma conexão foi aberta.

## 25. Simulacao Export PII

CONFIRMADO: exportação fictícia de 20.000 registros pessoais incluiu Security/LGPD, marcou ActionProposal/approval como necessários e produziu Capability Gap para `fictional-pii-export`. Status `BLOCKED_CAPABILITY_GAP`; nenhuma exportação ocorreu.

## 26. Simulacao Destructive

CONFIRMADO: TRUNCATE fictício incluiu Security/LGPD, marcou ActionProposal como requerido e produziu Capability Gap para `fictional-destructive-operation`. O plano foi bloqueado, sem tool e com bypass proibido.

## 27. Reauditoria

CONFIRMADO: Agent Factory v2 AUDIT permaneceu `WARN`. O auditor e os manifests não foram alterados.

## 28. Findings Antes e Depois

| Finding | Antes | Depois |
| --- | ---: | ---: |
| `AFV2-GOV-001` | 8 | 8 |
| `AFV2-GATEWAY-001` | 4 | 4 |
| Total | 12 | 12 |

## 29. Gaps

AINDA_NAO_MAPEADO: NLU/parser de linguagem natural, criação concreta de ActionProposal, execução do Policy Engine pelo fluxo, approval persistence e Tool Gateway. CONFIRMADO: `soma-database-write` continua ausente.

## 30. Riscos

INFERIDO: mappings determinísticos podem crescer de forma frágil se usados como catálogo paralelo. A mitigação v1 é manter apenas três regras inequívocas e preferir capabilities explícitas. INFERIDO: consumidores podem confundir `READY_FOR_GOVERNANCE` com autorização; os campos negativos explícitos e a separação do Policy Engine mitigam esse risco.

## 31. Arquivos Criados

- `tools/agents/structured-action-context.js`
- `tools/agents/governed-orchestrator.js`
- `tools/agents/governed-orchestrator.test.js`
- `docs/audits/OrchestratorV1-StructuredActionContext-e-GovernedRouting.md`
- `docs/audits/OrchestratorV1-PlanResults.json`

## 32. Arquivos Alterados

CONFIRMADO: nenhum arquivo existente foi alterado. Contract, schema, manifests, Factory, Registry, Linx/WISE e Showcase permaneceram intactos.

## 33. Git Diff

CONFIRMADO: o escopo é composto somente pelos cinco arquivos novos listados. Mudanças preexistentes no worktree serão excluídas do staging e commit.

## 34. Proximos Passos

1. Definir contrato de NLU que produza StructuredActionContext sem executar orchestration.
2. Criar adapter explícito de contexto validado para ActionProposal concreto.
3. Integrar Policy Engine sem transferir autorização ao Orchestrator.
4. Implementar approval persistence e Tool Gateway em etapas separadas.
5. Product Owner decidir a Capability Gap `soma-database-write`.

## Validacao Final

```text
PASS: 8 Agent Contract v1.1 manifests validated
PASS: 7 negative validator scenarios rejected
PASS: Agent Factory v2 lifecycle, audit and safety tests
PASS: Runtime Registry v1 discovery, routing, gaps, conflicts and safety tests
PASS: Governed Orchestrator v1 context, routing, cross-cutting and safety tests
PASS: WISE Agent offline safety invariants
PASS: Showcase Agent offline safety invariants
PASS: no concrete secret material detected in scoped files
```

CONFIRMADO: testes .NET não foram necessários porque nenhum arquivo .NET foi alterado; os nomes e semânticas dos enums existentes foram inspecionados diretamente. O secret scan final não encontrou material secreto concreto.

## Estado Final

CONFIRMADO: CONTEXT -> CAPABILITIES -> REGISTRY -> CROSS-CUTTING -> GOVERNED PLAN -> GAP/CONFLICT -> PARAR foi cumprido.
