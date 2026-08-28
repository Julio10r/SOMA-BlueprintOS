# Banco de Dados

O backend possui um `DbContext` real: `BlueprintOSDbContext` (`backend/src/BlueprintOS.Infrastructure/Persistence/`), usando Entity Framework Core com SQL Server. Ele persiste o domínio de Fornecedores (cadastro, descoberta, sincronização com o ERP e histórico de consulta de CNPJ — ver [Procurement.md](../backend/procurement/Procurement.md)), com migrations reais aplicadas nesse mesmo projeto (`Persistence/Migrations/`).

O banco é sempre externo — bancos corporativos `MAISCOMPRAS`/`SOMA_DESENV`, acessados via VPN — nunca um SQL Server local ou em container. Não há pasta `database/` na raiz do repositório nem scripts/seeds de banco separados; a persistência dos demais módulos (ex.: `Documentation`, `Knowledge`) permanece em memória ou em arquivos Markdown.

Este documento é atualizado conforme novos módulos passarem a persistir dados.

## Blueprint administrativo — fatia de Identidade e RBAC

> Blueprint **incremental** (ADR-0021, decisão D7): cada Work Order de domínio documenta a sua própria
> fatia ao concluir a implementação real, nunca como documento desconectado. As fatias de Usuários,
> Filiais, Centros de Custo e Unidades de Alocação serão acrescentadas por O1.6–O1.9.

### Autenticação e Bootstrap (O1.4.2 / O1.4.3)

| Tabela | Chave | Função |
|---|---|---|
| `UnidadesNegocio` | `Id` | Unidade de Negócio; `Slug` único |
| `Usuarios` | `Id` | Usuário do +Compras; `Email` (minúsculo), `Nome`, `UnidadeNegocioId`, `Status` |
| `CodigosVerificacaoOtp` | `Id` | Códigos OTP (hash), uso único |
| `OtpRequestThrottles` | — | Controle de taxa de solicitação de OTP por e-mail |
| `SessoesAutenticacao` | `Id` | Sessão server-side; identificador opaco por hash |
| `BootstrapEstados` | `Id` | Estado do Bootstrap Mode; `Concluido` + `RowVersion` (compare-and-swap) |
| `BootstrapSessoes` | `Id` | Sessão de Bootstrap, distinta e de privilégio limitado |
| `UsuariosCentrosCusto` | (`UsuarioId`,`CentroCustoCodigoErp`) | Escopo de dados operacionais (modelo preparado; gestão em O1.6/O1.7) |

### RBAC (O1.5) — implementado e persistido

| Tabela | Chave | Colunas | Constraints |
|---|---|---|---|
| `Perfis` | `Id` | `Nome` (120), `Descricao` (400), `UnidadeNegocioId`, `Ativo`, `CriadoEm`, `AtualizadoEm` | Único `IX_Perfis_UnidadeNegocioId_Nome`. Sem exclusão física (só ativação/inativação) |
| `Permissoes` | `Id` | `Codigo` (100), `Descricao` (400) | `Codigo` único. Dado de **referência**: 14 linhas semeadas com Ids estáveis; sem tela de criação |
| `PerfisPermissoes` | (`PerfilId`,`PermissaoId`) | — | FK → `Perfis`, FK → `Permissoes`, ambas `ON DELETE RESTRICT`; índice em `PermissaoId` |
| `UsuariosPerfis` | (`UsuarioId`,`PerfilId`) | — | FK → `Usuarios`, FK → `Perfis`, ambas `ON DELETE RESTRICT`; índice em `PerfilId` |

Cardinalidades:

```
UnidadeNegocio 1 ──── n Perfil
Perfil         n ──── n Permissao   (via PerfisPermissoes)
Usuario        n ──── n Perfil      (via UsuariosPerfis)  ← usuário pode ter VÁRIOS Perfis
Usuario        ─╳─ Permissao        ← PROIBIDO por ADR-0020 (itens 7/8/10): não existe, e não deve existir,
                                      nenhuma tabela de permissão individual por usuário
```

**Permissões efetivas de um usuário** = união (`DISTINCT`) das permissões de todos os seus Perfis
**ativos** da Unidade de Negócio da sessão. Resolvida no backend a cada requisição — ver
[rbac-o1.5.md](../architecture/rbac-o1.5.md), seção 4.

Migration correspondente: `20260811143355_AddRbacPerfilPermissaoCatalogo`, aplicada ao banco de
desenvolvimento `MaisCompras` em 11/08/2026. Detalhe do conteúdo e dos dois blocos de SQL manual
(backfill de timestamps e concessão do catálogo ao Administrador Sênior já existente) em
[rbac-o1.5.md](../architecture/rbac-o1.5.md), seção 9.

### Filiais e Centros de Custo integrados ao ERP (O1.7) — implementado e persistido

> ERP canônico × +Compras (ADR-0020, item 2/3; D3, ADR-0021): Filial e Centro de Custo são dado mestre
> do ERP `SOMA_DESENV`, lido em tempo real por `IFilialErpReader`/`SomaFilialReader` e
> `ICentroCustoErpReader`/`SomaCentroCustoReader` (introspecção dinâmica de schema via
> `INFORMATION_SCHEMA.COLUMNS`, mesmo padrão de `SomaFornecedorReader`, B2.1/B2.1.2) — **nunca**
> persistido como cópia local. O +Compras persiste **apenas** os dois metadados locais permitidos
> (`DescricaoMaisCompras` opcional, `AtivoNoMaisCompras`), em uma tabela própria por domínio, correlacionada
> ao ERP unicamente pelo código (`CodigoErp`/`CodigoCliFor`) — nunca pelo conteúdo (descrição/nome), que
> permanece somente leitura, de origem ERP.

| Tabela | Chave | Colunas | Constraints |
|---|---|---|---|
| `FiliaisMetadados` | `Id` | `CodigoErp` (correlação com `CADASTRO_CLI_FOR`/ERP), `DescricaoMaisCompras` (opcional), `AtivoNoMaisCompras`, `UnidadeNegocioId`, `CriadoEm`, `AtualizadoEm` | Único `IX_FiliaisMetadados_UnidadeNegocioId_CodigoErp` — cada Unidade de Negócio mantém seu próprio metadado local independente para a mesma Filial ERP. Sem exclusão física (só ativação/inativação) |
| `CentrosCustoMetadados` | `Id` | `CodigoErp` (correlação com `CENTRO_CUSTO`/ERP), `DescricaoMaisCompras` (opcional), `AtivoNoMaisCompras`, `UnidadeNegocioId`, `CriadoEm`, `AtualizadoEm` | Único **global** `IX_CentrosCustoMetadados_CodigoErp` — um Centro de Custo só pode estar ancorado a **uma** Unidade de Negócio por vez (decisão deliberada: fecha o vetor cross-BU do vínculo Usuário×Centro de Custo, O1.6-L2). Sem exclusão física |

Ausência de metadado local para um código ERP retornado pela leitura em tempo real é tratada como
**Ativo por padrão** (decisão da O1.7, ver `.ai/BACKLOG.md`) — o metadado só é criado na primeira
edição/ativação manual pela tela de gestão, ou "sob demanda" pelo validador de vínculo Usuário×Centro
de Custo (abaixo).

**Resolução da dívida O1.6-L2** (`UsuariosCentrosCusto.CentroCustoCodigoErp`, texto livre desde O1.4.2,
sem tabela local): a coluna continua texto (sem FK física — ver decisão em `.ai/BACKLOG.md`, O1.6-L2),
mas passa a ser **validada em tempo de execução** por `ICentroCustoVinculoValidator`/
`CentroCustoVinculoValidator` antes de qualquer persistência em `UsuariosCentrosCusto`: código
inexistente no ERP é rejeitado; código já ancorado a outra Unidade de Negócio (`CentrosCustoMetadados`)
é rejeitado; código válido e ainda não ancorado tem seu `CentroCustoMetadado` criado "sob demanda",
ancorado à Unidade de Negócio do ator. A integridade do vínculo é garantida pelo índice único global de
`CentrosCustoMetadados.CodigoErp`, não por FK física.

Migration correspondente: `20260811173904_AddFilialCentroCustoMetadadosO17`, **gerada mas não aplicada**
ao banco de desenvolvimento `MaisCompras` — sem VPN/acesso ao SQL Server corporativo disponível na
sessão de implementação (mesma dependência ambiental de B2.1.3/O1.7, ver Work Order, seção "Riscos").

### Unidades de Alocação (O1.8) e vínculo N:N com Centro de Custo (O1.9) — implementado e persistido

> Unidade de Alocação é conceito **exclusivo do +Compras** (ADR-0020, item 4): nunca integrado do ERP,
> ao contrário de Filial e Centro de Custo.

| Tabela | Chave | Colunas | Constraints |
|---|---|---|---|
| `UnidadesAlocacao` | `Id` | `Nome`, `Descricao`, `UnidadeNegocioId`, `Status`, `CriadoEm`, `AtualizadoEm` | Único `(UnidadeNegocioId, Nome)`. Sem exclusão física |
| `CentrosCustoUnidadesAlocacao` | `Id` | `CentroCustoMetadadoId`, `UnidadeAlocacaoId`, `Padrao` | Único `(CentroCustoMetadadoId, UnidadeAlocacaoId)`; índice único filtrado `WHERE [Padrao]=1` por `CentroCustoMetadadoId` (no máximo uma Unidade de Alocação padrão por Centro de Custo); FK `ON DELETE RESTRICT` para `CentrosCustoMetadados` e `UnidadesAlocacao` |

Cardinalidade: `CentroCustoMetadado n ──── n UnidadeAlocacao` (via `CentrosCustoUnidadesAlocacao`), com no máximo um vínculo marcado `Padrao=1` por Centro de Custo — regra de negócio da ADR-0020 (item 5), garantida por índice único filtrado no banco (não apenas na aplicação).

Migrations: `20260811183058_AddUnidadeAlocacaoO18`, `20260811193304_AddCentroCustoUnidadeAlocacaoO19`.

### Multi-Unidade de Negócio e Configuração (O1.11) — implementado e persistido

| Tabela | Chave | Colunas | Constraints |
|---|---|---|---|
| `IdentityProviders` | `Id` | `UnidadeNegocioId`, `Tipo`, `Status` (`StatusConfiguracaoTecnica`) | Índice não único em `UnidadeNegocioId`. **Sem FK física** para `UnidadesNegocio` |
| `ConfiguracoesErp` | `Id` | `UnidadeNegocioId`, parâmetros de conexão/adaptador ERP, `Status` | Único `UnidadeNegocioId` (1:1 por BU). **Sem FK física** |
| `Parametros` | `Id` | Parâmetros gerais por Unidade de Negócio | — |
| `FeatureFlags` | `Id` | `Nome` | Único `Nome` |
| `FeatureFlagsUnidadesNegocio` | `Id` | `FeatureFlagId`, `UnidadeNegocioId` | Único `(FeatureFlagId, UnidadeNegocioId)`. **Sem FK física** declarada para nenhum dos dois lados — apenas índice |
| `ConfiguracoesNotificacao` | `Id` | `UnidadeNegocioId`, canais/config de notificação | Único `UnidadeNegocioId` (1:1 por BU). **Sem FK física** |

> **Observação estrutural:** todas as tabelas de configuração Multi-BU acima têm coluna `UnidadeNegocioId`, mas nenhuma possui **FK física** para `UnidadesNegocio` — o isolamento por Unidade de Negócio depende inteiramente da camada de aplicação, sem garantia declarativa do schema. Não é um erro de implementação (é o mesmo padrão adotado deliberadamente para `AlcadaAprovacao`/`RegraWorkflow`/`RegraOrcamentaria` abaixo), mas fica registrado como GAP de hardening — ver seção "Dívidas técnicas e GAPs consolidados".

Migrations: `20260811203728_AddConfiguracaoTecnicaO111`, `20260811212801_AddConfiguracaoNotificacaoO111`.

### Workflow, Alçadas de Aprovação e Controle Orçamentário (O1.12) — implementado e persistido

> Fundação **configurável** da Onda 1 (não o motor de aprovação operacional da Onda 3). `CriterioAlcada`
> e `PeriodoOrcamentario` são **enums** (não tabelas) — colunas `int` em `AlcadaAprovacao`/`RegraOrcamentaria`.

| Tabela | Chave | Colunas | Constraints |
|---|---|---|---|
| `RegrasWorkflow` | `Id` | `UnidadeNegocioId`, definição da regra | Índice simples em `UnidadeNegocioId`. Sem FK física |
| `AlcadasAprovacao` | `Id` | `UnidadeNegocioId`, `CriterioAlcada` (enum), `AprovadorUsuarioId`/`AprovadorPerfilId` (Guids soltos) | Índice simples em `UnidadeNegocioId`. **Nenhuma FK física** para `Usuario`/`Perfil`/`CentroCustoMetadado` — referências fracas, validadas apenas na aplicação (comentário explícito no código) |
| `RegrasOrcamentarias` | `Id` | `UnidadeNegocioId`, `CentroCustoMetadadoId`, `Periodo` (enum `PeriodoOrcamentario`) | Índice composto `(UnidadeNegocioId, CentroCustoMetadadoId, Periodo)`, **não único** |

Migration: `20260811215629_AddAdministracaoWorkflowAlcadaOrcamentoO112`.

### Fornecedores (B1/B2/B2.1) — implementado e persistido

> Único domínio administrativo cujo isolamento multi-BU é feito por **coluna de texto livre**
> (`BusinessUnit`), não por `UnidadeNegocioId`/FK — herdado do modelo B1/B2 (anterior à introdução formal
> de `UnidadeNegocio` em O1.4.2). Registrado como GAP transversal aberto (ver seção de dívidas).

| Tabela | Chave | Colunas | Constraints |
|---|---|---|---|
| `Fornecedores` | `Id` | `Cnpj_Cpf` (`VARCHAR(14)`), `RazaoSocial`, `NomeFantasia`, `BusinessUnit` (string), `TemporaryUserId` | Único `Cnpj_Cpf`; índices em `RazaoSocial`, `TemporaryUserId`. **Sem `UnidadeNegocioId`** |
| `FornecedoresDominiosErp` | `Id` | `Tipo`, `BusinessUnit`, `ErpSistema`, `CodigoERP` | Único `(Tipo, BusinessUnit, ErpSistema, CodigoERP)` |
| `FornecedoresDescobertos` | `Id` | `TemporaryUserId`, `DescobertoEm`, score (100/80/60/40) | Índice `(TemporaryUserId, DescobertoEm)` |
| `FornecedoresSincronizacoes` | `Id` | `BusinessUnit`, `ErpSistema`, `ErpFornecedorId`, `ExecutadaEm`, `FornecedorId` (nullable) | Índice `(BusinessUnit, ErpSistema, ErpFornecedorId, ExecutadaEm)`. `FornecedorId` **sem FK** física para `Fornecedores` |
| `SincronizacoesFornecedores` | `Id` | `BusinessUnit`, execução em lote (B2.1.3) | 1:N real com `ErrosSincronizacoesFornecedores` (`OnDelete(Cascade)`) — única cardinalidade com FK/cascade declarada no domínio de Fornecedor |
| `ErrosSincronizacoesFornecedores` | `Id` | `SincronizacaoFornecedorId`, mensagem, identificação do fornecedor | FK Restrict/Cascade → `SincronizacoesFornecedores` |
| `FornecedoresEnriquecimentoAnalises` | `Id` | `FornecedorId`, `Campo`, `DataHora`, divergência ERP × CNPJ | Índice `(FornecedorId, Campo, DataHora)`. Sem FK física para `Fornecedores` |
| `FornecedoresCnpjConsultas` | `Id` | `BusinessUnit`, `Cnpj_Cpf`, `DataConsulta` | Índice `(BusinessUnit, Cnpj_Cpf, DataConsulta)` |

Fonte canônica: campo `OrigemInformacao` em `Fornecedor` (`"ERP"` quando aplicado por sincronização via `SincronizarFornecedoresErpUseCase`/`SomaFornecedorReader`; `"MaisCompras"` quando criado localmente; `"ConsultaCnpj"` quando enriquecido por `BrasilApiCnpjProvider`) — fluxo bidirecional documentado em [Procurement.md](../backend/procurement/Procurement.md).

### Conhecimento Linx / Agents Especialistas (O1.13.5) — implementado e persistido

> `LinxConhecimentoProveniencia`, `LinxConhecimentoCategoria` e `LinxEspecialista` são **enums**
> (`HasConversion<int>()`), não tabelas — todos colapsados como colunas `int`/`nvarchar` de uma única
> tabela real.

| Tabela | Chave | Colunas | Constraints |
|---|---|---|---|
| `LinxConhecimentoEntradas` | `Id` | `VersaoRaizId`, `EntradaAnteriorId`, `Versao`, `Especialista` (enum), `Categoria` (enum), `Assunto`, `Conteudo` (máx. 8000), `Proveniencia` (enum: Descoberto→Inferido→Validado→Aprovado), `Fonte`, `Ator`, `UnidadeNegocioId` (nulo = conhecimento global), `Tags` (JSON em `nvarchar(2000)`) | Índices em `VersaoRaizId`, `(Especialista, Categoria)`, `UnidadeNegocioId`. `EntradaAnteriorId`/`VersaoRaizId` **deliberadamente sem FK** (evita ciclo de cascata em dado de auditoria/proveniência) |

Modelo é **append-only versionado**: nenhuma linha é atualizada — `NovaVersao()` sempre insere uma nova linha encadeada por `EntradaAnteriorId`, agrupada por `VersaoRaizId`. Fonte canônica: nativo do +Compras (conhecimento gerado por Agents, nunca replicado do ERP). Migration: `20260811230715_AddLinxKnowledgeO1135` (inclui seed das permissões `ConhecimentoLinx.Gerenciar`/`ConhecimentoLinx.Aprovar`).

**Reconciliação implementado × arquitetura-alvo (AI Factory, `docs/agents/ai-factory/`):** a O1.13.5 entrega apenas o MVP de memória persistente + versionamento + proveniência com RBAC, com busca puramente **textual/estruturada** (`Contains` em memória sobre `Assunto`/`Conteudo`, mais filtros por especialista/categoria/BU/proveniência — `LinxKnowledgeRepository.BuscarUltimasVersoesAsync`). Os documentos da AI Factory descrevem RAG completo com chunking/embeddings/vector store (pgvector/Qdrant/Pinecone/Weaviate/Azure AI Search), orquestração multi-agente (Maestro), motor de workflow e observabilidade via OpenTelemetry/Prometheus/Grafana — **nenhum desses itens tem evidência de implementação no código**; são arquitetura-alvo/roadmap, explicitamente sinalizados como tal em `docs/agents/Agents.md`, e permanecem como tal após esta sprint. Não há ingestão dos +300 `obj_*.prg`/documentação Linx (fora de escopo declarado da O1.13.5).

## Catálogo de permissões RBAC (consolidado, 19 permissões)

Seed único em `PermissaoConfiguration.HasData`, fonte `PermissaoCatalogo.Todas`:

`UnidadeNegocio.Gerenciar`, `Usuario.Gerenciar`, `Perfil.Gerenciar`, `Filial.Gerenciar`, `CentroCusto.Gerenciar`, `UnidadeAlocacao.Gerenciar`, `ConfiguracaoErp.Gerenciar`, `Sistema.Gerenciar`, `Fornecedor.Criar`, `Fornecedor.Editar`, `Fornecedor.Aprovar`, `Pedido.Criar`, `Pedido.Aprovar`, `Pedido.Cancelar`, `Workflow.Gerenciar`, `Alcada.Gerenciar`, `Orcamento.Gerenciar`, `ConhecimentoLinx.Gerenciar`, `ConhecimentoLinx.Aprovar`.

Enforcement real via Minimal APIs (`RequireAuthorization(RbacPolicies.For(...))`, não `[Authorize(Policy=...)]`), confirmado em 17 controllers (`FornecedoresController`, `FornecedorSyncController`, `LinxKnowledgeController`, `FeatureFlagsController`, `RegrasWorkflowController`, `CentrosCustoController`, `MonitoramentoOperacionalController`, `ParametrosController`, `IdentityProvidersController`, `ConfiguracaoNotificacaoController`, `FiliaisController`, `UnidadesNegocioController`, `AlcadasAprovacaoController`, `PerfisController`, `ConfiguracaoErpController`, `RegrasOrcamentariasController`, `UsuariosController`, `UnidadesAlocacaoController`).

**Sem enforcement em nenhum controller:** `Pedido.Criar`, `Pedido.Aprovar`, `Pedido.Cancelar` — consistente com a inexistência de um módulo de Pedido de Compra implementado (fora de escopo de todas as sprints da Onda 1 até aqui). Classificação: **NÃO APLICÁVEL** (permissão do catálogo pré-provisionada para módulo ainda não construído, não um enforcement esquecido).

## Fonte canônica por domínio (ERP × +Compras)

| Domínio | Fonte canônica | Evidência |
|---|---|---|
| Filial | **ERP** (`SOMA_DESENV`) | `IFilialErpReader`/`SomaFilialReader`; `+Compras` só grava `FiliaisMetadados` (Descrição/Ativo local) |
| Centro de Custo | **ERP** (`SOMA_DESENV`) | `ICentroCustoErpReader`/`SomaCentroCustoReader`; `+Compras` só grava `CentrosCustoMetadados` |
| Fornecedor | **Bidirecional** (ERP aplica dado mestre; +Compras cria/enriquece) | Campo `OrigemInformacao` (`ERP`/`MaisCompras`/`ConsultaCnpj`) em `Fornecedor` |
| Unidade de Alocação | **+Compras** (exclusivo, nunca ERP) | ADR-0020 item 4; sem reader ERP associado |
| Perfil / Permissão / RBAC | **+Compras** (exclusivo) | Sem qualquer sincronização ERP |
| Usuário | **+Compras** (identidade própria) | `Usuario` não corresponde a nenhuma entidade do ERP Linx |
| Unidade de Negócio / Multi-BU (IdentityProvider, ConfiguracaoErp, FeatureFlag, ConfiguracaoNotificacao, Parametro) | **+Compras** (exclusivo) | Configuração da própria plataforma, sem espelho no ERP |
| Workflow / Alçada / Orçamento | **+Compras** (exclusivo) | Fundação configurável própria da Onda 1 |
| Conhecimento Linx (`LinxConhecimentoEntradas`) | **+Compras** (gerado, não replicado) | Conhecimento produzido pelos Agents a partir de descoberta read-only do ERP, nunca cópia direta |

## Estado das migrations (verificado em 11/08/2026, execução real)

```
$ dotnet ef migrations list --project src/BlueprintOS.Infrastructure --startup-project src/BlueprintOS.Api
... (22 migrations, de 202607300001_B1FornecedorPersistence a 20260811230715_AddLinxKnowledgeO1135)

$ dotnet ef migrations has-pending-model-changes --project src/BlueprintOS.Infrastructure --startup-project src/BlueprintOS.Api
No changes have been made to the model since the last migration.
```

O modelo EF Core em código está **100% sincronizado** com a última migration aplicada — nenhuma migration pendente, nenhum drift entre código e histórico de migrations detectado. Exceção operacional já registrada: `20260811173904_AddFilialCentroCustoMetadadosO17` foi **gerada mas ainda não aplicada** ao banco `MaisCompras` por indisponibilidade de VPN durante a implementação da O1.7 (dependência ambiental, não drift de modelo).

## Mapa de relacionamentos (cardinalidades reais confirmadas em Fluent API)

```
UnidadeNegocio   1 ──── n  Perfil
Perfil           n ──── n  Permissao                (via PerfisPermissoes, FK Restrict)
Usuario          n ──── n  Perfil                    (via UsuariosPerfis, FK Restrict)
CentroCustoMetadado n ── n UnidadeAlocacao           (via CentrosCustoUnidadesAlocacao, FK Restrict,
                                                       no máx. 1 marcado Padrao=1 por CC — índice único filtrado)
FeatureFlag      n ──── n  UnidadeNegocio            (via FeatureFlagsUnidadesNegocio — SEM FK física, só índice)
SincronizacaoFornecedor 1 ── n ErroSincronizacaoFornecedor  (FK + Cascade — única cardinalidade com
                                                              cascade real fora de RBAC/UA×CC)
Usuario          ─╳─ Permissao                       ← PROIBIDO por ADR-0020: nunca permissão individual
```

