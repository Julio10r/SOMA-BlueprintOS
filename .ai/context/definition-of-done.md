# context/definition-of-done.md

> Este documento substitui e unifica as definições de Done anteriormente descritas em STANDARDS.md §26 e WORKFLOW.md §19. É a Definition of Done canônica do SOMA BlueprintOS.

## Checklist canônico

Uma tarefa só é considerada concluída (Done) quando todos os itens abaixo forem verdadeiros:

- ✓ Código implementado, respeitando ARCHITECTURE.md e STANDARDS.md
- ✓ Build compila sem erros
- ✓ Testes passam (unitários, integração e demais aplicáveis — ver [testing.md](./testing.md))
- ✓ Documentação atualizada (inclui docs de módulo e, quando aplicável, este handbook)
- ✓ Agente registrado no Runtime, quando a tarefa envolver criação/alteração de agente (ver [agents.md](./agents.md))
- ✓ Logs implementados para os pontos relevantes da execução (ver [observability.md](./observability.md))
- ✓ Métricas implementadas quando a tarefa introduzir uma nova operação observável (ver [observability.md](./observability.md))
- ✓ Sem TODOs críticos pendentes
- ✓ Revisão concluída (arquitetura, padrões, nomenclatura, testes, performance, segurança, documentação — ver WORKFLOW.md §10)
- ✓ ADR criada quando a tarefa envolver decisão arquitetural relevante (ver [DECISIONS.md](../DECISIONS.md))
- ✓ Aprovada pelo Product Owner (ver WORKFLOW.md §12)
- ✓ Memória atualizada (`.ai/memory/completed_sprints.md`, `known_issues.md`, `patterns.md`, quando aplicável — ver WORKFLOW.md §14)

## Uso

Este checklist é o único a ser consultado ao avaliar se uma tarefa está concluída. STANDARDS.md e WORKFLOW.md apontam para este arquivo em vez de manter checklists próprios, para evitar divergência entre os dois.
