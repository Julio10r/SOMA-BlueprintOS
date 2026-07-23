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

## Sprint A9 — Publication Engine

**Status:** Concluída

**Escopo:** Implementação do Publication Engine: geração automática de três documentos profissionais para apresentação (Relatório Executivo, Guia do Cliente, Guia de Engenharia), cada um em Markdown, HTML e PDF, publicados em `dist/{executive,client,engineering}/`. O conteúdo reaproveita integralmente os 19 geradores de documentação da Sprint A8 (nenhum dado fabricado); o Relatório Executivo acrescenta indicadores reais de build/testes coletados em tempo real (`dotnet build` + contagem de `[Fact]`/`[Theory]`) e dívidas técnicas/próximos passos extraídos diretamente de `.ai/memory/known_issues.md` e `.ai/ROADMAP.md`.

**Entregas:**
- Módulo `Publication` em `backend/src/BlueprintOS.Core/Publication/` (Contracts + Models, incluindo o modelo comum `ContentBlock`/`InlineSpan`) e `backend/src/BlueprintOS.Infrastructure/Publication/` (Content + Rendering + Publishers + orquestrador), seguindo o mesmo padrão dos módulos `Documentation`/`Knowledge`.
- Modelo comum (ViewModel) único por documento: Markdown bruto dos geradores é convertido uma única vez em `ContentBlock`s (`MarkdownContentParser`); os três renderizadores (`MarkdownRenderer`, `HtmlRenderer`, `PdfRenderer` via `QuestPDF`) consomem exatamente os mesmos blocos, sem duplicar lógica de interpretação — nenhum deriva HTML→PDF.
- Três publicadores de relatório (`ExecutivePublisher`, `ClientPublisher`, `EngineeringPublisher`), orquestrados por `PublicationService`; novos formatos (Word, PowerPoint, site estático) podem ser adicionados implementando apenas `IContentRenderer`, sem alterar os publicadores.
- `QualityMetricsProvider`, que coleta build status, warnings, erros e quantidade de testes em tempo real (sem valores fabricados).
- Ponto único de entrada `dotnet run -- publish` em `backend/src/BlueprintOS.Api/Program.cs`, que resolve a raiz do repositório via `.git` para funcionar independente do diretório de execução.
- ADR-0007 registrada em `.ai/DECISIONS.md`. `dist/` adicionado ao `.gitignore` (artefato gerado, não versionado).
- Suíte de testes unitários (xUnit, fakes manuais) cobrindo o parser de conteúdo, o parser de ênfase inline, os três renderizadores, o orquestrador e a coleta de indicadores.

**Resultado da validação:** `dotnet build` sem erros/warnings; `dotnet test` com 100% dos testes passando (158 testes unitários + 1 teste de integração); `dotnet run -- publish` executado com sucesso, gerando os 9 arquivos esperados em `dist/`.