Relações **lógicas sem FK física no schema** (isolamento garantido só pela aplicação): `AlcadaAprovacao.AprovadorUsuarioId/AprovadorPerfilId`, `RegraOrcamentaria.CentroCustoMetadadoId`, `RegraWorkflow.UnidadeNegocioId`, `IdentityProvider/ConfiguracaoErp/ConfiguracaoNotificacao.UnidadeNegocioId`, `Fornecedor.BusinessUnit` (nem é FK, é string), `FornecedorEnriquecimentoAnalise.FornecedorId`, `FornecedorSincronizacao.FornecedorId`, `UsuarioCentroCusto.CentroCustoCodigoErp` (mitigado por `ICentroCustoVinculoValidator` em tempo de execução, não por constraint).

## Isolamento Multi-BU — achado de auditoria (O1.14)

A auditoria desta sprint encontrou um candidato a exposição cross-BU: `ObterSincronizacaoFornecedorUseCase`/`SincronizacaoFornecedorMonitorRepository.ObterPorIdComErrosAsync` busca `SincronizacaoFornecedor` apenas por `Id`, **sem filtrar por `BusinessUnit`/BU da sessão**. O endpoint correspondente (`MonitoramentoOperacionalController`) exige a permissão `Sistema.Gerenciar` (perfil administrativo de plataforma). Classificação de severidade: **MEDIUM** — exposição de metadados de execução de sincronização (não dado de negócio sensível de terceiros) e restrita a um perfil administrativo já concebido como transversal à plataforma; não caracteriza escalonamento de privilégio nem vazamento de dado de outro usuário final. Registrado no inventário de dívidas para avaliação de hardening (decisão de produto pendente: `Sistema.Gerenciar` é intencionalmente global ou deveria ser escopado por BU). Não bloqueia o fechamento desta sprint.

## Matriz de rastreabilidade (Domínio → Entidade → Tabela → Migration → API → Frontend)

| Domínio | Entidade principal | Tabela | Migration | Endpoint/Controller | Frontend (Vertical Slice) |
|---|---|---|---|---|---|
| RBAC | `Perfil`/`Permissao` | `Perfis`/`Permissoes`/`PerfisPermissoes` | `AddRbacPerfilPermissaoCatalogo` | `PerfisController` | `administration/profiles` |
| Usuário | `Usuario`/`UsuarioPerfil` | `Usuarios`/`UsuariosPerfis` | `AddUsuarioGestaoO16` | `UsuariosController` | `administration/users` |
| Filial | `FilialMetadado` | `FiliaisMetadados` | `AddFilialCentroCustoMetadadosO17` | `FiliaisController` | `administration/branches` |
| Centro de Custo | `CentroCustoMetadado` | `CentrosCustoMetadados` | `AddFilialCentroCustoMetadadosO17` | `CentrosCustoController` | `administration/cost-centers` |
| Unidade de Alocação | `UnidadeAlocacao` | `UnidadesAlocacao` | `AddUnidadeAlocacaoO18` | `UnidadesAlocacaoController` | `administration/allocation-units` |
| CC × UA (N:N) | `CentroCustoUnidadeAlocacao` | `CentrosCustoUnidadesAlocacao` | `AddCentroCustoUnidadeAlocacaoO19` | `CentrosCustoController`/`UnidadesAlocacaoController` | `administration/cost-centers` (seleção real de UA) |
| Multi-BU (IdP/ERP/Notif./Flags) | `IdentityProvider`/`ConfiguracaoErp`/`ConfiguracaoNotificacao`/`FeatureFlag` | `IdentityProviders`/`ConfiguracoesErp`/`ConfiguracoesNotificacao`/`FeatureFlags` | `AddConfiguracaoTecnicaO111`/`AddConfiguracaoNotificacaoO111` | `IdentityProvidersController`/`ConfiguracaoErpController`/`ConfiguracaoNotificacaoController`/`FeatureFlagsController` | `administration` (sub-telas O1.11) |
| Workflow/Alçada/Orçamento | `RegraWorkflow`/`AlcadaAprovacao`/`RegraOrcamentaria` | `RegrasWorkflow`/`AlcadasAprovacao`/`RegrasOrcamentarias` | `AddAdministracaoWorkflowAlcadaOrcamentoO112` | `RegrasWorkflowController`/`AlcadasAprovacaoController`/`RegrasOrcamentariasController` | `administration` (sub-telas O1.12) |
| Fornecedor | `Fornecedor` | `Fornecedores` | `B1FornecedorPersistence` + evoluções B2/B2.1 | `FornecedoresController`/`FornecedorSyncController` | `procurement/suppliers` |
| Monitoramento de sincronização | `SincronizacaoFornecedor` | `SincronizacoesFornecedores` | (B2.1.3, pré-existente) | `MonitoramentoOperacionalController` | `administration` (O1.13, Monitor/Auditoria) |
| Conhecimento Linx | `LinxKnowledgeEntry` | `LinxConhecimentoEntradas` | `AddLinxKnowledgeO1135` | `LinxKnowledgeController` | Não exigido (sem frontend administrativo dedicado nesta fundação) |

> Nível de campo de formulário agora consolidado na seção "Matriz tela × campo × entidade (entregável #37)"
> abaixo — mantida sincronizada com os arquivos `types/*.ts` de cada Vertical Slice (fonte primária); esta
> tabela consolida o nível Domínio → Tabela → API → Frontend exigido pela O1.14.

## Validação funcional executada (O1.14)

- `dotnet build BlueprintOS.sln`: aprovado, 0 erros, 0 avisos.
- `dotnet test BlueprintOS.sln`: aprovado, 689/689 (682 unitários + 7 integração) — sem regressão em relação à baseline da O1.13.5.
- `dotnet ef migrations list` / `dotnet ef migrations has-pending-model-changes`: executados e reconciliados nesta seção — nenhuma migration pendente, nenhum drift entre modelo EF e histórico aplicado.
- Frontend: nenhum arquivo alterado nesta sprint (documentação/banco); suíte de 116 testes (Vitest) não reexecutada por ausência de mudança — baseline válida desde O1.13.5.

## Mapeamento de APIs (entregável #39)

> Levantamento exaustivo de todos os endpoints HTTP reais mapeados via Minimal APIs em
> `backend/src/BlueprintOS.Api/**/*.cs` (`MapGet`/`MapPost`/`MapPut`/`MapPatch`/`MapDelete`), incluindo
> `Program.cs`. Política global: `AuthorizationOptions.FallbackPolicy` exige usuário autenticado em
> **todo** endpoint por padrão (`Program.cs`) — anônimo só existe onde `.AllowAnonymous()` aparece
> explicitamente no código. "Autenticado apenas" na coluna de permissão significa que o endpoint depende
> só dessa policy global (sessão válida), sem `RbacPolicies.For(...)` adicional.

### Administração (base `/api/administracao`, `PerfisController.BaseRoute`)

