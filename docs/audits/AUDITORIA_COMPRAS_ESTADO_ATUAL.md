# Auditoria Completa — Estado Atual do +Compras (SOMA BlueprintOS)

Data da auditoria: 19/08/2026
Ambiente: backend `http://localhost:5262` (BlueprintOS.Api), frontend `http://localhost:5173` (Vite dev server)
Metodologia: leitura de código-fonte (frontend `frontend/web/src`, backend `backend/src`) + navegação real via Chrome DevTools MCP (login OTP, snapshots de acessibilidade, console e network) + inspeção dos endpoints do backend.
Regra seguida: **somente auditoria** — nenhuma alteração de código, layout, dados ou git foi feita.

Legenda de evidência usada em todo o documento:
- **[VISUAL]** — elemento existe na tela (botão, campo, badge) mas seu comportamento não foi verificado.
- **[FRONTEND]** — existe handler/chamada de API no código React, mas a chamada não foi exercitada nesta auditoria.
- **[EXECUTADO]** — a ação foi de fato clicada/observada durante esta auditoria (com screenshot/network confirmando).
- **[BACKEND]** — existe endpoint mapeado no `BlueprintOS.Api` correspondente.
- **[MOCK]** — dado estático no código frontend, sem chamada de API.
- **[REAL]** — dado veio de uma chamada de rede observada retornando 2xx do backend real.
- **[INDETERMINADO]** — não foi possível confirmar por código nem por execução dentro do escopo desta auditoria.

---

## 1. Resumo executivo

O +Compras hoje é um portal **híbrido**: um núcleo administrativo (Perfis, Usuários, Filiais, Centros de Custo, Unidades de Alocação/Negócio, e as telas técnicas de Governança/Configuração) com integração real e razoavelmente completa ao backend `BlueprintOS.Api`, convivendo com 5 telas de domínio de negócio (**Pedidos, Negociações, Indicadores, Agentes IA, Configurações**) que são **100% mock**, sem nenhuma chamada de API, e que o próprio código já rotula como "Em desenvolvimento" ou "Visão futura".

Total de telas mapeadas na navegação: **21** (Dashboard, Fornecedores, Pedidos, Negociações, Indicadores, Regras de Workflow, Alçadas de Aprovação, Regras Orçamentárias, Perfis, Usuários, Filiais, Centros de Custo, Unidades de Alocação, Unidades de Negócio, Configuração do ERP, Identity Providers, Parâmetros, Feature Flags, Configuração de Notificações, Monitoramento, Agentes IA, Configurações).

Classificação por estado (com base em código + amostragem em execução):
- **Funcional com backend real**: Dashboard (parcial), Fornecedores (fluxo CNPJ completo), Perfis, Usuários, Filiais, Centros de Custo, Unidades de Alocação, Unidades de Negócio, Regras de Workflow, Alçadas de Aprovação, Regras Orçamentárias, Identity Providers, Configuração do ERP, Parâmetros, Feature Flags, Configuração de Notificações, Monitoramento — **16 telas**.
- **Mock/placeholder explícito (rotulado no próprio produto)**: Pedidos, Negociações, Indicadores, Agentes IA, Configurações — **5 telas**.
- Nenhuma tela apresentou estado de "erro" ou "planejado sem UI" na amostragem feita; Agentes IA é a única classificada como "Planejado" mesmo dentro do grupo de telas mock (o próprio texto do produto diz "não possui Work Order aprovada").

**Achado crítico** (detalhado na seção 11): o endpoint `DELETE /parametros/{id}` (tela **Parâmetros**, Administração) executa exclusão física real no banco (`db.Parametros.Remove(...)` + `SaveChangesAsync`), com botão "Excluir" visível na UI, `window.confirm` e chamada `deleteParametro()` já implementada no frontend. Isso contradiz o padrão arquitetural declarado do projeto (nunca fazer DELETE físico; remoções devem ser inativação/status). Não foi executado durante esta auditoria (regra de não alterar dados), apenas confirmado por leitura de código em toda a cadeia (controller → use case → repositório EF Core).

Pontos fortes observados:
- Núcleo administrativo (Perfis/Usuários/Filiais/Centros de Custo/Unidades) segue um padrão consistente: listar + filtrar + criar + editar + inativar/reativar, sempre com endpoint real, sem exclusão física (exceto a exceção crítica de Parâmetros).
- O fluxo de Fornecedores (consulta CNPJ → comparação de divergências → aprovação/rejeição) tem uma state machine explícita e documentada em comentários no código (ADR-0023), com cuidado deliberado para nunca persistir dados apenas por "consultar".
- As telas mock são autodeclaradas como tal na UI ("Em desenvolvimento"/"Visão futura"), o que reduz o risco de o usuário confundir dados de demonstração com dados reais.
- Nenhum erro de console além de 1 issue de acessibilidade leve (campo de formulário sem `id`/`name`) observado na amostragem.

Lacunas/observações sem maquiagem:
- Pedidos, Negociações, Indicadores e Agentes IA — os 4 módulos que dão nome ao "ciclo de compras" propriamente dito (para além do cadastro de fornecedores e da administração) — não têm nenhuma implementação funcional; são apenas vitrines visuais com dados fixos no código-fonte.
- A rota DELETE de Parâmetros é uma inconsistência arquitetural real e ativa em produção de desenvolvimento.
- A rota `DELETE /fornecedores/{id}` existe com verbo HTTP DELETE mas na prática invoca `IInativarFornecedorUseCase` (inativação, não exclusão) — nomenclatura REST enganosa, mas comportamento correto/seguro.

---

## 2. Mapa da aplicação

```
INÍCIO
└─ Dashboard                          /                                          [REAL parcial / MOCK parcial]

FORNECEDORES
└─ Fornecedores                       /fornecedores                              [REAL]

COMPRAS
├─ Pedidos                            /pedidos                                   [MOCK]
├─ Negociações                        /negociacoes                               [MOCK]
└─ Indicadores                        /indicadores                               [MOCK]

GOVERNANÇA DE COMPRAS
├─ Regras de Workflow                 /administracao/regras-workflow             [REAL]
├─ Alçadas de Aprovação               /administracao/alcadas-aprovacao           [REAL]
└─ Regras Orçamentárias               /administracao/regras-orcamentarias        [REAL]

ADMINISTRAÇÃO
├─ Perfis                             /administracao/perfis                     [REAL]
├─ Usuários                           /administracao/usuarios                   [REAL] (verificado em execução)
├─ Filiais                            /administracao/filiais                    [REAL]
├─ Centros de Custo                   /administracao/centros-custo              [REAL]
├─ Unidades de Alocação               /administracao/unidades-alocacao          [REAL]
├─ Unidades de Negócio                /administracao/unidades-negocio           [REAL]
├─ Configuração do ERP                /administracao/configuracao-erp           [REAL]
├─ Identity Providers                 /administracao/identity-providers         [REAL]
├─ Parâmetros                         /administracao/parametros                 [REAL] (⚠ DELETE físico)
├─ Feature Flags                      /administracao/feature-flags              [REAL]
├─ Configuração de Notificações       /administracao/configuracao-notificacao   [REAL]
└─ Monitoramento                      /administracao/monitoramento              [REAL] (somente leitura)

AGENTES IA
└─ Agentes IA                         /agentes-ia                                [MOCK / Planejado]

(sem grupo de sidebar dedicado, item solto no fim)
└─ Configurações                      /configuracoes                             [MOCK]
```

Todas as rotas acima estão registradas em `frontend/web/src/core/AppRoutes.tsx`, protegidas em bloco por `RequireAuth` + `BusinessUnitGate` (nenhuma delas é pública, exceto `/login/*` e `/bootstrap/*`). Não há rota "não listada na navegação" — a árvore de `AppRoutes.tsx` e a árvore da sidebar (via snapshot de acessibilidade) coincidem 1:1.

---

## 3. Matriz de telas

| Tela | Rota | Estado | Dados | CRUD | Backend | Observações |
|---|---|---|---|---|---|---|
| Dashboard | `/` | Funcional parcial | REAL (cadastros recentes de Fornecedores) + MOCK (Pedidos/Negociações rotulados "Demo") | N/A (somente leitura) | Parcial | Cards de "Pedidos em aberto" e "Negociações ativas" mostram "--" com badge "Demo" |
| Fornecedores | `/fornecedores` | Funcional | REAL | Create, Read, Update (via aprovação de divergência), Inativar | Real | Único módulo de domínio de negócio com integração completa |
| Pedidos | `/pedidos` | Mock explícito | MOCK | N/A | Nenhum | Aviso "Em desenvolvimento" no próprio código/UI |
| Negociações | `/negociacoes` | Mock explícito | MOCK | N/A | Nenhum | Aviso "Em desenvolvimento" |
| Indicadores | `/indicadores` | Mock explícito | MOCK | N/A | Nenhum | Aviso "Em desenvolvimento" |
| Regras de Workflow | `/administracao/regras-workflow` | Funcional | REAL | CRUD + status | Real | Escopo por Unidade de Negócio |
| Alçadas de Aprovação | `/administracao/alcadas-aprovacao` | Funcional | REAL | CRUD + status | Real | Escopo por Unidade de Negócio |
| Regras Orçamentárias | `/administracao/regras-orcamentarias` | Funcional | REAL | CRUD + status | Real | Escopo por Unidade de Negócio |
| Perfis | `/administracao/perfis` | Funcional | REAL | CRUD + status | Real | Catálogo de permissões dedicado |
| Usuários | `/administracao/usuarios` | Funcional (verificado em execução) | REAL | Create, Read, Update, Inativar (sem delete) | Real | Confirmado em execução: 3 registros reais, filtro por status, ações Visualizar/Editar/Inativar |
| Filiais | `/administracao/filiais` | Funcional (metadado) | REAL | Read + Update de metadado | Real | Sem Create/Delete — Filiais vêm do ERP |
| Centros de Custo | `/administracao/centros-custo` | Funcional (metadado) | REAL | Read + Update de metadado + vínculo com Unidades de Alocação | Real | Sem Create/Delete — mesmo padrão de Filiais |
| Unidades de Alocação | `/administracao/unidades-alocacao` | Funcional | REAL | CRUD + status | Real | |
| Unidades de Negócio | `/administracao/unidades-negocio` | Funcional | REAL | Create, Read, Update (renomear), status | Real | |
| Configuração do ERP | `/administracao/configuracao-erp` | Funcional | REAL | Read + Update ("Salvar") + status | Real | 1 registro por Unidade de Negócio |
| Identity Providers | `/administracao/identity-providers` | Funcional | REAL | CRUD + status | Real | Escopo por Unidade de Negócio |
| Parâmetros | `/administracao/parametros` | Funcional (com achado crítico) | REAL | CRUD + **DELETE físico real** | Real | Ver seção 11 |
| Feature Flags | `/administracao/feature-flags` | Funcional | REAL | Create, Read, status por Unidade de Negócio | Real | Sem Update de conteúdo, só ativa/inativa por BU |
| Configuração de Notificações | `/administracao/configuracao-notificacao` | Funcional | REAL | Read + Update ("Salvar") | Real | 1 registro por Unidade de Negócio |
| Monitoramento | `/administracao/monitoramento` | Funcional (somente leitura) | REAL | Read-list + Read-detalhe | Real | Sincronizações de Fornecedores com ERP |
| Agentes IA | `/agentes-ia` | Mock / Planejado | MOCK | N/A | Nenhum | Rotulado "Visão futura...sem Work Order aprovada" |
| Configurações | `/configuracoes` | Mock explícito | MOCK | N/A | Nenhum | Reflete `appsettings.json`, mas somente leitura ilustrativa |

---

## 4. Matriz CRUD consolidada

