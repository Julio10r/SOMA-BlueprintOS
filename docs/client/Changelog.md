# Changelog

> Documento gerado automaticamente pelo Portal de Documentação Viva do BlueprintOS. Não editar manualmente.

- **Versão:** 1.0.0
- **Gerado em:** 2026-08-05 15:25:57 UTC
- **Última atualização:** 2026-08-05

---

## Changelog

### Sprint de Infraestrutura — Remoção do Docker e Consolidação do Ambiente Local


### Sprint B2.1.3 — Endurecimento da Integração ERP de Fornecedores

transformar a sincronização ERP SOMA → +Compras em rotina operacional rastreável, paginada e resiliente a erros parciais: leitura paginada (`IFornecedorErpReader`/`SomaFornecedorReader` com `OFFSET/FETCH`), orquestração em lotes (`SincronizarFornecedoresErpUseCase`), histórico de execução (`SincronizacaoFornecedor`) e erros parciais persistidos (`ErroSincronizacaoFornecedor`), migration `202608020001_B213FornecedorErpSyncHardening`, logs estruturados e retorno detalhado do endpoint `GET /api/fornecedores/sincronizar-erp`.

### Sprint B2.1 — Validação Operacional e Sincronização de Fornecedores com ERP


### Sprint B2 — Descoberta Inicial de Fornecedores

consulta somente leitura ao ERP SOMA_DESENV por item, descrição ou categoria, score explicável 100/80/60/40, persistência de descobertas no +Compras e endpoints de descoberta/consulta.

### Sprint B1 — Persistência de Fornecedores

agregado `Fornecedor`, value object `Cnpj`, DbContext EF Core/SQL Server, migration versionada, repositório assíncrono, casos de uso CRUD, endpoints REST `/fornecedores` e validador somente leitura das conexões de +Compras e ERP.

### Sprint A13 — Primeiro Vertical Slice do +Compras

endpoint consultivo `POST /api/v1/negociacoes/recomendacoes`, orquestrado por caso de uso Application sobre memória e estratégia existentes, sem persistência ou alteração de estado.

### Sprint A12 — Especificação Oficial das 56 Work Orders

consolidação exclusivamente documental do catálogo estratégico de oito fases e 56 Work Orders, sem implementação de funcionalidades de negócio.

### Sprint A11 — Engineering Blueprint

Consolidação documental de arquitetura, implementação, roadmap técnico, Work Orders e operação, sem alteração de funcionalidades.

### Sprint A10 — Governance and Work Order Foundation

Fundação de governança, visão e Work Orders do SOMA BlueprintOS / +COMPRAS, incluindo a normalização documental já iniciada. Não implementa funcionalidade de negócio.

### Sprint A9 — Publication Engine

Implementação do Publication Engine: geração automática de três documentos profissionais para apresentação (Relatório Executivo, Guia do Cliente, Guia de Engenharia), cada um em Markdown, HTML e PDF, publicados em `dist/{executive,client,engineering}/`. O conteúdo reaproveita integralmente os 19 geradores de documentação da Sprint A8 (nenhum dado fabricado); o Relatório Executivo acrescenta indicadores reais de build/testes coletados em tempo real (`dotnet build` + contagem de `[Fact]`/`[Theory]`) e dívidas técnicas/próximos passos extraídos diretamente de `.ai/memory/known_issues.md` e `.ai/ROADMAP.md`.

### Sprint A8 — Audience-Specific Publishers (Portal de Documentação Viva)

Implementação do Portal de Documentação Viva: publicação automática de documentação executiva, de cliente e de engenharia (19 geradores) a partir de fontes reais do repositório (`.ai/ROADMAP.md`, `.ai/memory/completed_sprints.md`, `.ai/memory/known_issues.md`, `.ai/DECISIONS.md`, metadados de módulo e o grafo real de dependências entre projetos), com publicação em disco sob `docs/` (`IDocumentPublisher`/`MarkdownPublisher`/`DocumentationPublisher`) e sincronização automática dos artefatos de memória da AI Factory.

### Sprint A7 — Sistema de Documentação do BlueprintOS

Implementação do módulo backend `Documentation`, responsável por gerenciar a documentação do próprio BlueprintOS: estrutura de documentos (`DocumentationEntry` + `IDocumentationRepository`), versionamento de documentos (`IDocumentVersioningService`), registro de alterações/changelog (`IChangeLogService`), Architecture Decision Records (`AdrRecord` + `IAdrService`, persistidas como Markdown), geradores de documentação técnica, funcional, para IA (`.ai/context`) e para desenvolvedores, gerador de diagramas Mermaid (`IMermaidDiagramGenerator`), sincronização automática e detecção de documentação desatualizada (`IDocumentationSyncService` / `IStaleDocumentationDetector`), integração de leitura com Git (`IGitLogReader` / `GitCliDocumentationService`) e ponto de extensão para integração futura com um módulo de Memória genérico (`IDocumentationMemoryNotifier`).