| Método | Rota completa | Controller | Permissão RBAC | Descrição breve |
|---|---|---|---|---|
| GET | `/api/administracao/permissoes` | `PerfisController` | `Perfil.Gerenciar` | Lista o catálogo global de permissões (19 permissões seedadas) |
| GET | `/api/administracao/perfis` | `PerfisController` | `Perfil.Gerenciar` | Lista Perfis da Unidade de Negócio da sessão |
| GET | `/api/administracao/perfis/{id:guid}` | `PerfisController` | `Perfil.Gerenciar` | Obtém um Perfil por Id |
| POST | `/api/administracao/perfis` | `PerfisController` | `Perfil.Gerenciar` | Cria Perfil |
| PUT | `/api/administracao/perfis/{id:guid}` | `PerfisController` | `Perfil.Gerenciar` | Atualiza Nome/Descrição/Permissões do Perfil |
| PATCH | `/api/administracao/perfis/{id:guid}/status` | `PerfisController` | `Perfil.Gerenciar` | Ativa/inativa Perfil (sem exclusão física) |
| GET | `/api/administracao/usuarios` | `UsuariosController` | `Usuario.Gerenciar` | Lista Usuários da BU da sessão |
| GET | `/api/administracao/usuarios/{id:guid}` | `UsuariosController` | `Usuario.Gerenciar` | Obtém Usuário por Id |
| POST | `/api/administracao/usuarios` | `UsuariosController` | `Usuario.Gerenciar` | Cria Usuário |
| PUT | `/api/administracao/usuarios/{id:guid}` | `UsuariosController` | `Usuario.Gerenciar` | Atualiza Usuário (Perfis, Centros de Custo, etc.) |
| PATCH | `/api/administracao/usuarios/{id:guid}/status` | `UsuariosController` | `Usuario.Gerenciar` | Ativa/inativa Usuário |
| GET | `/api/administracao/filiais` | `FiliaisController` | `Filial.Gerenciar` | Lista Filiais (dado mestre ERP + metadado local) |
| PUT | `/api/administracao/filiais/{codigoCliFor}` | `FiliaisController` | `Filial.Gerenciar` | Atualiza metadado local (`DescricaoMaisCompras`/`AtivoNoMaisCompras`) |
| GET | `/api/administracao/centros-custo` | `CentrosCustoController` | `CentroCusto.Gerenciar` | Lista Centros de Custo (dado mestre ERP + metadado local) |
| PUT | `/api/administracao/centros-custo/{codigoErp}` | `CentrosCustoController` | `CentroCusto.Gerenciar` | Atualiza metadado local do Centro de Custo |
| GET | `/api/administracao/centros-custo/{codigoErp}/unidades-alocacao` | `CentrosCustoController` | `CentroCusto.Gerenciar` | Lista vínculos N:N com Unidade de Alocação |
| PUT | `/api/administracao/centros-custo/{codigoErp}/unidades-alocacao` | `CentrosCustoController` | `CentroCusto.Gerenciar` | Substitui o conjunto de vínculos com Unidade de Alocação |
| GET | `/api/administracao/unidades-alocacao` | `UnidadesAlocacaoController` | `UnidadeAlocacao.Gerenciar` | Lista Unidades de Alocação |
| GET | `/api/administracao/unidades-alocacao/{id:guid}` | `UnidadesAlocacaoController` | `UnidadeAlocacao.Gerenciar` | Obtém Unidade de Alocação por Id |
| POST | `/api/administracao/unidades-alocacao` | `UnidadesAlocacaoController` | `UnidadeAlocacao.Gerenciar` | Cria Unidade de Alocação |
| PUT | `/api/administracao/unidades-alocacao/{id:guid}` | `UnidadesAlocacaoController` | `UnidadeAlocacao.Gerenciar` | Atualiza Nome/Descrição |
| PATCH | `/api/administracao/unidades-alocacao/{id:guid}/status` | `UnidadesAlocacaoController` | `UnidadeAlocacao.Gerenciar` | Ativa/inativa |
| GET | `/api/administracao/unidades-negocio` | `UnidadesNegocioController` | `UnidadeNegocio.Gerenciar` | Lista Unidades de Negócio |
| POST | `/api/administracao/unidades-negocio` | `UnidadesNegocioController` | `UnidadeNegocio.Gerenciar` | Cria Unidade de Negócio |
| PUT | `/api/administracao/unidades-negocio/{id:guid}` | `UnidadesNegocioController` | `UnidadeNegocio.Gerenciar` | Renomeia Unidade de Negócio |
| PATCH | `/api/administracao/unidades-negocio/{id:guid}/status` | `UnidadesNegocioController` | `UnidadeNegocio.Gerenciar` | Ativa/inativa |
| GET | `/api/administracao/unidades-negocio/{unidadeNegocioId:guid}/identity-providers` | `IdentityProvidersController` | `Sistema.Gerenciar` | Lista Identity Providers configurados na BU |
| POST | `/api/administracao/unidades-negocio/{unidadeNegocioId:guid}/identity-providers` | `IdentityProvidersController` | `Sistema.Gerenciar` | Cria Identity Provider |
| PUT | `/api/administracao/unidades-negocio/{unidadeNegocioId:guid}/identity-providers/{id:guid}` | `IdentityProvidersController` | `Sistema.Gerenciar` | Atualiza Identity Provider (tipo/domínios/parâmetros) |
| PATCH | `/api/administracao/unidades-negocio/{unidadeNegocioId:guid}/identity-providers/{id:guid}/status` | `IdentityProvidersController` | `Sistema.Gerenciar` | Ativa/inativa |
| GET | `/api/administracao/unidades-negocio/{unidadeNegocioId:guid}/configuracao-erp` | `ConfiguracaoErpController` | `ConfiguracaoErp.Gerenciar` | Obtém configuração de ERP da BU |
| PUT | `/api/administracao/unidades-negocio/{unidadeNegocioId:guid}/configuracao-erp` | `ConfiguracaoErpController` | `ConfiguracaoErp.Gerenciar` | Salva configuração de ERP (sistema/parâmetros de conexão) |
| PATCH | `/api/administracao/unidades-negocio/{unidadeNegocioId:guid}/configuracao-erp/status` | `ConfiguracaoErpController` | `ConfiguracaoErp.Gerenciar` | Ativa/inativa |
| GET | `/api/administracao/parametros` | `ParametrosController` | `Sistema.Gerenciar` | Lista Parâmetros (globais e por BU) |
| POST | `/api/administracao/parametros` | `ParametrosController` | `Sistema.Gerenciar` | Cria Parâmetro |
| PUT | `/api/administracao/parametros/{id:guid}` | `ParametrosController` | `Sistema.Gerenciar` | Atualiza Valor/Descrição |
| DELETE | `/api/administracao/parametros/{id:guid}` | `ParametrosController` | `Sistema.Gerenciar` | Exclui Parâmetro (única exclusão física do catálogo administrativo) |
| GET | `/api/administracao/feature-flags` | `FeatureFlagsController` | `Sistema.Gerenciar` | Lista Feature Flags e status por Unidade de Negócio |
| POST | `/api/administracao/feature-flags` | `FeatureFlagsController` | `Sistema.Gerenciar` | Cria Feature Flag |
| PATCH | `/api/administracao/feature-flags/{id:guid}/unidades-negocio/{unidadeNegocioId:guid}` | `FeatureFlagsController` | `Sistema.Gerenciar` | Ativa/inativa a flag para uma BU específica |
| GET | `/api/administracao/unidades-negocio/{unidadeNegocioId:guid}/configuracao-notificacao` | `ConfiguracaoNotificacaoController` | `Sistema.Gerenciar` | Obtém configuração de notificação (canal e-mail) da BU |
| PUT | `/api/administracao/unidades-negocio/{unidadeNegocioId:guid}/configuracao-notificacao` | `ConfiguracaoNotificacaoController` | `Sistema.Gerenciar` | Salva configuração de notificação |
| GET | `/api/administracao/unidades-negocio/{unidadeNegocioId:guid}/alcadas-aprovacao` | `AlcadasAprovacaoController` | `Alcada.Gerenciar` | Lista Alçadas de Aprovação da BU |
| POST | `/api/administracao/unidades-negocio/{unidadeNegocioId:guid}/alcadas-aprovacao` | `AlcadasAprovacaoController` | `Alcada.Gerenciar` | Cria Alçada de Aprovação |
| PUT | `/api/administracao/unidades-negocio/{unidadeNegocioId:guid}/alcadas-aprovacao/{id:guid}` | `AlcadasAprovacaoController` | `Alcada.Gerenciar` | Atualiza Alçada de Aprovação |
| PATCH | `/api/administracao/unidades-negocio/{unidadeNegocioId:guid}/alcadas-aprovacao/{id:guid}/status` | `AlcadasAprovacaoController` | `Alcada.Gerenciar` | Ativa/inativa |
| GET | `/api/administracao/unidades-negocio/{unidadeNegocioId:guid}/regras-orcamentarias` | `RegrasOrcamentariasController` | `Orcamento.Gerenciar` | Lista Regras Orçamentárias da BU |
| POST | `/api/administracao/unidades-negocio/{unidadeNegocioId:guid}/regras-orcamentarias` | `RegrasOrcamentariasController` | `Orcamento.Gerenciar` | Cria Regra Orçamentária |
| PUT | `/api/administracao/unidades-negocio/{unidadeNegocioId:guid}/regras-orcamentarias/{id:guid}` | `RegrasOrcamentariasController` | `Orcamento.Gerenciar` | Atualiza Regra Orçamentária |
| PATCH | `/api/administracao/unidades-negocio/{unidadeNegocioId:guid}/regras-orcamentarias/{id:guid}/status` | `RegrasOrcamentariasController` | `Orcamento.Gerenciar` | Ativa/inativa |
| GET | `/api/administracao/unidades-negocio/{unidadeNegocioId:guid}/regras-workflow` | `RegrasWorkflowController` | `Workflow.Gerenciar` | Lista Regras de Workflow da BU |
| POST | `/api/administracao/unidades-negocio/{unidadeNegocioId:guid}/regras-workflow` | `RegrasWorkflowController` | `Workflow.Gerenciar` | Cria Regra de Workflow |
| PUT | `/api/administracao/unidades-negocio/{unidadeNegocioId:guid}/regras-workflow/{id:guid}` | `RegrasWorkflowController` | `Workflow.Gerenciar` | Atualiza Regra de Workflow |
| PATCH | `/api/administracao/unidades-negocio/{unidadeNegocioId:guid}/regras-workflow/{id:guid}/status` | `RegrasWorkflowController` | `Workflow.Gerenciar` | Ativa/inativa |
| GET | `/api/administracao/monitoramento/sincronizacoes-fornecedores` | `MonitoramentoOperacionalController` | `Sistema.Gerenciar` | Lista execuções de sincronização de fornecedores (paginado, filtros status/BU) |
| GET | `/api/administracao/monitoramento/sincronizacoes-fornecedores/{id:guid}` | `MonitoramentoOperacionalController` | `Sistema.Gerenciar` | Detalhe de uma execução, incluindo erros. **GAP conhecido:** busca só por `Id`, sem filtro de `BusinessUnit` (ver seção "Isolamento Multi-BU — achado de auditoria") |
| GET | `/api/administracao/conhecimento-linx/` | `LinxKnowledgeController` | Autenticado apenas | Busca entradas de Conhecimento Linx (por especialista/categoria/BU/proveniência) |
| GET | `/api/administracao/conhecimento-linx/{versaoRaizId:guid}/historico` | `LinxKnowledgeController` | Autenticado apenas | Histórico de versões de uma entrada |
| POST | `/api/administracao/conhecimento-linx/` | `LinxKnowledgeController` | `ConhecimentoLinx.Gerenciar` | Registra nova entrada/versão de conhecimento |
| POST | `/api/administracao/conhecimento-linx/{id:guid}/validar` | `LinxKnowledgeController` | `ConhecimentoLinx.Gerenciar` | Avança proveniência para Validado |
| POST | `/api/administracao/conhecimento-linx/{id:guid}/aprovar` | `LinxKnowledgeController` | `ConhecimentoLinx.Aprovar` | Avança proveniência para Aprovado |

