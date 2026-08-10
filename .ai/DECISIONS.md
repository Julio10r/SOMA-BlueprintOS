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

---

## ADR-0011: Identidade temporária de desenvolvimento para antecipar a persistência de fornecedores

**Status:** Aceito

**Contexto:** O +COMPRAS precisa evoluir do slice transitório para cadastro persistente de fornecedores antes da disponibilidade corporativa do Microsoft Entra ID. Adiar toda persistência até H1/H2 impediria validar o próximo fluxo de negócio; porém, tratar uma identidade provisória como mecanismo de produção criaria risco de segurança e retrabalho.

**Decisão:** As próximas entregas de negócio podem usar um adaptador de identidade temporária, configurado exclusivamente para o ambiente `Development`. O adaptador fornecerá um identificador estável de usuário e um perfil mínimo por requisição; registros persistidos de fornecedor deverão manter o vínculo de autoria/responsabilidade com esse identificador temporário. A camada de negócio dependerá somente de um contrato de identidade, para que o adaptador seja substituído pelo Entra ID sem alterar entidades, casos de uso ou contratos de Procurement. Em ambiente diferente de `Development`, a aplicação não poderá iniciar com esse adaptador. A implantação produtiva continua bloqueada até H1/H2.

**Consequências:**
- B1 pode implementar persistência de fornecedor e autoria temporária sem aguardar o tenant corporativo.
- A Work Order de B1 deverá definir o contrato, o formato de configuração e os perfis mínimos; ela não pode expor headers temporários como autenticação de produção.
- Entra ID e segregação de funções permanecem entregas obrigatórias antes de qualquer uso produtivo, integração corporativa ou exposição externa.
- Dados persistidos deverão suportar a migração do identificador temporário para o identificador corporativo, com plano de migração definido em H1.

---

## ADR-0012: Persistência de fornecedores isolada por repositório e identidade abstrata

**Status:** Aceito

**Contexto:** B1 introduz o primeiro dado de negócio durável antes do Microsoft Entra ID. A persistência não pode vazar detalhes de EF Core, SQL Server ou da identidade temporária para regras de negócio.

**Decisão:** O agregado `Fornecedor` permanece no Domain; Application depende de `IFornecedorRepository` e `ICurrentIdentity`; Infrastructure implementa o repositório com EF Core/SQL Server e mantém `TemporaryUserId` em cada registro. Consultas sempre recebem o identificador atual e são filtradas por ele. CNPJ possui índice único e é normalizado por value object. A ConnectionString `MaisComprasConnection` é a única usada pelo DbContext e pelas migrations; `ErpConnection` permanece isolada, sem acesso nesta sprint.

**Consequências:** A troca do adaptador de Development por Entra ID não altera entidades, contratos ou casos de uso. A migration inicial estabelece os índices de CNPJ, nome e identidade temporária. A13 não foi alterada e sua lógica de IA continua consultiva.

---

## ADR-0013: Estratégia de Evolução Incremental da Plataforma Operacional e Inteligente do +Compras

**Status:** Aceito

**Data:** 31/07/2026

**Contexto:** O roadmap inicial antecipava capacidades inteligentes antes de uma base operacional completa de fornecedores, itens e pedidos. O ERP mantém os cadastros corporativos, mas não possui os relacionamentos fornecedor × item, família e categoria necessários ao mecanismo completo de descoberta. Esses relacionamentos, assim como dados próprios de operação, precisam existir no +Compras; o histórico de compras será obtido progressivamente pelos pedidos do ERP. O produto deve entregar valor e manter operações críticas mesmo quando o modelo, o provedor de IA ou um agente estiver indisponível.

**Opções consideradas:**

1. Priorizar inteligência avançada antes dos fluxos operacionais — descartada por depender de dados inexistentes e tornar a operação crítica dependente de IA.
2. Construir a plataforma operacional primeiro e evoluir a inteligência sobre dados reais — adotada.

**Decisão:** O +Compras será construído inicialmente como uma plataforma operacional completa, composta pelo portal web, APIs, banco próprio, integrações ERP, módulos operacionais, workflows, auditoria e agentes do SOMA BlueprintOS. O portal é a interface do próprio +Compras, não um produto ou módulo separado.

Fornecedores, itens e pedidos terão fluxos básicos completos antes da inteligência avançada. Agentes atuam inicialmente como operadores assistidos: interpretam solicitações, consultam dados, preenchem informações, apresentam opções, criam rascunhos e executam somente operações confirmadas pelo usuário. Decisões críticas exigem confirmação humana.

Toda funcionalidade inteligente deve possuir alternativa operacional manual equivalente. A indisponibilidade de IA não pode impedir cadastrar ou selecionar fornecedor e item, criar pedido, enviá-lo ao ERP ou acompanhar a integração. Agentes usam contratos e casos de uso da Application; não acessam diretamente banco ou ERP.

Cada BU pode possuir um ERP distinto. Integrações permanecem desacopladas por adaptadores vinculados à BU. O banco +Compras armazena dados da aplicação e relacionamentos ausentes no ERP; capacidades inteligentes são acrescentadas progressivamente sobre dados operacionais reais.

**Fontes de verdade iniciais:**

| Sistema | Fonte de verdade |
|---|---|
| ERP | códigos externos e cadastro corporativo de fornecedor/item, pedidos efetivamente registrados, dados fiscais e transacionais oficiais |
| +Compras | sites e canais comerciais, catálogos, relacionamentos fornecedor × item/família/categoria, scores, recomendações, solicitações e rascunhos, contexto conversacional, evidências de agentes, status de integração, auditoria e decisões assistidas |

Essa divisão será refinada por ADRs futuras conforme os módulos forem implementados.

**Consequências positivas:** entrega antecipada de valor, homologação progressiva com compradores, geração de dados reais para inteligência, menor dependência de IA, testes mais objetivos, isolamento de falhas entre portal, aplicação, banco e ERP, e continuidade operacional sem IA.

**Trade-offs:** inteligência avançada é postergada; CRUDs e fluxos operacionais precedem automações; estruturas de B2 permanecem iniciais até haver dados reais; modelos podem ser ajustados quando Compras for detalhado; haverá duplicidade controlada e será necessário definir a fonte de verdade de cada campo.

**Ações posteriores:** reorganizar Work Orders futuras para fornecedores, itens, pedidos, portal integrado e integrações; validar B2.1 em ambiente com acesso ao ERP; não iniciar B3 sem aprovação.

