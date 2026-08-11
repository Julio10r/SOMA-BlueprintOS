# DASHBOARD_STATE

> **Read Model oficial do projeto. Documento derivado. Não é fonte de verdade. Não editar manualmente fora do fluxo `[atualizar dashboard]` ou de Work Order explícita que autorize esta edição.** Gerado a partir da leitura de `.ai/ROADMAP.md`, `.ai/BACKLOG.md`, `.ai/PROJECT_STATE.md`, `.ai/CURRENT_SPRINT.md`, `.ai/DOCUMENTATION_STRATEGY.md`, `.ai/DECISIONS.md`, `docs/product/` e da baseline oficial de datas registrada pelo Product Owner via Work Order. Único consumível por qualquer Dashboard (HTML, React, Power BI, Grafana ou tecnologia futura) — nenhuma interface pode depender diretamente dos documentos do projeto.

## Cabeçalho

| Campo | Valor |
|---|---|
| Dashboard State | v2 |
| Schema Version | 2.3.0 |
| Project Version | `v0.9.0-blueprint-foundation` |
| Última Atualização (Dashboard) | 11/08/2026 20:21 (horário de Brasília) |
| Generated At | 11/08/2026 |
| Last Update | 11/08/2026 — Execução do comando `[atualizar dashboard]`: leitura de `.ai/PROJECT_STATE.md`, `.ai/BACKLOG.md`, `.ai/CURRENT_SPRINT.md` e das Work Orders `.ai/work-orders/completed/O1.6` a `O1.13.5` identificou a conclusão formal de **oito sprints consecutivas da Onda 1 (O1.6 a O1.13)**, além da fundação arquitetural adicional O1.13.5 (Agents Especialistas Linx), que não altera o denominador dos 41 entregáveis oficiais por decisão já registrada em `.ai/BACKLOG.md`. Vinte entregáveis passam de Planejado/Em desenvolvimento para **Concluído**: #3 Seleção da Unidade de Negócio, #13 Cadastro de Unidades de Negócio, #14 Empresas e filiais, #15 Usuários, #16 Usuário por Unidade de Negócio, #18 Centros de Custo, #19 Unidades de Alocação, #20 Identity Providers por Unidade de Negócio, #21 Configuração de ERP por Unidade de Negócio, #22 Parâmetros gerais por Unidade de Negócio, #23 Feature Flags, #24 Configuração de notificações, #25 Estrutura de Workflow, #26 Configuração de alçadas, #27 Estrutura de aprovação, #28 Estrutura de controle orçamentário, #29 Administração operacional, #30 Monitor de integrações, #31 Monitor de filas e reprocessamentos, #32 Auditoria e histórico de sincronizações. Onda 1 mantém 41 entregáveis: **28 Concluído / 7 Em desenvolvimento / 6 Planejado** (antes: 8/11/22). Progresso Técnico da Onda 1 recalculado (28 ÷ 41 = 68,2927% exato, exibido como **68%**, antes 20%); Contribuição ao MVP da Onda 1 recalculada de 3,9 para **13,7 pontos** (20% × 68,2927%); Percentual Global do MVP 1.0 recalculado de 30,90% para **40,66%** exato (Foundation 20,0 + Onda 1 13,66 + Onda 2 7,0), exibido como **41%** (antes 31%). Nenhuma sprint está ativa: a pasta `.ai/work-orders/active/` contém apenas a Work Order O1.14 (Blueprint de Banco e Validação Funcional Final), cujo próprio campo Status interno permanece **Draft (Planejada)**, sem data de aprovação e sem nenhum critério de aceite marcado — não tratada como sprint iniciada. O entregável #11 "Módulo de Administração" permanece **Em desenvolvimento**, sem mudança: a última justificativa textual explícita nas fontes oficiais é anterior à O1.8 e não foi atualizada apesar de todas as suas sub-telas hoje possuírem persistência real — eventual reclassificação depende de decisão explícita do Product Owner, não realizada silenciosamente por este processo. Testes recalculados: backend 682 unitários + 7 de integração (689/689 aprovados, valor anterior de 295 estava desatualizado); frontend 116/116 aprovados. Nenhum entregável foi criado, retirado, absorvido ou substituído. Registro anterior: 11/08/2026 — Fechamento formal da Sprint O1.5 — RBAC Real (edição manual autorizada por Work Order, não execução do comando `[atualizar dashboard]`): ressalvas da Security Validation independente aceitas formalmente pelo Product Owner, entregável #17 "Perfis, papéis e permissões" concluído, #9 "Perfis de usuário simulados" reclassificado para Em desenvolvimento; Onda 1 então em 8/11/22 (20% exibido). Ver histórico deste documento no git para o texto completo de cada registro anterior. |
| Status | Fundação concluída; MVP 1.0 replanejado; Onda 1 em desenvolvimento avançado — Gate Administrativo aprovado; Autenticação (O1.4.2), Bootstrap Mode/Administrador Sênior (O1.4.3), RBAC Real (O1.5) e as oito sprints O1.6–O1.13 (Usuários, Filiais/Centros de Custo, Unidades de Alocação e seu vínculo N:N, Configuração Multi-Unidade de Negócio, Workflow/Alçadas/Orçamento, Administração Operacional/Monitoramento) implementados e formalmente concluídos, com ressalvas não bloqueantes aceitas pelo Product Owner; **nenhuma sprint ativa** no momento (O1.14 permanece Draft/Planejada, sem aprovação); 2 pendências MEDIUM herdadas da O1.4.3 continuam bloqueando explicitamente a promoção para Homologação |

## Foundation

| Campo | Valor |
|---|---|
| Status | Concluído |
| Progresso Técnico | 100% |
| Peso no MVP | 20% |
| Contribuição ao MVP | 20,0 pontos |
| Data Real | 05/08/2026 (merge em `main`, tag `v0.9.0-blueprint-foundation`) |
| Observações | Arquitetura, padrões, Publication Engine e governança de Work Orders — ver `.ai/ROADMAP.md` §"Fundação arquitetural" |

> Foundation não possui baseline de datas planejadas anterior (concluída antes da existência desta política de acompanhamento) — por isso este componente registra somente a Data Real.

## Roadmap

| Campo | Onda 1 | Onda 2 | Onda 3 | Onda 4 | Onda 5 |
|---|---|---|---|---|---|
| Nome | Fundação Funcional | Cadastros | Processo de Compras | Integrações Operacionais | Go Live - MVP 1.0 funcional |
| Objetivo | Entregar o produto completo navegável, validar sua experiência e implementar a base administrativa/configurável real do +Compras | Entregar os cadastros essenciais, sincronizados com o ERP e preparados para o processo operacional de compras | Entregar o fluxo completo de compras, da solicitação à criação do pedido | Completar o fluxo operacional após o pedido, integrando recebimento, fiscal e pagamento | Homologar, estabilizar e colocar a versão 1.0 em produção com segurança operacional |
| Resultado Esperado | Produto completo navegável, base administrativa funcional e especificação suficiente para implementar os módulos reais sem redesenhar o produto | Cadastros básicos operacionais e disponíveis para o ciclo de compras | Primeiro ciclo completo de compras funcionando ponta a ponta até o pedido no ERP | Fluxo operacional completo entre +Compras, ERP, fiscal e financeiro | Versão 1.0 disponível em produção e operacionalmente assistida |
| Peso no MVP | 20% | 20% | 20% | 10% | 10% |
| Progresso Técnico | 68% | 35% | 0% | 0% | 0% |
| Contribuição ao MVP | 13,7 pontos | 7,0 pontos | 0,0 pontos | 0,0 pontos | 0,0 pontos |
| Status | Em desenvolvimento | Planejado | Planejado | Planejado | Planejado |
| Gate | Frontend navegável e Administração aprovados | Cadastros homologados | Processo completo de compras funcionando ponta a ponta | Integrações ERP, Fiscal e Pagamentos homologadas | Go Live aprovado |
| Critério do Gate | Produto navegável, Administração operável, blueprint de banco completo e aprovado | Todos os cadastros operáveis pelo frontend e sincronizados com o ERP | Ciclo solicitação→pedido operável de ponta a ponta, com aprovação e orçamento funcionando | Integrações operando ponta a ponta, sem alteração estrutural no ERP | Homologação aprovada, observabilidade/segurança mínimas operando, performance validada |
| Duração Planejada | 12 dias corridos | 15 dias corridos | 15 dias corridos | 12 dias corridos | 10 dias corridos |
| Início Planejado | 03/08/2026 | 15/08/2026 | 30/08/2026 | 14/09/2026 | 26/09/2026 |
| Fim Planejado | 14/08/2026 | 29/08/2026 | 13/09/2026 | 25/09/2026 | 05/10/2026 |
| Início Real | 03/08/2026 | — | — | — | — |
| Fim Real | — | — | — | — | — |
| Início Replanejado | — | — | — | — | — |
| Fim Replanejado | — | — | — | — | — |
| Observações | — | Fornecedores já implementado tecnicamente antes do replanejamento (B1/B2/B2.1-B2.2) | Depende da Onda 2 | Depende da Onda 3 | Depende das Ondas 1-4 |

