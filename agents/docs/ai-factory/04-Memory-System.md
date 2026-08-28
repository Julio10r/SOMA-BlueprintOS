# MEMORY_SYSTEM.md

# Sistema de Memória
## SOMA BlueprintOS

---

# Objetivo

O Sistema de Memória é responsável por armazenar, organizar, recuperar e evoluir o conhecimento da AI Factory.

Seu objetivo é garantir que o sistema aprenda continuamente sem perder consistência, evitando repetição de contexto, desperdício de tokens e decisões contraditórias.

A memória é tratada como um ativo estratégico do BlueprintOS.

---

# Princípios

O sistema de memória deve ser:

- Persistente
- Versionado
- Auditável
- Contextual
- Escalável
- Pesquisável
- Seguro
- Independente do modelo de IA utilizado

Nenhum modelo deve depender exclusivamente da janela de contexto (context window).

---

# Arquitetura

```
                Usuário
                    │
                    ▼
               Maestro
                    │
        ┌───────────┼───────────┐
        ▼           ▼           ▼
 Curto Prazo   Médio Prazo   Longo Prazo
        │           │           │
        └───────────┼───────────┘
                    ▼
            Knowledge Index
                    ▼
              RAG Retriever
                    ▼
             Context Builder
                    ▼
               Agente IA
```

---

# Camadas de Memória

## 1. Memória de Curto Prazo (Short-Term Memory)

Armazena o contexto da sessão atual.

Características:

- Conversa ativa
- Plano em execução
- Tasks abertas
- Decisões temporárias
- Estado dos agentes

Vida útil:

Até o encerramento da sessão ou conclusão do fluxo.

Não é persistida permanentemente.

---

## 2. Memória de Médio Prazo (Working Memory)

Armazena conhecimento reutilizável durante um projeto.

Exemplos:

- decisões arquiteturais
- convenções adotadas
- padrões da empresa
- prompts em evolução
- workflows
- componentes reutilizáveis

É persistida.

Pode ser atualizada.

Possui versionamento.

---

## 3. Memória de Longo Prazo (Knowledge Base)

Armazena conhecimento consolidado.

Exemplos:

- documentação oficial
- arquitetura
- APIs
- contratos
- padrões técnicos
- regras de negócio
- playbooks
- lições aprendidas
- boas práticas

Nunca é alterada diretamente.

Toda alteração gera uma nova versão.

---

# Knowledge Index

Toda memória é indexada.

Cada item recebe metadados.

Exemplo:

ID

Título

Tipo

Projeto

Módulo

Tags

Autor

Versão

Data

Fonte

Confiança

Relacionamentos

---

# Classificação do Conhecimento

Tipos:

- Documento
- Arquitetura
- Regra de negócio
- API
- Workflow
- SQL
- Prompt
- Template
- Decisão
- ADR
- Código
- Manual
- FAQ

---

# Ciclo de Evolução

Novo conhecimento

↓

Validação

↓

Indexação

↓

Embeddings

↓

Disponível para RAG

↓

Uso pelos agentes

↓

Feedback

↓

Nova versão

---

# Promoção de Conhecimento

Nem todo conteúdo deve virar memória permanente.

Fluxo:

Sessão

↓

Curto Prazo

↓

Validação

↓

Médio Prazo

↓

Revisão

↓

Longo Prazo

---

Critérios para promoção:

- reutilização
- estabilidade
- qualidade
- aprovação
- valor estratégico

---

# Versionamento

Toda memória possui:

Versão

Autor

Data

Motivo da alteração

Histórico completo

Nunca existe edição destrutiva.

---

# Embeddings

Todo documento elegível gera embeddings.

Objetivos:

- busca semântica
- recuperação contextual
- similaridade
- recomendação

Os embeddings podem ser regenerados sempre que necessário.

---

# RAG

Fluxo:

Pergunta

↓

Retriever

↓

Busca semântica

↓

Ranking

↓

Context Builder

↓

Resposta

---

O RAG nunca envia toda a base.

Apenas os trechos mais relevantes.

---

# Context Builder

Responsável por montar o contexto enviado ao modelo.

Prioridade:

1. Contexto atual

2. Tasks abertas

3. Memória de curto prazo

4. Memória de médio prazo

5. Base permanente

6. Conhecimento externo

O objetivo é reduzir tokens mantendo alta precisão.

---

# Memória dos Agentes

Cada agente possui memória própria.

Exemplo:

Backend

- padrões backend
- APIs
- serviços

SQL

- índices
- procedures
- otimizações

Frontend

- Design System
- componentes

DevOps

- pipelines
- infraestrutura

Segurança

- políticas
- LGPD

---

# Memória Compartilhada

Existe uma memória compartilhada contendo:

- arquitetura
- decisões globais
- padrões
- documentação oficial

Todos os agentes possuem acesso de leitura.

Apenas o Maestro pode solicitar alterações.

---

# Auditoria

Toda alteração registra:

Quem alterou

Quando

Por quê

Versão anterior

Versão atual

Aprovação

---

# Segurança

As memórias podem possuir níveis de acesso.

Exemplos:

Pública

Projeto

Equipe

Administrador

Confidencial

---

# Expiração

Alguns conhecimentos podem expirar.

Exemplo:

Tokens

Credenciais

URLs temporárias

Versões antigas

Itens expirados nunca são removidos automaticamente.

São apenas marcados como obsoletos.

---

# Qualidade

Cada item possui um nível de confiança.

Exemplo:

100%

Validado oficialmente.

90%

Documentação técnica.

80%

Conhecimento consolidado.

60%

Experimental.

30%

Hipótese.

O RAG utiliza esse índice para priorização.

---

# Métricas

Itens indexados

Uso por agente

Documentos mais consultados

Taxa de reutilização

Tempo médio de busca

Precisão do RAG

Tempo de resposta

Conhecimento promovido

---

# Objetivo Final

Criar um sistema de memória vivo, versionado e auditável, capaz de preservar o conhecimento da organização, acelerar o desenvolvimento, reduzir consumo de tokens e permitir que a AI Factory evolua continuamente sem perder consistência.