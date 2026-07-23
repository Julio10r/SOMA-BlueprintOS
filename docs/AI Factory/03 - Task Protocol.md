# TASK_PROTOCOL.md

## Objetivo

Definir o protocolo oficial de criação, execução, acompanhamento e encerramento de tarefas (Tasks) dentro da AI Factory do SOMA BlueprintOS.

Toda comunicação entre agentes ocorre exclusivamente através de Tasks estruturadas.

---

# Filosofia

Uma Task representa uma unidade mínima de trabalho.

Ela deve possuir:

- objetivo único
- escopo definido
- entradas conhecidas
- saídas esperadas
- critérios de qualidade

Tasks nunca devem possuir múltiplos objetivos.

---

# Ciclo de Vida

NEW
↓
PLANNED
↓
ASSIGNED
↓
IN_PROGRESS
↓
WAITING_REVIEW
↓
APPROVED
↓
COMPLETED

Fluxos alternativos:

IN_PROGRESS
↓
BLOCKED

WAITING_REVIEW
↓
CHANGES_REQUESTED
↓
IN_PROGRESS

Em qualquer momento:

CANCELLED

---

# Estrutura da Task

Cada Task deve possuir obrigatoriamente:

## Identificação

Task ID

Título

Descrição

Projeto

Sprint

Módulo

Prioridade

Tipo

Complexidade

---

## Responsável

Agente responsável

Criador

Data de criação

Última atualização

---

## Contexto

Objetivo

Problema

Documentos relacionados

Dependências

Premissas

Restrições

---

## Entradas

Arquivos

Prompt

APIs

Banco

Memória

Contexto do Maestro

---

## Saídas Esperadas

Código

Documento

SQL

Workflow

API

Tela

Teste

Relatório

Checklist

---

## Critérios de Aceite

Toda Task deve definir claramente:

O que caracteriza sucesso.

Exemplo:

- compila
- testes passam
- documentação atualizada
- cobertura mínima
- sem erros
- segue padrões

---

## Critérios de Qualidade

A solução deve:

- ser legível
- reutilizável
- desacoplada
- documentada
- segura
- performática
- testável

---

# Tipos de Task

## Analysis

Análise

Sem implementação.

---

## Architecture

Arquitetura.

---

## Backend

Implementação backend.

---

## Frontend

Implementação frontend.

---

## Database

Banco de dados.

---

## AI

Prompts

Agentes

RAG

Embeddings

Memória

---

## Workflow

n8n

Automações

Integrações

---

## DevOps

Infraestrutura

Deploy

Docker

CI/CD

---

## Security

Segurança

LGPD

Autenticação

---

## Testing

Testes

QA

Validação

---

## Documentation

Documentação

---

## Bug

Correção

---

## Refactor

Melhoria técnica

Sem alteração funcional.

---

# Prioridades

P0

Crítico

Sistema parado.

---

P1

Alta

Impacta funcionalidades importantes.

---

P2

Normal

Desenvolvimento regular.

---

P3

Baixa

Melhorias.

---

# Complexidade

XS

Até 30 minutos

---

S

Até 2 horas

---

M

Até 1 dia

---

L

Até 3 dias

---

XL

Mais de 3 dias

---

# Dependências

Toda Task pode depender de outras.

Exemplo:

Task-204

Depende:

Task-187

Task-192

Enquanto dependências não estiverem concluídas:

Status:

BLOCKED

---

# Protocolo de Execução

1.

Maestro cria Task.

↓

2.

Task recebe contexto.

↓

3.

Especialista valida entendimento.

↓

4.

Execução.

↓

5.

Autovalidação.

↓

6.

Entrega.

↓

7.

Revisão.

↓

8.

Aprovação.

↓

9.

Conclusão.

---

# Revisão

Toda entrega deve responder:

Objetivo foi cumprido?

Todos critérios atendidos?

Existem riscos?

Há dívida técnica?

Existe impacto em outros módulos?

Documentação foi atualizada?

---

# Cancelamento

Uma Task pode ser cancelada quando:

- deixou de fazer sentido
- requisito alterado
- duplicidade
- inviabilidade técnica

Cancelamento nunca apaga histórico.

---

# Rastreabilidade

Cada Task registra:

quem criou

quem executou

quem aprovou

quando iniciou

quando terminou

documentos envolvidos

commits relacionados

PR relacionada

workflow relacionado

---

# Métricas

Tempo estimado

Tempo real

Retrabalho

Número de revisões

Bloqueios

Tempo em espera

Lead Time

Cycle Time

---

# Relação com Git

Uma Task pode gerar:

1 ou mais commits

1 Pull Request

1 Release

Toda referência deve utilizar o Task ID.

Exemplo:

TASK-238

---

# Integração com Memória

Ao finalizar uma Task, o Maestro decide se o conhecimento gerado deve ser promovido para:

Memória de Curto Prazo

↓

Memória de Médio Prazo

↓

Base de Conhecimento Permanente

Nem toda Task gera conhecimento persistente.

---

# Objetivo Final

Garantir que todo trabalho executado pela AI Factory seja rastreável, padronizado, auditável e reutilizável, permitindo que dezenas de agentes atuem em paralelo com segurança, previsibilidade e qualidade.