---

## ADR-0014: Estratégia de LLM para Desenvolvimento e Produção

**Status:** Aceito

**Data:** 31/07/2026

**Contexto:** O +Compras utilizará agentes de IA em diversos módulos. Depender de APIs pagas ou de um fornecedor específico durante o desenvolvimento aumenta custos, dificulta testes locais e cria lock-in. Em homologação e produção, o consumo de IA será padronizado e governado pela Infraestrutura/Arquitetura Corporativa, cuja plataforma pode mudar ao longo do tempo.

**Opções consideradas:**

1. Acoplar a aplicação a um fornecedor de IA — descartada, pois exige alteração de regras de negócio a cada troca de fornecedor e contraria Clean Architecture.
2. Consumir modelos por contratos de aplicação e adaptadores de infraestrutura configurados por ambiente — adotada.

**Decisão:** Toda comunicação com LLMs ocorre exclusivamente pelos contratos existentes `IAIProvider` e `IAIRuntime`. O runtime seleciona um adaptador pelo identificador de provedor presente no modelo solicitado. Domain, Application, agentes e regras de negócio não podem conhecer SDKs, APIs, tipos ou credenciais de OpenAI, Azure OpenAI, Claude, Gemini, Llama, Qwen, Mistral, DeepSeek ou qualquer outro fornecedor. Implementações específicas pertencem somente à Infrastructure e são registradas por injeção de dependência.

**Estratégia por ambiente:**

| Ambiente | Estratégia |
|---|---|
| Desenvolvimento | Ollama local é o padrão arquitetural. Usar o menor modelo que atenda aos testes funcionais, com preferência inicial por modelos de 3B a 4B parâmetros, para validar agentes, prompts, memória, ferramentas, orquestração e fluxos. |
| Homologação | Usar preferencialmente a plataforma corporativa disponibilizada pela Infraestrutura. Na ausência dela, permitir provedor compatível temporário, configurável e restrito à validação. |
| Produção | O +Compras não escolhe o fornecedor. Consome exclusivamente a plataforma corporativa definida pela Infraestrutura/Arquitetura Corporativa por configuração e adaptador. |

O adaptador `OpenAIProvider` existente é uma implementação de Infrastructure preservada por compatibilidade; esta ADR não altera seu comportamento nem configura Ollama automaticamente. A adoção do adaptador local e da seleção por ambiente requer Work Order de implementação aprovada.

**Consequências positivas:** desenvolvimento local de baixo custo, testes mais acessíveis, ausência de lock-in na aplicação, troca de fornecedor sem mudança no domínio e maior aderência à governança corporativa.

**Trade-offs:** adaptadores precisam manter paridade de contratos e capacidades; qualidade do modelo local pode ser inferior; configuração, telemetria, credenciais e limites de cada ambiente exigirão uma Work Order futura.

**Regras:** nenhum código fora de Infrastructure acessa API de IA diretamente; nenhum adaptador é assumido pela regra de negócio; a troca de fornecedor não altera o Domain; e integrações devem permanecer Ports & Adapters, configuradas por ambiente.

---

## ADR-0015: Contrato canônico e sincronização bidirecional de fornecedores

**Status:** Aceito para a reabertura da B2.1

**Data:** 01/08/2026

**Contexto:** A primeira entrega da B2.1 sincronizava somente um subconjunto corporativo e não representava a atualização, inativação, conflito temporal e auditoria exigidos pelo contrato operacional. A procedure `LX_AZZ_GERAR_FORNECEDOR_LINX` é apenas referência funcional; não pode ser dependência da aplicação.

**Decisão:** O Domain mantém `FornecedorCanonico` sem nomes físicos de ERP. A Application usa `IIntegracaoFornecedorErp`/`IErpFornecedorAdapter` e resolve o adaptador por BU; tabelas, procedures, connection strings e regras físicas ficam exclusivamente na Infrastructure. O modelo canônico cobre identificação, endereço, contatos, fiscal, bancário, comercial, classificação, categorias e indicadores de fornecimento.

**Regra temporal:** timestamps são normalizados para `America/Sao_Paulo` e comparados até o segundo. Registro ERP mais recente atualiza o +Compras; registro +Compras mais recente atualiza o ERP; empate com dados divergentes favorece o +Compras; empate com dados iguais não altera nenhum sistema.

**Auditoria e idempotência:** cada operação gera evento imutável em `FornecedoresSincronizacoes`, com origem/destino, timestamps originais e normalizados, decisão, antes/depois, hashes, tentativa, duração, erro sanitizado e `CorrelationId`. Reexecuções sem mudança não repetem escrita nem alteram o timestamp; consultas e nenhuma alteração continuam auditáveis.

**Consequências:** o +Compras passa a preservar campos exclusivos e o vínculo externo por BU/ERP/ID; fornecedores são inativados logicamente e nunca removidos pela sincronização. A migration complementar altera somente o banco +Compras. O adaptador SOMA_DESENV traduz apenas os campos que o ERP suporta e preserva chaves protegidas por FK.

### Complemento ADR-0015: Sequencial Linx e timestamp efetivo do fornecedor

**Status:** Aceito para a validação operacional final da B2.1

**Data:** 01/08/2026

**Decisão:** A criação no adaptador Linx usa exclusivamente `LX_SEQUENCIAL` para `FORNECEDORES.CLIFOR`, com `@EMPRESA = 1`, dentro da transação controlada que grava `CADASTRO_CLI_FOR` e `FORNECEDORES`. O domínio e a Application recebem somente o identificador externo retornado. Não são permitidos `MAX + 1`, contador local, valor fixo, prefixo ou reaproveitamento.

O timestamp primário de transferência confirmado no cadastro Linx é `CADASTRO_CLI_FOR.DATA_PARA_TRANSFERENCIA`; `FORNECEDORES.DATA_PARA_TRANSFERENCIA` é consultado como espelho/fallback. Ambos são normalizados para `America/Sao_Paulo`, com precisão até o segundo, e a auditoria preserva os valores original e normalizado. A confirmação remota precede a persistência do vínculo; se a persistência local falhar, a reconciliação consulta o identificador externo já confirmado.

**Consequências:** concorrência depende do mecanismo oficial do ERP e não duplica códigos; falhas não criam vínculo falso; o registro inválido `00000*` permanece preservado e é tratado por inativação lógica; nenhuma transação distribuída entre os bancos é introduzida.

## ADR-0016: Modelo Canônico de Fornecedor Integrado ao ERP Linx

**Status:** Aceita

**Data:** 01/08/2026

**Contexto:** A B2.1.2 iniciou o diagnóstico estrutural entre ERP Linx e +Compras para alinhar tipos, tamanhos, nulabilidade, collation, validações e limitações operacionais antes de qualquer migration. O ERP Linx possui o cadastro mestre em `CADASTRO_CLI_FOR`; um mesmo registro pode representar fornecedor, cliente ou filial. A tabela `FORNECEDORES` é uma extensão desse cadastro mestre para o papel de fornecedor, enquanto o +Compras trabalha inicialmente somente com fornecedores.

No Linx, `CADASTRO_CLI_FOR.NOME_CLIFOR`, `FORNECEDORES.FORNECEDOR` e o conceito de nome fantasia representam a mesma chave operacional. Esse campo é protegido por regra operacional/FK e não pode ser alterado livremente pelo +Compras.

**Decisão:** O modelo canônico de fornecedor integrado ao ERP Linx deve refletir a separação estrutural do Linx entre cadastro mestre e extensão de fornecedor.

1. Documento fiscal

O conceito atual `Cnpj` deve evoluir para `Cnpj_Cpf`, mantendo compatibilidade com `CGC_CPF` do Linx:

```text
Cnpj_Cpf varchar(14)
TipoPessoa varchar(20)
```

Regras aprovadas:
- aceitar CPF e CNPJ;
- permitir caracteres alfanuméricos no banco;
- não restringir somente números na persistência;
- manter validações de formato na API e no frontend;
- usar `TipoPessoa` para distinguir `PJ` e `PF`.

Exemplos:

```text
Cnpj_Cpf = 10285590000108
TipoPessoa = PJ
```

```text
Cnpj_Cpf = 12345678901
TipoPessoa = PF
```

2. Modelo de nomes

A separação de nomes deve ser explícita:

| Origem Linx | Campo canônico +Compras |
|---|---|
| `RAZAO_SOCIAL` | `RazaoSocial` |
| `NOME_CLIFOR` / `FORNECEDORES.FORNECEDOR` | `NomeFantasia` |

O conceito `NomeOperacionalERP` não deve ser criado. `NomeFantasia` é controlado pelo ERP Linx. O fluxo permitido para alteração desse campo é somente `ERP -> +Compras`; o fluxo `+Compras -> ERP` não é permitido para nome fantasia. Alterações de nome fantasia só podem ser aplicadas no +Compras quando originadas no ERP.

3. Domínios e tabelas FK

Campos controlados por FK ou domínio no Linx não devem permanecer como texto livre no +Compras. Devem ser modeladas estruturas equivalentes de domínio sincronizadas a partir do ERP, por Business Unit e sistema ERP.

Exemplos iniciais:
- `TipoFornecedor`;
- `SubtipoFornecedor`;
- `CondicaoPagamento`;
- demais domínios identificados na continuidade do levantamento estrutural.

Modelo aprovado:

```text
ERP Linx
    |
    | sincronização
    v
Tabela domínio +Compras
    |
    | FK
    v
Fornecedor
```

Cada tabela de domínio deverá possuir, no mínimo:
- `Id`;
- `CodigoERP`;
- `Descricao`;
- `BusinessUnit`;
- `ErpSistema`;
- `Status`.

**Consequências:** O modelo atual de fornecedor precisará ser ajustado em sprint posterior para suportar CPF/CNPJ, separar razão social de nome fantasia e substituir textos livres por domínios sincronizados do Linx. Essas mudanças não são executadas na etapa diagnóstica B2.1.2; elas serão planejadas como migrations, ajustes de contrato API e validações de frontend após a conclusão do levantamento estrutural. A regra preserva o Linx como fonte de verdade para `NomeFantasia` e para domínios corporativos, reduzindo risco de rejeição em exportações e divergência operacional.

---

## ADR-0017: Estratégia de Construção do Portal Operacional +Compras

**Status:** Aceita

**Data:** 01/08/2026

**Contexto:** O +Compras evolui como uma plataforma operacional integrada aos ERPs das Business Units. O primeiro domínio com integração real é Fornecedores, consolidado nas sprints B2.1 e B2.1.1 e em evolução estrutural na B2.1.2. Construir telas isoladas por sprint fragmentaria a experiência, enquanto tratar módulos ainda não implementados como funcionais criaria uma expectativa incorreta.

**Opções consideradas:**

1. Construir apenas telas isoladas à medida que cada módulo fosse implementado.
2. Construir um portal completo de navegação e identidade visual desde a primeira versão, evoluindo a capacidade funcional de cada módulo conforme o roadmap.

**Decisão:** A segunda opção foi adotada. O frontend será um Portal Operacional +Compras, com estrutura de navegação, identidade visual e módulos previstos pelo produto desde a primeira versão visual. A presença de um módulo no portal não comprova funcionalidade: cada módulo deve apresentar um estado explícito — `🟢 Funcional`, `🟡 Estrutura visual` ou `⚪ Planejado`.

**Mapa oficial do portal:**

```text
+Compras
├── Dashboard
├── Fornecedores
│   ├── Lista
│   ├── Cadastro
│   ├── Detalhes
│   ├── Sincronização ERP
│   └── Auditoria
├── Pedidos
├── Cotações
├── Negociações
├── Contratos
├── Indicadores
└── Agentes IA
```

**Primeira vertical slice funcional:** Fornecedores. Ela reúne consulta, cadastro, edição, detalhes, sincronização ERP, histórico e auditoria e deve consumir os contratos oficiais do backend. Sua evolução acompanha B2.1, B2.1.1, B2.1.2 e B2.2; a ADR não declara que toda essa interface já está implementada.

**Regras arquiteturais:**

- O frontend consome apenas APIs e DTOs oficiais; regras de negócio e regras de integração permanecem no backend.
- Cada domínio evolui no fluxo `Backend → contrato de API → frontend → experiência operacional`.
- O portal utiliza o [AZZAS 2154 — GDT Design System](../docs/design-system/README.md); componentes e linguagem visual devem consultar `docs/design-system/` antes de implementação.
- Autenticação corporativa futura segue Microsoft Entra ID e não é simulada como controle de acesso definitivo no portal.
- Toda implementação frontend deve ler `.ai/PROJECT.md`, `.ai/ARCHITECTURE.md`, `.ai/DECISIONS.md`, `.ai/CURRENT_SPRINT.md`, `docs/design-system/` e `docs/engineering/`.

