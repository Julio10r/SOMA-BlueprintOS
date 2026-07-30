# Work Order — [ID e título]

## Metadados

- Status: Draft | Approved | In Progress | Completed | Blocked
- Responsável:
- Prioridade:
- Dependências:
- Data de aprovação:

## Objetivo

## Contexto

Antes de executar, ler [VISION.md](../VISION.md), [WORKFLOW.md](../WORKFLOW.md), [PROJECT_STATE.md](../PROJECT_STATE.md) e [CURRENT_SPRINT.md](../CURRENT_SPRINT.md), além da documentação específica da sprint.

## Escopo

## Fora do escopo

## Arquitetura

Descrever o impacto nas camadas e contratos existentes. Seguir Clean Architecture, SOLID, Design System vigente e os padrões do BlueprintOS; não criar arquitetura paralela. Registrar ADR quando houver mudança arquitetural aprovada.

## Banco

Descrever impacto em MSSQL, modelo, migrações, compatibilidade e dados, quando aplicável. Seguir os padrões existentes.

## Testes

- Build:
- Unitários:
- Integração:
- Smoke test:

## Documentação

Atualizar somente os documentos afetados, incluindo `PROJECT_STATE.md`, `CURRENT_SPRINT.md`, histórico e documentação específica da sprint quando aplicável.

## Critérios de aceite

- [ ] Objetivo e escopo aprovados foram atendidos.
- [ ] Compatibilidade e funcionalidades existentes foram preservadas.
- [ ] Build sem erros e sem warnings críticos.
- [ ] Testes aplicáveis aprovados.
- [ ] Documentação e evidências atualizadas.

## Git Workflow

Seguir o Git Flow e Conventional Commits do projeto. Antes do commit, executar `git status` e `git diff --stat`; concluir revisão, validações e aprovação conforme [WORKFLOW.md](../WORKFLOW.md).

## Relatório final

- Objetivo entregue:
- Decisões técnicas:
- Arquivos alterados:
- Testes executados:
- Resultado do build:
- Riscos:
- Próximos passos:
- Commit e push:
