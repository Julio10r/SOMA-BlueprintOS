# ROADMAP.md

> Roadmap de alto nível do SOMA BlueprintOS, por fases. Não descreve sprints — para detalhe de sprint atual, ver `.ai/CURRENT_SPRINT.md`; para histórico de sprints concluídas, ver `.ai/memory/completed_sprints.md`.

O projeto encontra-se em estágio inicial: `.ai/memory/completed_sprints.md` ainda não registra nenhuma sprint concluída, portanto o BlueprintOS está atualmente na **Fase 0 - Fundação**.

---

## Fase 0 - Fundação (status: em andamento)

Objetivo: estabelecer as bases de arquitetura, padrões, processo e infraestrutura antes de construir funcionalidade de negócio.

- Definição da arquitetura oficial (Modular Monolith + Clean Architecture + DDD pragmático).
- Padrões de engenharia (STANDARDS.md) e workflow da AI Factory (WORKFLOW.md).
- Engineering Handbook (`.ai/`) completo e navegável.
- Estrutura de pastas `/src/Apps`, `/src/BuildingBlocks`, `/src/Modules` criada.
- Infraestrutura básica: Docker, pipeline de build, ambiente GCP inicial.

- Portal de documentação viva (dashboards, guias, changelog, ADRs) publicado automaticamente em `docs/` (Sprint A8).
- **EPIC A7 — Sistema de Documentação: ✅ Concluído (23/07/2026).** Sprints A7 (módulo Documentation), A8 (Portal de Documentação Viva), A9 (Publication Engine) e A7.4/A7.5 (homologação final e hotfix de saúde da documentação). Ver `.ai/memory/completed_sprints.md` para o detalhe de cada sprint e ADR-0009 em `.ai/DECISIONS.md` para a organização final de diretórios. Tag de release: `v0.4.0-documentation`.

---

## Fase 1 - Módulos Core

Objetivo: entregar os módulos que sustentam identidade, planejamento e automação de processo.

- **Identity** — autenticação (Entra ID), autorização, multi-tenant.
- **Planner** — decomposição e execução de planos de trabalho.
- **Workflow** — motor de fluxos de processo de negócio.

---

## Fase 2 - Conhecimento e Memória

Objetivo: dar à plataforma capacidade de reter e recuperar conhecimento, e de operar agentes de IA.

- **Knowledge** — ingestão, indexação e recuperação de conhecimento organizacional.
- **Memory** — memória de curto, médio e longo prazo para agentes e execuções.
- **Agents** — runtime de agentes especializados, registro e execução.

---

## Fase 3 - Automação e Integrações

Objetivo: conectar a plataforma a processos de negócio reais e sistemas externos.

- **Procurement** — automação de processos de compras.
- **Notifications** — notificações e comunicação com usuários e sistemas externos.
- Integrações externas (ERPs, n8n, APIs corporativas).

---

## Fase 4 - Observabilidade e Escala

Objetivo: preparar a plataforma para operação em produção multi-tenant e em escala.

- **Dashboard** — visibilidade operacional e de negócio.
- **Analytics** — indicadores e análises avançadas.
- Observabilidade completa (métricas, logs, tracing) em produção.
- Preparação para separação em microsserviços quando necessário (ver ARCHITECTURE.md §13).
- Escalabilidade horizontal e revisão de multi-tenant em produção.

---

## Observações

- As fases são sequenciais em intenção, mas podem se sobrepor conforme prioridade do Product Owner.
- Nenhuma fase avança sem que a fase anterior tenha fundação arquitetural estável.
- Este roadmap deve ser revisado a cada mudança relevante de escopo, e não substitui o planejamento de sprint (ver WORKFLOW.md §5 e §17).