**Consequências:** A navegação e a linguagem visual passam a ser planejadas como produto único, enquanto a entrega funcional continua incremental e verificável por domínio. Pedidos, Cotações, Negociações, Contratos e Indicadores terão inicialmente somente estrutura visual; Agentes IA permanece planejado. O Dashboard será a página inicial para visão executiva, indicadores, integrações, alertas e atividades recentes, sem substituir os módulos operacionais. Nenhum código é criado ou alterado por esta ADR.

---

## ADR-0018: Ambiente de execução do Portal +Compras é Desenvolvimento Local (Mac)

**Status:** Aceito

**Registro:** Desenvolvimento local definido como padrão. Tentativa de publicação n8n descartada como estratégia de desenvolvimento.

**Contexto:** Uma tentativa inicial de publicar o frontend do Portal +Compras como demo pública usou o n8n como servidor de HTML estático (via webhook), com o backend exposto temporariamente por túnel ngrok. Essa estratégia esbarrou em limitações reais: o n8n só serve HTML como string única (sem suporte nativo a uma pasta `dist/` com múltiplos assets), o backend não tinha nenhum ambiente publicado além de localhost, e o túnel ngrok é temporário e inadequado para o ciclo de desenvolvimento corrente. Diante disso, foi decidido tratar o ambiente atual do projeto como Desenvolvimento Local, adiando a publicação externa.

**Decisão:** Desenvolvimento ocorre localmente no Mac com frontend React e API .NET. Persistência utiliza SQL Server corporativo acessível via VPN. Homologação futura será realizada em Windows Server/IIS.

Detalhamento:
- Frontend: React + TypeScript via Vite, `npm run dev`, URL padrão `http://localhost:5173`.
- Backend: API .NET executando localmente via `dotnet run` (perfil `http` do `launchSettings.json`, porta `5262`).
- Dados: o banco oficial de desenvolvimento é o SQL Server corporativo (ambiente `SOMA_DESENV`), acessado via VPN. Connection strings permanecem configuráveis via user-secrets/variáveis de ambiente (`ConnectionStrings:MaisComprasConnection`, `ConnectionStrings:ErpConnection`), sem valores hardcoded no repositório.
- CORS do backend liberado apenas para as origens de desenvolvimento local: `http://localhost:5173` e `http://127.0.0.1:5173`.
- Publicação via n8n/GCP passa a ser tratada como opção futura de homologação/demonstração, não como ambiente corrente.

**Atualização (03/08/2026):** Docker foi removido do fluxo de desenvolvimento (commits `601d937`, `7bf3bf4`). `Makefile`, `Dockerfile` e `docker-compose.yml` foram descontinuados; scripts locais (`start-dev.sh`, `stop-dev.sh`, `health-check.sh`) passam a orquestrar backend e frontend. O ambiente oficial de Desenvolvimento Local é, a partir desta data, 100% sem containers.

**Consequências:**
- Simplifica o ciclo de desenvolvimento: sem dependência de túneis temporários (ngrok) ou de um servidor de HTML estático improvisado (n8n).
- Demonstrações completas (Fornecedores, consulta CNPJ, enriquecimento, aprovação/rejeição) exigem VPN corporativa ativa e o backend rodando localmente — não há URL pública permanente neste momento.
- Homologação/demo formal fica registrada como pendência futura, a ser resolvida com um ambiente Windows Server/IIS dedicado (fora do escopo desta ADR).
- Nenhuma regra de negócio existente foi alterada; apenas configuração de ambiente (portas, CORS, `.env.example`) e remoção de artefatos específicos da tentativa de publicação via n8n.

---

## ADR-0019: `docs/` como fonte canônica única da documentação técnica, organizada por domínio

**Status:** Aceito

**Contexto:** A ADR-0009 definia `docs/{executive,client,engineering,assets}` como estrutura oficial, organizada por público-alvo, publicada automaticamente pelo Portal de Documentação Viva (19 geradores) e pelo Publication Engine. Na prática, essa estrutura resultou em `docs/` sendo simultaneamente fonte autoral (arquivos como `FornecedorErpSynchronization.md`, `Frontend.md`, escritos por humanos) e destino de geração automática (os 19 arquivos com o banner "Não editar manualmente"), sem separação clara entre o que é documentação técnica permanente e o que é saída derivada de `.ai/`. Isso violava o princípio de que documentação técnica não deve duplicar estado operacional volátil, e deixava `dist/` sem função real como saída publicável.

**Decisão:**
- `docs/` passa a ser a **única fonte canônica da documentação técnica** do SOMA BlueprintOS — descreve como o sistema funciona, escrita por humanos (ou por IA em nome de humanos), nunca gerada automaticamente.
- `.ai/` permanece exclusivamente conhecimento operacional da IA — estado, sprint, roadmap, backlog, decisões (ADRs) e memória — nunca copiado em `docs/`.
- `dist/` permanece saída regenerável do Publication Engine — descartável, não versionada, nunca editada manualmente, nunca fonte de verdade.
- `resources/` é a pasta de materiais institucionais e visuais (design system, apresentações) — fora do fluxo de documentação técnica e fora do fluxo operacional da IA.
- A documentação técnica em `docs/` é organizada **por domínio de negócio e capacidade técnica** (`architecture/`, `backend/{procurement,integration,orchestration,shared}`, `frontend/`, `database/`, `agents/`, `operations/`, `testing/`, `releases/`), não por público-alvo nem pela estrutura física do código.
- O Publication Engine deixará de gerar documentação autoral e passará a ter exclusivamente a responsabilidade de **publicar** `docs/` em `dist/` (descoberta de documentos → montagem de índice → renderização), sem lógica por audiência. Essa refatoração **ainda não foi executada** — é o objeto de uma etapa de implementação posterior; até lá, o Publication Engine, `DocumentationPublishService` e os comandos `publish`/`publish-docs`/`publish-executive-blueprint` continuam com o comportamento legado descrito na ADR-0009.
- Esta ADR **substitui a ADR-0009** nas decisões sobre arquitetura documental. A ADR-0009 permanece registrada como decisão histórica, não é reescrita nem removida.