> **Dois indicadores distintos, nunca misturados:**
> - **Progresso Técnico** — quanto dos entregáveis da Onda já está tecnicamente realizado, independentemente de a Onda ter sido formalmente iniciada. Fórmula: (entregáveis com status "Concluído" ÷ total de entregáveis da Onda), com a parcela de entregáveis "Em desenvolvimento" somada apenas quando esse entregável possuir percentual individual explicitamente registrado na tabela de Entregáveis — na ausência de percentual individual, o item "Em desenvolvimento" não contribui nenhuma fração ao Progresso Técnico (nunca estimado silenciosamente). Isso representa o **progresso técnico mínimo confirmado**, nunca um valor otimista. O Status formal da Onda (Planejado, Em desenvolvimento, Concluído) descreve o estágio de execução do cronograma, mas **não impede** que o Progresso Técnico já realizado seja contado.
> - **Contribuição ao MVP** — quanto a Onda contribui, em pontos, para o Percentual Global do MVP 1.0. Fórmula: Peso Gerencial da Onda × Progresso Técnico da Onda. Contribui proporcionalmente **mesmo quando o Status da Onda ainda é "Planejado"** — o progresso técnico já comprovado sempre soma ao MVP Global, independentemente do início formal da execução da Onda.
>
> Exemplo vigente — Onda 2: 17 entregáveis, 6 concluídos, 3 em desenvolvimento (sem percentual individual registrado), 8 planejados → Progresso Técnico = 6 ÷ 17 = 35,29% → exibido como **35%**. Contribuição ao MVP = Peso Gerencial (20%) × Progresso Técnico (35%) = **7,0 pontos**, mesmo com Status = **Planejado**.

> **Baseline de datas:** a Data Planejada de cada Onda é imutável a partir deste registro (baseline oficial definida em 03/08/2026). Ao concluir uma Onda, sua Data Real é registrada e as Datas Replanejadas das Ondas seguintes são recalculadas a partir do desvio observado — as Datas Planejadas originais nunca são alteradas.

## Entregáveis

### Onda 1 — Fundação Funcional