| Módulo | Listar | Visualizar | Criar | Editar | Ativar/Inativar | Persistência | Mock/Real | Observações |
|---|---|---|---|---|---|---|---|---|
| Fornecedores | Sim | Sim | Sim (via Review) | Sim (aprovar/rejeitar divergência) | Sim (endpoint DELETE = inativação) | EF Core | Real | Verbo HTTP DELETE mapeado para inativação — nomenclatura enganosa |
| Pedidos | Sim (mock) | N/A | N/A | N/A | N/A | Nenhuma | Mock | Sem qualquer API |
| Negociações | Sim (mock) | N/A | N/A | N/A | N/A | Nenhuma | Mock | Sem qualquer API |
| Indicadores | N/A | N/A | N/A | N/A | N/A | Nenhuma | Mock | Apenas cards/gráfico estáticos |
| Regras de Workflow | Sim | N/A (linha de tabela) | Sim | Sim | Sim (status) | EF Core | Real | |
| Alçadas de Aprovação | Sim | N/A | Sim | Sim | Sim (status) | EF Core | Real | |
| Regras Orçamentárias | Sim | N/A | Sim | Sim | Sim (status) | EF Core | Real | |
| Perfis | Sim | Sim (detalhe) | Sim | Sim | Sim (status) | EF Core | Real | |
| Usuários | Sim | Sim (detalhe) | Sim | Sim | Sim (status, "Inativar") | EF Core | Real | Sem delete físico — confirmado na UI e no controller |
| Filiais | Sim | Sim (detalhe) | N/A (origem ERP) | Sim (metadado) | N/A | EF Core | Real | |
| Centros de Custo | Sim | Sim (detalhe) | N/A (origem ERP) | Sim (metadado + vínculos) | N/A | EF Core | Real | |
| Unidades de Alocação | Sim | Sim (detalhe) | Sim | Sim | Sim (status) | EF Core | Real | |
| Unidades de Negócio | Sim | N/A | Sim | Sim (renomear) | Sim (status) | EF Core | Real | |
| Configuração do ERP | Sim (1 por BU) | N/A | N/A (Obter/Salvar) | Sim | Sim (status) | EF Core | Real | |
| Identity Providers | Sim | N/A | Sim | Sim | Sim (status) | EF Core | Real | |
| Parâmetros | Sim | N/A | Sim | Sim | N/A (sem status) | EF Core | Real | **DELETE físico real e ativo** — ver seção 11 |
| Feature Flags | Sim | N/A | Sim | N/A | Sim (status por BU) | EF Core | Real | |
| Configuração de Notificações | Sim (1 por BU) | N/A | N/A (Obter/Salvar) | Sim | N/A | EF Core | Real | |
| Monitoramento | Sim | Sim (detalhe) | N/A | N/A | N/A | EF Core (leitura) | Real | Módulo 100% read-only por natureza |
| Agentes IA | Sim (mock) | N/A | N/A | N/A | N/A | Nenhuma | Mock | |
| Configurações | N/A (cards estáticos) | N/A | N/A | N/A | N/A | Nenhuma | Mock | |

---

## 5. Ficha detalhada de cada tela

### 5.1 Dashboard (`/`)
- **Objetivo aparente**: visão executiva com KPIs e cadastros recentes.
- **Estado**: funcional parcial — cards de Fornecedores são reais **[REAL]**; cards de Pedidos/Negociações são explicitamente "Demo" **[VISUAL][MOCK]**.
- **Estrutura visual**: título "+COMPRAS" + "Dashboard", subtítulo, 4 cards de KPI (Fornecedores cadastrados=3, Pedidos em aberto="--"/Demo, Negociações ativas="--"/Demo, Alertas de integração=0/"Nenhum alerta"), seção "Cadastros recentes" com 3 cards de fornecedor (nome, CNPJ/CPF, tipo de pessoa, localização, contato).
- **Componentes principais**: KPI card, SupplierCard (reuso do módulo Fornecedores).
- **Ações disponíveis**: nenhuma ação de escrita; navegação implícita via sidebar.
- **CRUD**: N/A (tela somente leitura).
- **Evidência de rede [EXECUTADO]**: nenhuma chamada de API dedicada observada no carregamento do Dashboard além das globais de bootstrap/auth; os dados de "Cadastros recentes" aparentam vir de uma chamada já feita para `/fornecedores` (reaproveitada) — **[INDETERMINADO]** se há endpoint dedicado ao Dashboard, não localizado nesta auditoria.