**Consequências:**
- Os 19 documentos gerados por público (`docs/{executive,client,engineering}/*.md` com o banner "Não editar manualmente") foram removidos da árvore versionada; seu conteúdo técnico único foi extraído para os novos documentos por domínio, e o restante era redundante com `.ai/`.
- `docs/executive/BlueprintOS_Executive_Blueprint.{html,pdf}` e `docs/DocumentationHealth.md` continuam temporariamente versionados dentro de `docs/` — são artefatos do pipeline legado (P3/health check) e só migram para `dist/` quando o Publication Engine for refatorado.
- Quem procurar pela estrutura `docs/{executive,client,engineering}` descrita na ADR-0009 deve procurar em `docs/README.md`, que indexa a nova árvore por domínio.
- Nenhum código de negócio, teste ou comportamento do Publication Engine foi alterado por esta ADR — apenas a árvore de documentação e correções pontuais de caminho necessárias pela movimentação de arquivos.

**Atualização (05/08/2026):** o Publication Engine foi refatorado para o novo componente único `DocsPublisher`, que descobre `docs/**/*.md`, publica em `dist/` preservando a estrutura de domínio e gera um índice navegável — sem nenhuma lógica por audiência. `ExecutivePublisher`, `ClientPublisher`, `EngineeringPublisher`, `ExecutiveBlueprintPublisher`, `DocumentationPublishService` e seus 19 geradores foram removidos. Os comandos `publish-docs` e `publish-executive-blueprint` foram descontinuados (retornam erro claro apontando para `publish`). `docs/executive/BlueprintOS_Executive_Blueprint.{html,pdf}` e `docs/DocumentationHealth.md` (artefatos gerados versionados em `docs/`) foram removidos; o relatório de saúde agora é escrito em `dist/health/`. A decisão descrita nesta ADR está integralmente implementada.

---

## ADR-0020: Modelo Administrativo, Cadastros Integrados, Segurança RBAC e Arquitetura Frontend

**Status:** Aceita (atualizada pelas revisões arquiteturais R1.2 e R2)

**Data:** 06/08/2026 (criação — revisão R1.1); atualizada em 06/08/2026 (revisão R1.2); atualizada em 06/08/2026 (revisão R2 — Arquitetura Frontend)

**Contexto:** A revisão arquitetural R1.1 da Onda 1 (`.ai/work-orders/completed/O1.1-ConsolidacaoFuncionalMaisCompras.md`) registrou oito dúvidas de produto sem resposta: o corte entre `Administração`, `Administração do Sistema` e `Configurações` era apenas uma proposta não aprovada; o conceito de "Gestão de Empresas" havia sido usado informalmente para a classificação gerencial de despesa, sem refletir a separação real entre cadastro corporativo (ERP) e classificação operacional (+Compras); Filiais e Centros de Custo, sendo dados integrados do ERP, não tinham regra explícita sobre o que pode ou não ser alterado localmente; não havia modelo aprovado para relacionar Centro de Custo a uma classificação gerencial de despesa; a separação entre o cadastro mestre de Centro de Custo e a autorização de acesso do usuário a Centros de Custo não estava definida; e o modelo de permissões (perfis vs. permissões individuais) não estava decidido, apesar de o próprio texto de `ComprasFuncional.md` já sugerir perfis como unidade de agrupamento.

**Contexto da atualização R1.2:** após a aceitação da ADR-0020 pela R1.1, a revisão arquitetural R1.2 resolveu pendências adicionais deixadas em aberto: nomenclatura definitiva de sub-telas de `Administração`/`Administração do Sistema`; a relação entre os dois vínculos de acesso de um usuário (Perfis e Centros de Custo); o mecanismo de Login da Onda 1 (pendência nº 5 registrada em `ComprasFuncional.md` pela R1.1); a forma de inicializar o sistema antes de existir qualquer Administrador (nenhum mecanismo de "primeiro acesso" estava definido); e a exigência de revisão de segurança para funcionalidades de autenticação, que ainda não estava formalizada como requisito obrigatório do processo. Esta atualização amplia a ADR-0020 original — não a substitui nem cria uma nova ADR.

**Contexto da atualização R2:** após o encerramento da Revisão Arquitetural R1, a revisão R2 definiu a arquitetura oficial do Frontend do +Compras. Até então, a ADR-0017 (Portal Operacional +Compras) definia a navegação e a evolução incremental por domínio, mas não estabelecia um padrão arquitetural de organização física do código React — deixando em aberto se o frontend cresceria por tecnologia (pastas horizontais `pages`/`components`/`hooks`/`services`) ou por domínio de negócio. A R2 aprovou a arquitetura **Vertical Slice** como padrão obrigatório para toda a aplicação React do +Compras, impactando toda a implementação da Onda 1. Esta atualização amplia a ADR-0020 original — não a substitui nem cria uma nova ADR.

**Decisão:**

1. **Organização administrativa.** O corte entre as três áreas do índice fica definido como: **Administração** — Gestão de Unidades de Negócio, Gestão de Unidades de Alocação, Gestão de Filiais, Gestão de Centros de Custo, Gestão de Usuários, Gestão de Perfis, Workflow, Alçadas, Controle Orçamentário, Configuração ERP, Notificações; **Administração do Sistema** — Identity Providers, Feature Flags, Integrações, Monitor, Filas, Reprocessamentos, Auditoria, Logs, Saúde; **Configurações** — Conta, Preferências, Tema, Idioma, Preferências pessoais. Este corte substitui a distribuição provisória registrada na O1.1 (que agrupava Workflow, Aprovação e Controle Orçamentário em `Configurações`); Workflow, Alçadas e Controle Orçamentário passam a `Administração`, e `Configurações` passa a ser exclusivamente preferência pessoal do usuário autenticado, não motor de regra de negócio da Unidade. **Atualização R1.2:** `Notificações de negócio` é renomeada para `Notificações`; `Monitor de filas` (Administração do Sistema) é desmembrada em duas sub-telas distintas, `Monitor` e `Filas`; `Saúde do sistema` é renomeada para `Saúde`. Nenhuma mudança de escopo funcional é implicada por essas correções de nome — apenas alinhamento à nomenclatura oficial aprovada na R1.2.