| Entregável | Status | Observações |
|---|---|---|
| Arquitetura de login por Unidade de Negócio | Concluído | O1.4.2 implementou a arquitetura recomendada pela Security Design Review (O1.4.1): resolução de Unidade de Negócio antes da criação da sessão, backend `Domain/Application/Infrastructure/Api.Identity`; Security Implementation Gate III aprovado com pendências não bloqueantes para Development |
| Tela de Login | Concluído | O1.4.2 — Login Passwordless OTP e Sessão Segura: backend real (`Api.Identity`) + frontend `auth/` (LoginPage), OTP hash+salt/uso único/rate limiting, sessão server-side, cookie seguro, CSRF/CORS/headers; 275/275 testes de backend e 38/38 de frontend aprovados; Security Implementation Gate III aprovado com pendências não bloqueantes para Development; validação end-to-end contra o banco compartilhado real bloqueada por dessincronização pré-existente de migrations, não relacionada a este entregável |
| Seleção da Unidade de Negócio | Concluído | O1.11 — Fundação Multi-Unidade de Negócio e Configuração (11/08/2026): `business-unit/*` real, `MeController` (`GET /me/unidades-negocio`), gate de seleção coberto por `BusinessUnitGate.test.tsx`. Ver `.ai/work-orders/completed/O1.11-FundacaoMultiUnidadeDeNegocioEConfiguracao.md` |
| Shell principal da aplicação | Em desenvolvimento | AppShell.tsx existe no Portal +Compras (header e navegação por módulos); não contempla ainda os módulos de Administração da Onda 1 |
| Menu e navegação completa | Em desenvolvimento | Navegação por react-router-dom existe para os módulos atuais; não cobre ainda os itens de Administração |
| Dashboard inicial do produto | Em desenvolvimento | Dashboard.tsx busca fornecedores reais via API; demais indicadores (pedidos, negociações) permanecem mockados |
| Frontend mockado navegável da v1.0 | Em desenvolvimento | Módulos Pedidos/Negociações/Indicadores/Agentes IA/Configurações são telas demonstrativas honestas; Fornecedores conectado à API real |
| Estados de loading, vazio, sucesso e erro | Em desenvolvimento | Implementados em CnpjSearch, ApprovalPanel e CadastroFornecedor; cobertura ainda parcial nas demais telas |
| Perfis de usuário simulados | Em desenvolvimento | O1.5 (11/08/2026) entregou RBAC real com enforcement: modelo `Perfil`/`Permissao`/`PerfilPermissao`/`UsuarioPerfil` persistido, catálogo de 14 permissões, policies do ASP.NET Core e endpoints protegidos (401/403/200 comprovados). **Reclassificado de "Planejado" para "Em desenvolvimento" no fechamento formal da O1.5 (11/08/2026)**, aplicando exatamente a reclassificação já recomendada por escrito na edição anterior deste documento. Não é "Concluído": o catálogo definitivo de **Perfis** de negócio permanece pendência de conteúdo do Product Owner. **Sem percentual individual registrado, contribui 0 ao Progresso Técnico** (regra oficial: progresso mínimo confirmado, nunca estimado) |
| Contexto UnidadeNegocioId | Concluído | `SessionCurrentIdentity` (O1.4.2) resolve identidade e sessão real server-side fora de Development, substituindo o stub `DevelopmentRequestIdentity` |
| Módulo de Administração | Em desenvolvimento | Cinco Vertical Slices em `administration/`. `profiles` deixou de ser mockada na O1.5 (11/08/2026): backend, persistência e RBAC reais. `users`, `branches`, `cost-centers` e `allocation-units` permanecem mockadas, sem backend/persistência (O1.6–O1.8) |
| Bootstrap Mode e Administrador Sênior | Concluído | Work Order técnica O1.4.3 FORMALMENTE CONCLUÍDA (10/08/2026): Fundação Backend (O1.4.3.1), Conclusão Transacional e Administrador Sênior (O1.4.3.2 — compare-and-swap via `RowVersion`, 388/388 testes), Frontend Bootstrap (O1.4.3.3 — wizard completo, 53/53 testes, smoke test real ponta a ponta aprovado pelo Product Owner/CTO) e Security Self-Review dedicada (O1.4.3.4). Security Validation independente: 0 CRITICAL/0 HIGH, aprovada com ressalvas aceitas formalmente pelo Product Owner (15 findings remanescentes, nenhum bloqueante para esta etapa); 2 MEDIUM (ausência de `Cache-Control: no-store`; Bootstrap Secret sem validação de entropia) bloqueiam explicitamente a promoção para Homologação. Migrations validadas mas não aplicadas ao banco compartilhado |
| Cadastro de Unidades de Negócio | Concluído | O1.11 (11/08/2026): `UnidadesNegocioController`, `UnidadeNegocioAdminUseCases` e tela `administration/business-units/*` com backend real, protegidos por RBAC (`Sistema.Gerenciar`). Ver `.ai/work-orders/completed/O1.11-FundacaoMultiUnidadeDeNegocioEConfiguracao.md` |
| Empresas e filiais | Concluído | O1.7 — Filiais e Centros de Custo Integrados ao ERP (11/08/2026): `IFilialErpReader` real e `FilialMetadado` persistido, migration aplicada; mock removido de `administration/branches`. Ver `.ai/work-orders/completed/O1.7-FiliaisECentrosDeCustoIntegradosAoErp.md` |
| Usuários | Concluído | O1.6 — Gestão de Usuários Backend Real (11/08/2026): backend real com `Usuario.UnidadeNegocioId` como escopo obrigatório de leitura/escrita; `administration/users` consumindo API real, mock removido. Ver `.ai/work-orders/completed/O1.6-GestaoDeUsuariosBackendReal.md` |
| Usuário por Unidade de Negócio | Concluído | Mesma implementação da O1.6 (11/08/2026): todo acesso a `Usuario` é escopado obrigatoriamente por `UnidadeNegocioId` da sessão. Ver `.ai/work-orders/completed/O1.6-GestaoDeUsuariosBackendReal.md` |
| Perfis, papéis e permissões | Concluído | O1.5 — RBAC Real, **formalmente concluída em 11/08/2026** com aceite das ressalvas pelo Product Owner. Mock removido do repositório; `administration/profiles` consome API real (`/api/administracao/perfis`) com persistência em SQL Server; permissões efetivas = união dos Perfis ativos da Unidade de Negócio da sessão, resolvidas no backend a cada requisição (revogação imediata); enforcement real por policies do ASP.NET Core comprovado em host HTTP real (401/403/200) e em smoke test com banco real; 477 testes de backend e 61 de frontend aprovados. Ver `docs/architecture/rbac-o1.5.md` e `.ai/work-orders/completed/O1.5-RbacReal.md`. **Reclassificado de "Em desenvolvimento" para "Concluído" neste fechamento**: a única condição registrada que o mantinha em "Em desenvolvimento" era o aceite formal das ressalvas da Security Validation independente, satisfeita em 11/08/2026. Ressalvas remanescentes (O1.5-M1, L1-L3, I1-I4) seguem abertas e rastreadas em `.ai/BACKLOG.md`; enforcement de `Fornecedor.*`/`Pedido.*` permanece fora de escopo por decisão do Product Owner |
| Centros de Custo | Concluído | O1.7 (11/08/2026): `ICentroCustoErpReader` real e `CentroCustoMetadado` persistido; mock removido de `administration/cost-centers`. Ver `.ai/work-orders/completed/O1.7-FiliaisECentrosDeCustoIntegradosAoErp.md`. Vínculo N:N com Unidade de Alocação concluído na O1.9 (ver entregável "Unidades de Alocação") |
| Unidades de Alocação | Concluído | O1.8 — Unidades de Alocação Persistência Real (11/08/2026): backend/persistência real (Domain/Application/Infrastructure/Api), cadastro completo sem exclusão física. O1.9 (11/08/2026) concluiu o relacionamento N:N real com Centro de Custo (índice único filtrado, Unidade de Alocação padrão), encerrando a pendência da ADR-0021 (D4). Ver `.ai/work-orders/completed/O1.8-UnidadesDeAlocacaoPersistenciaReal.md` e `.ai/work-orders/completed/O1.9-CentroDeCustoXUnidadeDeAlocacaoNN.md` |
| Identity Providers por Unidade de Negócio | Concluído | O1.11 (11/08/2026): `IdentityProvidersController`, tela `administration/identity-providers/*` com backend real. Ver `.ai/work-orders/completed/O1.11-FundacaoMultiUnidadeDeNegocioEConfiguracao.md` |
| Configuração de ERP por Unidade de Negócio | Concluído | O1.11 (11/08/2026): `ConfiguracaoErpController`, tela `administration/erp-configuration/*` com backend real. Ver `.ai/work-orders/completed/O1.11-FundacaoMultiUnidadeDeNegocioEConfiguracao.md` |
| Parâmetros gerais por Unidade de Negócio | Concluído | O1.11 (11/08/2026): `ParametrosController`, tela `administration/parameters/*` com backend real. Ver `.ai/work-orders/completed/O1.11-FundacaoMultiUnidadeDeNegocioEConfiguracao.md` |
| Feature Flags | Concluído | O1.11 (11/08/2026): `FeatureFlagsController`, tela `administration/feature-flags/*` com backend real. Ver `.ai/work-orders/completed/O1.11-FundacaoMultiUnidadeDeNegocioEConfiguracao.md` |
| Configuração de notificações | Concluído | O1.11 (11/08/2026): `ConfiguracaoNotificacaoController`, migration `AddConfiguracaoNotificacaoO111` — escopo mínimo de fundação por decisão explícita do Product Owner (sem motor de envio, sem catálogo de eventos configuráveis, que fica como dívida de produto registrada). Ver `.ai/work-orders/completed/O1.11-FundacaoMultiUnidadeDeNegocioEConfiguracao.md` |
| Estrutura de Workflow | Concluído | O1.12 — Workflow, Alçadas, Aprovação e Controle Orçamentário (11/08/2026): `RegraWorkflow` persistido, migration `AddAdministracaoWorkflowAlcadaOrcamentoO112`; catálogo de `TipoProcesso` fica como dívida de produto registrada. Ver `.ai/work-orders/completed/O1.12-WorkflowAprovacaoAlcadasOrcamento.md` |
| Configuração de alçadas | Concluído | O1.12 (11/08/2026): `AlcadaAprovacao` (por Valor/Categoria/Centro de Custo, aprovador Usuário XOR Perfil). Ver `.ai/work-orders/completed/O1.12-WorkflowAprovacaoAlcadasOrcamento.md` |
| Estrutura de aprovação | Concluído | O1.12 (11/08/2026): tratada como parte de `AlcadaAprovacao` — apenas a estrutura configurável; o motor operacional de aprovação pertence à Onda 3. Ver `.ai/work-orders/completed/O1.12-WorkflowAprovacaoAlcadasOrcamento.md` |
| Estrutura de controle orçamentário | Concluído | O1.12 (11/08/2026): `RegraOrcamentaria` com FK real `CentroCustoMetadadoId`. Ver `.ai/work-orders/completed/O1.12-WorkflowAprovacaoAlcadasOrcamento.md` |
| Administração operacional | Concluído | O1.13 — Administração Operacional e Monitoramento (11/08/2026): `MonitorIntegracoesPage` + `MonitoramentoOperacionalController` reais. Ver `.ai/work-orders/completed/O1.13-AdministracaoOperacionalEMonitoramento.md` |
| Monitor de integrações | Concluído | O1.13 (11/08/2026): listagem/filtro de execuções em lote sobre dados reais de sincronização. Ver `.ai/work-orders/completed/O1.13-AdministracaoOperacionalEMonitoramento.md` |
| Monitor de filas e reprocessamentos | Concluído | O1.13 (11/08/2026): `SincronizacaoDetalhesPage` (fila de erros) com reprocessamento real. Ver `.ai/work-orders/completed/O1.13-AdministracaoOperacionalEMonitoramento.md` |
| Auditoria e histórico de sincronizações | Concluído | O1.13 (11/08/2026): `AuditoriaFornecedorPage` real sobre `FornecedorSincronizacaoRepository` (dados já existentes desde B2.1.3); RBAC ausente em `FornecedoresController`/`FornecedorSyncController` foi corrigido nesta sprint. Ver `.ai/work-orders/completed/O1.13-AdministracaoOperacionalEMonitoramento.md` |
| +Compras Funcional | Concluído | Estrutura documental inicial criada em docs/product/ComprasFuncional.md; especificação funcional completa por tela ainda pendente |
| +Compras UX | Concluído | Estrutura documental inicial criada em docs/product/ComprasUX.md; especificação de UX completa por tela ainda pendente |
| +Compras Data Model | Concluído | Estrutura documental inicial criada em docs/product/ComprasDataModel.md; modelo de dados completo por tela ainda pendente |
| Blueprint funcional do banco por tela | Planejado | — |
| Matriz tela × campo × entidade | Planejado | — |
| Matriz +Compras × ERP | Planejado | — |
| Mapeamento inicial de APIs | Planejado | — |
| Mapeamento inicial de integrações | Planejado | — |
| Validação funcional com os envolvidos | Planejado | — |

> Progresso Técnico da Onda 1 = **68%** (68,2927% exato = **28** de **41** entregáveis concluídos; 7 Em desenvolvimento — nenhum com percentual individual registrado, logo contribuindo 0 — e 6 Planejado). Contribuição ao MVP da Onda 1 = Peso Gerencial (20%) × Progresso Técnico (68,2927%) = **13,7 pontos**. Recalculado em 11/08/2026 na execução do comando `[atualizar dashboard]` (antes: 8 / 11 / 22 → 19,5122% → 3,9 pontos), por conclusão formal das sprints O1.6 a O1.13 (20 entregáveis reclassificados para "Concluído" — ver "Last Update" no cabeçalho para a lista completa).

### Onda 2 — Cadastros