### 5.2 Fornecedores (`/fornecedores`)
- **Objetivo aparente**: consultar CNPJ/CPF, comparar com dados do +Compras, aprovar/rejeitar divergências, ou cadastrar fornecedor novo.
- **Estado**: Funcional, com backend real completo.
- **Estrutura visual**: título "Fornecedores", linha de status dinâmica ("Informe um CNPJ..."), formulário "Consultar fornecedor" com campo texto único (CNPJ/CPF, aceita alfanumérico até 14 chars) e botão "Consultar CNPJ", painel colapsável "Detalhes técnicos" (estado da state machine, fonte, data/hora, usuário, CorrelationId).
- **Componentes principais**: `CnpjSearch`, `SupplierComparison`/`InfoCard`/`ExistingSupplierSnapshot`, `ApprovalPanel`, `NovoFornecedorPanel`, `StatusBadge`, `SituacaoCadastralBadge`.
- **Máquina de estados (código, `CadastroFornecedor.tsx`)**: `Idle → Validating → Consulting → Review → Persisting → Success`, com ramos de erro `ErrorValidacao`, `ErrorConsulta`, `ErrorPersistencia`. Comentário no código (ADR-0023) enfatiza que "Review" nunca escreve dados — só o clique explícito em "Cadastrar fornecedor" ou "Aprovar/Rejeitar" persiste.
- **Validação/máscara**: regex `^[A-Za-z0-9]{1,14}$` — aceita qualquer alfanumérico até 14 caracteres, não é uma máscara de CNPJ/CPF estrita (não valida dígito verificador no cliente) **[FRONTEND]**.
- **Provider de consulta externa**: BrasilAPI, conforme rótulo em `ConfiguracoesPage` (`Provedor: BrasilAPI`) e função `consultCnpj` em `supplierEnrichmentApi.ts` **[FRONTEND][INDETERMINADO]** (fonte final não confirmada no backend nesta auditoria).
- **Erros tratados no código**: documento inválido (`ErrorValidacao`), falha de consulta externa (`ErrorConsulta`), fornecedor já existente com reconsulta falha (tratado como sucesso degradado, não como erro — mostra dados já cadastrados com alerta) **[FRONTEND]**.
- **Tela de Review**: mostra comparação campo a campo entre dados consultados e cadastrados (`SupplierComparison`), com campos protegidos (`NomeFantasia`, `Cnpj_Cpf` — não editáveis/selecionáveis) e demais campos com checkbox de seleção para aprovar/rejeitar individualmente **[FRONTEND]**.
- **Situação cadastral que exige confirmação explícita**: `Baixada`, `Suspensa`, `Inapta` — checkbox de confirmação obrigatório antes de aprovar/cadastrar **[FRONTEND]**.
- **Novo fornecedor**: painel `NovoFornecedorPanel` com campos razão social, nome fantasia, e-mail, telefone, CEP, logradouro, número, complemento, bairro, cidade, estado — pré-preenchidos pela consulta externa quando disponíveis **[FRONTEND]**.
- **Bloco CRUD**:
  - CREATE: botão "Cadastrar fornecedor" → `createSupplierDraft()` → `POST /fornecedores` **[FRONTEND][BACKEND]**, exige permissão `FornecedorCriar` (RBAC no controller). Não executado nesta auditoria (evitado para não criar fixture).
  - READ-LIST: `GET /fornecedores` (busca por documento) — **[EXECUTADO]** (network log mostrou `GET /fornecedores?q=` 200 ao carregar a tela).
  - READ-DETAIL: `GET /fornecedores/{id}` **[BACKEND]**, não exercitado diretamente.
  - UPDATE: aprovação/rejeição de divergências → `POST /{id}/enriquecimento-cnpj/aprovar|rejeitar` **[FRONTEND][BACKEND]**; edição direta via `PUT /{id}` também existe no controller, exige `FornecedorEditar` **[BACKEND]**, sem confirmação de uso pela UI nesta auditoria — **[INDETERMINADO]**.
  - INATIVAR: `DELETE /{id}` no controller na prática chama `IInativarFornecedorUseCase` (inativação, não exclusão) **[BACKEND]**.
  - DELETE FÍSICO: não existe para Fornecedores — o verbo HTTP é DELETE mas o comportamento real é inativação.
- **Status de sincronização ERP**: existe `FornecedorSyncController` com `POST /sincronizar`, `POST /sincronizar/lote`, `GET /sincronizar-erp`, `GET /{id}/sincronizacoes` (auditoria) e `POST /{id}/garantir-erp` **[BACKEND]**; a tela de Monitoramento (`/administracao/monitoramento`) expõe consulta de sincronizações de fornecedores em `SincronizacoesFornecedoresTable`/`SincronizacaoDetalhesPage` — não navegado em detalhe nesta auditoria por prioridade de tempo, classificado **[INDETERMINADO]** quanto a retry visual.

### 5.3 Pedidos (`/pedidos`)
- **Objetivo aparente**: acompanhar pedidos de compra.
- **Estado**: Mock explícito. Comentário no código: *"Tela demonstrativa (sem chamadas de API). O domínio de Pedidos ainda não possui backend integrado."*
- **Estrutura visual**: aviso amarelo "Em desenvolvimento", tabela com 4 pedidos fixos (`PC-2026-0341`...), colunas Pedido/Fornecedor/Categoria/Valor/Status/Atualizado em, badge de status.
- **CRUD**: N/A total — nenhuma ação de escrita ou leitura dinâmica.
- **Backend**: nenhum endpoint de Pedidos localizado em `backend/src`.

### 5.4 Negociações (`/negociacoes`)
- **Objetivo aparente**: acompanhar negociações e recomendações.
- **Estado**: Mock explícito, mesmo padrão de aviso do módulo Pedidos.
- **Estrutura visual**: cards com ID, fornecedor, objetivo, badge de fase, campos "Economia estimada" e "Fase atual".
- **CRUD**: N/A. **Backend**: nenhum endpoint localizado.

### 5.5 Indicadores (`/indicadores`)
- **Objetivo aparente**: KPIs consolidados de compras.
- **Estado**: Mock explícito.
- **Estrutura visual**: 4 KPI cards fixos + gráfico de barras horizontal estático ("Participação por categoria").
- **CRUD**: N/A. **Backend**: nenhum endpoint localizado.

### 5.6 Regras de Workflow, Alçadas de Aprovação, Regras Orçamentárias (Governança de Compras)
- **Objetivo aparente**: parametrizar regras de aprovação/orçamento por Unidade de Negócio.
- **Estado**: Funcional, real.
- **Padrão comum**: todas seguem o mesmo desenho — tabela com Form + Table components dedicados (`RegraWorkflowForm/Table`, `AlcadaAprovacaoForm/Table`, `RegraOrcamentariaForm/Table`), hooks próprios (`useRegrasWorkflow`, `useAlcadasAprovacao`, `useRegrasOrcamentarias`), endpoints escopados por `unidadeNegocioId` no path.
- **CRUD**: Create (`POST`), Read-list (`GET`), Update (`PUT`), Ativar/Inativar (`PATCH .../status`) — **[BACKEND]** confirmado nos 3 controllers; execução em UI não testada nesta auditoria (fora do escopo priorizado dado o volume de telas).
- **DELETE físico**: nenhum dos 3 controllers expõe `MapDelete`.

### 5.7 Perfis (`/administracao/perfis`)
- **Objetivo aparente**: gestão de perfis de acesso (RBAC) e suas permissões.
- **Estado**: Funcional, real.
- **Estrutura**: `PerfisPage` → `PerfilTable`, `PerfilForm`, `PermissoesResumo`, `ConfirmStatusModal`, páginas de detalhe/edição dedicadas (`PerfilDetalhesPage`, `PerfilFormPage`).
- **CRUD**: `GET /perfis`, `GET /perfis/{id}`, `POST /perfis`, `PUT /perfis/{id}`, `PATCH /perfis/{id}/status` **[BACKEND]**. Endpoint auxiliar `GET /permissoes` para catálogo de permissões.
- **DELETE físico**: não existe.

