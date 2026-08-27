# Agents

Esta pasta e o endereco canonico dos Agents do SOMA BlueprintOS.

Cada Agent existente ou futuro deve possuir um manifesto:

```text
agents/<agent-id>/agent.yaml
```

O manifesto e a declaracao canonica de identidade e configuracao do Agent. Ele nao substitui codigo, conhecimento, prompts, runbooks, scripts ou politicas especificas. Ele aponta para essas fontes.

## Fontes Canonicas

- `agents/EXECUTION_POLICY.md`: politica global de execucao, delegacao, no-bypass e credenciais para qualquer IA.
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

## Agent Factory v2

O `agent-factory` e o Agent responsavel por lifecycle e conformidade. Sua implementacao em `tools/agents/agent-factory-v2.js` oferece CREATE, VALIDATE, AUDIT, UPDATE, REGISTER, CATALOG, TEST e SECURITY_CHECK reutilizando o validator canonico.

A `AgentFactory` C# existente permanece um instanciador runtime simples. A Factory v2 nao e Runtime Registry, Tool Gateway ou Policy Engine. Operacoes de auditoria nao modificam Agents; novo Agent e mudanca material exigem autorizacao humana explicita.

```bash
node tools/agents/agent-factory-cli.js validate
node tools/agents/agent-factory-cli.js audit
node tools/agents/agent-factory-cli.js security-check agent-factory
```

## Bootstrap Para Humanos E IAs

Se voce clonou este projeto e esta usando uma IA para operar o BlueprintOS, ela deve carregar e obedecer `agents/EXECUTION_POLICY.md` e `agents/AGENT_CONTRACT.md` antes de executar qualquer tarefa governavel. Arquivos especificos de provider podem apontar para essas fontes, mas nao substitui-las.

A IA atua como orquestradora: deve identificar o Agent responsavel, respeitar delegacao obrigatoria e registrar Capability Gap quando faltar conhecimento ou capacidade. Saber usar SQL, shell, MCP, browser ou API nao concede bypass.

Credenciais pertencem ao usuario que executa. Configure-as diretamente em User Secrets, secret manager da plataforma/corporativo ou secret store local seguro; Keychain e Credential Manager sao estrategias previstas para adapters futuros. Nunca envie senha, token ou cookie no chat. `.env` local e ignorado pelo Git e seus templates versionados devem permanecer sem valores secretos.
