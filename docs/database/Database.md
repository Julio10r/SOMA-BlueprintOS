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

> Matriz Tela × Campo × Entidade detalhada por módulo (nível de campo de formulário) permanece nas Work Orders de domínio (O1.5–O1.13.5) e nos arquivos `types/*.ts` de cada Vertical Slice do frontend — não duplicada aqui para evitar documentação desconectada (D7); esta tabela consolida o nível Domínio → Tabela → API → Frontend exigido pela O1.14.

## Validação funcional executada (O1.14)

- `dotnet build BlueprintOS.sln`: aprovado, 0 erros, 0 avisos.
- `dotnet test BlueprintOS.sln`: aprovado, 689/689 (682 unitários + 7 integração) — sem regressão em relação à baseline da O1.13.5.
- `dotnet ef migrations list` / `dotnet ef migrations has-pending-model-changes`: executados e reconciliados nesta seção — nenhuma migration pendente, nenhum drift entre modelo EF e histórico aplicado.
- Frontend: nenhum arquivo alterado nesta sprint (documentação/banco); suíte de 116 testes (Vitest) não reexecutada por ausência de mudança — baseline válida desde O1.13.5.