2. **Cadastros integrados do ERP.** O ERP permanece a fonte canônica de todo dado corporativo sincronizado. Dados sincronizados do ERP são imutáveis no +Compras — nenhuma tela de gestão altera dados mestres de origem ERP. O +Compras pode armazenar apenas metadados locais não pertencentes ao ERP: `DescricaoMaisCompras` (opcional) e `AtivoNoMaisCompras`, além de metadados locais futuros que vierem a ser aprovados. Toda tela de gestão de dado integrado exibe três colunas distintas — código ERP, descrição ERP, descrição +Compras — nunca substituindo ou ocultando a descrição oficial do ERP.

3. **Gestão de Filiais.** Filiais são integradas do ERP e não podem ser criadas ou alteradas no +Compras; só podem ser ativadas/inativadas para uso no +Compras, sem que essa inativação local altere o ERP. `Código CliFor` e `Nome CliFor` são persistidos no banco +Compras por comporem chaves usadas pelo ERP; `DescricaoMaisCompras` é opcional. O nome funcional oficial da tela é **"Gestão de Filiais"** — "Cadastro de Filiais" é nome incorreto e não deve ser usado.

4. **Gestão de Centros de Custo.** Centros de custo são integrados do ERP; seus dados mestres não podem ser alterados no +Compras, apenas ativados/inativados localmente, sem alterar o ERP. `DescricaoMaisCompras` é opcional. O nome funcional oficial da tela é **"Gestão de Centros de Custo"** — "Cadastro de Centros de Custo" é nome incorreto e não deve ser usado.

5. **Unidades de Alocação.** Substituem, formalmente, o conceito anterior e informal de "Gestão de Empresas". Representam a classificação gerencial da despesa para operação, orçamento e relatórios. Tipos iniciais: Marca, Corporativo, Localidade, Outro. Origem: ERP (ex.: tabela Rede de Lojas) ou cadastro local no +Compras. Campos funcionais iniciais: Identificador, Código ERP (quando existir), Descrição ERP, Descrição +Compras (opcional), Tipo, Origem, Ativa no +Compras, Unidade de Negócio. Exemplos: Animale, Farm, Fábula, SOMA Corporativo, Corporativo Jardim Botânico.

6. **Relação Centro de Custo × Unidade de Alocação.** Relação muitos-para-muitos: cada Centro de Custo ativo pode ter uma ou mais Unidades de Alocação permitidas, com possibilidade de uma Unidade de Alocação padrão. Ao selecionar um Centro de Custo em uma requisição, o sistema filtra as Unidades de Alocação disponíveis; se houver apenas uma permitida, ela pode ser preenchida automaticamente; não é permitido selecionar Unidade de Alocação fora do vínculo configurado.

7. **Centros de custo por usuário.** A Gestão de Centros de Custo (cadastro mestre) é separada da autorização de acesso do usuário. Após a integração/configuração dos centros de custo, um usuário pode ter acesso a um, vários, ou a todos os centros de custo ativos. Essa autorização nunca altera o cadastro mestre do Centro de Custo.

8. **Perfis e permissões (RBAC).** O modelo de segurança é RBAC baseado exclusivamente em perfis. Cada Perfil contém nome, descrição, status, Unidade de Negócio (quando aplicável) e lista de permissões. Um usuário pode ter um ou vários perfis; suas permissões efetivas são a união das permissões de todos os perfis vinculados. Regra obrigatória: **usuários nunca recebem permissões individuais ou exceções diretas** — toda necessidade de permissão diferente exige a criação de um novo perfil (ex.: "Analista": criar, aprovar e cancelar pedido; "Analista Jr": somente criar pedido).

**Decisões acrescentadas pela revisão arquitetural R1.2 (data abaixo):**

9. **Usuários — consolidação do vínculo de acesso.** Todo usuário do +Compras carrega dois vínculos de acesso independentes entre si: um ou mais **Perfis** (governam permissões, item 8) e um ou mais **Centros de Custo** (governam o escopo de dados operacionais, item 7). Nenhum dos dois vínculos substitui o outro — um usuário pode ter um Perfil amplo e acesso restrito a poucos Centros de Custo, ou vice-versa. O acesso a **todos** os Centros de Custo ativos é uma opção explícita de configuração por usuário (equivalente a "todos", não a uma listagem manual de todos os códigos existentes), para que a entrada de novos Centros de Custo não exija reconfiguração retroativa de usuários já marcados como "acesso total".

10. **Modelo RBAC — reafirmação sem exceção.** Reafirma-se, sem ambiguidade, a regra já aprovada no item 8: permissões pertencem exclusivamente a Perfis, nunca a usuários individualmente; o conjunto de permissões efetivas de um usuário é sempre a união das permissões de todos os seus Perfis vinculados; e toda exceção de acesso necessária gera obrigatoriamente um novo Perfil — em nenhuma hipótese uma permissão pontual anexada diretamente a um usuário.

11. **Autenticação — Login Passwordless via OTP por e-mail.** O mecanismo de login da Onda 1 é definido: autenticação **passwordless**, por código de verificação (OTP) enviado ao e-mail corporativo, resolvendo a pendência registrada pela O1.1/R1.1 sobre o mecanismo de Login. Regras: (a) o domínio do e-mail informado deve pertencer a um domínio autorizado pela Unidade de Negócio ou pelo Identity Provider configurado; (b) apenas usuário com status Ativo pode concluir a autenticação; (c) a autenticação sempre resolve/confirma o vínculo do usuário com uma Unidade de Negócio antes de liberar a sessão; (d) uma Unidade de Negócio pode ter múltiplos Identity Providers configurados simultaneamente (OTP por e-mail e, quando aprovado, outros); (e) o Microsoft Entra ID é o provedor corporativo definitivo de produção e é projetado para **coexistir** com o OTP por e-mail como Identity Providers alternativos de uma mesma Unidade de Negócio, não para substituí-lo compulsoriamente — a escolha de qual Identity Provider usar em cada login segue a configuração de `Administração do Sistema > Identity Providers`.

12. **Bootstrap Mode.** É introduzido o conceito de **Bootstrap Mode**: um modo de inicialização disponível **somente** enquanto não existir nenhum Administrador Sênior cadastrado no +Compras. Nesse modo, e apenas nele, é possível criar a primeira Unidade de Negócio, o primeiro usuário com perfil de Administrador Sênior, e a configuração inicial mínima necessária para o sistema operar. Assim que o primeiro Administrador Sênior é criado com sucesso, o Bootstrap Mode é **encerrado permanentemente** — não há reabertura, nem por perda de acesso, nem por remoção posterior de todos os Administradores Sênior; qualquer necessidade de recuperação de acesso após o encerramento do Bootstrap é tratada por procedimento operacional de suporte, não pelo Bootstrap Mode.

