# Changelog

Todas as mudanças notáveis deste projeto são documentadas neste arquivo.

O formato segue [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/) e o projeto adota versionamento semântico adaptado a marcos de EPIC.

---

## [v0.4.0-documentation] - 2026-07-23

### EPIC A7 — Sistema de Documentação (concluído)

Encerramento oficial do EPIC de Documentação do BlueprintOS, homologado de ponta a ponta (build limpo, 184 testes passando, pipeline executado com sucesso).

**Adicionado**

- **Sistema de documentação por audiência**: geração automática de documentação para três públicos distintos — Diretoria (`docs/executive/`), Cliente (`docs/client/`) e Engenharia (`docs/engineering/`) — a partir de 19 geradores que leem exclusivamente dados reais do repositório (`.ai/ROADMAP.md`, `.ai/DECISIONS.md`, `.ai/memory/*.md`, metadados de módulo e o grafo real de dependências), nunca conteúdo fabricado.
- **Publishers inteligentes**: `ExecutivePublisher`, `ClientPublisher` e `EngineeringPublisher`, orquestrados por `PublicationService`, produzindo cada relatório em três formatos (Markdown, HTML, PDF) a partir de um único modelo comum (`PublicationDocument`/`ContentBlock`), sem duplicar lógica de interpretação de conteúdo entre renderizadores.
- **Documentation Health**: verificação automática de integridade da documentação publicada — cobertura, estrutura (título, seções, ordem de headings), links e imagens quebrados, conteúdo duplicado ou abaixo do mínimo — com relatório em `docs/DocumentationHealth.md` gerado a cada execução do pipeline.
- **Documentation Assets**: diagramas Mermaid (`architecture.mmd`, `dependencies.mmd`, `agents.mmd`) e mapa de estrutura do repositório (`solution-tree.md`), publicados em `docs/assets/` e reutilizáveis em qualquer renderizador Mermaid.
- **Executive Blueprint** consolidado (`docs/executive/BlueprintOS_Executive_Blueprint.{md,html,pdf}`), distinguindo explicitamente o que já está implementado do que é arquitetura-alvo ou roadmap.
- Três pontos de entrada de CLI em `BlueprintOS.Api`: `dotnet run -- publish`, `-- publish-docs`, `-- publish-executive-blueprint`.

**Corrigido**

- Link relativo quebrado (`CURRENT_SPRINT.md`) que aparecia em todo documento que referenciava o roadmap, corrigido na fonte (`.ai/ROADMAP.md`) para uma referência textual válida em qualquer profundidade de publicação.
- Heading duplicado ("Resumo Executivo") no Relatório Executivo, causado por `DashboardGenerator` reemitir seu próprio título além do já adicionado pelo publisher.
- Exceção registrada no Documentation Health para headings que se repetem por design entre blocos de módulo ("Contratos", "Classes", "Banco de Dados"), evitando warnings permanentes sobre um padrão esperado.

**Alterado**

- Estrutura oficial de diretórios de documentação formalizada como `docs/{executive,client,engineering,assets}` (ADR-0009 em `.ai/DECISIONS.md`); diretórios de scaffolding não utilizados pelo pipeline (`docs/architecture/`, `docs/api/`, `docs/adr/`) removidos.

### Homologação completa

- Build: 0 erros, 0 warnings.
- Testes: 184/184 passando (183 unitários + 1 de integração).
- Documentation Health: 0 erros, 0 warnings — Executive, Client e Engineering saudáveis.
- Pipeline (`publish`, `publish-docs`, `publish-executive-blueprint`) executado com sucesso, refletindo o estado final do EPIC.