### 5.8 Usuários (`/administracao/usuarios`) — verificado em execução
- **Objetivo aparente**: gestão de usuários, vínculo com Perfis e Centros de Custo.
- **Estado**: **[EXECUTADO]** — navegado ao vivo. Tela carregada com sucesso, 200 OK em `GET /api/administracao/usuarios` (observado 2x no network log, refletindo dupla chamada por render/efeito React — possível oportunidade de otimização, não um erro).
- **Estrutura visual observada**: breadcrumb/rótulo "ADMINISTRAÇÃO", título "Gestão de Usuários", texto explicativo ("Usuários recebem acesso ao +Compras por meio de Perfis e Centros de Custo. Nunca há permissão individual."), botão "Novo usuário", campo "Pesquisar", combobox "Status" (Todos/Ativo/Inativo), tabela com colunas Nome/E-mail/Perfis/Centros de Custo/Status/Ações. 3 linhas reais: "Administradora BU Teste Gate", "Julio Cesar" (o próprio usuário logado), "Maria Teste O1.6" — todos "Ativo", cada um com botões "Visualizar", "Editar", "Inativar".
- **CRUD**: Create (botão "Novo usuário" → `UsuarioFormPage`), Read-list (confirmado, real), Read-detail (`UsuarioDetalhesPage`), Update (`UsuarioFormPage` em modo edição), Inativar (botão "Inativar" com `ConfirmToggleAtivoUsuarioModal`) — todos com endpoint real (`GET/POST/PUT /usuarios`, `PATCH /usuarios/{id}/status`) **[BACKEND]**. Não há botão de exclusão física na UI, nem endpoint `MapDelete` no `UsuariosController` — **[VISUAL confirmado ausente][BACKEND confirmado ausente]**, consistente com a política do projeto.
- **Console**: 1 issue de acessibilidade — "A form field element should have an id or name attribute" (2 ocorrências) — provavelmente o campo "Pesquisar" ou o combobox "Status" sem `id`/`name` explícito.
- **Responsividade**: testada em 1024x768 nesta tela — sem overflow horizontal perceptível com o volume atual de 3 linhas; não é garantia para volumes maiores (nomes/e-mails mais longos podem forçar quebra ou scroll — **[INDETERMINADO]** em escala).

### 5.9 Filiais, Centros de Custo (metadado sobre dados de origem ERP)
- **Objetivo aparente**: complementar/gerenciar metadados de Filiais e Centros de Custo que vêm do ERP.
- **Estado**: Funcional, real.
- **Padrão**: sem Create/Delete (a entidade "nasce" no ERP) — só `GET` (listar) e `PUT` (atualizar metadado); Centros de Custo adicionalmente expõe `GET/PUT .../unidades-alocacao` para vincular Unidades de Alocação.
- **CRUD**: Read-list, Read-detail (páginas `FilialDetalhesPage`/`CentroCustoDetalhesPage`), Update (`FilialEditarPage`/`CentroCustoEditarPage`). Create e Delete físico: **N/A por design** (dado mestre do ERP).

### 5.10 Unidades de Alocação, Unidades de Negócio
- **Objetivo aparente**: estrutura organizacional usada para escopar regras, permissões e integrações.
- **Estado**: Funcional, real.
- **CRUD**: Unidades de Alocação — `GET` lista/detalhe, `POST` criar, `PUT` atualizar, `PATCH status`. Unidades de Negócio — `GET` lista, `POST` criar, `PUT` renomear, `PATCH status`. Nenhum `MapDelete`.

### 5.11 Configuração do ERP, Identity Providers, Parâmetros, Feature Flags, Configuração de Notificações, Monitoramento (telas técnicas)
- **Configuração do ERP**: 1 registro por Unidade de Negócio, `GET`/`PUT` (Salvar) + `PATCH status` — sem Create/Delete explícitos (registro é obtido ou criado implicitamente pelo "Salvar" — **[INDETERMINADO]** se `PUT` faz upsert).
- **Identity Providers**: CRUD completo por Unidade de Negócio (`GET/POST/PUT` + `PATCH status`), com seletor de Unidade de Negócio (`UnidadeNegocioSeletor`) na UI.
- **Parâmetros**: **CRUD completo incluindo DELETE físico real** — ver seção 11 (achado crítico).
- **Feature Flags**: `GET` listar, `POST` criar, `PATCH .../unidades-negocio/{id}` para alterar status por Unidade de Negócio — não há `PUT` de edição de conteúdo da flag, sugerindo que uma flag criada não pode ser editada, só ativada/desativada por BU.
- **Configuração de Notificações**: 1 registro por Unidade de Negócio, `GET`/`PUT` (Salvar) — sem status/ativação separada.
- **Monitoramento**: módulo 100% leitura — `GET /sincronizacoes-fornecedores` (lista) e `GET /sincronizacoes-fornecedores/{id}` (detalhe), com página `AuditoriaFornecedorPage` e `SincronizacaoDetalhesPage`, `StatusExecucaoBadge` para indicar status de execução. Não há endpoint de retry manual localizado nos controllers de Monitoramento — **[INDETERMINADO]** se existe ação de retry em outro controller (ex.: `FornecedorSyncController.Sync`).

### 5.12 Agentes IA (`/agentes-ia`)
- **Estado**: Mock/Planejado — o próprio código rotula: *"Visão futura do módulo Agentes IA. Tela demonstrativa, sem chamadas de API e sem estrutura funcional... conforme docs/product/PortalMapa.md (estado 'Planejado')."*
- **Estrutura**: 3 cards fixos (Agente de Triagem de CNPJ, Agente de Recomendação de Negociação, Agente de Monitoramento de Risco), todos com badge "Planejado".
- **CRUD**: N/A. **Backend**: nenhum.