13. **Segurança — revisão obrigatória de autenticação.** Toda funcionalidade de autenticação (Login, OTP, Identity Providers, Bootstrap Mode, sessão, e qualquer evolução futura dessas áreas) deve obrigatoriamente passar por: (a) revisão arquitetural do Agente Engenheiro de Segurança Sênior antes da implementação; e (b) validação de segurança dedicada após a implementação, antes de qualquer homologação ou uso além de ambiente de desenvolvimento local. Nenhuma funcionalidade de autenticação é considerada "Pronta" (`ROADMAP.md`) sem essas duas revisões documentadas.

## Arquitetura Frontend

**Decisão acrescentada pela revisão arquitetural R2 (data abaixo):**

14. **Vertical Slice como arquitetura obrigatória do Frontend.** O frontend do +Compras utilizará obrigatoriamente arquitetura **Vertical Slice** para toda a aplicação React, em substituição a qualquer estrutura horizontal organizada por tipo técnico. Regras:
    - A organização do código é orientada ao **domínio do negócio**, nunca à tecnologia utilizada. Não são criadas pastas horizontais de topo separadas por `pages`, `components`, `hooks`, `services` ou `models` abrangendo toda a aplicação.
    - Cada módulo de domínio possui **autonomia funcional**: `pages`, `components`, `hooks`, `services`, `routes`, `models`, `types` e `tests` relativos a um domínio permanecem agrupados dentro da própria fatia (slice) desse domínio, não espalhados em diretórios técnicos compartilhados de topo.
    - A arquitetura cresce por domínio: cada novo módulo funcional do negócio é adicionado como uma nova Vertical Slice, seguindo exatamente a mesma estrutura interna das slices já existentes — não é aceita uma estrutura ad hoc por módulo.
    - Frontend e Backend compartilham a mesma visão arquitetural de organização por domínio (Backend já segue Modular Monolith + Clean Architecture + DDD pragmático, ADR-0001); a Vertical Slice é a expressão dessa mesma visão no frontend, não uma arquitetura conflitante.
    - Elementos genuinamente compartilhados entre múltiplos domínios (Design System, utilitários transversais) residem exclusivamente em uma área `shared`/`design-system` de escopo explícito — nunca como destino padrão de código que poderia pertencer a um domínio.
    - Estrutura conceitual de referência (não é estrutura física obrigatória nesta ADR — apenas o padrão arquitetural; a criação física dos diretórios é responsabilidade de Work Order de Estrutura futura):
      ```
      src/
        core/
        authentication/
        administration/
          usuarios/
          perfis/
          filiais/
          centros-custo/
          unidades-alocacao/
        procurement/
        workflow/
        shared/
        design-system/
      ```
      Esta estrutura é conceitual e pode evoluir (novos domínios, novas subdivisões) sem quebrar os princípios arquiteturais acima — o que é obrigatório é o princípio (organização por domínio, autonomia por slice, elementos técnicos agrupados dentro do domínio), não os nomes exatos de pasta listados aqui.
    - Esta decisão não implementa nenhuma estrutura de pastas, não altera código de frontend ou backend existente, e não é uma migration — é decisão exclusivamente arquitetural/documental, a ser aplicada pela Work Order de Estrutura da O1.2 e por toda implementação futura do frontend +Compras.

**Alternativas consideradas:**

1. Manter "Gestão de Empresas" como nome do conceito de classificação gerencial — descartada por confundir empresa jurídica (ERP) com classificação de despesa (+Compras) e por não refletir a origem mista (ERP e local) dos registros.
2. Permitir edição de dados mestres de Filiais/Centros de Custo diretamente no +Compras — descartada por violar o princípio do ERP como fonte canônica e criar risco de divergência entre os dois sistemas.
3. Permitir permissões individuais por usuário além dos perfis, para casos excepcionais — descartada por criar drift de autorização não auditável e por já existir mecanismo equivalente e mais rastreável (criação de um novo perfil).
4. *(R1.2)* Autenticação exclusiva por Microsoft Entra ID desde a Onda 1, sem alternativa própria — descartada por bloquear o início da Onda 1 até a disponibilidade corporativa do Entra ID; o OTP por e-mail permite operar imediatamente e coexistir com o Entra ID quando este for aprovado, sem exigir retrabalho de arquitetura (múltiplos Identity Providers já são suportados por Unidade de Negócio).
5. *(R1.2)* Permitir criação de Administrador por qualquer usuário autenticado quando não houver nenhum cadastrado, sem um modo dedicado — descartada por criar uma janela de escalonamento de privilégio implícita e não auditável; o Bootstrap Mode torna esse estado explícito, temporário e encerrado permanentemente após uso.
6. *(R1.2)* Reabrir o Bootstrap Mode em caso de perda de acesso de todos os Administradores Sênior — descartada por reintroduzir a mesma janela de risco que o Bootstrap Mode foi criado para eliminar; recuperação de acesso é tratada por procedimento operacional de suporte, fora do escopo desta ADR.
7. *(R2)* Estrutura horizontal por tipo técnico (`src/pages`, `src/components`, `src/hooks`, `src/services` abrangendo toda a aplicação) — descartada por dispersar os artefatos de um mesmo domínio de negócio em múltiplos diretórios de topo, aumentando o custo de navegação e a chance de acoplamento acidental entre domínios não relacionados à medida que o Portal +Compras cresce (ADR-0017).
8. *(R2)* Arquitetura de microfrontends (um deployment separado por domínio) — descartada nesta revisão por ser prematura para o estágio atual do Portal +Compras; a Vertical Slice obtém o principal benefício pretendido (autonomia por domínio, crescimento sem reorganização) sem o custo operacional de múltiplos deployments, permanecendo como possível evolução futura se a escala do produto exigir.

**Consequências positivas:**

