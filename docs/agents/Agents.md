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

Os dois nunca duplicam responsabilidade entre si (ver detalhe em `wise-knowledge.md`, seção "Relação com o Agent Linx").

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