### Identity / Bootstrap / Autenticação

| Método | Rota completa | Controller | Permissão RBAC | Descrição breve |
|---|---|---|---|---|
| POST | `/auth/otp/request` | `AuthController` | Anônimo (`.AllowAnonymous()`) | Solicita OTP de login por e-mail (rate limited) |
| POST | `/auth/otp/verify` | `AuthController` | Anônimo (`.AllowAnonymous()`) | Valida OTP e cria sessão (rate limited) |
| POST | `/auth/logout` | `AuthController` | Anônimo (`.AllowAnonymous()`) | Encerra a sessão atual (idempotente mesmo sem sessão) |
| GET | `/auth/me` | `AuthController` | Autenticado apenas | Dados do usuário autenticado (produz 401 via `FallbackPolicy` se sem sessão) |
| GET | `/me/unidades-negocio` | `MeController` | Autenticado apenas | Lista Unidades de Negócio às quais o usuário autenticado tem acesso |
| GET | `/bootstrap/estado` | `BootstrapController` | Anônimo (`.AllowAnonymous()`) | Consulta se o Bootstrap Mode já foi concluído |
| POST | `/bootstrap/iniciar` | `BootstrapController` | Anônimo (`.AllowAnonymous()`) | Inicia Bootstrap (secret + e-mail pré-autorizado; rate limited) |
| POST | `/bootstrap/otp/verificar` | `BootstrapController` | Anônimo (`.AllowAnonymous()`) | Valida OTP do Bootstrap e cria Sessão de Bootstrap (rate limited) |
| POST | `/bootstrap/concluir` | `BootstrapController` | `BootstrapAuthorizationPolicies.BootstrapAuthenticated` (exige Sessão de Bootstrap válida, não a policy RBAC comum) | Conclui o Bootstrap: cria Unidade de Negócio, Usuário e vínculo de Administrador Sênior |
| GET | `/dev/otp` | `DevelopmentOtpDiagnosticsController` | Anônimo, mas restrito a `IHostEnvironment.IsDevelopment()` + loopback TCP real | Diagnóstico: recupera o último OTP gerado para um e-mail (Development apenas, nunca em Staging/Production) |
| GET | `/health` | `Program.cs` | Anônimo (`.AllowAnonymous()`) | Health check de orquestração/monitoramento |

### Fornecedores (Procurement)

| Método | Rota completa | Controller | Permissão RBAC | Descrição breve |
|---|---|---|---|---|
| POST | `/api/fornecedores/descobrir` | `FornecedorDiscoveryController` | Autenticado apenas | Dispara descoberta inteligente de fornecedores no ERP a partir de item/categoria |
| GET | `/api/fornecedores/descobertas` | `FornecedorDiscoveryController` | Autenticado apenas | Lista descobertas já executadas |
| GET | `/api/fornecedores/descobertas/{id:guid}` | `FornecedorDiscoveryController` | Autenticado apenas | Obtém detalhe de uma descoberta |
| POST | `/api/fornecedores/sincronizar` | `FornecedorSyncController` | `Sistema.Gerenciar` | Sincroniza um fornecedor pontual com o ERP |
| POST | `/api/fornecedores/sincronizar/lote` | `FornecedorSyncController` | `Sistema.Gerenciar` | Sincroniza fornecedores em lote |
| GET | `/api/fornecedores/sincronizar-erp` | `FornecedorSyncController` | `Sistema.Gerenciar` | Dispara sincronização completa fornecedores × ERP |
| GET | `/api/fornecedores/{fornecedorId:guid}/sincronizacoes` | `FornecedorSyncController` | `Sistema.Gerenciar` | Histórico/auditoria de sincronizações de um fornecedor |
| POST | `/fornecedores` | `FornecedoresController` | `Fornecedor.Criar` | Cria Fornecedor |
| GET | `/fornecedores` | `FornecedoresController` | Autenticado apenas | Busca/lista Fornecedores |
| POST | `/fornecedores/consulta-cnpj` | `FornecedoresController` | `Fornecedor.Criar` | Consulta CNPJ em provedor externo (BrasilAPI) |
| GET | `/fornecedores/{id:guid}` | `FornecedoresController` | Autenticado apenas | Obtém Fornecedor por Id |
| PUT | `/fornecedores/{id:guid}` | `FornecedoresController` | `Fornecedor.Editar` | Atualiza Fornecedor |
| DELETE | `/fornecedores/{id:guid}` | `FornecedoresController` | `Fornecedor.Editar` | Exclui Fornecedor |
| POST | `/fornecedores/{id:guid}/enriquecimento-cnpj` | `FornecedoresController` | `Fornecedor.Editar` | Analisa divergência ERP × CNPJ para enriquecimento |
| POST | `/fornecedores/{id:guid}/enriquecimento-cnpj/aprovar` | `FornecedoresController` | `Fornecedor.Aprovar` | Aprova enriquecimento de campo divergente |
| POST | `/fornecedores/{id:guid}/enriquecimento-cnpj/rejeitar` | `FornecedoresController` | `Fornecedor.Aprovar` | Rejeita enriquecimento de campo divergente |

> **Nota de padronização (GAP registrado):** `FornecedorDiscoveryController`/`FornecedorSyncController` usam prefixo `/api/fornecedores`, enquanto `FornecedoresController` usa `/fornecedores` sem `/api` — inconsistência de convenção de rotas dentro do mesmo domínio, sem impacto funcional (roteamento resolve normalmente), mas digna de padronização futura.

### Negociação (AI / Core)

