# ROADMAP.md

> Roadmap de alto nível do SOMA BlueprintOS, por fases. Não descreve sprints — para detalhe de sprint atual, ver `.ai/CURRENT_SPRINT.md`; para histórico de sprints concluídas, ver `.ai/memory/completed_sprints.md`.

O projeto encontra-se na **Fase 0 - Fundação**, ainda em andamento. As sprints A7, A8, A9, A10, A11 e A12 estão registradas em `.ai/memory/completed_sprints.md`; o estado operacional verificável está em `.ai/PROJECT_STATE.md`.

O catálogo estratégico oficial de oito fases e 56 Work Orders está em `.ai/workorders/README.md`; ele não altera o status comprovado das funcionalidades.

---

## Fase 0 - Fundação (status: em andamento)

Objetivo: estabelecer as bases de arquitetura, padrões, processo e infraestrutura antes de construir funcionalidade de negócio.

- Definição da arquitetura oficial (Modular Monolith + Clean Architecture + DDD pragmático).
- Padrões de engenharia (STANDARDS.md) e workflow da AI Factory (WORKFLOW.md).
- Engineering Handbook (`.ai/`) completo e navegável.
- Estrutura alvo de pastas `/src/Apps`, `/src/BuildingBlocks`, `/src/Modules` definida, mas **ainda não adotada fisicamente**. O backend real está em `backend/src/BlueprintOS.{Api,Application,Core,Domain,Infrastructure,Shared}`.
- Infraestrutura básica: Docker Compose com SQL Server e API. Pipeline de CI e ambiente GCP inicial ainda não estão implementados.

- Portal de documentação viva (dashboards, guias, changelog, ADRs) publicado automaticamente em `docs/` (Sprint A8).
- **EPIC de documentação: concluído (23/07/2026).** A7 implementou o módulo Documentation; A8 comprovadamente adicionou publicadores por público e o Portal de Documentação Viva; A9 implementou o Publication Engine. A10–A12 consolidaram a governança e a especificação documental em 30/07/2026. Ver `.ai/memory/completed_sprints.md` e `.ai/PROJECT_STATE.md`.

---

## Fase 1 - Módulos Core

Objetivo: entregar os módulos que sustentam identidade, planejamento e automação de processo.

- **Identity** — autenticação (Entra ID), autorização, multi-tenant.
- **Planner** — decomposição e execução de planos de trabalho.
- **Workflow** — motor de fluxos de processo de negócio.

> Estado real: há somente um workflow sequencial básico no código. Identity e Planner não foram iniciados como módulos de produto; o motor de processo de negócio permanece planejado.

---

## Fase 2 - Conhecimento e Memória

Objetivo: dar à plataforma capacidade de reter e recuperar conhecimento, e de operar agentes de IA.

- **Knowledge** — ingestão, indexação e recuperação de conhecimento organizacional.
- **Memory** — memória de curto, médio e longo prazo para agentes e execuções.
- **Agents** — runtime de agentes especializados, registro e execução.

> Estado real: Knowledge por Markdown, memória de negociação em processo e runtime básico com `EchoAgent` e `KnowledgeAgent` existem. Memória corporativa genérica e agentes especializados de +COMPRAS permanecem planejados.

---

## Fase 3 - Automação e Integrações

Objetivo: conectar a plataforma a processos de negócio reais e sistemas externos.

- **Procurement** — automação de processos de compras.
- **Notifications** — notificações e comunicação com usuários e sistemas externos.
- Integrações externas (ERPs, n8n, APIs corporativas).

> Estado real: há persistência própria de fornecedores, APIs REST e descoberta de fornecedores somente leitura no ERP SOMA_DESENV; a validação operacional deste acesso está pendente por timeout de rede. Procurement completo, portal operacional, itens, pedidos, notificações e n8n não foram iniciados.

## Reorientação do roadmap do +Compras

A [ADR-0013](./DECISIONS.md) organiza a evolução em dois blocos sem alterar as oito fases e 56 Work Orders estratégicas: primeiro a plataforma operacional e, sobre seus dados reais, a plataforma inteligente.

1. **Operacional:** fornecedores, itens, compras/pedidos, portal como interface integrada, adaptadores ERP por BU e fluxo ponta a ponta com auditoria básica.
2. **Inteligente:** inteligência de fornecedores, itens e compras; negociação, orçamento, auditoria, compliance e autonomia progressiva.

O portal não é uma fase isolada: ele evolui com os módulos. Operações críticas mantêm caminho manual e confirmação humana; IA não é a única forma de concluí-las. B2.1 e sua subetapa B2.1.1 foram concluídas em 01/08/2026, com sincronização bidirecional, regra temporal, inativação, auditoria, concorrência e mapeamento canônico ERP → +Compras validados. A pendência B2.1.2 está em andamento para alinhar estruturalmente o ERP Linx ao contrato canônico antes de qualquer correção por migration ou validação adicional. B2.2 permanece em Draft para enriquecimento cadastral por CNPJ; B3 não está iniciada.

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
