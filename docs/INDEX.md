# BlueprintOS Documentation Index

## Visão Geral e classificação

Este documento é o índice oficial da documentação versionada em `docs/`. As fontes canônicas de visão, estado e processo ficam em `.ai/`; saídas por público em `docs/executive`, `docs/client` e `docs/engineering` são documentação derivada e devem ser regeneradas/revisadas a partir dessas fontes.

- **Implementado/versionado:** código, documentação por público, assets e templates que existem no Git.
- **Histórico:** documentos preservados para rastreabilidade, sem substituir as fontes canônicas.
- **Planejado/local vazio:** estruturas citadas sem conteúdo versionado; não representam implementação disponível.

---

## Arquitetura

> Estrutura oficial por público-alvo (ver ADR-0009 em `.ai/DECISIONS.md`): Architecture, API e ADR Index vivem como seções dentro de `engineering/` e `client/`, não em diretórios próprios de topo.

- `engineering/Architecture.md` — Documentação da arquitetura geral do BlueprintOS (estilo arquitetural, camadas, módulos e padrões técnicos).
- `engineering/APIs.md` / `client/API.md` — Documentação das APIs expostas pelo sistema, por público-alvo.
- `assets/architecture.mmd`, `assets/dependencies.mmd` — Diagramas Mermaid de arquitetura e dependências.
- `engineering/Decisions.md` — Architecture Decision Records (índice; texto completo em `.ai/DECISIONS.md`).

---

## AI Factory

- `00 - AI Factory` — Visão geral da AI Factory: objetivo, princípios e componentes.
- `01 - AI Orchestrator` — Documentação do orquestrador responsável por coordenar os agentes.
- `02 - AI Team` — Estrutura da equipe de agentes especialistas e hierarquia de responsabilidades.
- `03 - Task Protocol` — Protocolo oficial de criação, execução e encerramento de Tasks.
- `04 - Memory System` — Sistema de memória da AI Factory (curto, médio e longo prazo).
- `05 - Automation Roadmap` — Roadmap de automações e evolução da AI Factory.
- `Architecture/` — Documentação técnica detalhada da arquitetura interna da AI Factory (stack, runtime, orquestrador, RAG, memória, workflows e observabilidade).
- `Agents/` — Documentação individual de cada agente especialista.
- `Memory/` — Documentação específica sobre a implementação do sistema de memória.
- `Prompts/` — Prompts oficiais utilizados pelos agentes.
- `Examples/` — Exemplos práticos de uso da AI Factory.
- `Core/` — Documentação dos componentes centrais da AI Factory.

---

## Banco de Dados (planejado/local vazio)

`database/` existe apenas como estrutura local vazia; não há modelo, migrações, scripts ou seed versionados.

---

## Infraestrutura

- `infrastructure/docker/` — reservado (não usado no ambiente local; ver ADR-0018 em `.ai/DECISIONS.md`).
- `kubernetes/`, `monitoring/`, `nginx/` e `terraform/` — estruturas locais vazias e planejadas; não há implementação versionada.

## Fontes canônicas e histórico

- `.ai/VISION.md` — visão, escopo e direção estratégica.
- `.ai/PROJECT_STATE.md` — estado operacional comprovado.
- `.ai/WORKFLOW.md` — processo oficial de desenvolvimento.
- `.ai/CURRENT_SPRINT.md`, `.ai/workorders/` e `.ai/memory/completed_sprints.md` — execução, escopo e histórico de sprints.

---

## Sprints

Toda Sprint deve possuir sua documentação própria, registrando planejamento, execução, entregas e retrospectiva.

---

## Templates

- `ADR.md`
- `API.md`
- `Feature.md`
- `RFC.md`
- `Sprint.md`
- `Task.md`
- `Workflow.md`

---

## ADR

Architecture Decision Records (ADR) documentam formalmente as decisões arquiteturais relevantes do BlueprintOS, incluindo contexto, alternativas consideradas, decisão tomada e consequências.

---

## Convenções

- Documentação permanente fica em `docs/`.
- Estado operacional fica em `.ai/`.
- Código fica fora de `docs/`.
- Novas decisões arquiteturais devem gerar ADR.
