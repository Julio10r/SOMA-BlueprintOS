# STANDARDS.md

> Guia oficial de padrões de engenharia do SOMA BlueprintOS.

Versão: 1.0

---

# 1. Objetivo

Este documento define os padrões obrigatórios de desenvolvimento do BlueprintOS.

Seu objetivo é garantir que todo código produzido seja consistente, legível, escalável e de fácil manutenção, independentemente de quem o escreveu.

Aplica-se a:

- Desenvolvedores
- ChatGPT
- Codex
- Claude
- Futuros agentes de IA

---

# 2. Princípios

Todo código deve seguir cinco princípios fundamentais.

## Simplicidade

Prefira sempre a solução mais simples que resolva corretamente o problema.

---

## Clareza

Código deve ser escrito para pessoas lerem.

Legibilidade possui prioridade sobre complexidade.

---

## Consistência

O mesmo problema deve ser resolvido sempre da mesma maneira.

---

## Responsabilidade Única

Cada classe deve possuir apenas uma responsabilidade.

---

## Evolução

Toda implementação deve facilitar futuras alterações.

Nunca dificultá-las.

---

# 3. Idioma Oficial

Código:

Inglês

Documentação:

Português

Comentários:

Evitar.

O código deve ser autoexplicativo.

---

# 4. Convenções de Nome

## Classes

PascalCase

Exemplo

PurchaseOrderService

PlannerAgent

KnowledgeRepository

---

## Interfaces

Sempre iniciar com I

IPlanner

IKnowledgeRepository

---

## Métodos

PascalCase

CreatePurchaseOrder()

CalculateScore()

ExecuteWorkflow()

---

## Propriedades

PascalCase

UserName

CreatedAt

UpdatedAt

---

## Campos Privados

_prefixCamelCase

_repository

_logger

_context

---

## Variáveis

camelCase

purchaseOrder

supplier

workflowId

---

## Constantes

PascalCase

DefaultTimeout

MaxRetryAttempts

---

# 5. Estrutura de Pastas

Cada módulo deve seguir exatamente:

Module/

Domain/

Application/

Infrastructure/

Api/

Tests/

Nenhuma exceção.

---

# 6. Organização de Arquivos

Um arquivo.

Uma classe pública.

Nome do arquivo deve ser igual ao nome da classe.

---

# 7. Namespaces

Seguir a estrutura física.

Exemplo

Soma.BlueprintOS.Modules.Planner.Domain.Entities

Nunca utilizar namespaces genéricos.

---

# 8. Métodos

Métodos devem ser pequenos.

Objetivo:

Até 30 linhas.

Se crescerem demais, dividir.

---

# 9. Classes

Objetivo:

Até 300 linhas.

Acima disso, revisar responsabilidade.

---

# 10. Comentários

Comentários são exceção.

Preferir:

bons nomes

boas abstrações

métodos pequenos

Comentários permitidos:

TODO

HACK

WARNING

LINK

---

# 11. Tratamento de Erros

Nunca:

throw Exception()

Criar exceções específicas.

Utilizar Result Pattern sempre que possível.

---

# 12. Logging

Sempre utilizar ILogger.

Nunca utilizar:

Console.WriteLine()

Debug.Print()

---

# 13. Async

Toda operação de I/O deve ser assíncrona.

Utilizar:

async

await

CancellationToken

---

# 14. Injeção de Dependência

Nunca instanciar dependências diretamente.

Errado

new Repository()

Correto

Dependency Injection

---

# 15. Validação

Toda entrada deve ser validada.

Utilizar:

FluentValidation

Nunca confiar em dados externos.

---

# 16. Banco de Dados

ORM oficial:

Entity Framework Core

SQL manual apenas quando necessário.

Sempre parametrizado.

Nunca concatenar SQL.

---

# 17. APIs

Utilizar:

REST

JSON

Versionamento

DTOs

Nunca expor entidades diretamente.

---

# 18. Segurança

Nunca armazenar:

Senhas

Secrets

Tokens

Connection Strings

Utilizar variáveis de ambiente ou Secret Manager.

---

# 19. Testes

Todo caso de uso deve possuir testes.

Prioridade:

Application

Domain

Integration

End-to-End

---

# 20. Git

Branches:

feature/

bugfix/

hotfix/

release/

main

Nunca desenvolver diretamente na main.

---

# 21. Commits

Formato:

tipo: descrição

Exemplos

feat: add planner module

fix: correct workflow validation

docs: update architecture

refactor: simplify procurement service

test: add planner tests

---

# 22. Pull Request

Todo PR deve conter:

Objetivo

Mudanças

Impactos

Testes realizados

Checklist

---

# 23. Checklist Antes do Commit

✓ Build sem erros

✓ Testes executados

✓ Sem warnings críticos

✓ Sem código morto

✓ Sem TODO esquecidos

✓ Sem secrets

✓ Documentação atualizada

---

# 24. Código Proibido

Não utilizar:

#region

Console.WriteLine()

catch vazio

métodos gigantes

classes gigantes

duplicação

Service Locator

God Objects

SQL concatenado

dependências cíclicas

---

# 25. IA

Toda IA deve:

ler PROJECT.md

ler ARCHITECTURE.md

ler STANDARDS.md

ler CURRENT_SPRINT.md

antes de qualquer implementação.

---

# 26. Definition of Done

A Definition of Done canônica do projeto está definida em [context/definition-of-done.md](./context/definition-of-done.md).

---

# Histórico

Versão 1.0

Documento oficial de padrões de engenharia.