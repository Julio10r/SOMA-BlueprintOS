# DECISIONS.md

> Log oficial de Architecture Decision Records (ADRs) do SOMA BlueprintOS.

Este arquivo é o log canônico de ADRs do projeto, substituindo a convenção de arquivos individuais em `.ai/decisions/ADR-000N.md` mencionada em [ARCHITECTURE.md](./ARCHITECTURE.md) §14. Novas ADRs devem ser adicionadas ao final deste arquivo, com numeração sequencial.

Formato de cada ADR:

- **Título**
- **Status** (Proposto / Aceito / Rejeitado / Substituído)
- **Contexto** — o problema ou força que motivou a decisão
- **Decisão** — o que foi decidido
- **Consequências** — efeitos, positivos e negativos, da decisão

---

## ADR-0001: Adoção de Modular Monolith + Clean Architecture + DDD pragmático

**Status:** Aceito

**Contexto:** O BlueprintOS precisa suportar múltiplos domínios de negócio (Identity, Planner, Procurement, Workflow, Knowledge, Memory, Agents, Notifications, Dashboard, Analytics) evoluindo de forma independente, sem o custo operacional de microsserviços desde o início.

**Decisão:** Adotar Modular Monolith com Clean Architecture e DDD pragmático, organizando o código em `/src/Apps`, `/src/BuildingBlocks` e `/src/Modules`, cada módulo com camadas Domain/Application/Infrastructure/Api.

**Consequências:**
- Módulos evoluem com baixo acoplamento e alta coesão.
- Caminho aberto para extração futura em microsserviços sem reescrita (ver ARCHITECTURE.md §13).
- Exige disciplina para não criar dependências diretas entre módulos.

---

## ADR-0002: Seleção da stack tecnológica oficial

**Status:** Aceito

**Contexto:** O projeto precisa de uma stack única, moderna e suportada, adequada a uma plataforma corporativa multi-tenant hospedada em nuvem.

**Decisão:** Backend em .NET 9 / ASP.NET Core / C#; banco SQL Server com Entity Framework Core; frontend em React/TypeScript; containers via Docker; cloud Google Cloud Platform; autenticação via Microsoft Entra ID; controle de versão em Git/GitHub.

**Consequências:**
- Stack única reduz curva de aprendizado e custo de manutenção.
- Dependência do ecossistema .NET/Microsoft para autenticação e runtime.
- Ver [context/tech-stack.md](./context/tech-stack.md) para detalhes operacionais.

---

## ADR-0003: CQRS + MediatR + Domain Events como padrão de camada de aplicação

**Status:** Aceito

**Contexto:** A camada Application precisa de um padrão consistente para separar leitura de escrita e para propagar efeitos colaterais de domínio sem acoplar módulos entre si.

**Decisão:** Utilizar CQRS (Commands e Queries) via MediatR como mediador de handlers, e Domain Events para comunicar efeitos de domínio dentro do próprio módulo ou via Contracts entre módulos.

**Consequências:**
- Casos de uso ficam isolados em Commands/Queries/Handlers, facilitando testes.
- Domain Events permitem reagir a mudanças sem acoplamento direto.
- Exige disciplina para não transformar Domain Events em substituto de chamadas síncronas necessárias.

---

## ADR-0004: Result Pattern em vez de exceções para fluxos de negócio esperados

**Status:** Aceito

**Contexto:** Uso indiscriminado de exceções para controle de fluxo de negócio (ex.: validação, regra violada) torna o código difícil de ler e prejudica performance.

**Decisão:** Utilizar Result Pattern para representar sucesso/falha esperada de operações de negócio. Exceções ficam reservadas a erros verdadeiramente excepcionais (ex.: falha de infraestrutura).

**Consequências:**
- Fluxos de erro esperado tornam-se explícitos na assinatura dos métodos.
- Handlers e Controllers tratam falhas de forma uniforme.
- Exige padronização do tipo `Result`/`Result<T>` no SharedKernel.

---

## ADR-0005: Comunicação entre módulos exclusivamente via Contracts

**Status:** Aceito

**Contexto:** Módulos independentes precisam colaborar sem acessar Infrastructure, Repositories, DbContext ou entidades internas de outro módulo, sob risco de recriar um monolito acoplado.