### 5.13 Configurações (`/configuracoes`)
- **Estado**: Mock explícito, somente leitura ilustrativa.
- **Estrutura**: 3 grupos de cards (Integração ERP, Consulta de CNPJ, Notificações), cada item é um par label/valor estático refletindo (segundo comentário no código) o que hoje vive em `appsettings.json` do backend.
- **CRUD**: N/A. Importante notar que esta tela **não é o mesmo módulo** que "Configuração do ERP"/"Configuração de Notificações" em Administração — aquelas são reais e por Unidade de Negócio; esta é um resumo estático global.

---

## 6. Achados de UX (UX-P1/P2/P3)

- **UX-P1 (fato observado)**: existem duas telas com nomes de domínio muito parecidos e propósitos diferentes — "Configurações" (item solto, mock, global) vs. "Configuração do ERP"/"Configuração de Notificações" (dentro de Administração, reais, por Unidade de Negócio). Risco real de confusão do usuário sobre qual tela edita o quê.
- **UX-P1 (fato observado)**: o botão de "Excluir" em Parâmetros não tem nenhuma distinção visual (cor de risco, ícone de alerta) documentada até este ponto além de um `window.confirm` nativo do browser — para uma ação irreversível de exclusão física, o padrão do restante do produto usa "Inativar" com modal de confirmação dedicado (`ConfirmToggleAtivoUsuarioModal`, `ConfirmStatusModal`); Parâmetros diverge desse padrão de segurança visual.
- **UX-P2 (fato observado)**: a busca de rede mostrou chamadas duplicadas para `GET /api/administracao/usuarios` (e também `GET /bootstrap/estado`, `GET /auth/me`, `GET /me/unidades-negocio` duplicadas na carga inicial da sessão) — não é um erro funcional, mas é uma ineficiência de rede observável em quase todo carregamento de página.
- **UX-P2 (opinião/hipótese)**: a tela de Fornecedores usa um único campo de busca "CNPJ/CPF" sem indicar visualmente máscara ou feedback de formatação enquanto o usuário digita; a validação (regex alfanumérica até 14 caracteres) é mais permissiva do que a rotulagem "CNPJ/CPF" sugere ao usuário.
- **UX-P3 (opinião/hipótese)**: o painel "Detalhes técnicos" no Fornecedores (CorrelationId, estado interno) é útil para suporte, mas fica na mesma página do fluxo principal do usuário final — poderia estar atrás de uma permissão/rota de suporte, não necessariamente para todo usuário.
- **UX-P3 (fato observado)**: o texto de ajuda em Usuários ("Usuários recebem acesso ao +Compras por meio de Perfis e Centros de Custo. Nunca há permissão individual.") é um bom exemplo de comunicação de modelo mental — não encontrado em Perfis ou Filiais/Centros de Custo, que poderiam se beneficiar de frases equivalentes.

## 7. Estado visual atual + Achados visuais (VIS-P1/P2/P3)

- **VIS-P1 (fato observado)**: a issue de acessibilidade capturada no console ("A form field element should have an id or name attribute", 2 ocorrências) na tela de Usuários indica ao menos 2 campos de formulário sem atributo `id`/`name`, o que compromete labels/autofill e é detectável por ferramentas automáticas — vale checagem mais ampla em outras telas com formulário (não feita em 100% das telas nesta auditoria por tempo).
- **VIS-P2 (fato observado)**: o padrão tipográfico e de cards é consistente entre Dashboard, Fornecedores, Usuários e as telas mock (Pedidos/Negociações/Indicadores) — todas usam a mesma classe `card`/`card-heading`/`section-title`, sugerindo um sistema de design compartilhado coeso (`styles.css` único).
- **VIS-P3 (opinião/hipótese)**: o aviso "Em desenvolvimento"/"Visão futura" usa a mesma classe visual (`notice notice-warn`, cor de alerta) tanto para "ainda não foi entregue" (Pedidos) quanto para "sem Work Order aprovada" (Agentes IA) — mensagens de maturidade bem diferentes usam o mesmo peso visual, o que pode nivelar por baixo a percepção de quão distante cada módulo está.
- **VIS-P3 (fato observado)**: em 1024x768, a tabela de Usuários (6 colunas + ações) não apresentou overflow horizontal perceptível com o volume atual de dados (3 linhas, nomes/e-mails curtos); não foi possível confirmar o comportamento com volumes de dados maiores ou nomes mais longos dentro do tempo desta auditoria.

## 8. Funcionalidades ausentes ou incompletas

**A. Evidentemente incompleto** (o próprio produto assume isso):
- Pedidos, Negociações, Indicadores, Agentes IA — sem backend, dados fixos, autodeclarados "Em desenvolvimento"/"Planejado".

**B. UI sem implementação** (existe controle visual, comportamento não confirmado nesta auditoria):
- Botão "Editar" em Fornecedores (via `PUT /{id}`) não foi clicado/testado nesta auditoria.
- Ações de retry de sincronização ERP mencionadas no domínio (endpoints `Sync`/`SyncBatch`/`SyncErp`) não foram localizadas com um botão dedicado de "retry" na tela de Monitoramento dentro do tempo investigado.

**C. Implementação sem acesso claro pela UI**:
- `FornecedorDiscoveryController` (`POST /descobrir`, `GET /descobertas`, `GET /descobertas/{id}`) — não foi encontrada nenhuma tela/rota frontend que consuma esse endpoint de "descoberta" de fornecedores; pode ser um recurso backend sem UI ainda, ou consumido por processo automatizado fora do portal.

**D. Possível oportunidade de UX/produto**:
- Unificar/renomear "Configurações" (mock, global) para deixar claro que não é a mesma coisa que "Configuração do ERP"/"Configuração de Notificações" (reais, por BU).

**E. Indeterminado — requer decisão do PO**:
- Se o `DELETE /parametros/{id}` deve ser removido/trocado por inativação (alinhando com o restante do produto) ou se Parâmetros é uma exceção deliberada da política (ex.: parâmetros técnicos sem valor de auditoria histórica) — decisão de arquitetura/produto, não um bug óbvio a ser corrigido sem validação.
- Se o card "Pedidos em aberto"/"Negociações ativas" no Dashboard deve continuar mostrando "Demo" indefinidamente ou se há prazo para os domínios reais.

