# Agentes de IA

O módulo `BlueprintOS.Core.Agents` define o runtime de agentes especializados:

- `IAgent` — contrato base implementado por todos os agentes.
- `BaseAgent` — classe base que injeta `IAIRuntime` (e, opcionalmente, `IKnowledgeService`) nos agentes concretos.
- `EchoAgent` — agente de referência/diagnóstico.
- `KnowledgeAgent` — agente que consulta o módulo `Knowledge` para responder com base em conhecimento organizacional indexado.
- `AgentFactory` — fábrica que cria instâncias de agentes via reflexão, injetando o runtime de IA e o serviço de conhecimento quando aplicável.

O módulo `AI.Negotiation` complementa a fundação com memória de negociação (`INegotiationMemory`) e um motor de estratégia baseado em regras (`INegotiationStrategy`). Não há, no código atual, um agente concreto Buyer sênior — ver o backlog em `.ai/work-orders/backlog/fase-a/A6-agente-comprador-senior.md`.

Este documento descreve a arquitetura e responsabilidades dos agentes. Prompts, memória operacional e contexto de execução dos agentes vivem exclusivamente em `.ai/` (`.ai/prompts/`, `.ai/memory/`, `.ai/context/`) e nunca são duplicados aqui.

## Como o backend aciona os agentes hoje

Ver [docs/backend/orchestration/Orchestration.md](../backend/orchestration/Orchestration.md) para o único fluxo real de coordenação backend → estratégia/memória implementado até o momento (o vertical slice de recomendação consultiva de negociação).

## Especialistas de Conhecimento Operacional (camada `.ai/`)

Além dos agentes de runtime acima, o BlueprintOS mantém especialistas de conhecimento operacional persistido em Markdown (`.ai/context/`), acionáveis por prompt-gatilho (`.ai/prompts/`) e documentados por runbook (`docs/operations/`) — este é o caminho reutilizável para conhecimento de domínio ainda sem RAG/indexação automática (ver [context/knowledge.md](../../.ai/context/knowledge.md)):

- **Agent Linx** (código: `LinxErpSpecialistAgent` / `LinxDatabaseSpecialistAgent`, ver `backend/src/BlueprintOS.Application/Knowledge/Linx/LinxSpecialistAgents.cs`) — especialista no ERP Visual Linx: regras funcionais, fluxos e schema estrutural (`SOMA_DESENV`).
- **WISE Agent** (conhecimento: [`.ai/context/wise-knowledge.md`](../../.ai/context/wise-knowledge.md); acionamento: [`.ai/prompts/consultar-wise.md`](../../.ai/prompts/consultar-wise.md); runbook: [`docs/operations/WiseAgentRunbook.md`](../operations/WiseAgentRunbook.md)) — especialista no ambiente WISE: campanhas, saldo/estoque, estrutura `WS_*` e relacionamento com o Showcase. Consulta somente leitura por padrão; qualquer escrita exige autorização explícita do Product Owner e segue as regras já documentadas na rotina diária Linx/WISE ([`.ai/context/linx-wise-daily-integration.md`](../../.ai/context/linx-wise-daily-integration.md)).
- **Showcase Agent** (conhecimento: [`.ai/context/showcase-knowledge.md`](../../.ai/context/showcase-knowledge.md); acionamento: [`.ai/prompts/coletar-showcase.md`](../../.ai/prompts/coletar-showcase.md); runbook: [`docs/operations/ShowcaseAgentRunbook.md`](../operations/ShowcaseAgentRunbook.md); implementação: [`scripts/showcase_collector/`](../../scripts/showcase_collector/)) — especialista em coletar catálogo, grade e fotos do Showcase (Compuwise/WiseCommerce), genérico para qualquer marca/região disponível na sessão autenticada (nunca fixa marca). Somente leitura; nunca persiste token/cookie/segredo — apenas o mecanismo de obtenção da sessão. Fornece PRODUTO+COR ao WISE Agent para enriquecimento de saldo, sem duplicar o conhecimento interno do WISE.
- **Security/LGPD Agent** (código: `SecurityLgpdAgent`; governança: [`docs/architecture/AIGovernance.md`](../architecture/AIGovernance.md)) — especialista consultivo em segurança, privacidade e LGPD. Interpreta contexto, minimização, finalidade, exposição em logs/prompts/planilhas, dados pessoais e segredos. Não aprova execução e não substitui o `AIGovernancePolicyEngine`; a decisão bloqueante é determinística.

Linx, WISE e Showcase nunca duplicam responsabilidade entre si (ver detalhe em `wise-knowledge.md`, seção "Relação com o Agent Linx", e em `showcase-knowledge.md`, seção "Colaboração com o WISE Agent"). O Security/LGPD Agent é transversal e independente: agents de domínio propõem ações, mas não autoaprovam operações sensíveis.

## AI Governance Onda 1

A fundação implementada inclui `ActionProposal`, `RiskClassification`/`PolicyDecision`, `AIGovernancePolicyEngine`, `ApprovalRequest`, `ApprovalGrant`, `ApprovalPolicy`, `GovernanceAuditEntry` e `GovernedActionDemoFlow`.

Status real:

- **ENFORCED:** classificação determinística, hash de proposta, validação de approval por hash/expiração/revogação e fluxo demonstrativo protegido.
- **DOCUMENTAL:** guardrails operacionais de WISE/Showcase e parte da rotina Linx/WISE fora do backend.
- **PLANEJADO:** Tool Gateway universal, persistência de approvals/auditoria, UI de aprovação e migração de scripts externos.

## Catálogo Visual dos Agents

[`docs/agents/AgentsCatalog.html`](./AgentsCatalog.html) é o catálogo visual consolidado de todos os agents/especialistas do projeto — abre direto no navegador, sem servidor/build. É uma **visão para humanos**, não a fonte canônica: em caso de divergência, os documentos linkados nele (`.ai/context/`, `.ai/prompts/`, `docs/operations/`) sempre prevalecem.

**Regra permanente:** sempre que um agent/especialista for criado, removido, renomeado, tiver responsabilidade alterada, ou receber conhecimento persistente relevante (nova entrada `.ai/context/`, novo prompt-gatilho, novo runbook), revisar e atualizar `docs/agents/AgentsCatalog.html` na mesma mudança — não deixar para depois.

## AI Factory — fundamentos internos

Os documentos abaixo (migrados de `docs/AI Factory/`) descrevem a arquitetura conceitual interna da AI Factory — o conjunto de capacidades (orquestração, memória, protocolo de tarefas, observabilidade) que sustenta os agentes descritos acima. Alguns descrevem arquitetura-alvo ainda não implementada; não são evidência de código existente.

- [AI Factory — visão geral](./ai-factory/00-AI-Factory.md)
- [Protocolo de tarefas](./ai-factory/03-Task-Protocol.md)
- [Sistema de memória](./ai-factory/04-Memory-System.md)
- [Arquitetura RAG](./ai-factory/04-RAG-Architecture.md)
- [Motor de memória](./ai-factory/05-Memory-Engine.md)
- [Motor de workflow](./ai-factory/06-Workflow-Engine.md)
- [Observabilidade](./ai-factory/07-Observability.md)
- [Roadmap de IA](./ai-factory/08-AI-Roadmap.md)