**Decisão:** Toda comunicação entre módulos ocorre exclusivamente através de Contracts expostos em BuildingBlocks, nunca por acesso direto a camadas internas de outro módulo (ver ARCHITECTURE.md §9).

**Consequências:**
- Módulos podem evoluir e até ser extraídos para serviços separados sem quebrar consumidores.
- Contracts tornam-se superfície pública estável, exigindo cuidado ao alterá-los.
- Pode exigir duplicação controlada de DTOs entre módulos para evitar acoplamento.

---

## ADR-0006: Módulo Documentation implementado sobre a estrutura Core/Infrastructure atual, com pontos de extensão não disruptivos para a arquitetura alvo

**Status:** Aceito

**Contexto:** A Sprint A7 exige um sistema de documentação do BlueprintOS (estrutura de documentos, versionamento, changelog, ADRs, geração de documentação técnica/funcional/IA/desenvolvedor, diagramas Mermaid, sincronização/detecção de documentação desatualizada, integração com Git e um ponto de extensão para memória). Por decisão explícita do Product Owner, esta sprint não deve migrar o backend para a estrutura `Modules/` descrita em ARCHITECTURE.md (ainda não adotada por nenhum módulo existente), nem implementar frontend.

**Decisão:** Implementar o módulo `Documentation` seguindo exatamente o padrão já estabelecido pelo módulo `Knowledge` (`BlueprintOS.Core/Documentation/{Contracts,Models}` + `BlueprintOS.Infrastructure/Documentation/...`, registrado via `AddInfrastructure` em `ServiceCollectionExtensions.cs`, com `IOptions<DocumentationOptions>` para as configurações de persistência de ADRs). Como ponto de extensão pensando na futura migração para a arquitetura alvo (`Modules/Documentation/{Domain,Application,Infrastructure,Api}`), todos os contratos foram desenhados como interfaces coesas e de responsabilidade única em `Core.Documentation.Contracts`, sem dependência de tipos concretos de Infrastructure, de forma que possam ser realocados para `Modules/Documentation/Application` e `Modules/Documentation/Domain` sem alteração de assinatura quando a migração ocorrer. A integração com um módulo de Memória genérico foi deixada como ponto de extensão explícito via `IDocumentationMemoryNotifier`, com implementação no-op/log (`NoOpDocumentationMemoryNotifier`), já que hoje o BlueprintOS possui apenas memória específica de negociação (`INegotiationMemory`).

**Consequências:**
- O módulo Documentation fica imediatamente consistente com o restante do backend (mesmo padrão do Knowledge), sem exigir revisão arquitetural adicional nesta sprint.
- A migração futura para `Modules/` (ADR futura, quando ocorrer) poderá mover os arquivos de `Core.Documentation`/`Infrastructure.Documentation` com baixo retrabalho, pois os contratos já são desacoplados de detalhes de Infrastructure.
- A integração com Memória permanece incompleta (apenas no-op/log) até que um módulo de Memória genérico exista — registrado como dívida técnica em `.ai/memory/known_issues.md`.
- A persistência de `DocumentationEntry`, versões e changelog permanece em memória (não durável), adequado ao escopo desta sprint; persistência durável (arquivo ou banco) pode ser tratada em sprint futura sem alterar os contratos públicos.

---

## ADR-0007: Publication Engine gera documentos profissionais (HTML/PDF/Markdown) a partir de um modelo comum estruturado (ViewModel), reaproveitando os geradores do Portal de Documentação Viva e usando QuestPDF para PDF sem conversão de HTML

**Status:** Aceito

**Contexto:** A Sprint A9 exige um Publication Engine que gere, automaticamente e sem edição manual, três documentos de apresentação profissional (Relatório Executivo, Guia do Cliente, Guia de Engenharia) em `dist/{executive,client,engineering}/`, cada um em Markdown, HTML e PDF, com aparência moderna, capa, índice, cabeçalho, rodapé, tabelas e indicadores — e sempre a partir de dados reais do repositório, nunca fabricados. O repositório já possuía, da Sprint A8, 19 geradores de documentação (`Core.Documentation.Contracts.{Client,Engineering,Executive}`) que produzem Markdown a partir de fontes reais (`.ai/ROADMAP.md`, `.ai/memory/completed_sprints.md`, `.ai/memory/known_issues.md`, `.ai/DECISIONS.md`). Uma primeira versão desta sprint gerou o PDF via conversão do HTML (usando `Markdig` para Markdown→HTML e um parser de blocos Markdown à parte para o PDF); essa abordagem foi revisada por decisão explícita do Product Owner, que exigiu um único modelo estruturado comum como fonte de todos os formatos, sem conversão HTML→PDF e sem duplicação de lógica de interpretação de conteúdo entre renderizadores.

**Decisão:** Criar o módulo `Publication` (`Core.Publication.{Contracts,Models}` + `Infrastructure.Publication.{Content,Rendering,Publishers}`) em torno de um modelo comum (ViewModel) estruturado: `PublicationDocument` → `PublicationSection` → `ContentBlock` (`Heading`, `Paragraph`, `BulletList`, `Table`, `CodeBlock`) + `InlineSpan` (`Plain`, `Bold`, `Code`) para ênfase textual. O Markdown bruto retornado pelos 19 geradores existentes do módulo `Documentation` é convertido para `ContentBlock`s **uma única vez**, no momento em que cada `IReportPublisher` (`ExecutivePublisher`, `ClientPublisher`, `EngineeringPublisher`) monta o `PublicationDocument`, via `MarkdownContentParser` (`Infrastructure.Publication.Content`). A partir desse ponto, nenhum renderizador volta a interpretar texto: os três `IContentRenderer` (`MarkdownRenderer`, `HtmlRenderer`, `PdfRenderer`) consomem exatamente a mesma sequência de `ContentBlock`s e a mesma decomposição de `InlineSpan` (via `InlineSpanParser`, compartilhado entre HTML e PDF). `HtmlRenderer` escreve HTML diretamente a partir dos blocos (`ContentBlockHtmlWriter`), sem depender de nenhuma biblioteca de conversão Markdown→HTML (o pacote `Markdig`, usado na primeira versão, foi removido). `PdfRenderer` usa `QuestPDF` (licença Community, biblioteca .NET pura, sem Chromium/PuppeteerSharp e sem downloads em runtime) para desenhar os mesmos blocos diretamente com a Fluent API — não há, em nenhum momento, conversão de HTML para PDF. `MarkdownRenderer` serializa os blocos de volta para Markdown (round-trip), preservando a saída para versionamento no Git. Indicadores de build/testes exibidos no Relatório Executivo são coletados em tempo real por `QualityMetricsProvider` (executa `dotnet build` e conta `[Fact]`/`[Theory]` nos projetos de teste — nunca valores fabricados). O ponto único de entrada é `dotnet run -- publish` (tratado no início do `Program.cs` da API antes da inicialização do host web), resolvendo a raiz do repositório via `.git`.

**Consequências:**
- Existe uma única fonte de verdade de conteúdo por documento (`IReadOnlyList<ContentBlock>`); nenhuma lógica de interpretação de Markdown/ênfase é duplicada entre os três renderizadores. Novos formatos (Word, PowerPoint, site estático) podem ser adicionados implementando apenas `IContentRenderer` sobre o mesmo `PublicationDocument`/`ContentBlock`, sem qualquer alteração nos `IReportPublisher` — a lista de renderizadores é injetada via `IEnumerable<IContentRenderer>` e resolvida por DI.
- Uma única dependência de terceiro foi introduzida (`QuestPDF`); é uma biblioteca .NET pura, sem necessidade de binários externos ou acesso à rede em runtime, preservando builds 100% offline. `Markdig` foi avaliado e descartado nesta revisão, pois delegaria a interpretação de conteúdo do HTML a uma biblioteca externa desalinhada com o modelo comum usado pelo PDF.
- `QuestPDF` está sob licença Community (gratuita para empresas com receita anual abaixo de 1M USD); caso o BlueprintOS ultrapasse esse limite, será necessário adquirir uma licença comercial.
- HTML e PDF são gerados de forma independente a partir do mesmo `ContentBlock`/`InlineSpan`, preservando a mesma identidade visual (títulos, negrito, código, listas, tabelas) sem que um dependa do outro; ainda assim, não há garantia de fidelidade pixel-a-pixel de layout entre os dois formatos, pois cada um usa seu próprio motor de composição visual (HTML+CSS vs. QuestPDF).
- `dist/` é tratado como artefato gerado (adicionado ao `.gitignore`), assim como `bin/`/`obj/`; apenas `docs/` (Sprint A8) permanece versionado no Git, conforme reafirmado nesta sprint.

