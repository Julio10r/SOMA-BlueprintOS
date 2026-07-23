# CLAUDE.md

> Ponto de entrada do Engineering Handbook do SOMA BlueprintOS.

O SOMA BlueprintOS é uma plataforma corporativa de IA para automatizar processos de negócio através de agentes especializados, workflows, memória e planejamento, com integração a sistemas corporativos. Deve ser modular, escalável, seguro e multi-tenant.

---

## Leitura obrigatória (nesta ordem)

1. [PROJECT.md](./PROJECT.md)
2. [ARCHITECTURE.md](./ARCHITECTURE.md)
3. [STANDARDS.md](./STANDARDS.md)
4. [CURRENT_SPRINT.md](./CURRENT_SPRINT.md)

Nenhuma implementação deve começar sem essa leitura (ver PROJECT.md §7).

---

## Documentos de contexto (`context/`)

| Arquivo | Consultar quando... |
|---|---|
| [context/architecture.md](./context/architecture.md) | precisar relacionar módulos à AI Factory |
| [context/coding-standards.md](./context/coding-standards.md) | precisar de princípios SOLID/Clean Code além de STANDARDS.md |
| [context/runtime.md](./context/runtime.md) | for implementar execução de agentes/tarefas |
| [context/agents.md](./context/agents.md) | for criar ou alterar um agente |
| [context/memory.md](./context/memory.md) | precisar entender os níveis de memória |
| [context/planner.md](./context/planner.md) | for implementar planejamento/decomposição de tarefas |
| [context/knowledge.md](./context/knowledge.md) | for trabalhar no módulo Knowledge |
| [context/testing.md](./context/testing.md) | for escrever ou revisar testes |
| [context/security.md](./context/security.md) | for tratar autenticação, autorização ou LGPD |
| [context/observability.md](./context/observability.md) | for implementar logs, métricas ou tracing |
| [context/git-workflow.md](./context/git-workflow.md) | for commitar e enviar código |
| [context/tech-stack.md](./context/tech-stack.md) | precisar confirmar a stack oficial |
| [context/definition-of-done.md](./context/definition-of-done.md) | for verificar se uma tarefa está concluída |

---

## Templates de prompt (`prompts/`)

| Arquivo | Uso |
|---|---|
| [prompts/new-agent.md](./prompts/new-agent.md) | criar um novo agente de IA |
| [prompts/new-api.md](./prompts/new-api.md) | criar um novo endpoint/API |
| [prompts/new-database.md](./prompts/new-database.md) | criar tabela/migração de banco |
| [prompts/refactor.md](./prompts/refactor.md) | refatorar código existente |
| [prompts/tests.md](./prompts/tests.md) | escrever testes para uma funcionalidade |

---

## Outros documentos centrais

- [DECISIONS.md](./DECISIONS.md) — log de ADRs
- [ROADMAP.md](./ROADMAP.md) — roadmap de alto nível
- [WORKFLOW.md](./WORKFLOW.md) — fluxo oficial de trabalho
- [AI_TEAM.md](./AI_TEAM.md) — arquitetura multi-agente alvo

---

Este arquivo é o ponto de entrada e deve permanecer pequeno. Todos os detalhes vivem nos documentos linkados acima.
