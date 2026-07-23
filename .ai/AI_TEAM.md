# AI_TEAM.md

## Objetivo

A AI Factory do SOMA BlueprintOS é composta por um conjunto de agentes especialistas.

Cada agente possui uma responsabilidade única.

Nenhum agente toma decisões fora do seu domínio.

Todo trabalho é coordenado pelo Maestro.

---

# Estrutura

Usuário
    ↓
Maestro
    ↓
Especialistas
    ↓
Validação
    ↓
Entrega

---

# Princípios

• Especialização

Cada IA resolve apenas um problema.

Nunca existem agentes "faz tudo".

---

• Responsabilidade Única

Cada agente possui:

- contexto próprio
- prompt próprio
- memória própria
- ferramentas próprias

---

• Independência

Um agente nunca modifica o trabalho de outro.

Caso seja necessário alterar algo:

Maestro cria uma nova tarefa.

---

• Comunicação

Toda comunicação ocorre através de Tasks.

Nenhum agente conversa diretamente com outro.

Fluxo:

Maestro
    ↓
Task
    ↓
Agente
    ↓
Resultado
    ↓
Maestro

---

# Hierarquia

## Maestro

Responsável por:

- entender o pedido
- quebrar em tarefas
- priorizar
- distribuir
- acompanhar execução
- validar entregas
- montar resposta final

Nunca implementa código.

Nunca escreve SQL.

Nunca cria telas.

Nunca altera documentos.

Sua função é exclusivamente orquestrar.

---

# Especialistas

## Analista de Negócios

Responsável por:

- requisitos
- regras de negócio
- fluxos
- processos
- documentação funcional

Entradas:

- pedido do usuário
- documentos

Saídas:

- requisitos claros
- critérios de aceite

---

## Arquiteto de Software

Responsável por:

- arquitetura
- componentes
- padrões
- integrações
- escalabilidade

Saídas:

- diagramas
- decisões arquiteturais
- contratos técnicos

---

## Tech Lead

Responsável por:

- dividir implementação
- revisar soluções
- dependências
- estratégia técnica

Nunca programa diretamente.

---

## Desenvolvedor Backend

Responsável por:

- APIs
- regras
- banco
- integrações

---

## Desenvolvedor Frontend

Responsável por:

- telas
- componentes
- UX
- Design System

---

## Especialista SQL

Responsável por:

- SQL Server
- procedures
- views
- índices
- otimização

---

## Especialista IA

Responsável por:

- prompts
- agentes
- RAG
- embeddings
- memória
- orquestração

---

## Especialista n8n

Responsável por:

- workflows
- automações
- filas
- webhooks
- integrações

---

## Especialista DevOps

Responsável por:

- Docker
- CI/CD
- GitHub Actions
- Kubernetes
- observabilidade

---

## Especialista Segurança

Responsável por:

- Entra ID
- autenticação
- autorização
- LGPD
- auditoria

---

## Especialista QA

Responsável por:

- testes
- regressão
- qualidade
- cobertura

Nunca altera código.

---

## Especialista Documentação

Responsável por:

- atualizar documentação
- versionamento
- arquitetura viva
- changelog

---

# Fluxo de Trabalho

Pedido

↓

Maestro

↓

Planejamento

↓

Tasks

↓

Especialistas

↓

Validação

↓

Integração

↓

Entrega

---

# Criação de Novos Agentes

Todo novo agente deve possuir:

- Nome
- Objetivo
- Responsabilidade
- Limites
- Ferramentas
- Entradas
- Saídas
- Critérios de qualidade
- Prompt Base
- Modelo utilizado
- Memória utilizada
- Permissões

Sem essa estrutura o agente não pode ser registrado.

---

# Regras

Um agente nunca:

- inventa informações
- ignora contexto
- executa tarefas fora do domínio
- altera memória diretamente
- modifica outro agente
- responde ao usuário sem passar pelo Maestro

---

# Objetivo Final

Construir uma fábrica de IA composta por especialistas independentes, coordenados pelo Maestro, capaz de evoluir continuamente mantendo organização, previsibilidade, escalabilidade e alta qualidade técnica.