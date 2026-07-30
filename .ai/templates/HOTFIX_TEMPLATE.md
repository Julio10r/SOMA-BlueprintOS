# Hotfix — [ID e título]

## Metadados

- Status: Draft | Approved | In Progress | Completed | Blocked
- Responsável:
- Prioridade:
- Dependências:
- Data de aprovação:

## Incidente

## Contexto

Antes de executar, ler [VISION.md](../VISION.md), [WORKFLOW.md](../WORKFLOW.md), [PROJECT_STATE.md](../PROJECT_STATE.md) e [CURRENT_SPRINT.md](../CURRENT_SPRINT.md), além da documentação específica da sprint. Em urgência, registrar as evidências disponíveis e completar a análise assim que o serviço estiver estabilizado.

## Impacto

## Causa raiz

## Escopo da correção

## Fora do escopo

## Correção

Descrever a menor mudança segura. Preservar Clean Architecture, SOLID, Design System vigente e padrões existentes; não introduzir arquitetura paralela sob urgência.

## Banco

Descrever impacto em MSSQL, dados e migrações, incluindo segurança de execução e compatibilidade, quando aplicável.

## Testes

- Reprodução do incidente:
- Validação da correção:
- Regressão unitária:
- Integração e smoke test:

## Rollback

Definir gatilhos, responsável, procedimento e impacto esperado da reversão.

## Documentação

Atualizar apenas registros afetados, incluindo `PROJECT_STATE.md`, `CURRENT_SPRINT.md` e documentação da sprint quando aplicável.

## Critérios de aceite

- [ ] Incidente reproduzido ou evidenciado.
- [ ] Causa raiz e impacto registrados.
- [ ] Correção e rollback revisados.
- [ ] Testes aplicáveis e build aprovados.
- [ ] Sem regressão conhecida.

## Git Workflow

Seguir o fluxo de branch `hotfix/`, Conventional Commits, revisão e validações do [WORKFLOW.md](../WORKFLOW.md). Executar `git status` e `git diff --stat` antes do commit.

## Relatório final

- Incidente e impacto:
- Causa raiz:
- Correção aplicada:
- Testes e build:
- Rollback:
- Riscos remanescentes:
- Próximos passos:
- Commit e push:
