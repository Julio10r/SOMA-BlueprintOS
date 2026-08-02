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

## Sprint A8 — Audience-Specific Publishers (Portal de Documentação Viva)

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
- Módulo `Publication` em `backend/src/BlueprintOS.Core/Publication/` (Contracts + Models, incluindo o modelo comum `ContentBlock`/`InlineSpan` e o modelo rico `PublicationMetadata`/`PublicationAssets`/`PublicationTheme`) e `backend/src/BlueprintOS.Infrastructure/Publication/` (Content + Rendering + Publishers + orquestrador), seguindo o mesmo padrão dos módulos `Documentation`/`Knowledge`.
- Modelo comum (ViewModel) único por documento: Markdown bruto dos geradores é convertido uma única vez em `ContentBlock`s (`MarkdownContentParser`); os três renderizadores (`MarkdownRenderer`, `HtmlRenderer`, `PdfRenderer` via `QuestPDF`) consomem exatamente os mesmos blocos, sem duplicar lógica de interpretação — nenhum deriva HTML→PDF.
- `PublicationDocument` evoluído para documentos ricos: `Metadata` (autor, empresa, classificação, tags, histórico de revisões), `Assets` (imagens, logos, ícones SVG, gráficos, Mermaid, anexos, QR Codes, selos — cada um com suporte nativo de renderização nos três formatos) e `Theme` (paleta de cores por tipo de documento: executivo/cliente/engenharia). Suporte nativo funcional implementado para imagens/logos/ícones embutidos, anexos copiados para `dist/{categoria}/attachments/`, QR Codes gerados em tempo real (`QRCoder`, sem `System.Drawing`) e selos de build/testes/warnings renderizados localmente a partir de `QualityMetrics` real.
- Três publicadores de relatório (`ExecutivePublisher`, `ClientPublisher`, `EngineeringPublisher`), orquestrados por `PublicationService`; novos formatos (Word, PowerPoint, site estático) podem ser adicionados implementando apenas `IContentRenderer`, sem alterar os publicadores.
- `QualityMetricsProvider`, que coleta build status, warnings, erros e quantidade de testes em tempo real (sem valores fabricados), agora exibidos também como selos na capa do Relatório Executivo.
- Ponto único de entrada `dotnet run -- publish` em `backend/src/BlueprintOS.Api/Program.cs`, que resolve a raiz do repositório via `.git` para funcionar independente do diretório de execução.
- ADR-0007 (modelo comum de renderização) e ADR-0008 (documento rico: Metadata/Assets/Appendix/Theme) registradas em `.ai/DECISIONS.md`. `dist/` adicionado ao `.gitignore` (artefato gerado, não versionado).
- Suíte de testes unitários (xUnit, fakes manuais) cobrindo o parser de conteúdo, o parser de ênfase inline, os três renderizadores (incluindo blocos de imagem, selos, apêndice e anexos), o gerador de QR Code, o modelo de assets, o orquestrador e a coleta de indicadores.

**Resultado da validação:** `dotnet build` sem erros/warnings; `dotnet test` com 100% dos testes passando (167 testes unitários + 1 teste de integração); `dotnet run -- publish` executado com sucesso, gerando os 9 arquivos esperados em `dist/`, com selos, QR Code e histórico de versões visíveis nos três formatos do Relatório Executivo.

## Sprint A10 — Governance and Work Order Foundation

**Status:** Completed

**Escopo:** Fundação de governança, visão e Work Orders do SOMA BlueprintOS / +COMPRAS, incluindo a normalização documental já iniciada. Não implementa funcionalidade de negócio.

**Entregas comprovadas:**
- Criação de `.ai/PROJECT_STATE.md`, com estado de módulos, agentes, integrações, APIs, infraestrutura, riscos e evidências de validação.
- Atualização da sprint corrente, roadmap, documentação técnica e relatórios institucionais para distinguir implementação, parcialidade e planejamento.
- Atualização do registro da Sprint A8 para explicitar a evidência de publicadores por público (`Executive`, `Client`, `Engineering`) no código e no commit `3905290`.
- Criação de `docs/presentations/ROADMAP_UPDATE.md`, sem alterar o PowerPoint, com as correções factuais necessárias em cada slide do roadmap executivo +COMPRAS.

**Resultado da validação:** `dotnet build backend/BlueprintOS.sln --no-restore` com 0 avisos e 0 erros; 230 testes unitários e 1 teste de integração executados, todos aprovados, sem testes ignorados ou falhos.

**Complemento de governança:** `VISION.md`, `BACKLOG.md`, template oficial e 56 Work Orders foram criados. Sprints sem escopo ou evidência suficiente foram marcadas como `A detalhar`, `Planejado` ou `Não comprovado`.

## Sprint A11 — Engineering Blueprint

**Status:** Completed

**Escopo:** Consolidação documental de arquitetura, implementação, roadmap técnico, Work Orders e operação, sem alteração de funcionalidades.

**Entregas:** `ENGINEERING_BLUEPRINT.md` com 22 seções, índice e diagramas Mermaid; referências sincronizadas em `PROJECT_STATE.md`, `VISION.md` e `WORKFLOW.md`.

## Sprint A12 — Especificação Oficial das 56 Work Orders

**Status:** Completed

**Escopo:** consolidação exclusivamente documental do catálogo estratégico de oito fases e 56 Work Orders, sem implementação de funcionalidades de negócio.

**Entregas comprovadas:**
- As 56 Work Orders foram especificadas com objetivo, escopo, dependências, requisitos, critérios de aceite, testes, riscos e resultado de execução.
- O `BACKLOG.md`, o índice das Work Orders e o mapa de dependências foram sincronizados com os nomes oficiais e os arquivos reais.
- A evidência histórica da Fase A foi preservada: A1–A4 e A7 concluídas, A5 não comprovada e A6 parcial; as demais permanecem planejadas.
- As fontes externas de descoberta de Compras Indiretas foram registradas sem serem tratadas como evidência de implementação ou aprovação de escopo.

**Resultado da validação:** `dotnet build backend/BlueprintOS.sln --no-restore` com 0 avisos e 0 erros; `dotnet test backend/BlueprintOS.sln --no-build` com 230 testes unitários e 1 teste de integração aprovados, 0 ignorados e 0 falhos. As 56 Work Orders têm as 28 seções obrigatórias; links e referências do catálogo foram verificados.

## Sprint A13 — Primeiro Vertical Slice do +Compras

**Status:** Concluída em 30/07/2026.

**Escopo:** endpoint consultivo `POST /api/v1/negociacoes/recomendacoes`, orquestrado por caso de uso Application sobre memória e estratégia existentes, sem persistência ou alteração de estado.

**Evidência:** `NegotiationRecommendationController`, `NegotiationRecommendationUseCase` e o adaptador `DevelopmentRequestIdentity`; a resposta propaga `requestId`, justificativas, alertas, probabilidade suportada e `humanDecisionRequired: true`.

**Validação:** build sem erros, 231 testes unitários e 1 teste de integração aprovados; smoke test HTTP aprovado em Development e resposta 503 segura validada em Production para a identidade temporária.

## Sprint B1 — Persistência de Fornecedores

**Status:** Concluída em 30/07/2026.

**Escopo:** agregado `Fornecedor`, value object `Cnpj`, DbContext EF Core/SQL Server, migration versionada, repositório assíncrono, casos de uso CRUD, endpoints REST `/fornecedores` e validador somente leitura das conexões de +Compras e ERP.

**Identidade:** todos os acessos são isolados por `TemporaryUserId` recebido exclusivamente por `ICurrentIdentity`; a regra de negócio não conhece o adaptador de Development e permanece preparada para Entra ID.

**Validação:** `dotnet build backend/BlueprintOS.sln --no-restore` sem avisos ou erros; 234 testes unitários e 2 de integração aprovados. `validate-b1-connectivity` confirmou +Compras e ERP `SOMA_DESENV` por `SELECT 1`, sem migration, DDL ou escrita.

**Limite operacional:** a migration não foi aplicada por solicitação explícita; a criação física das tabelas no +Compras depende de autorização posterior.

## Sprint B2 — Descoberta Inicial de Fornecedores

**Status:** Concluída em 30/07/2026.

**Escopo:** consulta somente leitura ao ERP SOMA_DESENV por item, descrição ou categoria, score explicável 100/80/60/40, persistência de descobertas no +Compras e endpoints de descoberta/consulta.

**Evidência:** `a19e496`; `ErpFornecedorDiscoveryRepository`, `FornecedorDescoberto`, `ScoreFornecedor` e endpoints de descoberta. O fluxo não escreveu no ERP.

**Validação:** build sem erros ou avisos; 240 testes unitários e 2 de integração aprovados.

**Limite operacional:** o ambiente de execução não alcançou o SQL Server ERP (timeout). A validação operacional permanece pendente; o score é estrutura inicial e será evoluído somente quando existirem dados operacionais de itens, pedidos e relacionamentos.

## Sprint B2.1 — Validação Operacional e Sincronização de Fornecedores com ERP

**Status:** Concluída em 31/07/2026.

**Entregas:** contratos ERP por BU/adaptador, adaptador `SOMA_DESENV`, importação e exportação idempotentes, lote controlado, status/origem/última sincronização, histórico de tentativas, correlação, timeout/cancelamento, migration `202607310001_B21FornecedorSynchronization` aplicada no +Compras e endpoints operacionais.

**Evidências reais:** ERP_ID `277459` importado para um único fornecedor do +Compras e repetido sem duplicidade; fornecedor fictício +Compras `59d3f811-23ce-4589-9c15-1679cea59afd` criado no ERP como `999999`, atualizado por CNPJ de final `0195` para `0110` e reexecutado idempotentemente. O histórico do +Compras registrou as tentativas com status sanitizado.

**Validação:** build sem erros/avisos; 245 testes unitários e 3 testes de integração aprovados.

**Conclusão final em 01/08/2026:** a reabertura entregou o contrato canônico completo, sincronização temporal bidirecional, empate favorável ao +Compras, inativação lógica e auditoria append-only com snapshots antes/depois, hashes, `CorrelationId`, histórico e idempotência. `b08769f` e `3b6d54b` registram a implementação. Os CLIFORs reais `315501`, `315502`, `315503` e `315505` foram confirmados em `FORNECEDORES` e `CADASTRO_CLI_FOR`; a concorrência não gerou duplicidade.

**Limitação conhecida:** `FORNECEDORES.FORNECEDOR` é FK para `CADASTRO_CLI_FOR.NOME_CLIFOR`; o nome não foi alterado para evitar operação destrutiva. O adaptador atualiza CNPJ e campos corporativos compatíveis.

## Subetapa B2.1.1 — Completar Mapeamento Canônico ERP → +Compras

**Status:** Concluída em 01/08/2026.

**Entregas:** mapeamento do Linx para identificação, endereço, contatos, dados bancários, comerciais, fiscais e indicadores de fornecimento, sem expor tabelas do ERP à Application.

**Evidência:** `0240c35`; fornecedor ERP fictício `315504` com dados completos e hash persistido. O CNPJ `21855705000160` foi importado com cidade e UF; a reexecução retornou `NenhumaAlteracao`.

## Subetapa B2.1.2 — Modelo Canônico de Fornecedor ERP Linx

**Status:** Concluída em 01/08/2026.

**Resumo:** implementação do modelo canônico de fornecedor integrado ao ERP Linx.

**Entregas:** ADR-0016 criada e aceita; modelo fornecedor alinhado ao Linx; `Cnpj_Cpf` implementado; `TipoPessoa` implementado; `RazaoSocial` separado de `NomeFantasia`; `NomeFantasia` protegido como chave operacional ERP; `Beneficiador` implementado; `Licenciado` implementado; domínios ERP estruturados; FKs opcionais criadas; contrato frontend inicial criado.

**Evidência:** migration `202608010002_B212FornecedorLinxCanonicalModel` aplicada no +Compras dev, sem alteração estrutural no ERP Linx; commit `77861eb`.

**Validação:** build com 0 erros e 0 avisos; 256 testes unitários e 4 testes de integração aprovados.

## Sprint B2.1.3 — Endurecimento da Integração ERP de Fornecedores

**Status:** Concluída em 02/08/2026, com validação real contra API em Docker, VPN corporativa e banco `MaisCompras`.

**Escopo:** transformar a sincronização ERP SOMA → +Compras em rotina operacional rastreável, paginada e resiliente a erros parciais: leitura paginada (`IFornecedorErpReader`/`SomaFornecedorReader` com `OFFSET/FETCH`), orquestração em lotes (`SincronizarFornecedoresErpUseCase`), histórico de execução (`SincronizacaoFornecedor`) e erros parciais persistidos (`ErroSincronizacaoFornecedor`), migration `202608020001_B213FornecedorErpSyncHardening`, logs estruturados e retorno detalhado do endpoint `GET /api/fornecedores/sincronizar-erp`.

**Correções pós-entrega inicial (paginação):** dois bugs reais no loop de paginação, encontrados pelo teste `Execute_Should_Process_Multiple_Batches_And_Calculate_Totals`, foram corrigidos sem alterar regra de negócio: parada prematura quando o lote vinha menor que o esperado (commit `21f1a67`) e cálculo não determinístico do offset de paginação (commit `ca48dc3`).

**Hardening de execução real (02/08/2026):** a pedido explícito do Product Owner, a sprint foi revalidada rodando a API em Docker contra o ERP corporativo (`SOMA_DESENV`) e o banco `MaisCompras` reais, via VPN. Essa validação expôs e corrigiu três problemas adicionais:

1. **Docker bloqueava a subida da API:** `docker-compose.yml` tinha `depends_on: sqlserver: condition: service_healthy` no serviço `api`, obrigando-o a esperar o SQL Server local **opcional** (não usado pela aplicação, que sempre aponta para o banco corporativo). Sem `SA_PASSWORD` definido, o container `sqlserver` nunca ficava saudável e a API nunca subia. Corrigido removendo a dependência obrigatória; o serviço `sqlserver` permanece disponível como ambiente isolado opcional (ADR-0018). Criado `infrastructure/docker/.env.example` sem segredos reais.
2. **`limite` era tamanho de página, não teto total:** `SincronizarFornecedoresErpUseCase` usava `dto.Limite` apenas como tamanho de lote dentro de um `while(true)` que só parava com página vazia — `limite=50` varria a tabela inteira de fornecedores do ERP (confirmado na prática: 2.812 fornecedores processados antes de a chamada de teste ser interrompida manualmente). Corrigido para que `limite` seja o teto total de fornecedores processados na execução, com paginação interna preservada.
3. **Erro parcial de persistência virava HTTP 500:** quando `SaveChangesAsync` falhava para um fornecedor (ex.: violação de índice único de CNPJ), a entidade continuava rastreada no `DbContext`, e o `SaveChangesAsync` final (ao persistir `SincronizacaoFornecedor`) tentava salvá-la de novo, repetindo o erro fora do bloco de tratamento e derrubando a requisição inteira — mesmo com a maioria dos fornecedores processados com sucesso. Esse comportamento só aparecia contra SQL Server real; os testes unitários usam EF InMemory, que não impõe índices únicos. Corrigido com `context.ChangeTracker.Clear()` no `catch`, garantindo que a execução finalize como `Parcial` com o erro registrado e o histórico salvo.

**Evidência real:** `docker compose config` sem erros; API sobe isolada via `docker compose up -d api`; `GET /health` retornou `200 OK`; `GET /api/fornecedores/sincronizar-erp?businessUnit=DEFAULT&limite=50` contra ERP/`MaisCompras` reais retornou `{"status":"Parcial","consultados":50,"incluidos":48,"atualizados":1,"erros":1}`; consulta direta via `sqlcmd` confirmou o registro em `SincronizacoesFornecedores` (execução `49A9474D-6CDB-44C2-8D7E-165F79E3CFF7`) e o erro correspondente em `ErrosSincronizacoesFornecedores`.

**Validação:** `dotnet build backend/BlueprintOS.sln` com 0 erros e 0 avisos; `dotnet test backend/BlueprintOS.sln` com 282 testes aprovados (277 unitários + 5 integração), 0 falhas — incluindo o novo teste `Execute_Should_Finish_As_Parcial_And_Persist_Execucao_When_Individual_SaveChanges_Fails`, que simula uma falha real de `SaveChangesAsync` para reproduzir em teste unitário o comportamento antes só visível contra SQL Server real.

**Aprendizados:**
- O parâmetro `limite` de uma sincronização em lote deve sempre representar um teto operacional total, nunca apenas o tamanho de página — a ambiguidade permite que uma chamada aparentemente limitada varra a base inteira de um sistema externo.
- Tratamento de erro parcial em rotinas que usam EF Core precisa considerar o estado do `ChangeTracker`: uma entidade que falhou ao salvar continua rastreada e pode contaminar o próximo `SaveChangesAsync`, transformando um erro pontual em falha total.
- Testes com EF Core InMemory podem não reproduzir restrições reais do SQL Server (índices únicos, por exemplo); o comportamento de erro parcial deve ser coberto com um teste que simule a falha de persistência explicitamente, e idealmente confirmado contra o banco real antes de fechar a sprint.
