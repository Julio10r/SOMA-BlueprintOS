# PROJECT.md

> Fonte de verdade do projeto SOMA BlueprintOS.
> Este documento deve ser lido por qualquer agente de IA antes de iniciar uma tarefa.

---

# 1. Visão

O SOMA BlueprintOS é uma plataforma corporativa de Inteligência Artificial desenvolvida para automatizar processos de negócio através de agentes especializados, workflows, memória, planejamento e integração com sistemas corporativos.

O BlueprintOS deve ser modular, escalável, seguro e preparado para atender múltiplas empresas (multi-tenant), mantendo customizações por organização.

---

# 2. Objetivo

Construir uma plataforma de IA empresarial capaz de:

- Planejar
- Executar
- Aprender
- Armazenar conhecimento
- Integrar sistemas
- Automatizar processos

Tudo utilizando arquitetura moderna baseada em .NET e SQL Server.

---

# 3. Princípios

Todo desenvolvimento deve seguir os princípios abaixo.

## Simplicidade

A solução mais simples que atende ao requisito é a preferida.

## Clareza

Código legível é mais importante que código inteligente.

## Modularidade

Todo componente deve possuir responsabilidade única.

## Escalabilidade

A arquitetura deve suportar crescimento sem reescritas.

## Baixo acoplamento

Os módulos devem depender apenas de contratos públicos.

---

# 4. Tecnologias Oficiais

Backend

- .NET 9
- ASP.NET Core
- C#

Banco

- SQL Server

ORM

- Entity Framework Core

Frontend

- React
- TypeScript

Infraestrutura

- Docker

Cloud

- Google Cloud Platform

Autenticação

- Microsoft Entra ID

Controle de versão

- Git
- GitHub

---

# 5. Arquitetura

O projeto utiliza:

- Modular Monolith
- Clean Architecture
- CQRS
- MediatR
- Domain Events
- Dependency Injection

Detalhes completos encontram-se em ARCHITECTURE.md.

---

# 6. Organização

O projeto é dividido em módulos independentes.

Exemplos:

- Identity
- Procurement
- Planner
- Workflow
- Knowledge
- Memory
- Agents
- Dashboard
- Notifications

Cada módulo deve possuir:

- Domain
- Application
- Infrastructure
- Api

---

# 7. Regras para Agentes de IA

Antes de modificar qualquer código, todo agente deve:

1. Ler PROJECT.md
2. Ler ARCHITECTURE.md
3. Ler STANDARDS.md
4. Ler CURRENT_SPRINT.md

Nenhuma implementação deve ignorar esses documentos.

---

# 8. Fonte de Verdade

Quando houver conflito entre documentos:

PROJECT.md possui prioridade máxima.

---

# 9. Escopo da AI Factory

A AI Factory é responsável por:

- Governança técnica
- Arquitetura
- Planejamento
- Execução
- Revisão
- Documentação
- Automação

Ela não substitui a decisão humana.

Toda decisão estratégica deve ser aprovada pelo Product Owner.

---

# 10. Regra de Ouro

Nenhum agente pode:

- alterar arquitetura sem aprovação;
- criar dependências desnecessárias;
- duplicar código;
- quebrar padrões estabelecidos;
- modificar módulos fora do escopo da tarefa.

Em caso de dúvida, interrompa a implementação e solicite revisão.

---

# Histórico

Versão: 1.0

Status:
Em evolução.