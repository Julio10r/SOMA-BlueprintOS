# Agent Contract v1

Status: accepted  
Data: 2026-08-27  
Escopo: identidade canonica e machine-readable dos Agents do SOMA BlueprintOS.

## Contexto

O projeto ja possui agents runtime em C#, specialists operacionais baseados em `.ai/context`, prompts, runbooks e scripts, alem da fundacao AI Governance Onda 1. Antes de evoluir a Agent Factory, o projeto precisa de um contrato unico, versionado e validavel.

## Decisao

`agents/AGENT_CONTRACT.md` e `agents/agent.schema.json` passam a ser a fonte canonica do contrato estrutural dos Agents.

Cada Agent deve possuir:

```text
agents/<agent-id>/agent.yaml
```

O `agent.yaml` e a declaracao canonica de identidade/configuracao daquele Agent. Ele nao substitui conhecimento, runbook, codigo, prompt, script ou politica especifica; ele referencia esses arquivos.

## Precedencia

1. `agents/AGENT_CONTRACT.md`: semantica do contrato.
2. `agents/agent.schema.json`: validacao estrutural machine-readable.
3. `agents/<agent-id>/agent.yaml`: identidade/configuracao canonica de cada Agent.
4. Fontes apontadas pelo manifesto: codigo, prompts, contextos, runbooks, scripts, docs e testes.

Documentos legados como `.ai/AI_TEAM.md`, `.ai/context/agents.md`, `.ai/context/runtime.md`, `.ai/prompts/new-agent.md` e `docs/agents/Agents.md` continuam validos nesta etapa, mas devem convergir futuramente para este contrato quando houver migracao controlada.

## Tipos De Agent

- `runtime`: possui classe executavel no runtime de agents.
- `knowledge`: especializa consulta, injecao ou curadoria de conhecimento.
- `operational`: opera via contexto, prompt, runbook, script ou ferramenta externa, sem exigir classe C# propria.
- `hybrid`: combina runtime executavel com conhecimento, runbook ou comportamento operacional relevante.

Nao transforme automaticamente workflows, scripts, runbooks, services ou use cases em Agents. Eles podem ser relacionados por manifesto, mas so sao Agents quando houver identidade, responsabilidade e contrato de Agent.

## Enforcement Status

O campo `governance.enforcement_status` deve usar um dos valores:

- `ENFORCED`: existe bloqueio tecnico implementado no fluxo declarado.
- `PARTIAL`: ha enforcement tecnico em parte do fluxo, mas nao universal.
- `DOCUMENTAL`: as regras existem em documentos, runbooks, prompts ou scripts, mas nao ha bloqueio tecnico central.
- `PLANNED`: regra prevista, ainda nao aplicada ao fluxo.

Nao marque como `ENFORCED` algo que depende apenas de instrucao textual, convencao humana ou runbook.

## Campos Obrigatorios

Todo manifesto deve declarar:

- identidade: `schema_version`, `id`, `name`, `version`, `type`, `status`, `owner`;
- responsabilidade: `objective`, `responsibilities`, `non_goals`, `escalation_rules`;
- implementacao: paths e registro DI quando aplicavel;
- runtime: se existe runtime executavel e como ele e instanciado;
- capabilities: tools e operacoes permitidas/proibidas;
- data: sistemas, recursos, classificacoes e tratamento de PII/segredos;
- governance: ActionProposal, approval, audit, default risk e enforcement real;
- knowledge: memorias, labels de proveniencia e regras de atualizacao;
- observability: logs, metricas, eventos de auditoria e redaction;
- tests: unit, integration, safety e contract;
- relationships: upstream/downstream agents, workflows e conflitos;
- catalog: resumo e ordem de exibicao.

Quando um campo nao se aplica, use `null`, `[]` ou `false`, conforme permitido pelo schema. Nao invente capacidades para preencher campos.

## Seguranca

Todo Agent deve declarar explicitamente:

- se realiza somente leitura;
- se pode propor escrita;
- se pode executar escrita;
- se pode realizar operacao destrutiva;
- se `ActionProposal` e exigido;
- se approval e exigido;
- se o Policy Engine e exigido;
- qual e a situacao real de enforcement.

Nenhum Agent pode autoaprovar sua propria acao sensivel.

## Relacao Com A AgentFactory Atual

`backend/src/BlueprintOS.Core/Agents/AgentFactory.cs` continua sendo um instanciador runtime simples.

A Future Agent Factory sera outro nivel de responsabilidade: criacao, validacao, auditoria, registro, catalogo, testes e checagem de seguranca a partir deste contrato.

## Evolucao

Mudancas incompativeis no contrato devem criar nova versao de schema ou decisao arquitetural explicita. A evolucao futura esperada e:

```text
CONTRATO -> SCHEMA -> MANIFESTS -> VALIDACAO -> CONFORMIDADE
-> AGENT FACTORY V2 -> REGISTRY -> TOOL GATEWAY -> REORGANIZACAO FISICA
```