---

## ADR-0008: PublicationDocument evolui para um modelo rico (Metadata, Assets, Appendix, Theme), com pontos de extensão para recursos futuros sem refatoração

**Status:** Aceito

**Contexto:** Após a ADR-0007 estabelecer o modelo comum `PublicationDocument`/`ContentBlock`, o Product Owner solicitou que o modelo evoluísse para suportar documentos ricos: metadados completos (autor, empresa, classificação, tags, histórico de revisões), ativos visuais nativos (imagens, logos, ícones SVG, gráficos de KPI, diagramas Mermaid, anexos, QR Codes, selos de build/testes/cobertura) e identidade visual por tipo de documento — preparando a arquitetura para uma longa lista de recursos futuros (timeline de roadmap, gráficos de evolução de sprints/cobertura/dívida técnica, organogramas, BPMN, C4, assinatura eletrônica, numeração automática de figuras/tabelas, glossário, marca d'água, exportação para DOCX/PPTX/site estático, entre outros) sem exigir refatoração significativa quando esses recursos forem implementados.

**Decisão:** `PublicationDocument` passa a ser composto por `Metadata` (`PublicationMetadata`: título, subtítulo, público-alvo, versão, datas de geração/atualização, autor, empresa, classificação, tags e histórico de revisões via `PublicationRevision`), `Sections` (inalterado desde a ADR-0007), `Assets` (`PublicationAssets`: oito coleções independentes — `Images`, `Logos`, `Icons`, `Charts`, `Mermaid`, `Attachments`, `QrCodes`, `Badges` — cada uma com seu próprio tipo em `Core.Publication.Models.Assets`), `Appendix` (reaproveita o mesmo tipo `PublicationSection` de `Sections`, exibido após o corpo principal) e `Theme` (`PublicationTheme`: paleta de cores por tipo de documento via os factory methods `ForExecutive()`/`ForClient()`/`ForEngineering()`, mais cabeçalho/rodapé customizáveis). `ContentBlock` ganha um novo `Kind` (`Image`), que referencia um asset por `AssetId` através de `PublicationAssets.FindEmbeddableImage`, e um `Caption` opcional (ponto de extensão para numeração automática de figuras/tabelas). Os três `IContentRenderer` continuam consumindo exatamente o mesmo `PublicationDocument` — nenhum foi bifurcado por formato. Suporte nativo (funcional, não apenas modelado) foi implementado para: imagens/logos/ícones embutidos (Markdown via data URI base64, HTML via `<img>`/SVG inline, PDF via `Image()` do QuestPDF), anexos (copiados para `dist/{categoria}/attachments/` e referenciados por link, não embutidos), QR Codes (gerados em tempo real por `QrCodeImageGenerator`, usando `QRCoder`/`PngByteQRCode` — sem `System.Drawing.Common`, portanto sem dependência nativa por SO — sempre apontando para conteúdo real, como a URL do repositório) e selos de build/testes/warnings (`BadgeAsset`, renderizados localmente sem chamada a serviços externos como shields.io, populados a partir de `QualityMetrics` real no `ExecutivePublisher`). Gráficos (KPI) e diagramas Mermaid ganharam o modelo de dados completo (`ChartAsset`/`ChartDataPoint`, `MermaidAsset`) mas **não** um motor de renderização visual completo nesta sprint: na ausência de `RenderedImageBytes`, o Mermaid é exibido como bloco de código-fonte (honesto, sem fabricar uma imagem); nenhum publicador popula `Charts` ainda, por não haver fonte de dados real para um gráfico de KPI hoje.

**Consequências:**
- Todos os recursos futuros listados pelo Product Owner (timeline de roadmap, gráficos de evolução, organogramas, BPMN, C4, fluxos de agentes/integração, diagramas de banco, capturas automáticas) são expressáveis com os tipos de asset já existentes (`ChartAsset` para qualquer gráfico com pontos `(rótulo, valor)`; `MermaidAsset`/`ImageAsset` para qualquer diagrama) — não é esperado que a adição desses recursos exija novos tipos no modelo, apenas novos geradores de conteúdo e (quando aplicável) um pipeline de rasterização.
- Glossário, lista de acrônimos e histórico de versões/controle de revisão não exigem modelo novo: reaproveitam `Appendix` (mesmo tipo de `Sections`) e `PublicationMetadata.RevisionHistory`, já implementado nesta sprint (ver seção "Histórico de Versões" do Relatório Executivo).
- Marca d'água (Draft/Internal/Confidential) tem seu gatilho de dados pronto (`PublicationMetadata.Classification`), mas a renderização visual da marca d'água em si não foi implementada nesta sprint.
- Assinatura eletrônica, organogramas/BPMN/C4 "prontos" (com layout automático), captura automática de tela da aplicação, exportação para DOCX/PPTX/site estático e numeração automática de figuras/tabelas continuam **não implementados** — apenas os pontos de extensão (`Caption`, `IContentRenderer`, `ImageAssetKind.Screenshot`) existem para que sejam adicionados depois.
- Duas novas dependências de terceiros foram adicionadas: `QRCoder` (usada apenas via `PngByteQRCode`, evitando a dependência transitiva `System.Drawing.Common` em runtime) e, transitivamente, `Microsoft.Win32.SystemEvents`/`System.Drawing.Common` (não referenciadas diretamente pelo código do Publication Engine).
- `PublicationDocument` teve seus construtores existentes alterados (breaking change interno): `Title`/`Subtitle`/`ProjectVersion`/`GeneratedAt` foram movidos para dentro de `Metadata`; todos os publicadores e testes foram atualizados nesta mesma sprint.

---

## ADR-0009: Estrutura oficial de diretórios da documentação publicada é `docs/{executive,client,engineering,assets}`, não `docs/{architecture,api,adr}`

**Status:** Aceito

**Contexto:** A homologação final da Sprint A7 (documentação) identificou que `docs/architecture/`, `docs/api/` e `docs/adr/` existiam no repositório como pastas vazias, enquanto o Publication Engine e o Portal de Documentação Viva já publicam Architecture, API e ADR Index em `docs/engineering/Architecture.md`, `docs/engineering/APIs.md` / `docs/client/API.md` e `docs/engineering/Decisions.md` respectivamente — nenhum gerador ou publicador jamais escreveu nas três pastas vazias. `IAdrService`/`MarkdownAdrService` (que persistiria ADRs individuais em `docs/adr/ADR-{id}.md`) existe como contrato e implementação, mas não é chamado por nenhum ponto de entrada do CLI (`publish`, `publish-docs`, `publish-executive-blueprint`); o log de ADRs vigente é `.ai/DECISIONS.md`, consumido por `DecisionsGenerator`.

**Decisão:** A estrutura oficial de diretórios de documentação publicada (versionada em Git) é `docs/{executive,client,engineering,assets}`, organizada por público-alvo (Diretoria, Cliente, Desenvolvedores) e não por tipo de conteúdo. Architecture, API e ADR Index são seções dentro de `docs/engineering/` (e, quando aplicável ao público Cliente, também em `docs/client/`), não diretórios próprios de topo. As pastas `docs/architecture/`, `docs/api/` e `docs/adr/` são removidas por serem scaffolding não adotado pelo pipeline real — nenhum Publisher, Generator ou Pipeline foi alterado para justificar essa remoção; ela apenas reconhece formalmente a organização já em produção desde a ADR-0007/ADR-0008. `MarkdownAdrService`/`IAdrService` permanece implementado e testado como ponto de extensão para o dia em que ADRs passarem a ser persistidas também como arquivos individuais, mas não é invocado no fluxo atual — registrado aqui para não ser confundido com código morto em revisões futuras.

**Consequências:**
- Quem procurar por `docs/architecture/`, `docs/api/` ou `docs/adr/` deve procurar em `docs/engineering/` e `docs/client/`, conforme documentado em `docs/INDEX.md`.
- Nenhum diretório vazio permanece na estrutura oficial de `docs/`.
- Caso um dia se decida persistir ADRs individuais via `MarkdownAdrService`, basta invocar `IAdrService` a partir de um comando do CLI existente (ou um novo) — nenhuma mudança de contrato é necessária.