| Método | Rota completa | Controller | Permissão RBAC | Descrição breve |
|---|---|---|---|---|
| POST | `/api/v1/negotiations/history` | `NegotiationEndpoints` | Autenticado apenas | Registra histórico de negociação na memória do agente |
| GET | `/api/v1/negotiations/suppliers/{supplierId:guid}` | `NegotiationEndpoints` | Autenticado apenas | Consulta histórico de negociação por fornecedor |
| POST | `/api/v1/negotiations/recommendations` | `NegotiationEndpoints` | Autenticado apenas | Gera recomendação de negociação |
| POST | `/api/v1/negociacoes/recomendacoes` | `NegotiationRecommendationController` | Autenticado apenas | Endpoint equivalente em português (mapeado direto em `endpoints`, fora de `MapGroup`) |

**Total catalogado: 92 endpoints** distribuídos em 20 arquivos de controller/endpoint estático + `Program.cs` (health check).

## Matriz tela × campo × entidade (entregável #37)

> Um módulo por Vertical Slice de `frontend/web/src/administration/*`. Coluna "Entidade/Tabela" cruzada
> com as tabelas já documentadas nas seções de Blueprint administrativo acima. Campos derivados dos
> arquivos `types/*.ts` de cada slice (fonte real dos contratos DTO consumidos pela tela — não há
> duplicação de schema em outro lugar do frontend). "Editável" refere-se aos formulários de
> criação/edição (`*Input`/`*CriarInput`/`*UpdateInput`/`*AtualizarInput`); campos presentes apenas no
> tipo de leitura (`*Dto`/tipo principal) e ausentes do `*Input` correspondente são somente leitura na tela.

### Perfis (`administration/profiles`)

| Campo | Propriedade/Tipo (TS) | Entidade/Tabela | Editável |
|---|---|---|---|
| Nome | `nome: string` | `Perfis.Nome` | Sim |
| Descrição | `descricao: string` | `Perfis.Descricao` | Sim |
| Permissões | `permissoes: string[]` (códigos) | `PerfisPermissoes` (via `Permissoes.Codigo`) | Sim |
| Status | `ativo: boolean` | `Perfis.Ativo` | Sim (ação separada de ativar/inativar) |
| Unidade de Negócio | `unidadeNegocioId: string` | `Perfis.UnidadeNegocioId` | Não (resolvida pela sessão, nunca enviada pelo cliente) |
| Usuários vinculados | `usuariosVinculados: number` | Derivado de `UsuariosPerfis` (contagem) | Não |
| Criado em / Atualizado em | `criadoEm`/`atualizadoEm: string` | `Perfis.CriadoEm`/`AtualizadoEm` | Não |

### Usuários (`administration/users`)

| Campo | Propriedade/Tipo (TS) | Entidade/Tabela | Editável |
|---|---|---|---|
| Nome | `nome: string` | `Usuarios.Nome` | Sim |
| E-mail | `email: string` | `Usuarios.Email` | Sim |
| Perfis | `perfis: string[]` (Ids no input) / `UsuarioPerfilResumo[]` (leitura) | `UsuariosPerfis` | Sim |
| Centros de Custo | `centrosCusto: string[]` | `UsuariosCentrosCusto.CentroCustoCodigoErp` | Sim |
| Todos os Centros de Custo | `todosCentrosCusto: boolean` | `UsuariosCentrosCusto` (flag de escopo total) | Sim |
| Status | `ativo: boolean` | `Usuarios.Status` | Sim (ação separada) |
| Unidade de Negócio | `unidadeNegocioId: string` | `Usuarios.UnidadeNegocioId` | Não (sessão) |
| Criado em / Atualizado em | `criadoEm`/`atualizadoEm: string` | — | Não |

### Filiais (`administration/branches`)

| Campo | Propriedade/Tipo (TS) | Entidade/Tabela | Editável |
|---|---|---|---|
| Código CliFor | `codigoCliFor: string` | Correlação `FiliaisMetadados.CodigoErp` ↔ `CADASTRO_CLI_FOR` (ERP) | Não (somente leitura, origem ERP) |
| Nome CliFor | `nomeCliFor: string` | ERP (`CADASTRO_CLI_FOR`), nunca persistido localmente | Não |
| Descrição +Compras | `descricaoMaisCompras?: string` | `FiliaisMetadados.DescricaoMaisCompras` | Sim |
| Ativo no +Compras | `ativoNoMaisCompras: boolean` | `FiliaisMetadados.AtivoNoMaisCompras` | Sim |
| Tem metadado local | `temMetadadoLocal: boolean` | Derivado (existência de linha em `FiliaisMetadados`) | Não |
| Unidade de Negócio | `unidadeNegocioId: string` | `FiliaisMetadados.UnidadeNegocioId` | Não |

### Centros de Custo (`administration/cost-centers`)

| Campo | Propriedade/Tipo (TS) | Entidade/Tabela | Editável |
|---|---|---|---|
| Código ERP | `codigoErp: string` | Correlação `CentrosCustoMetadados.CodigoErp` ↔ `CENTRO_CUSTO` (ERP) | Não |
| Descrição ERP | `descricaoErp: string` | ERP (`CENTRO_CUSTO`) | Não |
| Descrição +Compras | `descricaoMaisCompras?: string` | `CentrosCustoMetadados.DescricaoMaisCompras` | Sim |
| Ativo no +Compras | `ativoNoMaisCompras: boolean` | `CentrosCustoMetadados.AtivoNoMaisCompras` | Sim |
| Unidade de Alocação padrão (nome) | `unidadeAlocacaoPadraoNome?: string` | `CentrosCustoUnidadesAlocacao` (linha com `Padrao=1`) via `UnidadesAlocacao.Nome` | Sim (por tela dedicada de vínculo) |
| Qtd. Unidades de Alocação vinculadas | `quantidadeUnidadesAlocacaoVinculadas: number` | Derivado de `CentrosCustoUnidadesAlocacao` | Não |
| Vínculos (tela de vínculo) | `UnidadeAlocacaoVinculoResumo[]` (`id`,`nome`,`ativo`,`padrao`) | `CentrosCustoUnidadesAlocacao` ⋈ `UnidadesAlocacao` | Sim |
| Unidade de Negócio | `unidadeNegocioId: string` | `CentrosCustoMetadados.UnidadeNegocioId` | Não |

### Unidades de Alocação (`administration/allocation-units`)

| Campo | Propriedade/Tipo (TS) | Entidade/Tabela | Editável |
|---|---|---|---|
| Nome | `nome: string` | `UnidadesAlocacao.Nome` | Sim |
| Descrição | `descricao: string` | `UnidadesAlocacao.Descricao` | Sim |
| Status | `status: "Ativo"\|"Inativo"` | `UnidadesAlocacao.Status` | Sim (ação separada) |
| Unidade de Negócio | `unidadeNegocioId: string` | `UnidadesAlocacao.UnidadeNegocioId` | Não (sessão) |

### Unidades de Negócio (`administration/business-units`)

| Campo | Propriedade/Tipo (TS) | Entidade/Tabela | Editável |
|---|---|---|---|
| Nome | `nome: string` | `UnidadesNegocio.Nome` | Sim |
| Slug | `slug: string` | `UnidadesNegocio.Slug` | Sim (só na criação — `UnidadeNegocioEditarInput` não tem `slug`) |
| Status | `status: "Ativo"\|"Inativo"` | `UnidadesNegocio.Status` | Sim (ação separada) |

### Identity Providers (`administration/identity-providers`)

| Campo | Propriedade/Tipo (TS) | Entidade/Tabela | Editável |
|---|---|---|---|
| Tipo | `tipo: string` | `IdentityProviders.Tipo` | Sim |
| Domínios autorizados | `dominiosAutorizados: string[]` | `IdentityProviders` (coluna de domínios) | Sim |
| Parâmetros configurados | `parametrosConfigurados: boolean` (leitura) / `parametros?: string` (input, segredo) | `IdentityProviders` (parâmetros de conexão) | Sim (segredo nunca retorna ao cliente; vazio preserva valor salvo) |
| Status | `status: "Ativo"\|"Inativo"` | `IdentityProviders.Status` | Sim (ação separada) |
| Unidade de Negócio | `unidadeNegocioId: string` | `IdentityProviders.UnidadeNegocioId` (sem FK física) | Não |

### Configuração de ERP (`administration/erp-configuration`)

