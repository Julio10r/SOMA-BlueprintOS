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

Ainda documental ou planejado (Showcase/WISE, ver Onda 2 abaixo para o que ja deixou de ser planejado):

- Tool Gateway universal para todos os Agents (Linx ERP/Banco ja tem `governed-execute` real; Showcase nao tem nenhum adapter real ainda por nao existir codigo de acesso real ao Showcase);
- UI de aprovacao humana (hoje a aprovacao e via CLI, nao via UI web);
- migracao completa dos scripts WISE/Showcase para o mesmo host `governed-execute`;
- interceptacao de MCP/pyodbc/manual SQL fora do escopo Linx PED;
- classificacao campo-a-campo automatica de PII;
- DLP de prompts/logs/exportacoes.

## Onda 2 — `governed-execute` (Execucao Real Persistida, 2026-08-28)

Status: implementado, homologado em SOMA_DESENV e executado de verdade em producao (SOMA, 192.168.9.200) — 77 ajustes de grade PED, zero rollback, zero bypass, volume conservado (15.240=15.240).

O que a Onda 2 fechou, que a Onda 1 deixava como planejado:

- **Persistencia real de `ApprovalGrant`/`GovernanceAuditEntry`**: nao e mais em memoria. E file-based, raiz `<repository-root>/runtime/{backups,governance}/`, organizada por `<agent-id>/<database>/...` com o database real validado (nunca inferido do nome do connection profile) — ver `RuntimeRootLocator`/`GovernanceDatabaseResolver` (`applications/mais-compras/backend/src/BlueprintOS.Infrastructure/Persistence/Governance/`).
- **Host de execucao real e invocavel**: `governed-execute` CLI (`applications/mais-compras/backend/src/BlueprintOS.Api/Governance/GovernedExecuteCliHandler.cs`), modos `propose`/`approve`/`run`/`rollback-plan`/`rollback`/`cleanup`. `governed-plan` (handler mais antigo) continua sendo so dry-run/in-memory, nunca executa de verdade — os dois nao devem ser confundidos.
- **Recovery Package sempre ANTES do write**, nunca depois. Formato de item unico e formato batch v2 com chunking (`BatchRecoveryPackageWriter`, manifest+items-index+chunks numerados com checksum por chunk) para operacoes multi-item, sem forcar sempre-um-por-item.
- **Post-Write Validation conhecida ANTES do write**: toda escrita governada precisa da regra de conferencia definida previamente; a ausencia disso e Knowledge Gap, nunca resolvido por suposicao.
- **Rollback nunca automatico**: `BatchRollbackOrchestrator` sempre exige nova aprovacao (a aprovacao da execucao original nunca autoriza rollback), com deteccao de concorrencia por item (inclusive em lote) que bloqueia rollback incompativel. Rollback = restaurar o estado anterior registrado no Recovery Package, nunca "SQL inverso" adivinhado.
- **Timezone canonico America/Sao_Paulo** (offset -03:00 explicito) para todo artefato novo persistido pelo `governed-execute`, via `BrazilTimeZoneProvider`/`SaoPauloTimeProvider` — nunca retroativo a artefatos historicos.
- **Retencao e cleanup**: Recovery Package expira conforme o `WriteVerificationProfile` (SOMA PROD = 30 dias); `governed-execute cleanup` e um modo real e invocavel do `RecoveryRetentionCleanupService` que remove o pacote fisico mas preserva Governance Audit e Recovery Index (marcado Expired) — limitacao conhecida e por design: nao ha agendador de SO embutido, precisa ser chamado externamente (cron/Task Scheduler).
- **Limitacao conhecida e por design do batch rollback**: nao suporta operacoes mistas (tipos de operacao diferentes) num unico lote.
- **Regra de ambiente ja existente, so referenciada aqui**: DEV homologa mecanismo, PROD determina estado real — ver `agents/DATABASE_CONNECTION_POLICY.md`.
- **Agents nunca executam SQL direto quando a capability governada estiver ausente** — isso e Capability Gap, deve ser identificado explicitamente, nunca contornado.

### Aprendizado funcional desta execucao (PED/grade) — mecanismo generalizavel, numeros nao

