# Work Order — A13 — Primeiro Vertical Slice do +Compras

## Metadados

- Status: Approved
- Responsável: Codex
- Prioridade: Alta
- Dependências: A2 e A6; ADR-0011.
- Data de aprovação: 30/07/2026

## Objetivo

Implementar o primeiro fluxo funcional de ponta a ponta do +Compras.

## Contexto

Foram lidos [VISION.md](../VISION.md), [WORKFLOW.md](../WORKFLOW.md), [PROJECT_STATE.md](../PROJECT_STATE.md), [CURRENT_SPRINT.md](../CURRENT_SPRINT.md), [DECISIONS.md](../DECISIONS.md) e a documentação específica aplicável. Esta Work Order está aprovada para planejamento e ainda não representa execução ou evidência de funcionalidade entregue.

## Task Packet

- ID: A13
- Título: Primeiro Vertical Slice do +Compras
- Descrição: expor o primeiro fluxo consultivo de negociação, a partir das capacidades já existentes.
- Executor: Codex
- Entradas: contexto da compra e dados de negociação aprovados para o fluxo.
- Saídas: recomendação consultiva e explicável.
- Critérios de aceite: contrato versionado, validação, decisão humana explícita, testes e documentação.
- Testes obrigatórios: build, unitários, integração e smoke test.

## Escopo

- Expor o primeiro fluxo consultivo de negociação do +Compras por contrato REST/JSON versionável.
- Reutilizar as capacidades existentes de memória e estratégia de negociação, sem reimplementar regras de negócio na API.
- Manter a recomendação estritamente consultiva, com decisão humana obrigatória.
- Aplicar a estratégia de identidade definida na [ADR-0011](../DECISIONS.md#adr-0011-identidade-temporária-de-desenvolvimento-para-antecipar-a-persistência-de-fornecedores): identidade temporária somente em `Development`, contrato desacoplado e preparado para futura substituição pelo Microsoft Entra ID.

## Fora do escopo

- Implementar Microsoft Entra ID, autenticação corporativa, autorização ou uso produtivo.
- Criar cadastro, persistência ou migração de fornecedores; essas capacidades pertencem à B1.
- Banco de dados, ERP, portal, frontend, pedidos, cotações ou execução automática de compras.
- Alterar a estratégia funcional de negociação existente.

## Arquitetura

Seguir a estrutura física atual sem criar arquitetura paralela. A API conterá somente contratos HTTP, validação, logging e mapeamento; memória e estratégia permanecem atrás de contratos coesos. Caso o fluxo futuro persista fornecedores, o vínculo de autoria/responsabilidade usará o identificador da identidade temporária em `Development`, por meio de contrato de identidade substituível. A13 não implementa essa persistência; ela preserva compatibilidade para a migração posterior ao Entra ID, conforme ADR-0011.

## Banco

Sem impacto nesta Work Order. A persistência de fornecedores e o vínculo ao usuário temporário serão definidos e implementados na B1, observando a ADR-0011 e a futura migração de identificadores para Entra ID.

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
