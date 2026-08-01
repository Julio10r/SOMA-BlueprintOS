# known_issues.md

> Log de dívidas técnicas e problemas conhecidos do BlueprintOS, atualizado ao final de cada sprint (ver WORKFLOW.md §14).

---

## A13 — Vertical Slice do +COMPRAS

- A recomendação de negociação não persiste histórico, fornecedores ou resultados; a memória existente é transitória.
- A identidade temporária aceita apenas em `Development` bloqueia a operação em Production até a futura implementação de identidade corporativa.

## B2 — Descoberta Inicial de Fornecedores

- A validação operacional de leitura no ERP SOMA_DESENV permanece pendente por timeout de rede no ambiente de execução; o adaptador é somente leitura.
- O score 100/80/60/40 é estrutura inicial. Relacionamentos fornecedor × item/família/categoria e histórico de pedidos ainda dependem dos módulos operacionais futuros.

## B2.1 — Validação Operacional e Sincronização

- A migration `202607310001_B21FornecedorSynchronization` foi aplicada em 31/07/2026 e a complementar `202608010001_B21CanonicalSupplierSynchronization` em 01/08/2026, ambas somente no +Compras dev.
- A sprint foi reaberta e a validação técnica foi concluída em 01/08/2026; o encerramento formal aguarda revisão do relatório.
- `LX_SEQUENCIAL` retornou `315501`, `315502` e `315503` em criações reais; os códigos foram confirmados nas tabelas `FORNECEDORES` e `CADASTRO_CLI_FOR`, inclusive em execução concorrente.
- O timestamp efetivo foi identificado em `CADASTRO_CLI_FOR.DATA_PARA_TRANSFERENCIA`, com `FORNECEDORES.DATA_PARA_TRANSFERENCIA` como espelho/fallback, normalizado em `America/Sao_Paulo` até o segundo.
- O registro inválido `00000*` foi preservado e inativado; correlação da correção: `b21-invalid-clifor-inactivate-final-erp`.
- B2.1.1 corrigiu o preenchimento do contrato canônico ERP → +Compras. O fornecedor fictício `315504` confirmou persistência de endereço, contatos, dados bancários/fiscais/comerciais, indicadores e hash. Classificações `TIPO`/`SUBTIPO_FORNECEDOR` são protegidas por FKs no ERP e devem usar valores válidos, nunca valores inventados no teste.
- O ERP limita `FORNECEDOR` a 25 caracteres e o mantém como FK para `CADASTRO_CLI_FOR.NOME_CLIFOR`; a sincronização não altera essa chave e atualiza CNPJ como campo corporativo permitido.
- A identidade de desenvolvimento continua limitando a operação a Development, conforme ADR-0011.

## Estado consolidado na Sprint A10 — Project State Consolidation

- **Fonte operacional de estado criada.** `.ai/PROJECT_STATE.md` passou a registrar a evidência atual de código, testes e Git. `CURRENT_SPRINT.md`, `ROADMAP.md` e o histórico de sprints foram alinhados a ela.
- **Roadmap executivo +COMPRAS ainda requer edição visual.** O PowerPoint não foi alterado nesta sprint; `docs/presentations/ROADMAP_UPDATE.md` detalha as correções factuais necessárias antes de qualquer publicação externa.
- **Os números históricos de testes não devem ser reutilizados como estado atual.** A validação da A10 executou 230 testes unitários e 1 de integração, todos aprovados. Documentos futuros devem registrar uma execução identificável, não reproduzir totais de sprints anteriores.

## Sprint A7 — Sistema de Documentação do BlueprintOS

- **Frontend React ainda não inicializado.** O projeto Web (React/TypeScript) previsto em PROJECT.md/ARCHITECTURE.md ainda não foi criado; toda entrega até aqui, incluindo a Sprint A7, é exclusivamente backend.
- **Biblioteca UI baseada no GDT não foi implementada.** A Sprint A8 comprovada no código entregou publicadores de documentação por público, não um frontend React. A tradução do GDT para componentes React continua não iniciada.
- **Migração completa da arquitetura Core/Infrastructure para a arquitetura alvo será realizada em sprint futura.** O backend ainda segue o padrão `Core/{Módulo}/Contracts,Models` + `Infrastructure/{Módulo}/...`, e não a estrutura `Modules/{Domain,Application,Infrastructure,Api}` definida em ARCHITECTURE.md. A migração para a estrutura alvo (incluindo o módulo `Documentation` criado nesta sprint) fica registrada para uma sprint futura (ver ADR-0006 em `.ai/DECISIONS.md`).


## Sprint A8 — Audience-Specific Publishers (Portal de Documentação Viva)

- **KPIs, FAQ e runbook operacional ainda estão vazios/mínimos.** O portal de documentação viva gera esses documentos de forma honesta (sem dados fabricados), mas eles permanecem sparse até que existam fontes reais (uso em produção, suporte ao cliente, incidentes reais).
- **Nenhum `DbContext`/schema de banco de dados existe ainda**, portanto `docs/engineering/database.md` reflete apenas essa ausência.
- **Atualização de `.ai/ROADMAP.md` e `.ai/memory/completed_sprints.md` via `DocumentationPublishService` é idempotente mas ainda manual** (sem acionamento automático por um motor de Workflow, que ainda não existe).

## Sprint A9 — Publication Engine

- **Artefatos gerados antes da A10 podem conter estado anterior.** Ao publicar novamente documentação, conferir que os geradores leiam `.ai/PROJECT_STATE.md` ou as fontes canônicas atualizadas e não reintroduzam textos históricos.
- **`QualityMetricsProvider` executa `dotnet build` de verdade a cada publicação**, o que torna `dotnet run -- publish` mais lento (build completo da solution) em troca de nunca fabricar o status de build/warnings/erros exibido no Relatório Executivo. Uma otimização futura poderia reaproveitar o resultado do build mais recente de CI, quando existir uma pipeline de CI.
- **HTML e PDF não têm fidelidade pixel-a-pixel de layout entre si.** Ambos são gerados a partir do mesmo modelo comum (`ContentBlock`/`InlineSpan`, via `MarkdownContentParser`/`InlineSpanParser`), garantindo consistência de conteúdo, mas cada um usa seu próprio motor de composição visual (HTML+CSS vs. QuestPDF); não há conversão de um formato para o outro (ver ADR-0007).
- **Licença QuestPDF (Community) é gratuita apenas para empresas com receita anual abaixo de 1M USD.** Caso o SOMA ultrapasse esse limite, será necessário adquirir uma licença comercial do QuestPDF (ver ADR-0007).
- **Diagramas Mermaid não são rasterizados para imagem.** `MermaidAsset` já modela `RenderedImageBytes`, mas nenhum pipeline de rasterização (ex.: Mermaid CLI headless) foi implementado nesta sprint; enquanto isso, os renderizadores exibem a definição Mermaid como bloco de código-fonte, de forma honesta (ver ADR-0008).
- **Nenhum publicador popula `PublicationAssets.Charts` ainda.** O modelo (`ChartAsset`/`ChartDataPoint`) existe e os renderizadores já sabem desenhar imagens referenciadas por `ContentBlock.Image`, mas não há hoje uma fonte de dados real para um gráfico de KPI (ex.: evolução de sprints, cobertura de testes) — populá-lo com dados fabricados violaria a política de não fabricar conteúdo.
- **Marca d'água, assinatura eletrônica, numeração automática de figuras/tabelas, exportação para DOCX/PPTX/site estático e diagramas com layout automático (organogramas, BPMN, C4) permanecem não implementados.** O modelo já expõe os pontos de extensão necessários (`Metadata.Classification`, `ContentBlock.Caption`, `IContentRenderer`, `ImageAssetKind`/`MermaidAsset`), mas a renderização desses recursos ainda não existe (ver ADR-0008 para o mapeamento completo).
