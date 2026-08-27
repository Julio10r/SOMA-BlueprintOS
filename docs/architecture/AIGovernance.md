# AI Governance Onda 1

Status: implementado como fundacao tecnica incremental em 2026-08-27.

## Objetivo

A AI Governance Onda 1 cria a primeira camada transversal para classificar risco, vincular autorizacao humana a uma proposta especifica e preparar o futuro Tool Gateway. Ela nao implementa Maestro completo, Planner completo, Tool Gateway universal, executor SQL arbitrario, nem migra os scripts externos existentes.

## Componentes Implementados

### ActionProposal

`ActionProposal` representa exatamente "o que esta sendo autorizado". O modelo vive em `backend/src/BlueprintOS.Core/AI/Governance/Models/ActionProposal.cs` e inclui ambiente, sistema, recurso, operacao, campos, filtro, quantidade prevista, finalidade, classificacao de dados, reversibilidade, runbook, agent solicitante e `ProposalHash`.

O `ProposalHash` e calculado de forma deterministica sobre o conteudo material da proposta. Se ambiente, sistema, tabela/recurso, operacao, campo, filtro, quantidade, finalidade, classificacao de dados, runbook ou agent solicitante mudar, a aprovacao anterior deixa de casar.

### Policy Engine

`AIGovernancePolicyEngine` e deterministico. Ele classifica propostas como:

- `Green`: leitura, schema discovery, metadata, analise, comparacao sem efeito externo.
- `Yellow`: escrita contextualizada, operacao prevista em runbook aprovado, dado pessoal nao sensivel, classificacao desconhecida, desvio material que exige reavaliacao.
- `Red`: destrutivo, privilegio, segredo, update sem filtro/contexto, update sem estimativa, procedure desconhecida, exportacao massiva de PII, dado pessoal sensivel, acao irreversivel relevante.

O engine nao depende de LLM para regras triviais e bloqueantes.

### Approval

`ApprovalRequest` representa o pedido objetivo de autorizacao especifica. `ApprovalGrant` representa a aprovacao concedida e carrega o `ProposalHash`.

Uma aprovacao e valida somente quando:

- o hash da proposta e igual ao hash do grant;
- a aprovacao nao expirou;
- a aprovacao nao foi revogada.

### Auditoria

`GovernanceAuditEntry` registra proposta, classificacao, motivos, agent solicitante, timestamps, approval request/grant quando houver e resultado. A implementacao inicial `InMemoryGovernanceAuditRecorder` prova o contrato sem migration nesta onda.

Regra permanente: auditoria de governanca nao deve registrar segredos nem payload pessoal desnecessario.

### Security/LGPD Agent

`SecurityLgpdAgent` e consultivo. Ele interpreta contexto, explica riscos e orienta revisao humana. Ele nao aprova execucao e nao substitui o Policy Engine.

### Fluxo Demonstrativo Protegido

`GovernedActionDemoFlow` demonstra enforcement sem executar SQL:

```text
Agent/Use Case
-> ActionProposal
-> AIGovernancePolicyEngine
-> ApprovalPolicy
-> GovernanceAuditRecorder
-> permitido/bloqueado
```

Esse fluxo prova que uma acao `Yellow` so segue com `ApprovalGrant` valido e que uma acao `Red` permanece bloqueada.

## LGPD e Privacidade

Classificacoes suportadas:

- `Public`
- `Internal`
- `Confidential`
- `PersonalData`
- `SensitivePersonalData`
- `SecretCredential`
- `Unknown`

O sistema nao inventa classificacao de campos. Quando a classificacao e `Unknown`, o Policy Engine trata com cautela e exige aprovacao. Dados pessoais sensiveis e segredos elevam risco para `Red`.

## Desvio Material

Nesta onda, desvio material e avaliado por contrato explicito de runbook: `IsRunbookApprovedOperation`, `RunbookReference`, `RunbookExpectedAffectedRows` e `ExpectedAffectedRows`.

Nao foi adotado um percentual magico. A regra inicial considera desvio material quando a diferenca absoluta entre o volume proposto e o volume esperado do runbook ultrapassa o maior valor entre `1000` registros e o proprio volume esperado. Essa heuristica deve migrar para politica configuravel por runbook quando os primeiros fluxos reais forem conectados ao gateway.

## Enforcement Atual

Enforced nesta onda:

- classificacao deterministica de `ActionProposal`;
- hash de proposta;
- validacao de approval por hash, expiracao e revogacao;
- bloqueio demonstrativo para `Red`;
- exigencia demonstrativa de approval para `Yellow`;
- registro de auditoria em memoria;
- DI dos componentes de governanca;
- `SecurityLgpdAgent` registrado como agent consultivo.

Ainda documental ou planejado:

- Tool Gateway universal;
- persistencia de approvals/auditoria em banco;
- UI de aprovacao humana;
- migracao dos scripts Linx/WISE e Showcase;
- interceptacao de MCP/pyodbc/manual SQL;
- classificacao campo-a-campo automatica de PII;
- DLP de prompts/logs/exportacoes.

## Relacao com Agents Existentes

- Agent Linx continua especialista de conhecimento. `LinxDatabaseSpecialistAgent` nao vira executor SQL.
- WISE Agent continua somente leitura por padrao; escrita real segue o runbook diario ou fluxo futuro governado.
- Showcase Agent continua somente leitura na API; exportacoes locais podem virar `ActionProposal` quando houver risco comercial/PII.
- Runtime agents genericos ainda nao passam automaticamente pelo Policy Engine.

## Roadmap para Tool Gateway

1. Persistir `ActionProposal`, `ApprovalRequest`, `ApprovalGrant` e `GovernanceAuditEntry`.
2. Criar endpoint/API de aprovacao com RBAC.
3. Criar `IToolGateway` obrigatorio para tools agent-driven.
4. Envolver primeiro o fluxo Linx/WISE de escrita em adapter governado.
5. Adicionar politica configuravel por runbook.
6. Adicionar classificacao explicita de dados por recurso/campo.
7. Integrar redacao/mascaramento para logs, prompts e exportacoes.