- A separação Administração / Administração do Sistema / Configurações deixa de ser uma proposta pendente (dúvida de produto nº 2 da O1.1) e passa a ser uma decisão aprovada, liberando a especificação de UX/Mock sem risco de retrabalho de rotas/componentes.
- O modelo de Unidades de Alocação preenche a lacuna deixada pela ADR-0013 (ERP × +Compras como fontes de verdade) para o caso específico de classificação gerencial de despesa, sem introduzir uma entidade "Empresa" que conflitaria com o ERP.
- A regra RBAC exclusiva por perfil elimina ambiguidade de autorização e simplifica auditoria: toda permissão efetiva de um usuário é rastreável a um ou mais perfis nomeados, nunca a uma exceção pontual.
- A separação entre cadastro mestre de Centro de Custo e autorização de acesso por usuário evita que a governança de acesso corrompa o dado sincronizado do ERP.
- *(R1.2)* O mecanismo de Login da Onda 1 deixa de ser pendência (dúvida de produto nº 5 registrada em `ComprasFuncional.md`), desbloqueando a especificação completa de UX/Mock de Login e Bootstrap para a O1.2.
- *(R1.2)* O Bootstrap Mode elimina a necessidade de qualquer intervenção manual em banco de dados ("inserir o primeiro admin via SQL") para iniciar um novo ambiente, tornando o processo de inicialização auditável e reproduzível.
- *(R1.2)* A exigência formal de revisão de segurança para toda funcionalidade de autenticação alinha o +Compras à prática de segurança-por-design antes da implementação, reduzindo o risco de retrabalho por falha encontrada tardiamente.
- *(R2)* A Vertical Slice alinha a organização do frontend à mesma visão arquitetural já adotada pelo backend (organização por domínio, ADR-0001), reduzindo a distância conceitual entre as duas camadas para quem trabalha em ambas.
- *(R2)* Cada novo módulo de domínio (Fornecedores, Materiais, Solicitações, etc., Ondas 2-3) é adicionado como uma nova slice autônoma, sem exigir reorganização de pastas técnicas compartilhadas já existentes — reduz o custo de crescimento do Portal +Compras previsto pela ADR-0017.
- *(R2)* A regra de "elementos compartilhados apenas em Shared/Design System" impede que a pasta compartilhada vire um destino padrão de conveniência, preservando a autonomia real de cada domínio ao longo do tempo.

**Riscos:**

- O catálogo definitivo de Perfis e Permissões (dúvida de produto nº 5 da O1.1) continua pendente; esta ADR define a mecânica (RBAC por perfil), não o conteúdo do catálogo.
- A relação N:N entre Centro de Custo e Unidade de Alocação introduz uma tabela de associação adicional a ser refletida no blueprint físico antes da Onda 5 (Go Live), quando a estrutura integrada ao ERP precisa reproduzir o ERP como modelo canônico.
- Nomes de tela incorretos ("Cadastro de Filiais", "Cadastro de Centros de Custo", "Gestão de Empresas") podem já existir em materiais de apresentação ou comunicação informal fora do escopo documental revisado por esta Work Order; devem ser corrigidos onde encontrados.
- *(R1.2)* O OTP por e-mail depende de um serviço de envio de e-mail transacional ainda não implementado nem contratado; a Work Order de Estrutura da O1.2 precisa tratar esse provedor como dependência explícita.
- *(R1.2)* O papel "Administrador Sênior" citado pelo Bootstrap Mode ainda não está no catálogo de Perfis aprovado (dúvida de produto nº 5/nº 6 de `ComprasFuncional.md`); a O1.2 precisa aprovar esse perfil como parte do catálogo antes de implementar o Bootstrap.
- *(R1.2)* O "Agente Engenheiro de Segurança Sênior" é referenciado como papel de revisão obrigatória, mas seu processo de acionamento (humano, agente de IA, ou ambos) e critérios de aprovação não são detalhados por esta ADR — a definir na Work Order de Estrutura que implementar a autenticação.
- *(R2)* A estrutura conceitual de exemplo (`core/`, `authentication/`, `administration/`, `procurement/`, `workflow/`, `shared/`, `design-system/`) ainda não foi validada contra o código React já existente do Portal +Compras (módulo Fornecedores, AppShell); a Work Order de Estrutura da O1.2 precisa definir como esse código atual migra para o padrão Vertical Slice, ou se nasce direto na nova estrutura.
- *(R2)* Sem definição explícita de onde termina "elemento compartilhado" e começa "elemento de domínio duplicado entre slices", a pasta `shared/` corre risco de virar destino de conveniência ao longo do tempo — mitigação depende de disciplina de revisão de código, não apenas desta ADR.

**Impactos:**

- `docs/product/ComprasFuncional.md`, `docs/product/ComprasUX.md` e `docs/product/ComprasDataModel.md` — novo conteúdo e reorganização das sub-telas de Administração conforme o corte desta ADR.
- `.ai/ENGINEERING_BLUEPRINT.md` — referência à decisão nas seções de Segurança (RBAC) e Banco de Dados (novas entidades de classificação gerencial).
- Nenhuma migration, código ou frontend é alterado por esta ADR — é decisão exclusivamente documental, a ser implementada por Work Order de Estrutura futura.
- *(R1.2)* `docs/product/ComprasFuncional.md`/`ComprasDataModel.md` — especificação da tela/fluxo de Login (OTP), do fluxo de Bootstrap e do vínculo Usuário × Centro de Custo/Perfil, e correção de nomenclatura das sub-telas de `Administração`/`Administração do Sistema`.
- *(R1.2)* `.ai/PROJECT_STATE.md` — registro de que as revisões R1.1 e R1.2 estão concluídas e que a O1.2 está liberada para início.
- *(R1.2)* Qualquer Work Order futura que implemente Login, OTP, Identity Providers ou Bootstrap Mode deve incluir, no seu próprio escopo, a revisão do Agente Engenheiro de Segurança Sênior antes e depois da implementação (item 13) — esta ADR não implementa código, apenas estabelece o requisito de processo.
- *(R2)* `docs/architecture/domain-principles.md` — novos princípios permanentes de organização do frontend por domínio (Vertical Slice).
- *(R2)* `.ai/ENGINEERING_BLUEPRINT.md` — nova seção descrevendo a arquitetura Frontend Vertical Slice e sua integração com o restante da arquitetura do BlueprintOS.
- *(R2)* `.ai/PROJECT_STATE.md` — registro de que a R2 está concluída, a arquitetura Frontend foi aprovada (Vertical Slice) e o frontend está liberado para implementação.
- *(R2)* Nenhuma estrutura de pastas é criada, nenhum código de frontend ou backend é alterado, e nenhuma migration é criada por esta ADR — é decisão exclusivamente arquitetural/documental; a criação física da estrutura é responsabilidade de Work Order de Estrutura futura.
