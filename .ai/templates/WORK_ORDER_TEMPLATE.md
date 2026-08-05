# Work Order — [ID e título]

## Metadados

- Fase: (quando a Work Order pertencer ao catálogo estratégico de fases A–H)
- Sprint: (quando aplicável)
- Status: Draft | Approved | In Progress | Completed | Blocked
- Responsável:
- Prioridade:
- Dependências:
- Data de aprovação:

## Objetivo

## Problema de negócio

Descrever, quando aplicável, a demanda ou dor de negócio que motiva a Work Order.

## Contexto

Antes de executar, ler [VISION.md](../VISION.md), [WORKFLOW.md](../WORKFLOW.md), [PROJECT_STATE.md](../PROJECT_STATE.md) e [CURRENT_SPRINT.md](../CURRENT_SPRINT.md), além da documentação específica da sprint.

## Escopo

## Requisitos funcionais

## Requisitos não funcionais

## Fora do escopo

## Arquitetura

Descrever o impacto nas camadas e contratos existentes, incluindo componentes afetados, modelo de domínio, casos de uso e contratos/APIs envolvidos. Seguir Clean Architecture, SOLID, Design System vigente e os padrões do BlueprintOS; não criar arquitetura paralela. Registrar ADR quando houver mudança arquitetural aprovada.

## Banco

Descrever impacto em MSSQL, modelo, migrações, compatibilidade e dados, quando aplicável. Seguir os padrões existentes.

## Integrações

Descrever integrações externas ou entre módulos, quando aplicável.

## Segurança e autorização

Descrever impacto em autenticação, autorização e proteção de dados, quando aplicável.

## Observabilidade

Descrever logging estruturado, correlação e métricas relevantes, quando aplicável.

## Tratamento de erros

Descrever o comportamento esperado para requests inválidos, falhas de domínio e falhas inesperadas, quando aplicável.

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

## Riscos

## Decisões pendentes

## Plano de implementação

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
