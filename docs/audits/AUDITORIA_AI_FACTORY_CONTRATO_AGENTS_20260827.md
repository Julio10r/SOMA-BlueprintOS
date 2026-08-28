# Auditoria AI Factory e Contrato Atual de Criacao de Agents

Data: 2026-08-27  
Repositorio: SOMA BlueprintOS  
Modo da auditoria: somente leitura  
Objetivo: mapear como agents sao criados, registrados, documentados e governados hoje antes de criar ou alterar uma Agent Factory.

## Regras Seguidas Nesta Auditoria

- Nao foi feito push.
- Nao foi feito commit.
- Nao foi criada Factory nova.
- Nao foram movidas pastas.
- Nao foram movidos agents.
- Nao houve alteracao de codigo.
- Este arquivo e apenas um briefing consolidado para continuidade em outro chat.

## Sumario Executivo

Existe uma `AgentFactory` real no codigo, em `backend/src/BlueprintOS.Core/Agents/AgentFactory.cs`, mas ela nao e ainda a "AI Factory" descrita nos documentos. Hoje ela funciona como um instanciador simples por reflexao para classes derivadas de `BaseAgent`, injetando `IAIRuntime` e, opcionalmente, `IKnowledgeService`.

A "AI Factory" mais ampla existe principalmente como arquitetura-alvo e contrato documental em `.ai/AI_TEAM.md`, `.ai/context/*`, `.ai/prompts/new-agent.md` e `docs/agents/ai-factory/*`. Nao foi encontrado um contrato tecnico canonico e machine-readable para agents.

Nao foram encontrados, como implementacao consolidada, os seguintes componentes:

- `AgentRegistry`
- `AgentContract`
- `AgentSpec`
- `AgentDefinition`
- `AgentBuilder`
- `agent.yaml`
- manifesto versionado de agent
- registry automatico de agents
- validacao automatica de conformidade entre docs, codigo, permissoes e governanca

## Arquivos e Areas Relevantes Encontradas

### Contrato Runtime em C#

- `backend/src/BlueprintOS.Core/Agents/Contracts/IAgent.cs`
- `backend/src/BlueprintOS.Core/Agents/BaseAgent.cs`
- `backend/src/BlueprintOS.Core/Agents/AgentFactory.cs`
- `backend/src/BlueprintOS.Core/Agents/EchoAgent.cs`
- `backend/src/BlueprintOS.Core/Agents/KnowledgeAgent.cs`
- `backend/src/BlueprintOS.Core/Agents/SecurityLgpdAgent.cs`
- `backend/src/BlueprintOS.Core/Agents/Models/AgentContext.cs`
- `backend/src/BlueprintOS.Core/Agents/Models/AgentResult.cs`

Contrato tecnico atual:

```text
IAgent.ExecuteAsync(AgentContext, CancellationToken) -> AgentResult

AgentContext:
- Input: string

AgentResult:
- Output: string
```

Esse contrato e propositalmente minimo. Ele nao declara metadata, identidade, tools, permissoes, modelo, memoria, governanca, observabilidade, status, dono, lifecycle ou catalogo.

### Factory Existente

Arquivo:

```text
backend/src/BlueprintOS.Core/Agents/AgentFactory.cs
```

Comportamento:

- recebe `IAIRuntime`;
- recebe opcionalmente `IKnowledgeService`;
- cria agents derivados de `BaseAgent`;
- tenta construtor `(IAIRuntime, IKnowledgeService)` quando existir e houver knowledge service;
- caso contrario usa `Activator.CreateInstance(typeof(TAgent), runtime)`.

Limites:

- nao possui registry;
- nao possui manifesto;
- nao valida template de `.ai/AI_TEAM.md`;
- nao valida permissoes;
- nao valida governanca;
- nao registra agent em catalogo;
- nao gera docs;
- nao executa migrations;
- nao sabe lidar genericamente com dependencias fora de `BlueprintOS.Core`;
- nao e a AI Factory conceitual.

### Agents Runtime Encontrados

#### EchoAgent

Arquivo:

```text
backend/src/BlueprintOS.Core/Agents/EchoAgent.cs
```

Funcao: agent de referencia/diagnostico. Encaminha `context.Input` ao `IAIRuntime` e devolve o texto.

#### KnowledgeAgent

Arquivo:

```text
backend/src/BlueprintOS.Core/Agents/KnowledgeAgent.cs
```

Funcao: consulta `IKnowledgeService`, injeta trechos encontrados no prompt e chama `IAIRuntime`.

#### SecurityLgpdAgent

Arquivo:

```text
backend/src/BlueprintOS.Core/Agents/SecurityLgpdAgent.cs
```

Funcao: especialista consultivo em seguranca, privacidade e LGPD. Ele interpreta contexto e explica riscos, mas nao aprova execucao e nao substitui o policy engine deterministico.

#### LinxErpSpecialistAgent e LinxDatabaseSpecialistAgent

Arquivo:

```text
backend/src/BlueprintOS.Application/Knowledge/Linx/LinxSpecialistAgents.cs
```

Funcao: agents especialistas de conhecimento Linx. Herdam de `BaseAgent`, mas nao passam pela `AgentFactory` atual porque dependem de `IBuscarConhecimentoUseCase`, localizado na Application layer.

Esse ponto e importante: a propria DI documenta que estender a reflexao da `AgentFactory` para reconhecer `IBuscarConhecimentoUseCase` inverteria dependencias entre Core e Application.

### Registro DI Encontrado

Arquivo:

```text
backend/src/BlueprintOS.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs
```

Registros relevantes:

```text
services.AddSingleton<AgentFactory>();
services.AddScoped<SecurityLgpdAgent>();
services.AddScoped<LinxErpSpecialistAgent>();
services.AddScoped<LinxDatabaseSpecialistAgent>();
```

Conclusao: registro de agents hoje e misto.

- Alguns agents podem ser criados pela `AgentFactory`.
- Alguns sao resolvidos diretamente via DI.
- Nao ha registry unico consultavel por id/capability.

## Workflow Runtime

Arquivos:

- `backend/src/BlueprintOS.Core/Workflows/Workflow.cs`
- `backend/src/BlueprintOS.Core/Workflows/WorkflowRunner.cs`
- `backend/src/BlueprintOS.Core/Workflows/Models/WorkflowStep.cs`

Modelo atual:

```text
Workflow
-> lista ordenada de IAgent
-> WorkflowRunner executa em sequencia
-> output de um agent vira input do proximo
```

Limites:

- sem metadata por etapa;
- sem selecao automatica de agent;
- sem registry;
- sem planner completo;
- sem policy por tool/action;
- sem approval nativo;
- sem auditoria por step alem do que cada componente venha a implementar.

## Contrato Documental Atual

### `.ai/AI_TEAM.md`

Define a AI Factory como conjunto de especialistas coordenados pelo Maestro.

Campos obrigatorios para criar novo agent:

- Nome
- Objetivo
- Responsabilidades
- Limites
- Ferramentas
- Entradas
- Saidas
- Criterios de qualidade
- Prompt Base
- Modelo utilizado
- Memoria utilizada
- Permissoes

Regra documental importante:

```text
Sem essa estrutura o agente nao podera ser registrado.
```

Na pratica, essa regra ainda nao tem enforcement tecnico automatico.

### `.ai/prompts/new-agent.md`

Funciona como prompt/template para criar agents. Reforca leitura previa de:

- `.ai/AI_TEAM.md`
- `.ai/context/agents.md`
- `.ai/context/runtime.md`
- `.ai/ARCHITECTURE.md`

Inclui placeholders para nome, objetivo, responsabilidades, limites, ferramentas, entradas, saidas, qualidade, prompt, modelo, memoria, permissoes e modulo relacionado.

### `.ai/context/agents.md`

Descreve ciclo de vida:

1. Criacao
2. Registro
3. Execucao
4. Observacao
5. Desativacao

Tambem acrescenta campos de runtime:

- identificador tecnico unico;
- escopo de modulos e contracts;
- estrategia de fallback.

### `.ai/context/runtime.md`

Descreve uma arquitetura onde o Runtime teria:

- registro de agents;
- descoberta automatica;
- selecao de agent por responsabilidade/tools;
- tools com interfaces definidas;
- Maestro como coordenador.

Status: esse desenho e arquitetura-alvo/documental. O codigo atual ainda nao implementa registry/discovery automatico.

## Agents Operacionais

### WISE Agent

Fonte principal:

```text
.ai/context/wise-knowledge.md
```

Complementos:

- `.ai/prompts/consultar-wise.md`
- `docs/operations/WiseAgentRunbook.md`
- `.ai/context/linx-wise-daily-integration.md`

Caracteristicas:

- agent operacional/documental;
- especialista em leitura e interpretacao do ambiente WISE;
- usa conhecimento persistido em Markdown;
- padrao somente leitura;
- escrita exige autorizacao explicita;
- nao escolhe `ID_CAMPANHA` sozinho;
- nao imprime token, senha, connection string ou segredo;
- deve usar `ActionProposal` no futuro quando houver Tool Gateway conectado.

Status de enforcement: majoritariamente documental/runbook.

### Showcase Agent

Fonte principal:

```text
.ai/context/showcase-knowledge.md
```

Complementos:

- `.ai/prompts/coletar-showcase.md`
- `docs/operations/ShowcaseAgentRunbook.md`
- `scripts/showcase_collector/`

Caracteristicas:

- agent operacional/documental;
- usa sessao autenticada do Product Owner;
- nunca preenche login/MFA;
- nunca persiste token/cookie/senha;
- executa coleta/leitura do Showcase;
- baixa fotos e gera artefatos locais;
- nao escreve em carrinho, pedido, cadastro ou configuracao;
- colabora com WISE Agent para saldo.

Status de enforcement: regras fortes em doc/script, mas ainda fora de Tool Gateway universal.

### Agent Linx

Fontes:

- `backend/src/BlueprintOS.Application/Knowledge/Linx/LinxSpecialistAgents.cs`
- `.ai/context/linx-wise-daily-integration.md`
- `docs/operations/LinxWiseDailyIntegrationRunbook.md`

Caracteristicas:

- possui runtime agents reais para conhecimento Linx;
- usa base de conhecimento persistida/versionada;
- diferencia `Validado`, `Aprovado`, `Descoberto` e `Inferido`;
- protege contra prompt injection tratando conhecimento recuperado como dado, nao instrucao;
- scripts/rotinas de escrita Linx/WISE seguem runbook especifico.

## AI Governance Onda 1

Arquitetura implementada:

```text
Agent/Use Case
-> ActionProposal
-> AIGovernancePolicyEngine
-> ApprovalPolicy
-> GovernanceAuditRecorder
-> permitido/bloqueado
```

Arquivos/conceitos:

- `ActionProposal`
- `PolicyDecision`
- `ApprovalRequest`
- `ApprovalGrant`
- `ApprovalPolicy`
- `GovernanceAuditEntry`
- `InMemoryGovernanceAuditRecorder`
- `GovernedActionDemoFlow`
- `SecurityLgpdAgent`

O que esta enforced:

- classificacao deterministica de risco;
- hash de proposta;
- validacao de approval por hash, expiracao e revogacao;
- bloqueio demonstrativo para `Red`;
- exigencia demonstrativa de approval para `Yellow`;
- auditoria em memoria;
- DI dos componentes.

O que ainda esta planejado/documental:

- Tool Gateway universal;
- persistencia de approvals/auditoria;
- UI de aprovacao humana;
- interceptacao automatica de SQL, MCP, scripts e exports;
- classificacao automatica campo-a-campo de PII;
- DLP de prompts/logs/exportacoes.

Conclusao: AI Governance Onda 1 e fundacao real, mas ainda nao e enforcement universal para todos os agents.

## Como Um Agent E Criado Hoje

Fluxo real pratico:

1. Ler os documentos base em `.ai/`.
2. Preencher o template de `.ai/AI_TEAM.md` ou `.ai/prompts/new-agent.md`.
3. Decidir se o agent sera:
   - runtime C#;
   - operational/documental;
   - script/runbook;
   - knowledge specialist.
4. Se for runtime C#, implementar `IAgent`, normalmente via `BaseAgent`.
5. Se o construtor for simples, usar caminho compativel com `AgentFactory`.
6. Se houver dependencias especificas, registrar direto via DI.
7. Criar/atualizar testes.
8. Criar/atualizar prompt, contexto, runbook e docs.
9. Atualizar `docs/agents/Agents.md`.
10. Atualizar `docs/agents/AgentsCatalog.html`.
11. Para operacoes sensiveis, documentar relacao com AI Governance e, no futuro, representar como `ActionProposal`.

Problema: esse fluxo nao e validado automaticamente por schema ou factory.

## Duplicacoes e Fronteiras Ambiguas

Principais pontos de duplicacao/fragmentacao:

- `.ai/AI_TEAM.md` tem o contrato organizacional.
- `.ai/prompts/new-agent.md` duplica parte do template.
- `.ai/context/agents.md` adiciona lifecycle e runtime.
- `.ai/context/runtime.md` promete registry/discovery ainda nao implementado.
- `docs/agents/Agents.md` e catalogo humano.
- `docs/agents/AgentsCatalog.html` e visual, mas precisa ser atualizado manualmente.
- WISE, Showcase e Linx tem regras proprias em contextos/runbooks.
- AI Governance adiciona outro eixo de contrato para permissoes e risco.

Risco principal: o projeto tem boas regras, mas espalhadas em fontes humanas. Isso aumenta drift.

## Arquitetura Atual

```text
.ai/AI_TEAM.md
.ai/prompts/new-agent.md
.ai/context/agents.md
.ai/context/runtime.md
docs/agents/Agents.md
        |
        v
convencao humana/LLM
        |
        v
backend runtime:
IAgent / BaseAgent / AgentFactory
        |
        +-- EchoAgent
        +-- KnowledgeAgent
        +-- SecurityLgpdAgent via DI
        +-- Linx specialists via DI direto

workflow:
Workflow -> WorkflowStep(IAgent) -> WorkflowRunner sequencial

operacional:
.ai/context/wise-knowledge.md
.ai/context/showcase-knowledge.md
.ai/context/linx-wise-daily-integration.md
docs/operations/*
scripts/*
```

## Arquitetura-Alvo Recomendada

```text
agents/AGENT_CONTRACT.md
agents/agent.schema.json
agents/<agent-id>/agent.yaml
        |
        v
Agent Factory
- create
- validate
- audit
- migrate
- update
- register
- catalog
- test
- security-check
        |
        v
runtime registry + DI metadata + generated docs/catalog
        |
        v
agent execution / operational adapters / scripts
        |
        v
AI Governance
ActionProposal -> Policy Engine -> Approval -> Audit -> Tool Gateway
```

## Proposta de Agent Contract v1

Campos recomendados:

```yaml
schema_version: 1
id: wise-agent
name: WISE Agent
version: 1.0.0
type: operational
status: active
owner: product-owner

responsibility:
  objective: ""
  responsibilities: []
  non_goals: []
  escalation_rules: []

implementation:
  code_paths: []
  prompt_paths: []
  context_paths: []
  runbook_paths: []
  script_paths: []
  di_registration: null

runtime:
  interface: null
  factory_supported: false
  constructor_dependencies: []
  dependencies: []

capabilities:
  tools: []
  allowed_operations: []
  forbidden_operations: []

data:
  systems: []
  resources: []
  classifications: []
  pii_allowed: false
  sensitive_pii_allowed: false
  secrets_allowed: false

governance:
  requires_action_proposal_for: []
  default_risk: unknown
  approval_required_for: []
  audit_required: true
  policy_engine_required: false

knowledge:
  memory_paths: []
  provenance_labels: []
  update_rules: []

observability:
  logs: []
  metrics: []
  audit_events: []
  redaction_required: true

tests:
  unit: []
  integration: []
  safety: []
  contract: []

relationships:
  upstream_agents: []
  downstream_agents: []
  conflicts_with: []

catalog:
  summary: ""
  docs_paths: []
  display_order: 0
```

## Sobre Criar `/agents`

Recomendacao: sim, faz sentido criar `/agents` como raiz canonica, mas sem mover codigo backend no primeiro momento.

Formato recomendado:

```text
agents/
  AGENT_CONTRACT.md
  agent.schema.json
  wise-agent/
    agent.yaml
  showcase-agent/
    agent.yaml
  linx-erp-specialist-agent/
    agent.yaml
  linx-database-specialist-agent/
    agent.yaml
  security-lgpd-agent/
    agent.yaml
  echo-agent/
    agent.yaml
  knowledge-agent/
    agent.yaml
```

Cada `agent.yaml` deve apontar para os arquivos reais ja existentes em `backend/`, `.ai/`, `docs/` e `scripts/`. Isso reduz risco e evita big-bang migration.

## Responsabilidades da Future Agent Factory

A futura Agent Factory deve ser responsavel por:

- criar scaffold de agent a partir do contrato;
- validar `agent.yaml` contra schema;
- auditar divergencia entre manifesto, docs, codigo e DI;
- registrar metadata no runtime;
- sugerir ou aplicar registro DI quando aplicavel;
- gerar/atualizar catalogo humano;
- validar permissao e governanca;
- exigir testes minimos;
- detectar agents sem owner/status/version;
- detectar docs desatualizadas;
- migrar agents existentes sem mover tudo de uma vez;
- impedir que mudancas relaxem seguranca sem ADR/aprovacao.

Regra fundamental:

```text
A Factory nao deve ser apenas geradora de arquivos. Ela deve ser guardia de conformidade entre contrato, codigo, documentacao, permissoes e governanca.
```

## Estrategia de Migracao Recomendada

### Fase 0: Inventario

Consolidar lista de agents existentes:

- EchoAgent
- KnowledgeAgent
- SecurityLgpdAgent
- LinxErpSpecialistAgent
- LinxDatabaseSpecialistAgent
- WISE Agent
- Showcase Agent
- rotina Linx/WISE como fluxo operacional governavel

### Fase 1: Contrato

Criar:

- `agents/AGENT_CONTRACT.md`
- `agents/agent.schema.json`

Sem alterar runtime ainda.

### Fase 2: Manifests

Criar `agents/<agent-id>/agent.yaml` para cada agent existente, apontando para arquivos atuais.

Sem mover codigo.

### Fase 3: Validator

Criar script/teste que valida:

- schema;
- paths existentes;
- owner/status/version;
- relacao com governanca;
- docs obrigatorias;
- tests declarados.

### Fase 4: Catalogo Gerado

Passar `docs/agents/AgentsCatalog.html` e parte de `docs/agents/Agents.md` a serem gerados ou validados a partir dos manifests.

### Fase 5: Runtime Registry

Adicionar registry tecnico que leia metadata dos manifests ou de codigo gerado, sem quebrar DI existente.

### Fase 6: Tool Gateway

Integrar actions sensiveis a:

```text
ActionProposal -> Policy Engine -> Approval -> Audit
```

## Riscos

- Confundir `AgentFactory` atual com AI Factory completa.
- Prometer registry/discovery automatico sem implementacao.
- Criar `/agents` e duplicar ainda mais docs sem schema/validator.
- Quebrar scripts/runbooks ao mover arquivos cedo demais.
- Catalogo HTML divergir da fonte canonica.
- Agents operacionais parecerem tecnicamente governados quando ainda dependem de runbook.
- Linx agents continuarem fora da `AgentFactory` se a Factory nao respeitar fronteiras Core/Application.
- Relaxar contrato de seguranca sem controle de versao.

## Arquivos Provaveis de Mudanca Futura

Novos arquivos provaveis:

- `agents/README.md`
- `agents/AGENT_CONTRACT.md`
- `agents/agent.schema.json`
- `agents/<agent-id>/agent.yaml`
- `tools/agents/validate-agents.*`
- `tools/agents/generate-catalog.*`

Arquivos existentes que provavelmente seriam afetados:

- `backend/src/BlueprintOS.Core/Agents/AgentFactory.cs`
- `backend/src/BlueprintOS.Core/Agents/Contracts/IAgent.cs`
- `backend/src/BlueprintOS.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`
- `docs/agents/Agents.md`
- `docs/agents/AgentsCatalog.html`
- `.ai/AI_TEAM.md`
- `.ai/prompts/new-agent.md`
- `.ai/context/agents.md`
- `.ai/context/runtime.md`
- `docs/architecture/AIGovernance.md`
- runbooks de WISE, Showcase e Linx/WISE

## Conclusao

O SOMA BlueprintOS ja tem uma boa base cultural e documental para agents, alem de runtime agents reais e uma fundacao inicial de AI Governance. O principal problema nao e ausencia total de Factory, mas ausencia de um contrato unico, versionado e validavel.

O proximo passo tecnicamente correto e criar primeiro o `Agent Contract v1` e manifests `agent.yaml` para os agents existentes, sem mover codigo. Depois disso, a Agent Factory pode nascer como validadora, registradora, geradora de catalogo e guardia de governanca.

Nao e recomendado criar uma Factory grande antes do contrato, porque isso cristalizaria as duplicacoes atuais em mais uma camada.
