# WORKFLOW.md

> Workflow oficial da AI Factory.

Versão: 1.0

---

# 1. Objetivo

Este documento define como uma demanda percorre toda a AI Factory, desde sua criação até a entrega em produção.

Todos os participantes devem seguir este fluxo.

---

# 2. Papéis

## Product Owner

Responsável por:

- definir prioridades;
- aprovar requisitos;
- validar entregas;
- aceitar ou rejeitar funcionalidades.

---

## ChatGPT (CTO)

Responsável por:

- arquitetura;
- decomposição de tarefas;
- decisões técnicas;
- criação de documentação;
- revisão técnica;
- definição de padrões;
- escolha do executor adequado.

---

## Codex

Responsável por:

- implementação;
- refatoração;
- testes;
- criação de arquivos;
- manutenção do código.

Nunca decide arquitetura.

---

## Claude

Responsável por:

- tarefas de grande contexto;
- infraestrutura;
- n8n;
- Design System;
- documentação extensa;
- análises arquiteturais.

---

# 3. Fluxo Oficial

Toda demanda segue exatamente esta sequência.

```text
Ideia

↓

Backlog

↓

Planejamento

↓

Arquitetura

↓

Task Packet

↓

Execução

↓

Revisão

↓

Testes

↓

Aprovação

↓

Merge

↓

Documentação

↓

Memória Atualizada

↓

Concluído
```

---

# 4. Backlog

Toda funcionalidade nasce no backlog.

Cada item deve possuir:

- ID
- título
- objetivo
- prioridade
- dependências
- critérios de aceite

---

# 5. Planejamento

Nesta etapa o ChatGPT:

- quebra a demanda em tarefas;
- estima esforço;
- identifica riscos;
- verifica dependências.

---

# 6. Arquitetura

Antes da implementação verificar:

Existe impacto arquitetural?

Se sim:

Criar ADR.

Atualizar documentação.

Somente depois iniciar implementação.

---

# 7. Task Packet

Toda tarefa gera um Task Packet.

Estrutura mínima:

- ID
- título
- descrição
- executor
- entradas
- saídas
- critérios de aceite
- testes obrigatórios

---

# 8. Escolha do Executor

## Utilizar Codex quando

- escrever código;
- criar arquivos;
- refatorar;
- implementar APIs;
- escrever testes.

---

## Utilizar Claude quando

- contexto muito grande;
- documentação extensa;
- infraestrutura;
- n8n;
- Design System;
- análise de múltiplos arquivos.

---

## Utilizar ChatGPT quando

- decidir arquitetura;
- revisar código;
- criar documentação;
- planejar sprints;
- definir padrões.

---

# 9. Execução

O executor:

implementa apenas o escopo definido.

Não altera arquitetura.

Não modifica módulos não relacionados.

---

# 10. Revisão

Toda implementação deve passar por revisão.

Itens obrigatórios:

✓ arquitetura

✓ padrões

✓ nomenclatura

✓ testes

✓ performance

✓ segurança

✓ documentação

---

# 11. Testes

Sequência:

Build

↓

Testes Unitários

↓

Integração

↓

Smoke Test

↓

Aceite

Nenhuma etapa pode ser ignorada.

---

# 12. Aprovação

Somente o Product Owner aprova uma entrega.

Após aprovação:

Merge autorizado.

---

# 13. Merge

Antes do merge:

✓ Build

✓ Testes

✓ Documentação

✓ ADR

✓ Memory

✓ Sem conflitos

---

# 14. Atualização da Memória

Toda tarefa concluída deve atualizar:

.ai/memory/

completed_sprints.md

known_issues.md

patterns.md

quando aplicável.

Ao concluir uma sprint, atualizar também os documentos canônicos aplicáveis (`PROJECT_STATE.md`, `CURRENT_SPRINT.md`, histórico e Work Order). Relatórios por público devem ser regenerados ou atualizados a partir dessas fontes, sem tratar saídas derivadas como fonte de verdade.

---

# 15. Fluxo de Correções

Bug

↓

Análise

↓

Correção

↓

Testes

↓

Revisão

↓

Merge

↓

Atualização da Memória

---

# 16. Fluxo de Arquitetura

Mudança arquitetural

↓

Discussão

↓

ADR

↓

Aprovação

↓

Implementação

↓

Documentação

---

# 17. Fluxo de Sprint

Planejamento

↓

Execução

↓

Review

↓

Retrospectiva

↓

Próxima Sprint

---

# 18. Definition of Ready

Uma tarefa só pode iniciar quando possuir:

✓ objetivo definido

✓ escopo claro

✓ critérios de aceite

✓ dependências identificadas

✓ executor definido

---

# 18.1 Governança de Work Orders