| Campo | Propriedade/Tipo (TS) | Entidade/Tabela | Editável |
|---|---|---|---|
| Sistema ERP | `sistemaErp: string` | `ConfiguracoesErp` (coluna de sistema/adaptador) | Sim |
| Parâmetros configurados | `parametrosConfigurados: boolean` (leitura) / `parametrosConexao?: string` (input, segredo) | `ConfiguracoesErp` (parâmetros de conexão) | Sim (segredo nunca retorna ao cliente) |
| Status | `status: "Ativo"\|"Inativo"` | `ConfiguracoesErp.Status` | Sim (ação separada) |
| Unidade de Negócio | `unidadeNegocioId: string` | `ConfiguracoesErp.UnidadeNegocioId` (sem FK física; único por BU) | Não |

### Configuração de Notificações (`administration/notification-configuration`)

| Campo | Propriedade/Tipo (TS) | Entidade/Tabela | Editável |
|---|---|---|---|
| E-mail ativado | `emailAtivado: boolean` | `ConfiguracoesNotificacao.EmailAtivado` | Sim |
| E-mail remetente | `emailRemetente: string \| null` | `ConfiguracoesNotificacao.EmailRemetente` | Sim |
| Nome do remetente | `nomeRemetente: string \| null` | `ConfiguracoesNotificacao.NomeRemetente` | Sim |
| Unidade de Negócio | `unidadeNegocioId: string` | `ConfiguracoesNotificacao.UnidadeNegocioId` (sem FK física; único por BU) | Não |

> Escopo mínimo de fundação (decisão formal do Product Owner, O1.11 #24): apenas canal e-mail; sem catálogo de eventos configuráveis nesta sprint.

### Feature Flags (`administration/feature-flags`)

| Campo | Propriedade/Tipo (TS) | Entidade/Tabela | Editável |
|---|---|---|---|
| Nome | `nome: string` | `FeatureFlags.Nome` | Sim (só na criação) |
| Descrição | `descricao: string` | `FeatureFlags` (coluna de descrição) | Sim (só na criação) |
| Status por Unidade de Negócio | `status: FeatureFlagStatusUnidade[]` (`unidadeNegocioId`, `unidadeNegocioNome`, `ativa`) | `FeatureFlagsUnidadesNegocio` ⋈ `UnidadesNegocio` (sem FK física) | Sim (toggle por BU via `PATCH .../unidades-negocio/{id}`) |

### Parâmetros (`administration/parameters`)

| Campo | Propriedade/Tipo (TS) | Entidade/Tabela | Editável |
|---|---|---|---|
| Chave | `chave: string` | `Parametros` (coluna de chave) | Sim (só na criação — `ParametroAtualizarInput` não tem `chave`) |
| Valor | `valor: string` | `Parametros` (coluna de valor) | Sim |
| Descrição | `descricao: string` | `Parametros` (coluna de descrição) | Sim |
| Unidade de Negócio | `unidadeNegocioId: string \| null` | `Parametros.UnidadeNegocioId` (`null` = parâmetro global) | Sim (só na criação) |

### Alçadas de Aprovação (`administration/alcadas`)

| Campo | Propriedade/Tipo (TS) | Entidade/Tabela | Editável |
|---|---|---|---|
| Nome | `nome: string` | `AlcadasAprovacao` (coluna de nome) | Sim |
| Critério | `criterio: CriterioAlcada` (enum 0/1/2 — Valor/Categoria/CentroCusto) | `AlcadasAprovacao.CriterioAlcada` (int) | Sim |
| Valor mínimo / máximo | `valorMinimo`/`valorMaximo: number \| null` | `AlcadasAprovacao` (colunas decimais) | Sim |
| Centro de Custo (metadado) | `centroCustoMetadadoId: string \| null` | Referência fraca a `CentrosCustoMetadados.Id` (sem FK física) | Sim |
| Nível | `nivel: number` | `AlcadasAprovacao.Nivel` | Sim |
| Aprovador Usuário / Perfil | `aprovadorUsuarioId`/`aprovadorPerfilId: string \| null` | Referência fraca a `Usuarios.Id`/`Perfis.Id` (sem FK física — exatamente um dos dois) | Sim |
| Status | `status: "Ativo"\|"Inativo"` | `AlcadasAprovacao.Status` | Sim (ação separada) |
| Unidade de Negócio | `unidadeNegocioId: string` | `AlcadasAprovacao.UnidadeNegocioId` (sem FK física) | Não (path explícito, não claim de sessão) |

### Regras Orçamentárias (`administration/orcamento`)

| Campo | Propriedade/Tipo (TS) | Entidade/Tabela | Editável |
|---|---|---|---|
| Nome | `nome: string` | `RegrasOrcamentarias` (coluna de nome) | Sim |
| Centro de Custo (metadado) | `centroCustoMetadadoId: string` | `RegrasOrcamentarias.CentroCustoMetadadoId` (índice composto, sem FK física) | Sim |
| Valor limite | `valorLimite: number` | `RegrasOrcamentarias` (coluna decimal) | Sim |
| Período | `periodo: PeriodoOrcamentario` (enum 0/1/2 — Mensal/Trimestral/Anual) | `RegrasOrcamentarias.Periodo` (int) | Sim |
| Status | `status: "Ativo"\|"Inativo"` | `RegrasOrcamentarias.Status` | Sim (ação separada) |
| Unidade de Negócio | `unidadeNegocioId: string` | `RegrasOrcamentarias.UnidadeNegocioId` | Não |

### Regras de Workflow (`administration/workflow`)

| Campo | Propriedade/Tipo (TS) | Entidade/Tabela | Editável |
|---|---|---|---|
| Nome | `nome: string` | `RegrasWorkflow` (coluna de nome) | Sim |
| Tipo de processo | `tipoProcesso: string` | `RegrasWorkflow` (coluna de tipo de processo) | Sim |
| Ordem | `ordem: number` | `RegrasWorkflow.Ordem` | Sim |
| Status | `status: "Ativo"\|"Inativo"` | `RegrasWorkflow.Status` | Sim (ação separada) |
| Unidade de Negócio | `unidadeNegocioId: string` | `RegrasWorkflow.UnidadeNegocioId` (sem FK física) | Não |

### Monitoramento Operacional (`administration/operational-monitoring`)

> Tela de auditoria/consulta — nenhum campo é editável (sem formulário de criação/edição; apenas listagem e detalhe).

| Campo | Propriedade/Tipo (TS) | Entidade/Tabela | Editável |
|---|---|---|---|
| Sistema origem | `sistemaOrigem: string` | `SincronizacoesFornecedores` (coluna de sistema ERP) | Não |
| Unidade de Negócio (Business Unit) | `businessUnit: string` | `SincronizacoesFornecedores.BusinessUnit` (texto livre, não `UnidadeNegocioId`) | Não |
| Data início / fim | `dataInicio: string` / `dataFim: string \| null` | `SincronizacoesFornecedores` | Não |
| Status | `status: "Sucesso"\|"Parcial"\|"Erro"` | `SincronizacoesFornecedores.Status` | Não |
| Totais (consultado/incluído/atualizado/sem alteração/erro) | `totalConsultado`/`totalIncluido`/`totalAtualizado`/`totalSemAlteracao`/`totalErro: number` | `SincronizacoesFornecedores` | Não |
| Tempo de execução | `tempoExecucaoMs: number` | `SincronizacoesFornecedores` | Não |
| Erros (detalhe) | `erros: ErroSincronizacaoFornecedor[]` (`fornecedorIdentificacao`, `mensagem`, `dataHora`) | `ErrosSincronizacoesFornecedores` (FK Cascade → `SincronizacoesFornecedores`) | Não |
| Histórico por fornecedor (auditoria #32) | `FornecedorSincronizacaoHistorico` (`erpFornecedorId`, `direcao`, `status`, `decisao`, `camposAlterados`, `tentativa`, `duracaoMs`, ...) | `FornecedoresSincronizacoes` | Não |

**Cobertura:** 15 dos 16 diretórios de `administration/*` documentados acima (14 telas administrativas + Monitoramento Operacional). Não incluído: nenhum tipo TS dedicado foi encontrado para um módulo próprio de "Conhecimento Linx" no frontend — o backend (`LinxKnowledgeController`) não possui, até esta sprint, uma Vertical Slice de tela correspondente em `administration/*` (consistente com a nota já registrada na fatia O1.13.5: "sem frontend administrativo dedicado nesta fundação").
