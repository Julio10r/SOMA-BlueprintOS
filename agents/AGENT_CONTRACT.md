# Agent Contract v1.1

Status: accepted  
Data: 2026-08-27  
Escopo: identidade, ownership, delegacao e seguranca canonica e machine-readable dos Agents do SOMA BlueprintOS.

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

1. `agents/EXECUTION_POLICY.md`: politica global de execucao por qualquer IA.
2. `agents/AGENT_CONTRACT.md`: semantica do contrato.
3. `agents/agent.schema.json`: validacao estrutural machine-readable.
4. `agents/<agent-id>/agent.yaml`: identidade/configuracao canonica de cada Agent.
5. Fontes apontadas pelo manifesto: codigo, prompts, contextos, runbooks, scripts, docs e testes.

Nenhuma fonte de menor precedencia pode remover guardrails globais silenciosamente.

Documentos legados como `.ai/AI_TEAM.md`, `.ai/context/agents.md`, `.ai/context/runtime.md`, `.ai/prompts/new-agent.md` e `docs/agents/Agents.md` continuam validos nesta etapa, mas devem convergir futuramente para este contrato quando houver migracao controlada.

## Politicas Canonicas Relacionadas

Alem deste contrato, todo Agent, atual ou futuro, e todo executor/IA (independente de provider) herdam automaticamente:

- `agents/USER_ARTIFACT_LEARNING_POLICY.md`: artefato de usuario (SQL, codigo, planilha, procedure, documento, exemplo, implementacao historica) e sempre evidencia/fonte de conhecimento, nunca instrucao executavel automatica; define proveniencia e nivel de confianca do conhecimento aprendido.
- `agents/CAPABILITY_GAP_AND_AGENT_EVOLUTION_POLICY.md`: formaliza o fluxo de Knowledge Gap e Capability Gap ja previsto em `EXECUTION_POLICY.md`, a ordem de preferencia (aprender > evoluir > criar) e a proibicao de autoexpansao.
- `agents/DATABASE_CONNECTION_POLICY.md`: define, para qualquer Agent/executor que toque banco Linx/SOMA, o ambiente authoritative para investigacao do estado atual (Producao, read-only por padrao), o ambiente de laboratorio para desenvolvimento/teste (`SOMA_DESENV`), a proveniencia obrigatoria de evidencia por ambiente, o tratamento de drift Development/Production, e a reproducao controlada PROD->DEV (minimizacao de dados, LGPD, governanca). Todo novo Agent que use banco herda esta regra sem precisar redeclara-la.

A heranca e automatica porque `agent.schema.json` ja fixa estruturalmente `gap_policy.direct_bypass_allowed = false`, `delegation.bypass_allowed = false` e a exigencia de autorizacao humana explicita para novo Agent/mudanca material — nao houve necessidade de alterar o schema. `tools/agents/agent-factory-v2.js` audita a presenca e a referencia destes dois documentos.

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

- identidade: `schema_version`, `contract_version`, `id`, `name`, `version`, `type`, `status`, `owner`;
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
- ownership: capabilities machine-readable, owner responsavel e politica de delegacao;
- transversalidade: se o Agent e `cross_cutting` e em quais criterios participa;
- gap policy: tratamento de capability ausente sem bypass;
- conexoes: profiles logicos e estrategia de credenciais, quando aplicavel.

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

## Mudancas Do v1 Para v1.1

O Contract v1.1 adiciona a Canonical AI Execution Policy, capability ownership, delegacao obrigatoria, no-direct-bypass, Capability Gap, evolucao controlada de Agents, autorizacao humana explicita para novo Agent, Agents transversais e seguranca canonica de conexoes/credenciais.

`schema_version` permanece `1` para preservar compatibilidade com o envelope estrutural do v1. `contract_version: 1.1` torna o nivel semantico explicito. Os manifests dos Agents passam para versao `1.1.0`.

## Capability Ownership E Delegacao

`capability_ownership` e um objeto cujas chaves sao IDs estaveis em kebab-case. Cada declaracao informa `responsible_agent_id`, `ownership` (`primary` ou `complementary`), `delegation_required` e `direct_execution_by_others_allowed`.

O Agent que declara ownership primario deve usar seu proprio ID como responsavel. Ownership complementar nao substitui o owner primario. Para os Agents atuais, bypass e execucao direta por terceiros permanecem proibidos.

`delegation.cross_cutting` identifica Agents transversais. Seus `participation_criteria` determinam quando devem participar sem transferir ownership do dominio. `security-lgpd-agent` e transversal e consultivo; o Policy Engine continua sendo a decisao deterministica.

## Capability Gap E Evolucao

`gap_policy.direct_bypass_allowed` deve ser `false`. O Agent para quando faltar conhecimento, evidencia, permissao ou Tool/Adapter governado e registra Capability Gap conforme `EXECUTION_POLICY.md`.

O contrato permite propor aprendizado do Agent existente e, depois de avaliar ownership por Agents existentes, propor novo Agent. Criacao de novo Agent e mudanca material de capability ou seguranca exigem autorizacao humana explicita.

Nenhum Agent pode autoexpandir capabilities sensiveis, escrita, destruicao, bypass, acesso, enforcement ou reduzir approval/participacao transversal.

## Connection Profiles E Credenciais

`connections.profiles` contem IDs logicos de recursos, nunca credenciais. Cada profile declara `configuration_reference`, ambiente, intent e classificacao baseada em evidencia. Campos tecnicos nao comprovados ficam ausentes ou `AINDA_NAO_MAPEADO`.

`connections.credential_policy` exige `least_privilege: true`, `privilege_escalation_allowed: false`, identidade individual e secret storage seguro. User Secrets/secret managers existentes sao reutilizados; Keychain e Credential Manager sao estrategias preferenciais previstas, ainda sem adapters completos.

Permissao da identidade e aprovacao BlueprintOS sao independentes e ambas obrigatorias. Agents nao pedem segredo no chat, nao procuram credenciais no Git e nao trocam/elevam identidade quando o acesso for negado.

## Relacao Com As Factories

`backend/src/BlueprintOS.Core/Agents/AgentFactory.cs` continua sendo o instanciador runtime simples e compativel com seus consumidores existentes.

`tools/agents/agent-factory-v2.js` e a Factory canonica de lifecycle: CREATE, VALIDATE, AUDIT, UPDATE, REGISTER, CATALOG, TEST e SECURITY_CHECK. Ela reutiliza o validator canonico, nao executa capabilities operacionais e nao substitui Policy Engine, ApprovalPolicy, Runtime Registry ou Tool Gateway.

A Factory v2 e governada por `agents/agent-factory/agent.yaml`. AUDIT nunca corrige findings; CREATE e UPDATE material exigem autorizacao humana explicita; fontes protegidas do contrato nao podem ser alteradas pela Factory para fazer um Agent passar.

## Evolucao

Mudancas incompativeis no contrato devem criar nova versao de schema ou decisao arquitetural explicita. A evolucao futura esperada e:

```text
CONTRATO -> SCHEMA -> MANIFESTS -> VALIDACAO -> CONFORMIDADE
-> AGENT FACTORY V2 -> AUDITORIA -> ADEQUACAO AUTORIZADA
-> REGISTRY -> TOOL GATEWAY -> REORGANIZACAO FISICA
```