| Entregável | Status | Observações |
|---|---|---|
| Cadastro de fornecedores | Concluído | B1 — CRUD completo, EF Core/SQL Server sobre MaisComprasConnection |
| Consulta e enriquecimento por CNPJ | Concluído | B2.2.1/B2.2.2 — ICnpjConsultaProvider, BrasilApiCnpjProvider |
| Comparação entre dados atuais e dados externos | Concluído | B2.2.3 — comparação campo a campo, aprovação/rejeição, atualização seletiva |
| Sincronização de fornecedores com ERP | Concluído | B2.1 — contrato canônico, adaptadores por BU, regra temporal, idempotência |
| Histórico e auditoria de sincronização | Concluído | B2.1.3 — SincronizacaoFornecedor/ErroSincronizacaoFornecedor, logs estruturados, métricas |
| Vínculo fornecedor × Unidade de Negócio × ERP | Concluído | Adaptadores por BU implementados em B2.1 |
| Catálogo de materiais | Planejado | — |
| Catálogo de serviços | Planejado | — |
| Categorias e famílias | Planejado | — |
| Vínculo item × ERP × Unidade de Negócio | Planejado | — |
| Centros de custo | Planejado | — |
| Contas contábeis | Planejado | — |
| Projetos | Planejado | — |
| Compradores | Planejado | — |
| Importação e sincronização de cadastros | Em desenvolvimento | Implementada para fornecedores (B2.1/B2.1.3); demais cadastros (materiais, serviços, centros de custo etc.) pendentes |
| Tratamento de divergências | Em desenvolvimento | Implementado para fornecedores (B2.2.3); demais cadastros pendentes |
| Reprocessamento de integrações cadastrais | Em desenvolvimento | Existe a nível de backend para fornecedores (B2.1.3); sem tela de administração |

> Progresso Técnico da Onda 2 = **35%** (6 de 17 entregáveis concluídos — ver cálculo acima). Contribuição ao MVP da Onda 2 = Peso Gerencial (20%) × Progresso Técnico (35%) = **7,0 pontos**, mesmo com Status = **Planejado** (o progresso técnico já comprovado contribui proporcionalmente ao MVP Global independentemente do início formal da execução da Onda).

### Onda 3 — Processo de Compras

| Entregável | Status | Observações |
|---|---|---|
| Lista de solicitações | Planejado | — |
| Nova solicitação de compra | Planejado | — |
| Solicitação por catálogo | Planejado | — |
| Item ou serviço fora do catálogo | Planejado | — |
| Edição de rascunho | Planejado | — |
| Anexos e justificativas | Planejado | — |
| Histórico da solicitação | Planejado | — |
| Fila de demandas para cotação | Planejado | — |
| Criação da cotação | Planejado | — |
| Seleção de fornecedores | Planejado | — |
| Registro e importação de propostas | Planejado | — |
| Mapa comparativo | Planejado | — |
| Comparação técnica e comercial | Planejado | — |
| Agente Comprador | Planejado | Não existe classe concreta de agente (SeniorBuyerAgent); apenas EchoAgent/KnowledgeAgent implementados |
| Recomendações de fornecedor | Planejado | — |
| Negociação assistida por IA | Planejado | Estratégia e memória de negociação existem em código isoladamente, sem produto de Onda 3 concluído |
| Dossiê de negociação | Planejado | — |
| Plano e estratégia de negociação | Planejado | NegotiationStrategy existe em código, sem produto de Onda 3 concluído |
| Registro de contrapropostas | Planejado | — |
| Memória de negociação | Planejado | NegotiationMemory existe em memória de processo (sem persistência durável), sem produto de Onda 3 concluído |
| Workflow por Unidade de Negócio | Planejado | — |
| Aprovação por alçada | Planejado | — |
| Aprovação por valor, categoria e centro de custo | Planejado | — |
| Controle orçamentário | Planejado | — |
| Consulta de saldo orçamentário | Planejado | — |
| Reserva, comprometimento e realizado | Planejado | — |
| Tratamento de exceções orçamentárias | Planejado | — |
| Caixa de aprovações | Planejado | — |
| Aprovar, reprovar e solicitar ajuste | Planejado | — |
| Histórico de aprovação | Planejado | — |
| Geração do pedido de compra | Planejado | — |
| Revisão do pedido | Planejado | — |
| Envio do pedido ao ERP | Planejado | — |
| Status, auditoria e reprocessamento | Planejado | — |

### Onda 4 — Integrações Operacionais

| Entregável | Status | Observações |
|---|---|---|
| Auditoria técnica das tabelas ERP envolvidas | Em desenvolvimento | Realizada para a tabela de fornecedores (B2.1); tabelas ERP de itens/pedidos ainda não auditadas |
| Estratégia de detecção de alterações por integração | Em desenvolvimento | Regra temporal e idempotência implementadas para fornecedores (B2.1); demais domínios pendentes |
| Integração de pedido com ERP | Planejado | — |
| Monitor de status do pedido | Planejado | — |
| Reprocessamento e reconciliação | Planejado | — |
| Recebimento de materiais | Planejado | — |
| Recebimento parcial | Planejado | — |
| Recebimento de serviços | Planejado | — |
| Divergências de quantidade ou qualidade | Planejado | — |
| Entrada de Nota Fiscal | Planejado | — |
| Vínculo Nota Fiscal × pedido × recebimento | Planejado | — |
| Conferência fiscal | Planejado | — |
| Tratamento e aprovação de divergências fiscais | Planejado | — |
| Envio da entrada fiscal ao ERP | Planejado | — |
| Monitor e reprocessamento fiscal | Planejado | — |
| Títulos financeiros | Planejado | — |
| Condições e parcelas | Planejado | — |
| Previsão de pagamento | Planejado | — |
| Integração de pagamento | Planejado | — |
| Status do pagamento | Planejado | — |
| Bloqueios e divergências financeiras | Planejado | — |
| Reconciliação financeira | Planejado | — |
| Logs e auditoria ponta a ponta | Planejado | — |
| Filas, retry e tratamento de falhas | Planejado | — |

### Onda 5 — Go Live - MVP 1.0 funcional

| Entregável | Status | Observações |
|---|---|---|
| Homologação com Compras | Planejado | — |
| Homologação com Fiscal | Planejado | — |
| Homologação com Financeiro | Planejado | — |
| Homologação administrativa | Planejado | — |
| Testes funcionais ponta a ponta | Planejado | — |
| Testes de integração | Planejado | — |
| Testes de regressão | Planejado | — |
| Testes de performance | Planejado | — |
| Revisão de segurança | Planejado | — |
| Revisão de permissões e segregação | Planejado | — |
| Observabilidade | Planejado | — |
| Monitoramento | Planejado | — |
| Alertas | Planejado | — |
| Runbooks operacionais | Planejado | — |
| Plano de suporte | Planejado | — |
| Correção dos defeitos de homologação | Planejado | — |
| Preparação do ambiente produtivo | Planejado | — |
| Carga inicial | Planejado | — |
| Validação das integrações produtivas | Planejado | — |
| Go Live | Planejado | — |
| Estabilização assistida | Planejado | — |
| Aceite final da versão 1.0 | Planejado | — |

## Percentual Global do MVP 1.0

**Fórmula oficial:** Percentual Global = Σ (Peso Gerencial do componente × Progresso Técnico do componente)

> A coluna "Progresso Técnico" abaixo é o mesmo Progresso Técnico registrado por componente nas seções "Foundation" e "Roadmap" acima. Cada componente contribui proporcionalmente ao MVP Global de acordo com o seu Progresso Técnico, **mesmo quando seu Status ainda é "Planejado"** — não há mais nenhuma condição de início formal da Onda para que sua contribuição seja contada.

| Componente | Peso Gerencial | Progresso Técnico | Contribuição ao MVP (pontos) |
|---|---|---|---|
| Foundation | 20% | 100% | 20,0 |
| Onda 1 | 20% | 68% | 13,7 |
| Onda 2 | 20% | 35% | 7,0 |
| Onda 3 | 20% | 0% | 0,0 |
| Onda 4 | 10% | 0% | 0,0 |
| Onda 5 | 10% | 0% | 0,0 |
| **Total** | **100%** | — | **40,7%** |

O valor exato do Total é **40,66%** (soma exata dos pontos acima: Foundation 20,0 + Onda 1 13,6585 + Onda 2 7,0; Progresso Técnico exato da Onda 1 = 28 ÷ 41 = 68,2927%); a apresentação visual em qualquer Dashboard deve arredondar este valor apenas para exibição (**41%**), mantendo o valor exato disponível em tooltip/detalhe acessível. Esta é a origem oficial da barra principal de qualquer Dashboard. Nenhum Dashboard recalcula este valor — ele apenas lê a linha "Total" acima. **A Contribuição ao MVP de cada componente é calculada durante a atualização deste documento — o Dashboard apenas renderiza os valores já calculados aqui, nunca recalcula a fórmula global em HTML ou JavaScript.**

## Resumo Executivo

> Gerado automaticamente — nunca editado manualmente.

**Situação Atual:** Fundação arquitetural concluída (tag `v0.9.0-blueprint-foundation`). MVP 1.0 avançando em 41% (40,66% exato). A Onda 1 deu um salto grande: oito sprints seguidas (O1.6 a O1.13) concluíram a base administrativa real do +Compras — Usuários, Filiais, Centros de Custo, Unidades de Alocação (com vínculo entre si), configuração multi-unidade de negócio, Workflow/Alçadas/Orçamento e o monitoramento operacional. A Onda 1 chega a 68% de progresso técnico comprovado (28 de 41 entregáveis concluídos), acima da fundação de RBAC (O1.5), Bootstrap (O1.4.3) e Login (O1.4.2), todas já concluídas com ressalvas não bloqueantes aceitas pelo Product Owner. Nenhuma sprint está em andamento agora — a próxima frente (O1.14, consolidação do blueprint de banco) ainda aguarda aprovação formal do Product Owner.

**Últimas Entregas:**
- O1.13.5 — Fundação dos Agents Especialistas Linx concluída (11/08/2026): base de conhecimento persistente e versionada com proveniência explícita e RBAC dedicado; não conta como um dos 41 entregáveis oficiais da Onda 1. Ver `.ai/work-orders/completed/O1.13.5-FundacaoAgentsEspecialistasLinx.md`.
- O1.13 — Administração Operacional e Monitoramento concluída (11/08/2026): telas reais de monitor de integrações, fila de reprocessamento e auditoria de sincronização. Ver `.ai/work-orders/completed/O1.13-AdministracaoOperacionalEMonitoramento.md`.
- O1.12 — Workflow, Alçadas, Aprovação e Controle Orçamentário concluída (11/08/2026): estruturas configuráveis reais (`RegraWorkflow`, `AlcadaAprovacao`, `RegraOrcamentaria`), sem motor operacional (fora de escopo — Onda 3). Ver `.ai/work-orders/completed/O1.12-WorkflowAprovacaoAlcadasOrcamento.md`.
- O1.11 — Fundação Multi-Unidade de Negócio e Configuração concluída (11/08/2026): Seleção/Cadastro de Unidade de Negócio, Identity Providers, ERP, Parâmetros, Feature Flags e Notificações (escopo mínimo) com backend real. Ver `.ai/work-orders/completed/O1.11-FundacaoMultiUnidadeDeNegocioEConfiguracao.md`.
- O1.10 — Conclusão do Vertical Slice concluída (11/08/2026): migração estrutural do frontend e RBAC real no menu de Administração.
- O1.9 — Centro de Custo × Unidade de Alocação (vínculo N:N real) concluída (11/08/2026). Ver `.ai/work-orders/completed/O1.9-CentroDeCustoXUnidadeDeAlocacaoNN.md`.
- O1.8 — Unidades de Alocação (Persistência Real) concluída (11/08/2026). Ver `.ai/work-orders/completed/O1.8-UnidadesDeAlocacaoPersistenciaReal.md`.
- O1.7 — Filiais e Centros de Custo Integrados ao ERP concluída (11/08/2026). Ver `.ai/work-orders/completed/O1.7-FiliaisECentrosDeCustoIntegradosAoErp.md`.
- O1.6 — Gestão de Usuários (Backend Real) concluída (11/08/2026). Ver `.ai/work-orders/completed/O1.6-GestaoDeUsuariosBackendReal.md`.
- O1.5 — RBAC Real (Perfis, Permissões, Policies, Enforcement) FORMALMENTE CONCLUÍDA (11/08/2026): 477 testes de backend e 61 de frontend aprovados. Ver `.ai/work-orders/completed/O1.5-RbacReal.md`.

