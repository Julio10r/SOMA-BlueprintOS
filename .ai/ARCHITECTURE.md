# ARCHITECTURE.md

> Documento mestre da arquitetura do SOMA BlueprintOS.
> Toda implementação deve seguir estas diretrizes.

---

# 1. Objetivo

Definir a arquitetura oficial do BlueprintOS.

Toda decisão técnica deve respeitar este documento.

---

# 2. Filosofia

A arquitetura foi projetada para priorizar:

- simplicidade;
- evolução contínua;
- baixo acoplamento;
- alta coesão;
- facilidade de manutenção;
- escalabilidade.

---

# 3. Estilo Arquitetural

O BlueprintOS utiliza:

- Modular Monolith
- Clean Architecture
- Domain Driven Design (DDD) (pragmático)
- CQRS
- Dependency Injection
- Domain Events

---

# 4. Estrutura Geral

/src

Apps/

BuildingBlocks/

Modules/

tests/

docs/

.ai/

Cada área possui responsabilidade única.

---

# 5. Apps

Responsáveis apenas por hospedar aplicações executáveis.

Exemplo:

Apps/
Api/
Web/
Worker.Orchestrator/
Worker.Notifications/

As Apps nunca implementam regra de negócio.

---

# 6. BuildingBlocks

Componentes compartilhados.

Exemplo:

SharedKernel

Contracts

Infrastructure

Common

Não devem conter regras específicas de um módulo.

---

# 7. Modules

Cada módulo representa um domínio independente.

Exemplo:

Identity

Planner

Procurement

Workflow

Knowledge

Memory

Agents

Notifications

Dashboard

Analytics

Cada módulo deve possuir:

Domain

Application

Infrastructure

Api

---

# 8. Camadas

## Domain

Contém:

- Entidades
- Value Objects
- Agregados
- Interfaces
- Eventos de domínio

Não referencia nenhuma outra camada.

---

## Application

Contém:

- Casos de uso
- Commands
- Queries
- Handlers
- DTOs
- Validators

Pode depender apenas de:

- Domain
- SharedKernel
- Contracts

---

## Infrastructure

Contém:

- EF Core
- Repositórios
- APIs externas
- Cache
- Mensageria
- Persistência

Nunca contém regra de negócio.

---

## Api

Responsável apenas por:

- Endpoints
- Controllers
- Minimal APIs
- Autenticação
- Autorização

Nenhuma regra de negócio deve existir aqui.

---

# 9. Comunicação entre módulos

Permitido:

Module A

↓

Contracts

↓

Module B

Não é permitido acessar diretamente:

Infrastructure

Repositories

DbContext

Entidades internas de outro módulo

---

# 10. Banco de Dados

Banco oficial:

SQL Server

ORM:

Entity Framework Core

Migrações devem ser versionadas.

Nunca alterar dados manualmente em produção.

O ERP é a fonte corporativa para códigos e cadastros oficiais de fornecedor e item, pedidos efetivados e dados fiscais/transacionais. O banco +Compras é a fonte para dados comerciais próprios, catálogos, relacionamentos fornecedor × item/família/categoria, rascunhos, contexto, evidências, status de integração e auditoria. Esta divisão inicial é definida pela ADR-0013 e será refinada por módulo.

## 10.1 Operação sem IA

O +Compras evolui primeiro como plataforma operacional. O portal web é sua interface integrada, não um produto separado. Toda operação crítica deve ter alternativa manual: cadastrar ou selecionar fornecedor/item, criar pedido, enviar ao ERP e acompanhar a integração. Agentes chamam contratos e casos de uso da Application; não acessam banco ou ERP diretamente. Escritas no ERP exigem confirmação humana e adaptadores desacoplados por BU.

---

# 11. Padrões

Obrigatórios:

Dependency Injection

Async/Await

CancellationToken

ILogger

FluentValidation

Result Pattern

Domain Events

---

# 12. Padrões proibidos

Não utilizar:

Service Locator

Classes estáticas para regra de negócio

Regiões (#region)

Métodos gigantes

Classes Deus

Acoplamento entre módulos

Lógica de negócio em Controllers

SQL dentro de Controllers

---

# 13. Escalabilidade

A arquitetura deve permitir futuramente:

Separação em microsserviços

Mensageria

Múltiplos Workers

Escalabilidade horizontal

Sem necessidade de reescrita.

---

# 14. Decisões Arquiteturais

Toda decisão relevante deve gerar uma ADR.

O log canônico de ADRs é [DECISIONS.md](./DECISIONS.md); novas decisões são adicionadas ao final desse arquivo com numeração sequencial.

---

# 15. Regra de Ouro

Antes de criar qualquer código pergunte:

Este código respeita a arquitetura?

Se a resposta for "não" ou "não sei",

não implemente.

Solicite revisão.

---

# Histórico

Versão: 1.0

Status:
Documento oficial da arquitetura.
