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

# 19. Definition of Done

Uma tarefa só termina quando:

✓ código implementado

✓ testes aprovados

✓ documentação atualizada

✓ revisão concluída

✓ memória atualizada

✓ aprovada pelo Product Owner

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