**Próximos Objetivos:**
- Decidir e autorizar a próxima frente de trabalho: O1.14 (Blueprint de Banco e Validação Funcional Final) está fisicamente na pasta de Work Orders ativas, mas seu próprio status interno segue Draft/Planejada, sem aprovação do Product Owner.
- Resolver as 2 pendências MEDIUM herdadas que bloqueiam explicitamente a promoção para Homologação: `Cache-Control: no-store` em `/bootstrap/*` e validação de entropia do Bootstrap Secret.
- Product Owner decidir sobre a reclassificação do entregável "Módulo de Administração" (#11), hoje "Em desenvolvimento" por justificativa desatualizada, apesar de todas as suas sub-telas já terem persistência real.
- Consolidar, na O1.14, o blueprint funcional do banco por tela, as matrizes tela×campo×entidade e +Compras×ERP, o mapeamento de APIs/integrações e a validação funcional com os envolvidos (entregáveis #36 a #41, ainda planejados).
- Definir catálogo de eventos de notificação, catálogo de `TipoProcesso` de Workflow e critérios de negócio de Alçada — dívidas de produto registradas nas sprints O1.11/O1.12.
- Aprovação formal da Onda 1 pelo Product Owner (Gate: Frontend navegável e Administração aprovados); Onda 1 ainda sem Fim Real registrado.

**Próximo Marco:** Decisão e aprovação formal do Product Owner para iniciar a O1.14 (consolidação do blueprint de banco e validação funcional final), resolução das duas pendências MEDIUM que bloqueiam a Homologação, e aprovação formal do Gate da Onda 1. Prazo planejado original da Onda 1: 14/08/2026 (sem Fim Real ainda registrado).

**Principais Riscos:**
- Duas pendências MEDIUM herdadas da Security Validation do Bootstrap continuam bloqueando explicitamente a promoção para Homologação: ausência de `Cache-Control: no-store` em `/bootstrap/*` e falta de validação de entropia do Bootstrap Secret.
- Migrations de Autenticação e Bootstrap validadas mas ainda não aplicadas ao banco compartilhado — decisão operacional pendente.
- Vínculo Usuário × Centro de Custo (O1.6) não está em transação explícita compartilhada com a criação do usuário — risco de registro órfão de metadado local em caso de corrida; sem risco de escalonamento ou de corrupção entre Unidades de Negócio; revisão consolidada prevista para depois da O1.14.
- `BusinessUnit` continua registrada como texto livre (não FK) em `SincronizacaoFornecedor`/`FornecedorSincronizacao`/`Fornecedor` — isolamento multi-unidade de negócio não é referencial nessa área; dívida herdada, deferida para o Gate Final da Onda 1.
- O1.6 não recebeu Security Validation independente dedicada (apenas autorrevisão) — recomendada cobertura consolidada de segurança antes do Gate Final da Onda 1.
- Onda 1 ainda sem Fim Real registrado — os 6 entregáveis restantes (#36 a #41, blueprint de banco e validação funcional) dependem da O1.14, ainda não aprovada. Prazo planejado original: 14/08/2026.
- Controle de acesso ainda não aplicado às telas de Fornecedores e Negociações — decisão do Product Owner manteve isso fora de escopo; pendência registrada no backlog.
- Dados de teste não limpos no banco de desenvolvimento (decisão deliberada do Product Owner, não bloqueante para Development): 4 Perfis inativos de smoke test (O1.5) e a usuária "Maria Teste O1.6"; saneamento previsto antes da promoção para Homologação.
- Bug pré-existente não relacionado a estas sprints: `/fornecedores?q=...` retorna 500 por dessincronização de schema, ainda registrado como pendência aberta no backlog.

## Roadmap dos Produtos

> Gerado automaticamente a partir do Roadmap oficial e da documentação de escopo — nunca editado manualmente. Fonte para a seção "Roadmap dos Produtos" da aba Executive; o Dashboard não lê `.ai/ROADMAP.md` ou `.ai/BACKLOG.md` diretamente.

**MVP 1.0:**
- **Objetivo geral:** Entregar a primeira versão funcional do +Compras em produção, cobrindo o fluxo completo de compras — da solicitação ao pagamento — com base administrativa, cadastros, processo de compras, integrações operacionais e Go Live homologado.
- **Percentual Global Atual:** 40,66% (exibido como 41%)
- **Onda Atual:** Onda 1 — Fundação Funcional (Em desenvolvimento)
- **Marco Final:** Go Live - MVP 1.0 funcional

**MVP 1.1:**
- **Objetivo geral:** Expandir o +Compras após a estabilização do MVP 1.0, adicionando capacidades avançadas, canais externos, inteligência analítica e governança ampliada.
- **Escopo adiado:** ESG, Portal de Fornecedores, Marketplace, Analytics avançado, Previsão de Demanda, Previsão de Preços, Jurídico, Compliance, Gestão de Riscos

## Métricas

| Métrica | Valor | Origem |
|---|---|---|
| Total de Work Orders (catálogo) | 56 | `.ai/BACKLOG.md` |
| Work Orders concluídas | 34 (lista completa em `.ai/work-orders/completed/`) | `.ai/BACKLOG.md` |
| APIs | `GET /health`, CRUD de fornecedores, descoberta de fornecedores, consulta CNPJ, recomendação de negociação, Identity/Bootstrap/RBAC, Usuários/Filiais/Centros de Custo/Unidades de Alocação, Unidades de Negócio/Identity Providers/ERP/Parâmetros/Feature Flags/Notificações, Workflow/Alçadas/Orçamento, Monitoramento Operacional, Conhecimento Linx | `.ai/PROJECT_STATE.md` |
| Telas | 0 concluídas / 19 previstas (índice `docs/product/`) | `docs/product/ComprasFuncional.md` |
| Entidades | Não registradas ainda em `docs/product/ComprasDataModel.md` | `docs/product/ComprasDataModel.md` |
| Integrações | ERP (fornecedores, filiais e centros de custo), BrasilAPI (CNPJ, implementada) | `.ai/PROJECT_STATE.md` |
| Agentes | `EchoAgent`, `KnowledgeAgent`, `LinxErpSpecialistAgent`, `LinxDatabaseSpecialistAgent` (O1.13.5) | `.ai/PROJECT_STATE.md` |
| Testes | Backend: 682 unitários + 7 de integração (689/689 aprovados). Frontend: 116/116 aprovados. Última execução registrada: O1.13.5 (11/08/2026) | `.ai/PROJECT_STATE.md` |
| Documentos oficiais | 6 (`Executive Report`, `Product Blueprint`, Documentação Técnica, `+Compras Funcional`, `+Compras UX`, `+Compras Data Model`) | `.ai/DOCUMENTATION_STRATEGY.md` |

Métricas sem dado oficial disponível não são exibidas com valor estimado — permanecem ausentes desta seção até existir fonte real.

## Decisões Recentes

| Data | Categoria | Resumo | Documento de origem |
|---|---|---|---|
| 11/08/2026 | Governança | Execução do comando `[atualizar dashboard]`: leitura de `.ai/PROJECT_STATE.md`, `.ai/BACKLOG.md`, `.ai/CURRENT_SPRINT.md` e das Work Orders O1.6 a O1.13.5 identificou a conclusão formal das oito sprints O1.6–O1.13; 20 entregáveis reclassificados para Concluído; Onda 1 passa de 8/11/22 (20%) para 28/7/6 (68%); Contribuição ao MVP da Onda 1 de 3,9 para 13,7 pontos; Percentual Global do MVP 1.0 de 30,90% para 40,66% exato (exibido 41%, antes 31%) | `.ai/dashboard/DASHBOARD_UPDATE_COMMAND.md` (execução do comando) |
| 11/08/2026 | Sprint | O1.13.5 — Fundação dos Agents Especialistas Linx FORMALMENTE CONCLUÍDA: base de conhecimento persistente e versionada (`LinxKnowledgeEntry`) com proveniência explícita, RBAC dedicado (`ConhecimentoLinx.Aprovar` separado de `ConhecimentoLinx.Gerenciar`), leitor read-only do ERP comprovadamente incapaz de escrita; dois Agents especialistas (`LinxErpSpecialistAgent`, `LinxDatabaseSpecialistAgent`); 0 CRITICAL/0 HIGH na self-review; migration `AddLinxKnowledgeO1135` aplicada ao banco de desenvolvimento; backend 682 testes unitários + 7 integração (689/689). Não corresponde a nenhum dos 41 entregáveis oficiais — denominador da Onda 1 inalterado por esta sprint | `.ai/work-orders/completed/O1.13.5-FundacaoAgentsEspecialistasLinx.md` |
| 11/08/2026 | Sprint | O1.13 — Administração Operacional e Monitoramento FORMALMENTE CONCLUÍDA: telas reais de Monitor de Integrações, Monitor de Filas/Reprocessamento e Auditoria de Sincronização sobre dados já persistidos (B2.1.3); RBAC ausente em `FornecedoresController`/`FornecedorSyncController` corrigido. Entregáveis #29–#32 → Concluído | `.ai/work-orders/completed/O1.13-AdministracaoOperacionalEMonitoramento.md` |
| 11/08/2026 | Sprint | O1.12 — Workflow, Alçadas, Aprovação e Controle Orçamentário FORMALMENTE CONCLUÍDA: `RegraWorkflow`, `AlcadaAprovacao` (Valor/Categoria/Centro de Custo) e `RegraOrcamentaria` (FK real `CentroCustoMetadadoId`) — estruturas configuráveis reais, sem motor operacional (fora de escopo, Onda 3). Entregáveis #25–#28 → Concluído | `.ai/work-orders/completed/O1.12-WorkflowAprovacaoAlcadasOrcamento.md` |
| 11/08/2026 | Sprint | O1.11 — Fundação Multi-Unidade de Negócio e Configuração FORMALMENTE CONCLUÍDA (7/7 entregáveis): Seleção/Cadastro de Unidade de Negócio, Identity Providers, ERP, Parâmetros, Feature Flags e Notificações (escopo mínimo de fundação, por decisão do Product Owner) com backend real e RBAC via `Sistema.Gerenciar`. Entregáveis #3, #13, #20–#24 → Concluído | `.ai/work-orders/completed/O1.11-FundacaoMultiUnidadeDeNegocioEConfiguracao.md` |
| 11/08/2026 | Sprint | O1.10 — Conclusão do Vertical Slice FORMALMENTE CONCLUÍDA: migração estrutural pura do frontend (remoção da pasta `pages/`) e RBAC real filtrando itens de menu de Administração pelo mesmo padrão de Perfis. Nenhum entregável muda de status nesta sprint | `.ai/work-orders/completed/O1.10-ConclusaoVerticalSlice.md` |
| 11/08/2026 | Sprint | O1.9 — Centro de Custo × Unidade de Alocação (Vínculo N:N Real) FORMALMENTE CONCLUÍDA: relacionamento N:N real com Unidade de Alocação padrão (índice único filtrado), encerrando a pendência da ADR-0021 (D4). Entregável #19 permanece Em desenvolvimento até a O1.8 concluir a persistência de base — ambos concluídos nesta janela | `.ai/work-orders/completed/O1.9-CentroDeCustoXUnidadeDeAlocacaoNN.md` |
| 11/08/2026 | Sprint | O1.8 — Unidades de Alocação (Persistência Real) FORMALMENTE CONCLUÍDA: backend/persistência real (Domain/Application/Infrastructure/Api), cadastro completo pelo +Compras sem exclusão física. Entregável #19 → Concluído | `.ai/work-orders/completed/O1.8-UnidadesDeAlocacaoPersistenciaReal.md` |
| 11/08/2026 | Sprint | O1.7 — Filiais e Centros de Custo Integrados ao ERP FORMALMENTE CONCLUÍDA: `IFilialErpReader`/`ICentroCustoErpReader` reais, `FilialMetadado`/`CentroCustoMetadado` persistidos, mocks removidos de `administration/branches` e `administration/cost-centers`. Entregáveis #14, #18 → Concluído | `.ai/work-orders/completed/O1.7-FiliaisECentrosDeCustoIntegradosAoErp.md` |
| 11/08/2026 | Sprint | O1.6 — Gestão de Usuários (Backend Real) FORMALMENTE CONCLUÍDA: backend real com `Usuario.UnidadeNegocioId` como escopo obrigatório de leitura/escrita; `administration/users` consumindo API real, mock removido; sem Security Validation independente dedicada (apenas autorrevisão, risco registrado). Entregáveis #15, #16 → Concluído | `.ai/work-orders/completed/O1.6-GestaoDeUsuariosBackendReal.md` |
| 10/08/2026 | Planejamento | Consolidação e Plano Executável de Conclusão da Onda 1: reconciliação dos 41 entregáveis oficiais (nenhum retirado, absorvido ou substituído; métrica inalterada — 7 Concluído / 11 Em desenvolvimento / 23 Planejado / 17%); decisões D1–D8 do Product Owner registradas na ADR-0021; 10 novas Work Orders propostas (O1.5–O1.14, todas Draft/Planejada, nenhuma aprovada/ativa); `PortalMaisComprasFrontend.md` movida para o novo diretório `superseded/`. Sessão exclusivamente documental — nenhum código, migration, frontend, backend, commit ou push | `docs/audits/Onda1-Reconciliacao-e-Plano-Execucao.md`, `.ai/DECISIONS.md` (ADR-0021), `.ai/BACKLOG.md` |
| 10/08/2026 | Segurança | Work Order técnica O1.4.3 (Bootstrap Mode e Administrador Sênior) FORMALMENTE CONCLUÍDA. Security Validation independente por revisor isolado: 0 CRITICAL, 0 HIGH, parecer "aprovada com ressalvas". Product Owner aceitou formalmente 15 findings remanescentes (4 MEDIUM + 6 LOW + 5 INFORMATIONAL); 2 MEDIUM (`Cache-Control: no-store` ausente em `/bootstrap/*`; Bootstrap Secret sem validação de entropia) bloqueiam explicitamente a promoção para Homologação | `.ai/work-orders/completed/O1.4.3-BootstrapEAdministradorSenior.md`, `.ai/BACKLOG.md` |
| 10/08/2026 | Sprint | O1.4.3.4 (Security Self-Review dedicada + preparação da Security Validation independente) concluída: nenhum CRITICAL/HIGH; 388/388 testes de backend e 53/53 de frontend sem regressão | `.ai/CURRENT_SPRINT.md` |
| 10/08/2026 | Sprint | O1.4.3.3 (Frontend Bootstrap) concluída: Vertical Slice `bootstrap/` (wizard e-mail + Bootstrap Secret + OTP → Unidade de Negócio → Administrador Sênior → confirmação), 53/53 testes de frontend; encerrada após smoke test real ponta a ponta em Chrome aprovado pelo Product Owner/CTO (fluxo completo até `POST /bootstrap/concluir` → 200 OK) | `.ai/CURRENT_SPRINT.md` |
| 10/08/2026 | Sprint | O1.4.3.2 (Conclusão Transacional e Administrador Sênior) concluída: `ConcluirBootstrapUseCase` com conclusão atômica via compare-and-swap (`RowVersion`), 388/388 testes aprovados; migration `AddBootstrapConclusaoConcurrency` validada mas não aplicada ao banco compartilhado | `.ai/CURRENT_SPRINT.md` |
| 11/08/2026 | Sprint | **Sprint O1.5 — RBAC Real FORMALMENTE CONCLUÍDA.** O Product Owner aceitou formalmente as ressalvas remanescentes da Security Validation independente (0 CRITICAL/0 HIGH remanescentes) e registrou 6 decisões: (1) ressalvas aceitas, mas mantidas abertas e rastreadas no backlog; (2) enforcement de `Fornecedor.*`/`Pedido.*` fica fora da O1.5, sem expansão de escopo; (3) migration `AddRbacPerfilPermissaoCatalogo` aceita como aplicada ao banco de desenvolvimento, produção não tocada; (4) cobertura automatizada do cenário "autenticado sem permissões" aceita como suficiente, sem usuário artificial e sem iniciar a O1.6; (5) os 4 Perfis inativos de smoke test **permanecem** no banco de desenvolvimento como dados técnicos, com saneamento remetido a atividade futura pré-Homologação; (6) catálogo de Perfis de negócio e nomenclatura de `CentroCusto.Acessar` seguem pendências de produto. Work Order movida para `completed/`; **nenhuma sprint ativa**. Progresso Técnico da Onda 1 recalculado de 17% para **20%** (entregável #17 → Concluído; #9 → Em desenvolvimento) | `.ai/work-orders/completed/O1.5-RbacReal.md`, `.ai/CURRENT_SPRINT.md`, `.ai/BACKLOG.md` |
| 11/08/2026 | Sprint | Abertura formal e execução da **Sprint O1.5 — RBAC Real** (ADR-0021, D2): RBAC com enforcement real no backend (Perfil → Permissões → Policies → endpoints protegidos), mock de Perfis removido, migration `AddRbacPerfilPermissaoCatalogo` aplicada, 477 testes backend e 61 frontend aprovados, smoke test real em Chrome. Security Validation independente **aprovada com ressalvas** (0 CRITICAL/0 HIGH após correção de 1 HIGH, 2 MEDIUM e 1 LOW) | `.ai/work-orders/completed/O1.5-RbacReal.md`, `docs/architecture/rbac-o1.5.md` |
| 10/08/2026 | Governança | Execução do comando `[atualizar dashboard]`: entregável "Bootstrap Mode e Administrador Sênior" passa de Em desenvolvimento para Concluído na Onda 1 (41 entregáveis: 7 Concluído / 11 Em desenvolvimento / 23 Planejado); Progresso Técnico da Onda 1 recalculado para 17,07% exato (exibido 17%, antes 15%); Contribuição ao MVP da Onda 1 recalculada de 2,9 para 3,4 pontos; Percentual Global do MVP 1.0 recalculado de 29,93% para 30,41% exato (exibido 30%, inalterado no arredondamento) | `.ai/dashboard/DASHBOARD_UPDATE_COMMAND.md` (execução do comando) |
| 07/08/2026 | Sprint | O1.4.2 (Login Passwordless OTP e Sessão Segura) concluída: Vertical Slice completa de autenticação (backend `Api.Identity` + frontend `auth/`), 275/275 testes de backend e 38/38 de frontend aprovados, Security Implementation Gate III aprovado com pendências não bloqueantes para Development. Entregáveis "Arquitetura de login por Unidade de Negócio", "Tela de Login" e "Contexto UnidadeNegocioId" passam de Planejado para Concluído na Onda 1 | `.ai/CURRENT_SPRINT.md`, `.ai/BACKLOG.md` |
| 07/08/2026 | Segurança | O1.4.1.1 (Formalização da Estratégia de Autenticação em Development) concluída — Development Auth Strategy aprovada, sem implementação de código | `docs/architecture/security-design-auth-o1.4.md`, seção 17 |
| 07/08/2026 | Segurança | O1.4.3 (Security Design do Bootstrap e Administrador Sênior) concluída — Bootstrap Security Design Gate aprovado com pendências não bloqueantes, sem implementação de código | `docs/architecture/security-design-auth-o1.4.md`, seção 20 |
| 07/08/2026 | Sprint | O1.4.3.1 (Fundação Backend do Bootstrap) concluída: `BootstrapEstado`/`BootstrapSessao`, endpoints `estado`/`iniciar`/`otp/verificar`, 369/369 testes aprovados; migrations validadas mas não aplicadas ao banco compartilhado. Novo entregável "Bootstrap Mode e Administrador Sênior" adicionado à Onda 1 como Em desenvolvimento | `.ai/CURRENT_SPRINT.md`, `.ai/work-orders/active/O1.4.3-BootstrapEAdministradorSenior.md` |
| 07/08/2026 | Governança | Execução do comando `[atualizar dashboard]`: contagem de entregáveis da Onda 1 passa de 40 para 41 (6 Concluído / 12 Em desenvolvimento / 23 Planejado); Progresso Técnico da Onda 1 recalculado para 14,63% exato (exibido 15%); Contribuição ao MVP da Onda 1 recalculada de 1,5 para 2,9 pontos; Percentual Global do MVP 1.0 recalculado de 28,5% para 29,93% exato (exibido 30%, antes 29%) | `.ai/dashboard/DASHBOARD_UPDATE_COMMAND.md` (execução do comando) |
| 07/08/2026 | Governança | Instituída reescrita executiva obrigatória para "Situação Atual", "Próximo Marco" e "Principais Riscos" do Resumo Executivo: antes de publicar, o texto consolidado das fontes oficiais passa a ser reescrito em linguagem curta e direta para leitura executiva, sem alterar fatos, números ou contexto. Regra aplicada nesta execução aos três campos (texto anterior mais longo preservado em versões anteriores deste documento no histórico do git) | `.ai/dashboard/DASHBOARD_UPDATE_COMMAND.md` (execução do comando) |
| 07/08/2026 | Governança | Execução do comando `[atualizar dashboard]` identificou e corrigiu omissão: o entregável "Unidades de Alocação" (O1.3.5) estava concluído e catalogado em `.ai/BACKLOG.md`/`.ai/PROJECT_STATE.md`/`.ai/CURRENT_SPRINT.md`, mas ausente da lista de entregáveis da Onda 1 no `DASHBOARD_STATE.md` — corrigido; contagem de entregáveis da Onda 1 passa de 39 para 40. Progresso Técnico da Onda 1 recalculado (7,5% exato, exibido 8%, inalterado na apresentação); Contribuição ao MVP da Onda 1 recalculada de 1,6 para 1,5 pontos; Percentual Global do MVP 1.0 recalculado de 28,6% para 28,5% exato (exibido ainda como 29%) | `.ai/dashboard/DASHBOARD_UPDATE_COMMAND.md` (execução do comando) |
| 06/08/2026 | Segurança | O1.4.1 (Security Design Review) concluída — threat model e controles obrigatórios para Login OTP, Bootstrap Mode e sessão/RBAC definidos, sem nenhuma implementação de código. Security Design Gate = Aprovado com pendências (catálogo de Perfis/Permissões, provedor de e-mail para OTP, escopo de Perfil/Permissao) | `docs/architecture/security-design-auth-o1.4.md` |
| 06/08/2026 | Governança | Gate Administrativo aprovado formalmente, encerrando a fundação administrativa da Onda 1 (Perfis, Usuários, Filiais, Centros de Custo, Unidades de Alocação); frente O1.4 (Autenticação e Segurança) liberada e iniciada | `.ai/PROJECT_STATE.md`, `.ai/CURRENT_SPRINT.md` |
| 06/08/2026 | Sprint | Encerramento da O1.3.4 — Vertical Slice mockada `administration/cost-centers` concluída; build e 25/25 testes aprovados; entregáveis de Administração da Onda 1 atualizados para Em desenvolvimento; nenhuma alteração no Percentual Global do MVP (Onda 1 permanece em 8% de Progresso Técnico) | `.ai/work-orders/completed/O1.3.4-GestaoDeCentrosDeCusto.md` |
| 06/08/2026 | Planejamento | Percentual Global do MVP 1.0 passa a ser Σ (Peso Gerencial × Progresso Técnico) de todas as Ondas, mesmo Planejadas (antes gated por início formal) — resultado atual 28,6% (exibido 29%); Onda 5 renomeada para "Go Live - MVP 1.0 funcional"; Gantt ajustado para caber no card com barras Planejado/Realizado; nova seção "Roadmap dos Produtos" na aba Executive | Work Order "Ajuste Final de Percentuais, Gantt e Resumo Executivo dos MVPs" |
| 06/08/2026 | Planejamento | Separação explícita entre Progresso Técnico (execução comprovada dos entregáveis) e Contribuição ao MVP (avanço reconhecido pelo cronograma executivo) por Onda; Onda 2 registrada com 35% de Progresso Técnico e 0% de Contribuição ao MVP; Gráfico de Gantt incluído na aba Roadmap | Work Order "Ajuste do Progresso Técnico e Inclusão do Gráfico de Gantt" |
| 06/08/2026 | Planejamento | Baseline oficial de datas por Onda (Início/Fim Planejado, Real, Replanejado) e listas completas de entregáveis por Onda registradas | Work Order "Correção Definitiva do Roadmap, Datas e Entregáveis por Onda" |
| 05/08/2026 | Arquitetura documental | Unificação do Publication Engine em `DocsPublisher`; `docs/` como única fonte técnica, `dist/` como único destino | `.ai/DECISIONS.md` (ADR-0019, nota de atualização) |
| 05/08/2026 | Planejamento | Replanejamento oficial do projeto para o MVP 1.0 sob a estratégia Frontend First, com 5 Ondas e versão 1.1 definida | `.ai/ROADMAP.md` |
| 05/08/2026 | Governança documental | Criação de `docs/product/` como área oficial de documentação funcional (`+Compras Funcional`, `+Compras UX`, `+Compras Data Model`) | `.ai/DOCUMENTATION_STRATEGY.md` |
| 05/08/2026 | Governança documental | `.ai/dashboard/DASHBOARD_STATE.md` estabelecido como Read Model oficial do projeto; nenhum Dashboard pode consumir a documentação diretamente | `.ai/dashboard/README.md` |

---

## Política dos pesos do MVP 1.0

**Estes são Pesos Gerenciais do Roadmap — não representam esforço técnico, quantidade de código, complexidade ou horas trabalhadas.** Sua única finalidade é permitir o acompanhamento executivo do progresso do MVP 1.0. Pesos fixos, registrados oficialmente:

| Componente | Peso Gerencial |
|---|---|
| Foundation | 20% |
| Onda 1 | 20% |
| Onda 2 | 20% |
| Onda 3 | 20% |
| Onda 4 | 10% |
| Onda 5 | 10% |

**Percentual Global do MVP = Σ (Peso Gerencial × Progresso Técnico)** de cada componente — o Progresso Técnico de todas as Ondas (e da Foundation) contribui proporcionalmente ao MVP Global, **mesmo quando o Status formal da Onda ainda é "Planejado"** (ver seção "Percentual Global do MVP 1.0" para o cálculo vigente). Esta é a origem oficial da barra principal do Dashboard.

## Política dos percentuais

Todo percentual é derivado por cálculo a partir da documentação oficial — nunca preenchido manualmente quando puder ser calculado. Cada Onda possui dois indicadores distintos, que nunca são misturados nem exibidos como um único número:

| Indicador | Fórmula | Origem |
|---|---|---|
| Percentual da Foundation (Progresso Técnico) | Binário: 100% quando `.ai/ROADMAP.md` registra "concluída" | `.ai/ROADMAP.md` |
| **Progresso Técnico de uma Onda** | (entregáveis com status "Concluído" ÷ total de entregáveis da Onda) + (fração de entregáveis "Em desenvolvimento" apenas quando possuírem percentual individual explicitamente registrado; sem percentual individual, contribuem 0 — nunca estimado) — independe de a Onda ter sido formalmente iniciada | Tabela "Entregáveis" acima |
| **Contribuição ao MVP de uma Onda (pontos)** | Peso Gerencial da Onda × Progresso Técnico da Onda — contribui proporcionalmente mesmo com Status "Planejado" | Tabela "Roadmap" acima |
| Percentual Global do MVP | Σ (Peso Gerencial do componente × Progresso Técnico do componente) — ver política dos pesos | Tabela "Percentual Global do MVP 1.0" |
| Percentual de Backlog concluído | (Work Orders com status Concluído) ÷ (total do catálogo de 56) | `.ai/BACKLOG.md` |

Quando um percentual não puder ser calculado por falta de dado real, o campo permanece ausente — nunca um valor estimado.

## Política dos status

**Ondas:** Planejado, Em desenvolvimento, Bloqueado, Concluído, Cancelado.

**Entregáveis:** Planejado, Em desenvolvimento, Concluído.

Nenhuma outra nomenclatura é permitida em nenhum dos dois níveis.

## Política das datas

Cada Onda possui Início Planejado, Fim Planejado, Início Real, Fim Real, Início Replanejado e Fim Replanejado. A **baseline planejada representa o compromisso oficial do projeto e nunca é alterada** após seu primeiro registro. As datas reais são registradas ao início/término efetivo da Onda. As datas replanejadas são recalculadas para as Ondas restantes sempre que uma Onda termina, com base no desvio observado.

**Regra de exibição:** um campo de data sem valor registrado não é exibido no Dashboard — nunca como travessão, "Pendente", "Não aplicável" ou explicação entre parênteses. Isso vale para toda informação vazia (datas, observações, riscos, percentuais opcionais, evidências).

Foundation é a única excepção estrutural: por não possuir baseline planejada anterior à existência desta política, exibe somente a Data Real.

## Comando de atualização — `[atualizar dashboard]`

Ao receber `[atualizar dashboard]`, o processo deve, nesta ordem:

1. Ler toda a documentação oficial (fontes listadas em `README.md`).
2. Validar consistência entre os documentos.
3. Atualizar este `DASHBOARD_STATE.md`.
4. Recalcular indicadores (Percentual Global, percentuais de Onda, Métricas).
5. Atualizar o Resumo Executivo.
6. Atualizar Decisões Recentes.
7. Atualizar Métricas.
8. Somente após a geração bem-sucedida deste documento, atualizar o Dashboard HTML.
9. Atualizar o workflow do n8n (quando existir integração de publicação via n8n).
10. Publicar.
11. Validar a publicação.

Se qualquer inconsistência for encontrada no passo 2, a atualização é interrompida, um relatório das inconsistências é apresentado, e nenhuma das etapas 3–11 é executada. Nenhuma informação inexistente é inventada.

## Responsabilidade do Dashboard (HTML ou tecnologia futura)

O Dashboard possui responsabilidade **exclusivamente visual**. Ele não interpreta documentação, não calcula indicadores, não cria regras de negócio, não infere estados. Toda informação exibida deve existir previamente neste documento. Sempre que um novo indicador for necessário, a ordem é sempre: (1) atualizar `DASHBOARD_STATE.md`, (2) atualizar o Dashboard — nunca o contrário.
