# BlueprintOS Documentation Index

## Visão Geral

Este documento é o índice oficial da documentação permanente do SOMA BlueprintOS. Ele organiza e referencia todo o conteúdo disponível em `docs/`, servindo como ponto de entrada para arquitetura, AI Factory, banco de dados, infraestrutura, sprints, templates e ADRs.

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

## Banco de Dados

- `database/`
  - `docs` — Documentação do modelo de dados e regras do banco.
  - `migrations` — Migrações versionadas do banco de dados.
  - `scripts` — Scripts auxiliares de banco de dados.
  - `seed` — Dados iniciais (seed) utilizados pelo sistema.

---

## Infraestrutura

- `docker` — Configurações e definições de containers Docker.
- `kubernetes` — Manifestos e configurações de orquestração Kubernetes.
- `monitoring` — Configurações de observabilidade e monitoramento.
- `nginx` — Configurações do proxy reverso Nginx.
- `terraform` — Definições de infraestrutura como código (IaC).

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
