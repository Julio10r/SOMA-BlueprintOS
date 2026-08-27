# Agent Contract v1 — Implementacao e Conformidade

Data: 2026-08-27  
Repositorio: SOMA BlueprintOS  
Commit base de governanca considerado: `c70ab7b feat(security): add AI governance policy and approval foundation`  
Fonte de auditoria usada: `.ai/AUDITORIA_AI_FACTORY_CONTRATO_AGENTS_20260827.md`

## 1. Resumo Executivo

CONFIRMADO: foi criada a raiz canonica `agents/` para identidade machine-readable dos Agents existentes do SOMA BlueprintOS.

CONFIRMADO: a implementacao desta etapa ficou limitada a contrato, schema, manifests, validator e relatorio. Nao houve reescrita da `AgentFactory.cs`, criacao de Runtime Registry, criacao de Tool Gateway, mudanca de comportamento dos Agents, movimentacao de codigo, scripts, prompts, runbooks ou knowledge.

CONFIRMADO: a lista definitiva desta etapa possui 7 Agents:

- `echo-agent`
- `knowledge-agent`
- `security-lgpd-agent`
- `linx-erp-specialist-agent`
- `linx-database-specialist-agent`
- `wise-agent`
- `showcase-agent`

CONFIRMADO: a integracao diaria Linx/WISE continua tratada como fluxo operacional governavel relacionado, nao como Agent independente.

## 2. Decisoes Do Agent Contract v1

CONFIRMADO: `agents/AGENT_CONTRACT.md` e `agents/agent.schema.json` sao a fonte canonica do contrato estrutural dos Agents.

CONFIRMADO: `agents/<agent-id>/agent.yaml` e a declaracao canonica de identidade e configuracao de cada Agent.

CONFIRMADO: o manifesto nao substitui codigo, conhecimento, prompt, runbook, script ou politica especifica. Ele aponta para essas fontes.

CONFIRMADO: tipos suportados:

- `runtime`
- `knowledge`
- `operational`
- `hybrid`

CONFIRMADO: status de enforcement suportados:

- `ENFORCED`
- `DOCUMENTAL`
- `PARTIAL`
- `PLANNED`

PROPOSTO: a Future Agent Factory deve nascer sobre este contrato como mecanismo de criacao, validacao, auditoria, registro, catalogo, testes e seguranca. A `AgentFactory` atual permanece um instanciador runtime simples.

## 3. Estrutura Criada

```text
agents/
  README.md
  AGENT_CONTRACT.md
  agent.schema.json
  echo-agent/agent.yaml
  knowledge-agent/agent.yaml
  security-lgpd-agent/agent.yaml
  linx-erp-specialist-agent/agent.yaml
  linx-database-specialist-agent/agent.yaml
  wise-agent/agent.yaml
  showcase-agent/agent.yaml

tools/
  agents/
    validate-agent-manifests.js

docs/
  audits/
    AgentContractV1-Implementacao-e-Conformidade.md
```

## 4. Schema Criado

CONFIRMADO: `agents/agent.schema.json` usa JSON Schema draft 2020-12 e valida estruturalmente:

- campos obrigatorios;
- tipos;
- enums de `type`, `status`, `default_risk` e `enforcement_status`;
- formato de `id`;
- formato semantico de `version`;
- blocos obrigatorios de responsabilidade, implementacao, runtime, capabilities, data, governance, knowledge, observability, tests, relationships e catalog;
- listas para paths, tests, tools, operacoes, relacionamentos e labels.

CONFIRMADO: o schema permite `null`, `[]` e `false` onde semanticamente adequado para Agents de natureza diferente.

## 5. Lista Definitiva De Agents

CONFIRMADO:

| ID | Nome | Tipo | Evidencia |
| --- | --- | --- | --- |
| `echo-agent` | EchoAgent | runtime | Classe C# em `backend/src/BlueprintOS.Core/Agents/EchoAgent.cs` |
| `knowledge-agent` | KnowledgeAgent | knowledge | Classe C# em `backend/src/BlueprintOS.Core/Agents/KnowledgeAgent.cs` |
| `security-lgpd-agent` | Security/LGPD Agent | hybrid | Classe C# e AI Governance em `backend/src/BlueprintOS.Core/Agents/SecurityLgpdAgent.cs` |
| `linx-erp-specialist-agent` | LinxErpSpecialistAgent | hybrid | Classe C# em `backend/src/BlueprintOS.Application/Knowledge/Linx/LinxSpecialistAgents.cs` |
| `linx-database-specialist-agent` | LinxDatabaseSpecialistAgent | hybrid | Classe C# em `backend/src/BlueprintOS.Application/Knowledge/Linx/LinxSpecialistAgents.cs` |
| `wise-agent` | WISE Agent | operational | `.ai/context/wise-knowledge.md`, prompt e runbook |
| `showcase-agent` | Showcase Agent | operational | `.ai/context/showcase-knowledge.md`, prompt, runbook e scripts |

CONFIRMADO: classes auxiliares de teste que implementam `IAgent` nao foram consideradas Agents do produto.

## 6. IDs Canonicos

CONFIRMADO: os IDs seguem kebab-case estavel e independente de nome de classe/display name:

```text
echo-agent
knowledge-agent
security-lgpd-agent
linx-erp-specialist-agent
linx-database-specialist-agent
wise-agent
showcase-agent
```

PROPOSTO: renomear display name no futuro nao deve renomear automaticamente o ID.

## 7. Resumo De Cada Manifesto

### echo-agent

PASS: possui classe runtime, contrato `IAgent`, teste unitario e suporte pela `AgentFactory` atual.  
WARN: observabilidade e safety tests especificos ainda nao existem.  
Enforcement: `PARTIAL`, pois o agent e simples/read-only, mas nao passa por gateway ou auditoria universal.

### knowledge-agent

PASS: possui classe runtime, uso de `IKnowledgeService`, teste unitario e suporte pela `AgentFactory` atual.  
WARN: nao ha teste de contrato dedicado alem do validator desta etapa.  
Enforcement: `PARTIAL`, pois consulta conhecimento sem escrita, mas nao ha governanca universal de prompts/logs.

### security-lgpd-agent

PASS: possui classe runtime, DI direto, relacao com `ActionProposal`, `PolicyDecision`, `AIGovernancePolicyEngine`, `ApprovalPolicy` e testes de governanca.  
WARN: Tool Gateway universal ainda nao existe; o agent e consultivo e nao barreira tecnica unica.  
Enforcement: `PARTIAL`.

### linx-erp-specialist-agent

PASS: possui classe runtime, DI direto, conhecimento versionado Linx e testes.  
WARN: nao passa pela `AgentFactory` atual por depender da Application layer.  
WARN: futuras escritas relacionadas a Linx/WISE ainda dependem de runbook/fluxo governavel.  
Enforcement: `PARTIAL`.

### linx-database-specialist-agent

PASS: possui classe runtime, DI direto, conhecimento versionado Linx, relacao com schema discovery read-only e testes.  
WARN: deve permanecer explicitamente nao executor SQL arbitrario.  
WARN: nao passa pela `AgentFactory` atual por depender da Application layer.  
Enforcement: `PARTIAL`.

### wise-agent

PASS: manifesto referencia knowledge, prompt, runbook, fluxo diario relacionado e script existente.  
WARN: sem classe C# propria e sem tests automatizados especificos do manifesto alem do validator.  
WARN: governanca ainda e documental/runbook ate existir Tool Gateway.  
Enforcement: `DOCUMENTAL`.

### showcase-agent

PASS: manifesto referencia knowledge, prompt, runbook e scripts reais de coleta.  
WARN: sem classe C# propria e sem tests automatizados especificos do manifesto alem do validator.  
WARN: protecao de token/cookie e read-only depende de script/runbook; nao ha Tool Gateway universal.  
Enforcement: `DOCUMENTAL`.

## 8. Relacao Com AI Governance

CONFIRMADO: os manifests incorporam a arquitetura:

```text
ActionProposal
-> AIGovernancePolicyEngine
-> ApprovalPolicy
-> GovernanceAuditRecorder
-> SecurityLgpdAgent consultivo
```

CONFIRMADO: todo manifesto declara:

- `read_only`;
- `can_propose_write`;
- `can_execute_write`;
- `can_execute_destructive_operation`;
- `policy_engine_required`;
- `requires_action_proposal_for`;
- `approval_required_for`;
- `audit_required`;
- `default_risk`;
- `enforcement_status`.

CONFIRMADO: nenhum Agent foi marcado como podendo autoaprovar acao sensivel.

## 9. Validacoes Executadas

Comando executado:

```bash
node tools/agents/validate-agent-manifests.js
```

Resultado:

```text
PASS: 7 agent manifests validated
PASS: IDs unique: echo-agent, knowledge-agent, linx-database-specialist-agent, linx-erp-specialist-agent, security-lgpd-agent, showcase-agent, wise-agent
PASS: required paths exist
PASS: no secret values detected in manifests
```

## 10. Resultado Dos Testes

CONFIRMADO: o validator criado nesta etapa passou.

CONFIRMADO: testes unitarios do backend executados com sucesso:

```bash
dotnet test backend/tests/BlueprintOS.UnitTests/BlueprintOS.UnitTests.csproj
```

Resultado:

```text
Aprovado: 861
Falha: 0
Ignorado: 0
Total: 861
```

WARN: a primeira tentativa dentro do sandbox falhou por `SocketException (13): Permission denied` ao MSBuild criar pipe/socket. A execucao foi repetida com permissao escalada e passou. Permaneceram warnings C# nullable preexistentes em entidades de dominio (`AlcadaAprovacao`, `RegraWorkflow`, `RegraOrcamentaria`), sem falhas de teste.

## 11. Conformidade Inicial Agent Por Agent

| Agent | Identidade | Implementacao | Governanca | Testes | Resultado |
| --- | --- | --- | --- | --- | --- |
| `echo-agent` | PASS | PASS | WARN | PASS | PASS com WARN |
| `knowledge-agent` | PASS | PASS | WARN | PASS | PASS com WARN |
| `security-lgpd-agent` | PASS | PASS | WARN | PASS | PASS com WARN |
| `linx-erp-specialist-agent` | PASS | PASS | WARN | PASS | PASS com WARN |
| `linx-database-specialist-agent` | PASS | PASS | WARN | PASS | PASS com WARN |
| `wise-agent` | PASS | PASS | WARN | WARN | PASS com WARN |
| `showcase-agent` | PASS | PASS | WARN | WARN | PASS com WARN |

## 12. PASS / WARN / FAIL

PASS:

- raiz `agents/` criada;
- contrato criado;
- schema criado;
- 7 manifests criados;
- IDs unicos;
- paths obrigatorios existem;
- enums validos;
- manifests parseados como YAML pelo validator;
- ausencia de valores secretos nos manifests;
- nenhum Agent conhecido ficou sem manifesto;
- integracao diaria Linx/WISE preservada como workflow relacionado.

WARN:

- WISE e Showcase ainda dependem de enforcement documental/runbook.
- Tool Gateway universal ainda nao existe.
- AgentFactory atual ainda nao le manifests.
- Catalogo visual ainda nao e gerado a partir dos manifests.
- Alguns Agents nao possuem safety tests dedicados.

FAIL:

- Nenhum FAIL bloqueante nesta etapa.

## 13. Gaps Encontrados

CONFIRMADO:

- nao ha Runtime Registry real;
- nao ha Tool Gateway universal;
- nao ha gerador de catalogo a partir dos manifests;
- nao ha teste C# integrado para validar manifests;
- `AgentFactory.cs` ainda e instanciador simples;
- WISE/Showcase ainda sao operacionais/documentais, nao runtime agents.

PROPOSTO:

- evoluir o validator para teste de CI;
- gerar `docs/agents/AgentsCatalog.html` a partir de `agents/*/agent.yaml`;
- conectar manifests ao futuro Runtime Registry;
- conectar tools sensiveis ao AI Governance Tool Gateway.