## 9. Mocks e placeholders

| Tela | Componente | O que simula | Visibilidade | Backend correspondente |
|---|---|---|---|---|
| Dashboard | Cards "Pedidos em aberto"/"Negociações ativas" | Contadores de domínio ainda não implementado | Visível, rotulado "Demo" | Nenhum |
| Pedidos | Tabela `pedidosMock` (4 linhas fixas) | Lista de pedidos de compra | Visível, aviso "Em desenvolvimento" | Nenhum |
| Negociações | Cards `negociacoesMock` (3 itens fixos) | Negociações em andamento | Visível, aviso "Em desenvolvimento" | Nenhum |
| Indicadores | `kpisMock` (4 KPIs) + `categoriasMock` (gráfico de barras) | KPIs consolidados de compras | Visível, aviso "Em desenvolvimento" | Nenhum |
| Agentes IA | `agentesMock` (3 cards) | Agentes de IA aplicados ao ciclo de compras | Visível, aviso "Visão futura" + badge "Planejado" em cada card | Nenhum |
| Configurações | `gruposMock` (3 grupos de parâmetros) | Parâmetros de `appsettings.json` do backend | Visível, aviso "Em desenvolvimento" | Nenhum (é distinto das telas reais de Configuração do ERP/Notificações) |

## 10. RBAC

- RBAC é aplicado no backend via `RequireAuthorization(RbacPolicies.For(PermissaoCatalogo....))`, confirmado explicitamente em `FornecedoresController` para `Create`, `ConsultCnpj` (ambos exigem `FornecedorCriar`) e `Update`/`Delete` (exigem `FornecedorEditar`) **[BACKEND]**.
- Os demais controllers de Administração (Perfis, Usuários, Filiais, etc.) não mostraram, na busca textual feita, o mesmo padrão explícito de `RequireAuthorization` por endpoint — **[INDETERMINADO]** se a autorização é aplicada em nível de grupo de rotas (`group.RequireAuthorization(...)` fora do `Map...` individual) ou middleware global; não confirmado dentro do tempo desta auditoria. Recomenda-se investigação dedicada antes de concluir que esses endpoints estão desprotegidos — a ausência de anotação por rota não implica ausência de proteção.
- Não foi possível testar RBAC visual com um segundo usuário de perfil restrito nesta auditoria (login único disponível: Julio Cesar/AZZAS 2154) — **[INDETERMINADO]** quanto a itens de sidebar escondidos por permissão para outros perfis.
- Nenhuma inconsistência flagrante UI-vs-backend foi encontrada na amostragem (o que a UI oferece como ação aparenta ter endpoint correspondente), mas a cobertura de verificação RBAC é parcial dado o escopo.

## 11. Dívidas e riscos observados

- **[CRÍTICO — achado funcional/arquitetural]** `DELETE /parametros/{id}` (`ParametrosController.Excluir` → `ExcluirParametroUseCase` → `ParametroRepository.RemoverAsync` → `db.Parametros.Remove(parametro)` + `SaveChangesAsync`) realiza **exclusão física real** no banco. A tela `ParametrosPage.tsx` tem um botão "Excluir" em `ParametroTable.tsx` totalmente ligado (`handleExcluir` → `window.confirm` → `deleteParametro()` em `parametrosApi.ts`). Isso contraria o padrão arquitetural declarado do projeto (nunca DELETE físico; remoções via inativação/status). Esta auditoria **não executou** a exclusão (para não destruir dados) — o achado é 100% por leitura de código, confirmado em toda a cadeia controller → use case → repositório.
- **[MÉDIO — nomenclatura REST enganosa]** `DELETE /fornecedores/{id}` usa verbo HTTP DELETE mas o comportamento real é inativação (`IInativarFornecedorUseCase`). Não é um risco de dado, mas é uma inconsistência semântica que pode confundir integração externa ou nova pessoa no time.
- **[BAIXO — eficiência]** Chamadas de rede duplicadas observadas em quase toda navegação (`/bootstrap/estado`, `/auth/me`, `/me/unidades-negocio`, `/api/administracao/usuarios` cada uma 2x) — sugere possível efeito de duplo-render/duplo-fetch no React (StrictMode ou hook sem guarda), sem impacto funcional aparente mas com custo de rede.
- **[BAIXO — acessibilidade]** Console reportou 2 campos de formulário sem `id`/`name` na tela de Usuários — não investigado quanto à extensão em outras telas dentro do tempo desta auditoria.
- **[INDETERMINADO — segurança/RBAC]** Cobertura de proteção por permissão nos endpoints de Administração fora de Fornecedores não foi confirmada individualmente (ver seção 10).
- **[INDETERMINADO — dados]** Fonte externa de consulta de CNPJ (BrasilAPI, conforme rótulo na UI) não foi confirmada no código do backend nesta auditoria (o rótulo pode estar desatualizado em relação ao provider real configurado).

## 12. Screenshot manifest

Nenhum arquivo de screenshot pôde ser persistido em disco nesta auditoria: a ferramenta `mcp__chrome-devtools__take_screenshot` rejeitou todos os caminhos de destino testados (incluindo o diretório de scratchpad da sessão) com o erro "not within any of the configured workspace roots" — restrição do sandbox da ferramenta MCP neste ambiente, não relacionada ao projeto. Como alternativa, toda a inspeção visual documentada neste relatório foi feita via `take_snapshot` (árvore de acessibilidade completa, com hierarquia de elementos, textos e estados) e leitura de código-fonte das páginas/componentes, o que permitiu reconstruir a estrutura visual de cada tela com um nível de detalhe equivalente para fins de auditoria, mas sem evidência fotográfica anexa.

Telas com snapshot de acessibilidade capturado nesta sessão: Dashboard (1440x900, snapshot completo com sidebar, header e cards), Fornecedores (1440x900, formulário de consulta CNPJ), Usuários (1440x900 e 1024x768, tabela com 3 registros reais).