0. `.ai/work-orders/` é o único local canônico de Work Orders, com três subpastas: `active/` (em execução), `backlog/` (planejado/aprovado/não iniciado/parcial/não comprovado) e `completed/` (concluído com evidências). `.ai/tasks/` e `.ai/workorders/` (sem hífen) não existem mais e não devem ser recriados. Cada Work Order existe em exatamente um arquivo, movido entre as três pastas conforme seu status muda — nunca duplicado.
1. Apenas uma Work Order pode ter status `Approved` por vez.
2. Codex só implementa a sprint explicitamente aprovada e registrada em `CURRENT_SPRINT.md`.
3. Antes da implementação, o executor lê `VISION.md`, `PROJECT_STATE.md`, `ENGINEERING_BLUEPRINT.md` e a Work Order correspondente.
4. Ao concluir, atualiza estado, histórico, documentação e o resultado da Work Order.
5. Melhorias fora do escopo são registradas como sugestão ou decisão pendente; não são implementadas.
6. Toda sprint termina com build, testes aplicáveis, commit e push.
7. Uma sprint que não cumpra todos os critérios permanece `In Progress`.

## Autonomia dos Agentes

`AI_AUTONOMY_POLICY.md` define os níveis de autonomia. O agente decide sozinho apenas melhorias internas autorizadas; mudanças de módulo, integração, arquitetura ou estrutura exigem proposta e aprovação; visão, roadmap, escopo, stack, autenticação e remoções oficiais não podem ser alterados sem aprovação.

Um agente pode enriquecer uma Work Order com diagramas, casos de uso, testes, exemplos, observações, riscos e melhorias. Nunca pode remover objetivo, valor entregue ou critérios de aceite.

## Checklist obrigatório antes do commit

1. A Work Order foi concluída?
2. Existe solução melhor?
3. Existe duplicação?
4. Existe débito técnico?
5. Alguma documentação ficou desatualizada?
6. Algum teste novo deve existir?
7. Alguma melhoria foi encontrada?
8. Alguma decisão arquitetural mudou?
9. O projeto ficou melhor?
10. Toda documentação foi sincronizada?

---

# 18.2 Estratégia Frontend First e Gates de Onda

Consolidada como estratégia oficial de desenvolvimento (ver `ROADMAP.md`): toda funcionalidade segue `Ideia → +Compras Funcional → Validação de negócio → UX → Mock navegável → Blueprint do Banco → APIs → Integrações → Implementação → Testes → Homologação`. Nenhuma funcionalidade é implementada antes da aprovação do Mock navegável; `+Compras Funcional` e `+Compras UX` (ver `DOCUMENTATION_STRATEGY.md`) precedem qualquer código.

Nenhuma Onda do roadmap inicia sem aprovação formal do Gate da Onda anterior pelo Product Owner (Gates detalhados em `ROADMAP.md`).

---

# 18.3 Processo de Homologação por Onda

Durante uma Onda, cada módulo é validado o suficiente para permitir avanço seguro — não é objetivo travar ou "superpolir" cada módulo isoladamente antes de existir o fluxo completo integrado. Isso não elimina os gates intermediários, que continuam existindo para impedir que um erro estrutural se propague para módulos seguintes.

Sequência oficial: **VALIDAR PARA AVANÇAR → INTEGRAR → VALIDAR END-TO-END → HOMOLOGAR A ONDA.**

A validação profunda de UX, regras de negócio, integrações, comportamento transversal e casos de uso ponta a ponta concentra-se principalmente na bateria/gate final da Onda, quando o fluxo completo já existe para ser testado de verdade — não antes, de forma isolada e prematura. Esta é uma estratégia deliberada para evitar otimização prematura e retrabalho, nunca uma desculpa para acumular dívida técnica: os gates intermediários seguem obrigatórios para tudo que já é conhecido e verificável naquele ponto.

# 19. Definition of Done

A Definition of Done canônica do projeto está definida em [context/definition-of-done.md](./context/definition-of-done.md).

A definição oficial de "Pronto" em nível de produto/Onda (Mock aprovado, `+Compras Funcional`/`+Compras UX` atualizados, banco/APIs/integrações/workflow/IA definidos, critérios de aceite, implementação, testes e homologação) está em `ROADMAP.md` — complementa, sem substituir, esta Definition of Done técnica. Implementação isoladamente não caracteriza conclusão.

---

# 20. Escalonamento

Em caso de dúvida:

Executor

↓

ChatGPT

↓

Product Owner

Nenhuma IA toma decisões estratégicas sozinha.

---

# 21. Regras Gerais

Nunca implementar fora do escopo.

Nunca alterar arquitetura sem ADR.

Nunca pular revisão.

Nunca ignorar testes.

Nunca concluir tarefa sem atualizar a documentação.

---

# Histórico

Versão 1.0

Workflow oficial da AI Factory.