## 14. Documentacao Legada Impactada Futuramente

Pode ser reduzida ou referenciar `agents/AGENT_CONTRACT.md`:

- `.ai/AI_TEAM.md`
- `.ai/prompts/new-agent.md`
- `.ai/context/agents.md`
- `.ai/context/runtime.md`

Deve continuar como documentacao humana/catalogo ate migracao futura:

- `docs/agents/Agents.md`
- `docs/agents/AgentsCatalog.html`

Deve continuar como fonte operacional especifica:

- `.ai/context/wise-knowledge.md`
- `.ai/context/showcase-knowledge.md`
- `.ai/context/linx-wise-daily-integration.md`
- `docs/operations/WiseAgentRunbook.md`
- `docs/operations/ShowcaseAgentRunbook.md`
- `docs/operations/LinxWiseDailyIntegrationRunbook.md`

## 15. Riscos

- Criar uma segunda fonte concorrente se manifests nao virarem fonte canonica de identidade.
- Deixar catalogo HTML divergir dos manifests.
- Declarar enforcement maior que o real.
- Tentar mover arquivos antes de haver validator/gerador.
- Confundir a `AgentFactory` atual com Future Agent Factory.
- Esquecer que Linx/WISE diario e fluxo governavel, nao Agent independente.

## 16. Proximos Passos

1. Adicionar o validator ao pipeline/testes.
2. Criar auditoria de conformidade que compare manifests, docs e codigo.
3. Gerar ou validar `docs/agents/AgentsCatalog.html` a partir dos manifests.
4. Evoluir Runtime Registry sem reescrever a `AgentFactory` atual de uma vez.
5. Criar Tool Gateway para conectar actions sensiveis a `ActionProposal`, policy, approval e audit.
6. Planejar migracao fisica apenas depois de estabilizar contrato, schema e geradores.

## 17. Arquivos Criados

- `agents/README.md`
- `agents/AGENT_CONTRACT.md`
- `agents/agent.schema.json`
- `agents/echo-agent/agent.yaml`
- `agents/knowledge-agent/agent.yaml`
- `agents/security-lgpd-agent/agent.yaml`
- `agents/linx-erp-specialist-agent/agent.yaml`
- `agents/linx-database-specialist-agent/agent.yaml`
- `agents/wise-agent/agent.yaml`
- `agents/showcase-agent/agent.yaml`
- `tools/agents/validate-agent-manifests.js`
- `docs/audits/AgentContractV1-Implementacao-e-Conformidade.md`

## 18. Arquivos Alterados

CONFIRMADO: nenhum arquivo preexistente foi alterado nesta etapa para implementar contrato/manifests. Apenas arquivos novos foram adicionados.

Observacao: o workspace possuia mudancas preexistentes nao relacionadas antes desta etapa; elas nao pertencem a este escopo.

## 19. Git Diff Resumido

Resumo staged conferido antes do commit:

```text
agents/AGENT_CONTRACT.md                           |  99 ++++++
agents/README.md                                   |  64 ++++
agents/agent.schema.json                           | 226 +++++++++++++
agents/echo-agent/agent.yaml                       | 101 ++++++
agents/knowledge-agent/agent.yaml                  | 117 +++++++
agents/linx-database-specialist-agent/agent.yaml   | 141 ++++++++
agents/linx-erp-specialist-agent/agent.yaml        | 135 ++++++++
agents/security-lgpd-agent/agent.yaml              | 149 ++++++++
agents/showcase-agent/agent.yaml                   | 156 +++++++++
agents/wise-agent/agent.yaml                       | 146 ++++++++
...AgentContractV1-Implementacao-e-Conformidade.md | 373 +++++++++++++++++++++
tools/agents/validate-agent-manifests.js           | 302 +++++++++++++++++
12 files changed, 2009 insertions(+)
```

CONFIRMADO: mudancas preexistentes nao relacionadas no workspace ficaram fora do staged set.
