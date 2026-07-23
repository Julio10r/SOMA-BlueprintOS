# completed_sprints.md

> Log de sprints concluídas do BlueprintOS, atualizado ao final de cada sprint (ver WORKFLOW.md §14).

---

## Sprint A7 — Sistema de Documentação do BlueprintOS

**Status:** Concluída

**Escopo:** Implementação do módulo backend `Documentation`, responsável por gerenciar a documentação do próprio BlueprintOS: estrutura de documentos (`DocumentationEntry` + `IDocumentationRepository`), versionamento de documentos (`IDocumentVersioningService`), registro de alterações/changelog (`IChangeLogService`), Architecture Decision Records (`AdrRecord` + `IAdrService`, persistidas como Markdown), geradores de documentação técnica, funcional, para IA (`.ai/context`) e para desenvolvedores, gerador de diagramas Mermaid (`IMermaidDiagramGenerator`), sincronização automática e detecção de documentação desatualizada (`IDocumentationSyncService` / `IStaleDocumentationDetector`), integração de leitura com Git (`IGitLogReader` / `GitCliDocumentationService`) e ponto de extensão para integração futura com um módulo de Memória genérico (`IDocumentationMemoryNotifier`).

**Decisão explícita do Product Owner:** frontend React, tradução do GDT para React e migração para a arquitetura alvo (`Modules/`) ficaram fora de escopo desta sprint.

**Entregas:**
- Módulo `Documentation` completo em `backend/src/BlueprintOS.Core/Documentation/` (Contracts + Models) e `backend/src/BlueprintOS.Infrastructure/Documentation/` (implementações), seguindo o mesmo padrão do módulo `Knowledge`.
- Registro de todos os serviços via `AddInfrastructure` em `ServiceCollectionExtensions.cs`, incluindo `IOptions<DocumentationOptions>`.
- Suíte de testes unitários (xUnit, fakes manuais, sem framework de mocking) espelhando a estrutura de produção em `backend/tests/BlueprintOS.UnitTests/Infrastructure/Documentation/`.
- ADR-0006 registrada em `.ai/DECISIONS.md`, documentando a decisão de manter a estrutura Core/Infrastructure e os pontos de extensão para a arquitetura alvo e para integração futura com Memória.
- Dívidas técnicas atualizadas em `.ai/memory/known_issues.md`.

**Resultado da validação:** `dotnet build` sem erros/warnings; `dotnet test` com 100% dos testes passando (99 testes unitários + 1 teste de integração).

## Sprint A8 — Portal de Documentação Viva

**Status:** Concluída

**Escopo:** Implementação do Portal de Documentação Viva: publicação automática de documentação executiva, de cliente e de engenharia (19 geradores) a partir de fontes reais do repositório (`.ai/ROADMAP.md`, `.ai/memory/completed_sprints.md`, `.ai/memory/known_issues.md`, `.ai/DECISIONS.md`, metadados de módulo e o grafo real de dependências entre projetos), com publicação em disco sob `docs/` (`IDocumentPublisher`/`MarkdownPublisher`/`DocumentationPublisher`) e sincronização automática dos artefatos de memória da AI Factory.

**Entregas:**
- Camada de publicação (`IDocumentPublisher`, `MarkdownPublisher`, `DocumentationPublisher`) em `backend/src/BlueprintOS.Infrastructure/Documentation/Publishing/`.
- 19 geradores de documentação (executivo, cliente, engenharia) em `backend/src/BlueprintOS.Infrastructure/Documentation/Generators/`.
- `DocumentationPublishService` (`IDocumentationPublishService.PublishAllAsync`), pronto para ser acionado por um futuro motor de Workflow.
- Documentos Markdown publicados em `docs/executive/`, `docs/client/`, `docs/engineering/` e `docs/engineering/Mermaid/`.
- Suíte de testes unitários (xUnit, fakes manuais) cobrindo publicadores, geradores e o serviço de publicação.

**Resultado da validação:** `dotnet build` sem erros/warnings; `dotnet test` com 100% dos testes passando.
