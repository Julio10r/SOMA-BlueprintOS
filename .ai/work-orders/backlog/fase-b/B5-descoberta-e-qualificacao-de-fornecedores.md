# Work Order — B5 — Portal Operacional Integrado

## Metadados

- Fase: Sourcing Intelligence
- Sprint: B5
- Status: Draft
- Prioridade: Priorização pendente do Product Owner
- Dependências: B1, B2 e B3.
- Data de aprovação: Não aprovada

## Objetivo

Evoluir o portal web como interface do próprio +Compras para fornecedor, item e pedido, com seleção e cadastro manuais.

## Problema de negócio

Reduzir fricção, risco ou perda de conhecimento no ciclo de Procurement, no limite definido pelo objetivo desta sprint.

## Valor entregue

Capacidade incremental e verificável para o +COMPRAS; métricas e metas financeiras são decisão pendente.

## Contexto técnico

Seguir .NET 9, Clean Architecture, DDD pragmático e contratos públicos. Não presumir persistência, fornecedor externo ou endpoint final sem decisão aprovada.

## Escopo incluído

Interface integrada aos casos de uso operacionais; não criar produto ou módulo de portal separado nem prometer recomendação inteligente real.

## Fora do escopo

Capacidades de outras Work Orders, mudanças de stack, integrações não aprovadas e automação sem controle humano.

## Requisitos funcionais

- Implementar o objetivo declarado com comportamento explicável.
- Registrar decisões e resultado conforme o nível de autonomia aplicável.

## Requisitos não funcionais

Segurança por design, tratamento de erros, logs proporcionais, testes, compatibilidade e documentação atualizada.

## Arquitetura esperada

Módulo coeso com Domain, Application, Infrastructure e Api na arquitetura alvo; até migração aprovada, respeitar a estrutura física atual sem acoplamento entre módulos.

## Componentes afetados

Definidos durante o design aprovado; não criar componente sem necessidade comprovada.

## Modelo de domínio

Entidades, value objects e agregados são decisão de modelagem. Usar nomes lógicos e evitar tabelas definitivas antes da persistência aprovada.

## Casos de uso

Criar, consultar e atualizar somente os fluxos necessários ao objetivo, com autorização e validação apropriadas.

## APIs e contratos

Contrato proposto REST/JSON versionável; endpoints, payloads e erros finais são decisão pendente até a fase de design.

## Persistência

SQL Server e EF Core são padrões alvo. Schema, migrations e nomes físicos serão definidos apenas quando a Work Order for aprovada para execução.

## Integrações

Somente interfaces/adapters aprovados. ERP, jurídico, n8n e provedores externos específicos são decisões pendentes.

## Segurança e autorização

Aplicar menor privilégio, segregação e auditoria quando houver identidade/autorização disponíveis; dependências de Entra ID são explícitas nesta Work Order.

## Observabilidade

Logs estruturados, correlation e métricas proporcionais ao risco; tracing, alertas e SLOs dependem da capacidade de observabilidade aplicável.

## Tratamento de erros

Validar entradas, retornar falhas específicas/Result Pattern quando aplicável, não expor dados sensíveis e manter operações idempotentes quando integrarem sistemas.

## Testes obrigatórios

Unitários de regras e casos de uso; integração para persistência/API/integração quando existirem; regressão para qualquer defeito corrigido.

## Documentação obrigatória

PROJECT_STATE, CURRENT_SPRINT, histórico, BACKLOG, Engineering Blueprint, contratos e decisões relevantes.

## Critérios de aceite

- [ ] Objetivo implementado sem expansão de escopo.
- [ ] Regras e falhas relevantes cobertas por testes.
- [ ] Interfaces não expõem dependências internas.
- [ ] Documentação e decisões sincronizadas.

## Definition of Done

Build sem warnings, testes aplicáveis aprovados, documentação atualizada, evidência registrada, commit e push realizados.

## Riscos

Dados insuficientes, integração indisponível, requisitos regulatórios, dependências de identidade/persistência e decisões de produto pendentes.

## Decisões pendentes

Prioridade, modelo detalhado, contratos finais, fontes de dados, integrações externas e critérios mensuráveis de sucesso.

## Dependências

B1, B2 e B3.

## Plano de implementação

1. Validar escopo e critérios com Product Owner.
2. Criar design/ADR se houver impacto arquitetural.
3. Implementar contratos e casos de uso mínimos.
4. Testar, documentar, revisar e versionar.

## Resultado da execução

Não executada. Preencher somente após conclusão comprovada.
