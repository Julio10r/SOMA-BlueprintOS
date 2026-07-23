# known_issues.md

> Log de dívidas técnicas e problemas conhecidos do BlueprintOS, atualizado ao final de cada sprint (ver WORKFLOW.md §14).

---

## Sprint A7 — Sistema de Documentação do BlueprintOS

- **Frontend React ainda não inicializado.** O projeto Web (React/TypeScript) previsto em PROJECT.md/ARCHITECTURE.md ainda não foi criado; toda entrega até aqui, incluindo a Sprint A7, é exclusivamente backend.
- **Biblioteca UI baseada no GDT será implementada na Sprint A8.** A tradução do Global Design Tokens (GDT) para componentes React está fora do escopo desta sprint e planejada para a Sprint A8.
- **Migração completa da arquitetura Core/Infrastructure para a arquitetura alvo será realizada em sprint futura.** O backend ainda segue o padrão `Core/{Módulo}/Contracts,Models` + `Infrastructure/{Módulo}/...`, e não a estrutura `Modules/{Domain,Application,Infrastructure,Api}` definida em ARCHITECTURE.md. A migração para a estrutura alvo (incluindo o módulo `Documentation` criado nesta sprint) fica registrada para uma sprint futura (ver ADR-0006 em `.ai/DECISIONS.md`).


## Sprint A8 — Portal de Documentação Viva

- **KPIs, FAQ e runbook operacional ainda estão vazios/mínimos.** O portal de documentação viva gera esses documentos de forma honesta (sem dados fabricados), mas eles permanecem sparse até que existam fontes reais (uso em produção, suporte ao cliente, incidentes reais).
- **Nenhum `DbContext`/schema de banco de dados existe ainda**, portanto `docs/engineering/database.md` reflete apenas essa ausência.
- **Atualização de `.ai/ROADMAP.md` e `.ai/memory/completed_sprints.md` via `DocumentationPublishService` é idempotente mas ainda manual** (sem acionamento automático por um motor de Workflow, que ainda não existe).

## Sprint A9 — Publication Engine

- **`RoadmapGenerator` (Sprint A8) contém um texto desatualizado/inconsistente.** Ele afirma que "`.ai/memory/completed_sprints.md` ainda não registra nenhuma sprint concluída" e que o projeto está na Fase 0, mas o arquivo já registra as Sprints A7/A8/A9. Esse texto é reproduzido literalmente no Relatório Executivo e no Guia do Cliente do Publication Engine (seção Roadmap), pois o Publication Engine reaproveita o gerador existente sem alterá-lo. Corrigir o `RoadmapGenerator` para refletir o estado real (fora do escopo desta sprint, que é a camada de publicação/renderização, não os geradores de conteúdo).
- **`QualityMetricsProvider` executa `dotnet build` de verdade a cada publicação**, o que torna `dotnet run -- publish` mais lento (build completo da solution) em troca de nunca fabricar o status de build/warnings/erros exibido no Relatório Executivo. Uma otimização futura poderia reaproveitar o resultado do build mais recente de CI, quando existir uma pipeline de CI.
- **HTML e PDF não têm fidelidade pixel-a-pixel de layout entre si.** Ambos são gerados a partir do mesmo modelo comum (`ContentBlock`/`InlineSpan`, via `MarkdownContentParser`/`InlineSpanParser`), garantindo consistência de conteúdo, mas cada um usa seu próprio motor de composição visual (HTML+CSS vs. QuestPDF); não há conversão de um formato para o outro (ver ADR-0007).
- **Licença QuestPDF (Community) é gratuita apenas para empresas com receita anual abaixo de 1M USD.** Caso o SOMA ultrapasse esse limite, será necessário adquirir uma licença comercial do QuestPDF (ver ADR-0007).
- **Diagramas Mermaid não são rasterizados para imagem.** `MermaidAsset` já modela `RenderedImageBytes`, mas nenhum pipeline de rasterização (ex.: Mermaid CLI headless) foi implementado nesta sprint; enquanto isso, os renderizadores exibem a definição Mermaid como bloco de código-fonte, de forma honesta (ver ADR-0008).
- **Nenhum publicador popula `PublicationAssets.Charts` ainda.** O modelo (`ChartAsset`/`ChartDataPoint`) existe e os renderizadores já sabem desenhar imagens referenciadas por `ContentBlock.Image`, mas não há hoje uma fonte de dados real para um gráfico de KPI (ex.: evolução de sprints, cobertura de testes) — populá-lo com dados fabricados violaria a política de não fabricar conteúdo.
- **Marca d'água, assinatura eletrônica, numeração automática de figuras/tabelas, exportação para DOCX/PPTX/site estático e diagramas com layout automático (organogramas, BPMN, C4) permanecem não implementados.** O modelo já expõe os pontos de extensão necessários (`Metadata.Classification`, `ContentBlock.Caption`, `IContentRenderer`, `ImageAssetKind`/`MermaidAsset`), mas a renderização desses recursos ainda não existe (ver ADR-0008 para o mapeamento completo).