A alteracao de `PRODUTOS.GRADE` de um produto para outro codigo de grade e apenas cadastral: ela **nao** realinha automaticamente os dados ja existentes em `COMPRAS_PRODUTO`. A escrita de PED usa quantidade final absoluta por posicao (nao delta, ver `agents/linx-database-specialist-agent/agent.yaml` para a diferenca de semantica entre os mecanismos PROG/OP/PED), entao a correcao acontece em uma unica operacao atomica dentro do `governed-execute run`. Isso e uma caracteristica do **mecanismo**, generalizavel a qualquer ajuste de grade PED futuro. Os numeros desta execucao especifica — 77 itens, 15.240 registros, posicoes 34-44 — sao **evidencia deste caso**, nao regra universal; nao assumir os mesmos numeros, posicoes ou volume em uma proxima execucao.

## Principio Permanente — Agent != LLM

Um Agent representa responsabilidade, autoridade, governanca, ownership, politicas e capabilities — nao e
sinonimo de "chamada a um LLM". Uma capability de um Agent pode ser inteiramente deterministica (leitura/escrita
governada, streaming, validacao, calculo); LLM e opcional e so entra quando a tarefa exige interpretacao/linguagem
natural.

Regra permanente: o caminho feliz (happy path) de uma integracao de alto volume deve preferencialmente executar
com **zero inferencia de LLM** quando a tarefa for deterministica. Nunca fazer uma chamada de Agent/LLM por
registro processado (nunca 1 chamada LLM por linha/registro de um lote).

Fluxo de referencia aprovado:

```text
Orchestrator -> Business Unit Context -> ERP/Linx Agent -> Policy/Gateway -> capability deterministica -> RAW -> REFINED -> Domain Agent
```

Este principio ja tem mecanismo real comprovado: `GovernedExecutionMode.LiveRead` (capability
`linx-dataset-snapshot-read`, `agents/linx-database-specialist-agent/agent.yaml`) executa streaming
`SqlDataReader -> SqlBulkCopy` sem materializar em memoria e com **zero chamadas a `IAIRuntime` no caminho
feliz**, comprovado por teste dedicado com um `IAIRuntime` fake que lanca excecao se for chamado
(`ToolGatewayLiveReadTests`). O Tool Gateway autoriza a execucao uma unica vez por execucao de dataset, nunca uma
vez por registro — mesma garantia ja provada do lado de escrita (`ToolGatewayLiveExecutionTests`,
`ExecuteCallCount == 1`).

## Relacao com Agents Existentes

- Agent Linx ERP e Agent Linx Banco tem capability real de escrita governada e persistida (`ped-grade-adjustment-write` / `soma-database-write-proposal`) via `governed-execute`, homologada e ja usada em producao. `LinxDatabaseSpecialistAgent` continua nao sendo um executor SQL arbitrario — toda escrita passa pelo Motor de Politicas, aprovacao, Recovery Package e Gateway.
- WISE Agent continua somente leitura por padrao; escrita real segue o runbook diario ou fluxo futuro governado (ainda documental/`DOCUMENTAL`, nao `ENFORCED`).
- Showcase Agent continua somente leitura na API; exportacoes locais podem virar `ActionProposal` quando houver risco comercial/PII.
- Runtime agents genericos ainda nao passam automaticamente pelo Policy Engine.

## Roadmap (atualizado apos Onda 2)

1. ~~Persistir `ActionProposal`, `ApprovalRequest`, `ApprovalGrant` e `GovernanceAuditEntry`~~ — feito (file-based) para a capability PED grade adjustment.
2. Criar endpoint/API de aprovacao com RBAC e UI de aprovacao humana (hoje via CLI).
3. Estender `governed-execute` (ou equivalente) para as demais capabilities/Agents (WISE, Showcase) quando existir codigo de acesso real a governar.
4. Adicionar politica configuravel por runbook.
5. Adicionar classificacao explicita de dados por recurso/campo.
6. Integrar redacao/mascaramento para logs, prompts e exportacoes.
7. Agendador de SO para `governed-execute cleanup` (hoje precisa ser chamado externamente).

