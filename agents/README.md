# Agents

Esta pasta e o endereco canonico dos Agents do SOMA BlueprintOS.

Cada Agent existente ou futuro deve possuir um manifesto:

```text
agents/<agent-id>/agent.yaml
```

O manifesto e a declaracao canonica de identidade e configuracao do Agent. Ele nao substitui codigo, conhecimento, prompts, runbooks, scripts ou politicas especificas. Ele aponta para essas fontes.

## Fontes Canonicas

- `agents/AGENT_CONTRACT.md`: contrato estrutural canonico para criacao, registro, evolucao e auditoria de Agents.
- `agents/agent.schema.json`: schema machine-readable usado para validar manifests.
- `agents/<agent-id>/agent.yaml`: manifesto canonico daquele Agent.

Conhecimento operacional continua nas fontes de dominio, como `.ai/context/*`. Runbooks continuam em `docs/operations/*`. Codigo C# continua em `backend/*`. Scripts continuam em `scripts/*`.

## O Que E Um Agent

Um Agent e uma unidade especializada com identidade estavel, responsabilidade clara, limites, capacidades, fontes de conhecimento, relacao com runtime ou operacao, e declaracao explicita de seguranca/governanca.

Tipos suportados pelo contrato v1:

- `runtime`: possui implementacao executavel no runtime de agents.
- `knowledge`: especializa consulta ou injecao de conhecimento.
- `operational`: opera por prompt, contexto, runbook, script ou ferramenta externa, sem exigir classe C# propria.
- `hybrid`: combina runtime executavel com base de conhecimento, runbook ou comportamento operacional relevante.

## Manifesto Versus Implementacao

O manifesto responde:

- quem e o Agent;
- qual e seu ID estavel;
- onde esta implementado;
- quais fontes de conhecimento usa;
- quais operacoes pode ou nao pode realizar;
- como se relaciona com AI Governance;
- quais testes cobrem seu comportamento;
- quais outros Agents ou workflows se relacionam a ele.

A implementacao continua onde ela faz sentido arquiteturalmente. Por exemplo, classes C# permanecem em `backend/`, runbooks em `docs/operations/`, prompts em `.ai/prompts/` e scripts em `scripts/`.

## Relacao Com AI Governance

Todo Agent deve declarar se e somente leitura, se pode propor escrita, se pode executar escrita, se pode realizar operacao destrutiva, quando `ActionProposal` e exigido, quando approval e exigido, e qual e o status real de enforcement.

Nenhum Agent pode autoaprovar acao sensivel. A decisao bloqueante pertence ao `AIGovernancePolicyEngine`, a validacao de autorizacao pertence ao `ApprovalPolicy`, e auditoria pertence ao `GovernanceAuditRecorder` ou ao mecanismo que vier a substitui-lo.

## Criacao Futura Pela Agent Factory

Nesta etapa, a Agent Factory completa ainda nao foi criada. A `AgentFactory` atual do backend continua sendo apenas um instanciador runtime simples.

Em etapa posterior, a Future Agent Factory devera usar este contrato para:

- criar manifests e scaffolds;
- validar conformidade;
- registrar agents;
- gerar catalogos;
- auditar seguranca;
- apontar gaps antes de alteracoes de comportamento.
