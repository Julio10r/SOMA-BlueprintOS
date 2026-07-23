# Changelog

> Documento gerado automaticamente pelo Portal de Documentação Viva do BlueprintOS. Não editar manualmente.

- **Versão:** 1.0.0
- **Gerado em:** 2026-07-23 15:00:02 UTC
- **Última atualização:** 2026-07-23

---

## Changelog

### Sprint A9 — Publication Engine

Implementação do Publication Engine: geração automática de três documentos profissionais para apresentação (Relatório Executivo, Guia do Cliente, Guia de Engenharia), cada um em Markdown, HTML e PDF, publicados em `dist/{executive,client,engineering}/`. O conteúdo reaproveita integralmente os 19 geradores de documentação da Sprint A8 (nenhum dado fabricado); o Relatório Executivo acrescenta indicadores reais de build/testes coletados em tempo real (`dotnet build` + contagem de `[Fact]`/`[Theory]`) e dívidas técnicas/próximos passos extraídos diretamente de `.ai/memory/known_issues.md` e `.ai/ROADMAP.md`.

### Sprint A8 — Portal de Documentação Viva

Implementação do Portal de Documentação Viva: publicação automática de documentação executiva, de cliente e de engenharia (19 geradores) a partir de fontes reais do repositório (`.ai/ROADMAP.md`, `.ai/memory/completed_sprints.md`, `.ai/memory/known_issues.md`, `.ai/DECISIONS.md`, metadados de módulo e o grafo real de dependências entre projetos), com publicação em disco sob `docs/` (`IDocumentPublisher`/`MarkdownPublisher`/`DocumentationPublisher`) e sincronização automática dos artefatos de memória da AI Factory.

### Sprint A7 — Sistema de Documentação do BlueprintOS

Implementação do módulo backend `Documentation`, responsável por gerenciar a documentação do próprio BlueprintOS: estrutura de documentos (`DocumentationEntry` + `IDocumentationRepository`), versionamento de documentos (`IDocumentVersioningService`), registro de alterações/changelog (`IChangeLogService`), Architecture Decision Records (`AdrRecord` + `IAdrService`, persistidas como Markdown), geradores de documentação técnica, funcional, para IA (`.ai/context`) e para desenvolvedores, gerador de diagramas Mermaid (`IMermaidDiagramGenerator`), sincronização automática e detecção de documentação desatualizada (`IDocumentationSyncService` / `IStaleDocumentationDetector`), integração de leitura com Git (`IGitLogReader` / `GitCliDocumentationService`) e ponto de extensão para integração futura com um módulo de Memória genérico (`IDocumentationMemoryNotifier`).
