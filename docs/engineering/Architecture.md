# Arquitetura

> Documento gerado automaticamente pelo Portal de Documentação Viva do BlueprintOS. Não editar manualmente.

- **Versão:** 1.0.0
- **Gerado em:** 2026-07-23 03:43:24 UTC
- **Última atualização:** 2026-07-23

---

## Arquitetura de engenharia

O backend do BlueprintOS segue Modular Monolith + Clean Architecture (ver ADR-0001).
Hoje os módulos de domínio são organizados como `Core/{Módulo}/{Contracts,Models}` +
`Infrastructure/{Módulo}/...`, e não ainda a estrutura alvo `Modules/` (ver ADR-0006).

# Documentação Técnica — Módulo Documentation

Gerencia a documentação viva do próprio BlueprintOS: entradas de documento, versionamento, changelog, ADRs e geração de documentação técnica/funcional/IA/desenvolvedor.

## Contratos

- `IDocumentationRepository`
- `IDocumentVersioningService`
- `IChangeLogService`
- `IAdrService`
- `ITechnicalDocumentationGenerator`
- `IMermaidDiagramGenerator`
- `IDocumentationSyncService`
- `IStaleDocumentationDetector`
- `IGitLogReader`

## Classes

- `MarkdownAdrService`
- `TechnicalDocumentationGenerator`
- `MermaidDiagramGenerator`
- `DocumentationSyncService`

---

# Documentação Técnica — Módulo Knowledge

Ingestão e recuperação de conhecimento organizacional a partir de conteúdo Markdown.

## Contratos

- `IKnowledgeProvider`
- `IKnowledgeService`

## Classes

- `MarkdownKnowledgeProvider`
- `KnowledgeService`

---

# Documentação Técnica — Módulo Agents

Runtime de agentes de IA especializados, construídos sobre um runtime de IA comum.

## Contratos

- `IAgent`
- `IAIRuntime`

## Classes

- `BaseAgent`
- `EchoAgent`
- `KnowledgeAgent`
- `AgentFactory`

---

# Documentação Técnica — Módulo AI.Negotiation

Memória de negociação e motor de estratégia de negociação baseado em regras para o agente Buyer sênior.

## Contratos

- `INegotiationMemory`
- `INegotiationMemoryStore`
- `INegotiationStrategy`
- `INegotiationStrategyRule`

## Classes

- `NegotiationMemory`
- `InMemoryNegotiationMemoryStore`
- `NegotiationStrategy`

---