## 13. Inventário de componentes compartilhados

| Componente | Onde usado (amostra) | Observações de consistência |
|---|---|---|
| `StatusBadge` (`shared/components/StatusBadge.tsx`) | Pedidos (mock), Fornecedores | Reuso entre módulo real e mock — bom sinal de sistema de design único |
| `SituacaoCadastralBadge` | Fornecedores | Específico do domínio de situação cadastral CNPJ |
| `PerfilTable`, `UsuarioTable`, `FilialTable`, `CentroCustoTable`, `UnidadeNegocioTable`, `UnidadeAlocacaoTable`, `RegraWorkflowTable`, `AlcadaAprovacaoTable`, `RegraOrcamentariaTable`, `IdentityProviderTable`, `FeatureFlagTable`, `ParametroTable`, `SincronizacoesFornecedoresTable` | Cada módulo administrativo tem sua própria tabela dedicada (não há um `<Table>` genérico compartilhado identificado) | Padrão repetido de "Form + Table" por módulo, mas sem componente de tabela genérico central — possível divergência sutil de comportamento (paginação, ordenação) entre módulos não auditada em detalhe |
| `ConfirmStatusModal` (Perfis), `ConfirmToggleAtivoUsuarioModal` (Usuários) | Confirmação de mudança de status | Padrão de modal de confirmação por módulo, não um `ConfirmModal` genérico único — Parâmetros usa `window.confirm` nativo em vez desse padrão, quebrando a consistência |
| `UserMenu` (`core/components/UserMenu.tsx`) | Header global | Único, compartilhado corretamente |
| `NavIcons` (`core/components/NavIcons.tsx`) | Sidebar | Ícones centralizados em um único arquivo |
| `UnidadeNegocioSeletor` | Identity Providers | Seletor de BU reutilizável, não confirmado se usado em outros módulos com escopo por BU (Workflow, Alçadas, Orçamentárias, Notificação, ERP) — **[INDETERMINADO]** se cada um reimplementa seu próprio seletor |
| `InfoCard` | Fornecedores | Card colapsável para detalhes técnicos |

## 14. O que está realmente pronto hoje

**Funcional e real**: Fornecedores (fluxo CNPJ completo), Perfis, Usuários, Filiais, Centros de Custo, Unidades de Alocação, Unidades de Negócio, Regras de Workflow, Alçadas de Aprovação, Regras Orçamentárias, Configuração do ERP, Identity Providers, Feature Flags, Configuração de Notificações, Monitoramento.

**Funcional mas com achado crítico**: Parâmetros (CRUD real, porém com DELETE físico ativo — funciona, mas viola a política declarada do projeto).

**Parcial**: Dashboard (mistura dados reais de Fornecedores com placeholders "Demo" de Pedidos/Negociações).

**Apenas visual (mock)**: Pedidos, Negociações, Indicadores, Configurações.

**Planejado/placeholder**: Agentes IA.

## 15. Perguntas para o Product Owner

1. O DELETE físico em Parâmetros é intencional (exceção deliberada à política de "nunca excluir fisicamente") ou é uma lacuna a ser substituída por inativação/status, alinhando com todos os outros módulos administrativos?
2. Existe prazo/priorização definida para os domínios de Pedidos, Negociações e Indicadores saírem do estado mock, ou eles devem continuar como vitrine visual por tempo indeterminado?
3. O módulo "Agentes IA" tem Work Order aprovada em andamento, ou permanece apenas como visão de produto sem compromisso de entrega?
4. `FornecedorDiscoveryController` (descoberta de fornecedores) é destinado a uma tela futura no portal, ou é consumido apenas por processo/integração fora da UI do +Compras?
5. A tela "Configurações" (item solto, mock) tem destino definido — será substituída, removida, ou promovida a uma tela real que agregue as configurações reais já existentes em Administração?

## 16. Confirmação de estado git (antes/depois) e veredito final

**Antes da auditoria** (conforme relatado na tarefa): branch `main`, `git status --short` mostrando apenas `M .ai/dashboard/DASHBOARD_STATE.md` (arquivo pré-existente, não tocado nesta auditoria), `origin/main...main` = 0 ahead / 14 behind.

**Depois da auditoria** (`git status --short` e `git rev-list --left-right --count origin/main...main` executados ao final):
```
 M .ai/dashboard/DASHBOARD_STATE.md
0	14
```
Estado idêntico ao inicial — nenhuma alteração inesperada no working tree. O único arquivo novo criado por esta auditoria é o próprio relatório (`.ai/AUDITORIA_COMPRAS_ESTADO_ATUAL.md`), conforme autorizado pela tarefa.

**Áreas não inspecionadas em profundidade** (para transparência, apesar da cobertura ampla obtida):
- Não foi possível salvar screenshots em disco (restrição de sandbox da ferramenta MCP de screenshot) — evidência visual documentada via snapshot de acessibilidade e código-fonte, não via imagem.
- Navegação em execução ao vivo (clique real na UI) foi feita em profundidade em Dashboard, Fornecedores e Usuários; os demais 13 módulos administrativos reais (Perfis, Filiais, Centros de Custo, Unidades de Alocação/Negócio, Regras de Workflow/Alçadas/Orçamentárias, Configuração do ERP, Identity Providers, Feature Flags, Configuração de Notificações, Monitoramento) foram auditados por leitura completa de código (rotas, controllers, use cases, repositórios) mas não navegados clique a clique nesta sessão.
- Teste de responsividade em 1440x900/1024x768 foi feito nas telas Dashboard, Fornecedores e Usuários; não foi replicado em Perfis nem em uma segunda tabela administrativa adicional, dentro do tempo disponível.
- Cobertura RBAC visual com perfil de usuário restrito não foi possível (apenas uma conta disponível para login).
- Retry de sincronização ERP e a extensão completa do módulo Monitoramento (telas de detalhe) não foram navegados em execução, apenas mapeados por código.

**Veredito final**:

AUDITORIA PARCIAL — ÁREAS NÃO INSPECIONADAS IDENTIFICADAS
