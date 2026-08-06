# DASHBOARD_STATE

> **Read Model oficial do projeto. Documento derivado. Não é fonte de verdade. Não editar manualmente fora do fluxo `[atualizar dashboard]` ou de Work Order explícita que autorize esta edição.** Gerado a partir da leitura de `.ai/ROADMAP.md`, `.ai/BACKLOG.md`, `.ai/PROJECT_STATE.md`, `.ai/CURRENT_SPRINT.md`, `.ai/DOCUMENTATION_STRATEGY.md`, `.ai/DECISIONS.md`, `docs/product/` e da baseline oficial de datas registrada pelo Product Owner via Work Order. Único consumível por qualquer Dashboard (HTML, React, Power BI, Grafana ou tecnologia futura) — nenhuma interface pode depender diretamente dos documentos do projeto.

## Cabeçalho

| Campo | Valor |
|---|---|
| Dashboard State | v2 |
| Schema Version | 2.3.0 |
| Project Version | `v0.9.0-blueprint-foundation` |
| Generated At | 06/08/2026 |
| Last Update | 06/08/2026 — Percentual Global do MVP 1.0 passa a somar a contribuição proporcional (Peso × Progresso Técnico) de todas as Ondas, mesmo as ainda Planejadas; Gráfico de Gantt ajustado para caber integralmente no card com barras Planejado/Realizado; Onda 5 renomeada para "Go Live - MVP 1.0 funcional"; nova seção "Roadmap dos Produtos" na aba Executive — via Work Order "Ajuste Final de Percentuais, Gantt e Resumo Executivo dos MVPs" |
| Status | Fundação concluída; MVP 1.0 replanejado; Onda 1 em desenvolvimento |

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
| Progresso Técnico | 8% | 35% | 0% | 0% | 0% |
| Contribuição ao MVP | 1,6 pontos | 7,0 pontos | 0,0 pontos | 0,0 pontos | 0,0 pontos |
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
| Arquitetura de login por Unidade de Negócio | Planejado | — |
| Tela de Login | Planejado | — |
| Seleção da Unidade de Negócio | Planejado | — |
| Shell principal da aplicação | Em desenvolvimento | AppShell.tsx existe no Portal +Compras (header e navegação por módulos); não contempla ainda os módulos de Administração da Onda 1 |
| Menu e navegação completa | Em desenvolvimento | Navegação por react-router-dom existe para os módulos atuais; não cobre ainda os itens de Administração |
| Dashboard inicial do produto | Em desenvolvimento | Dashboard.tsx busca fornecedores reais via API; demais indicadores (pedidos, negociações) permanecem mockados |
| Frontend mockado navegável da v1.0 | Em desenvolvimento | Módulos Pedidos/Negociações/Indicadores/Agentes IA/Configurações são telas demonstrativas honestas; Fornecedores conectado à API real |
| Estados de loading, vazio, sucesso e erro | Em desenvolvimento | Implementados em CnpjSearch, ApprovalPanel e CadastroFornecedor; cobertura ainda parcial nas demais telas |
| Perfis de usuário simulados | Planejado | — |
| Contexto UnidadeNegocioId | Planejado | Identidade atual (DevelopmentRequestIdentity) é um stub de Development, sem contexto de Unidade de Negócio |
| Módulo de Administração | Planejado | — |
| Cadastro de Unidades de Negócio | Planejado | — |
| Empresas e filiais | Planejado | — |
| Usuários | Planejado | — |
| Usuário por Unidade de Negócio | Planejado | — |
| Perfis, papéis e permissões | Planejado | — |
| Identity Providers por Unidade de Negócio | Planejado | — |
| Configuração de ERP por Unidade de Negócio | Planejado | — |
| Parâmetros gerais por Unidade de Negócio | Planejado | — |
| Feature Flags | Planejado | — |
| Configuração de notificações | Planejado | — |
| Estrutura de Workflow | Planejado | — |
| Configuração de alçadas | Planejado | — |
| Estrutura de aprovação | Planejado | — |
| Estrutura de controle orçamentário | Planejado | — |
| Administração operacional | Planejado | — |
| Monitor de integrações | Planejado | — |
| Monitor de filas e reprocessamentos | Planejado | — |
| Auditoria e histórico de sincronizações | Planejado | Dados de sincronização (SincronizacaoFornecedor/ErroSincronizacaoFornecedor) existem no backend para fornecedores; não há tela de Administração |
| +Compras Funcional | Concluído | Estrutura documental inicial criada em docs/product/ComprasFuncional.md; especificação funcional completa por tela ainda pendente |
| +Compras UX | Concluído | Estrutura documental inicial criada em docs/product/ComprasUX.md; especificação de UX completa por tela ainda pendente |
| +Compras Data Model | Concluído | Estrutura documental inicial criada em docs/product/ComprasDataModel.md; modelo de dados completo por tela ainda pendente |
| Blueprint funcional do banco por tela | Planejado | — |
| Matriz tela × campo × entidade | Planejado | — |
| Matriz +Compras × ERP | Planejado | — |
| Mapeamento inicial de APIs | Planejado | — |
| Mapeamento inicial de integrações | Planejado | — |
| Validação funcional com os envolvidos | Planejado | — |

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
| Onda 1 | 20% | 8% | 1,6 |
| Onda 2 | 20% | 35% | 7,0 |
| Onda 3 | 20% | 0% | 0,0 |
| Onda 4 | 10% | 0% | 0,0 |
| Onda 5 | 10% | 0% | 0,0 |
| **Total** | **100%** | — | **28,6%** |

O valor exato do Total é **28,6%** (soma exata dos pontos acima); a apresentação visual em qualquer Dashboard deve arredondar este valor apenas para exibição (**29%**), mantendo o valor exato disponível em tooltip/detalhe acessível. Esta é a origem oficial da barra principal de qualquer Dashboard. Nenhum Dashboard recalcula este valor — ele apenas lê a linha "Total" acima. **A Contribuição ao MVP de cada componente é calculada durante a atualização deste documento — o Dashboard apenas renderiza os valores já calculados aqui, nunca recalcula a fórmula global em HTML ou JavaScript.**

## Resumo Executivo

> Gerado automaticamente — nunca editado manualmente.

**Situação Atual:** Fundação arquitetural concluída e publicada (tag `v0.9.0-blueprint-foundation`). MVP 1.0 replanejado sob a estratégia Frontend First, com 5 Ondas, pesos, Gates e baseline oficial de datas definidos. Onda 1 em desenvolvimento desde 03/08/2026 (fim planejado 14/08/2026); demais Ondas planejadas. Percentual Global do MVP 1.0: 28,6% (exibido como 29%) — Foundation concluída (20,0 pontos) + Onda 1 com 8% de Progresso Técnico (1,6 ponto) + Onda 2 com 35% de Progresso Técnico (7,0 pontos), somados proporcionalmente mesmo com as Ondas 1 e 2 ainda em Status "Em desenvolvimento"/"Planejado".

**Últimas Entregas:**
- Baseline oficial de datas por Onda (Início/Fim Planejado, Real, Replanejado) registrada via Work Order.
- Listas completas de entregáveis por Onda (1 a 5) registradas, com status individual e evidência.
- Unificação do Publication Engine (`DocsPublisher`) e reorganização de `docs/`/`resources/` (ADR-0019).
- Merge da fundação em `main`, tag `v0.9.0-blueprint-foundation`.

**Próximos Objetivos:**
- Aprovação formal da Onda 1 pelo Product Owner (Gate: Frontend navegável e Administração aprovados).
- Especificação funcional completa por tela em `docs/product/ComprasFuncional.md`/`ComprasUX.md`.
- Blueprint completo do banco por tela.

**Próximo Marco:** Gate da Onda 1 — "Frontend navegável e Administração aprovados", com fim planejado em 14/08/2026.

**Principais Riscos:**
- Onda 1 não possui ainda Fim Real registrado — término efetivo depende da conclusão dos 38 entregáveis listados, dos quais 30 permanecem Planejados.
- `.ai/content/{executive,client,engineering}/` e três documentos institucionais em `docs/` permanecem como pendências de limpeza já registradas em `.ai/BACKLOG.md` (não bloqueiam a Onda 1).

## Roadmap dos Produtos

> Gerado automaticamente a partir do Roadmap oficial e da documentação de escopo — nunca editado manualmente. Fonte para a seção "Roadmap dos Produtos" da aba Executive; o Dashboard não lê `.ai/ROADMAP.md` ou `.ai/BACKLOG.md` diretamente.

**MVP 1.0:**
- **Objetivo geral:** Entregar a primeira versão funcional do +Compras em produção, cobrindo o fluxo completo de compras — da solicitação ao pagamento — com base administrativa, cadastros, processo de compras, integrações operacionais e Go Live homologado.
- **Percentual Global Atual:** 28,6% (exibido como 29%)
- **Onda Atual:** Onda 1 — Fundação Funcional (Em desenvolvimento)
- **Marco Final:** Go Live - MVP 1.0 funcional

**MVP 1.1:**
- **Objetivo geral:** Expandir o +Compras após a estabilização do MVP 1.0, adicionando capacidades avançadas, canais externos, inteligência analítica e governança ampliada.
- **Escopo adiado:** ESG, Portal de Fornecedores, Marketplace, Analytics avançado, Previsão de Demanda, Previsão de Preços, Jurídico, Compliance, Gestão de Riscos

## Métricas

| Métrica | Valor | Origem |
|---|---|---|
| Total de Work Orders (catálogo) | 56 | `.ai/BACKLOG.md` |
| Work Orders concluídas | 7 (A1, A2, A3, A4, A7, B1, B2) + 4 sub-etapas (B2.1, B2.1.1, B2.1.2, B2.1.3, B2.2 — 5 sub-etapas) | `.ai/BACKLOG.md` |
| APIs | `GET /health`, CRUD de fornecedores, descoberta de fornecedores, consulta CNPJ, recomendação de negociação | `.ai/PROJECT_STATE.md` |
| Telas | 0 concluídas / 19 previstas (índice `docs/product/`) | `docs/product/ComprasFuncional.md` |
| Entidades | Não registradas ainda em `docs/product/ComprasDataModel.md` | `docs/product/ComprasDataModel.md` |
| Integrações | ERP (fornecedores, parcial), BrasilAPI (CNPJ, implementada) | `.ai/PROJECT_STATE.md` |
| Agentes | `EchoAgent`, `KnowledgeAgent` (básicos) | `.ai/PROJECT_STATE.md` |
| Testes | 295 unitários/integração aprovados (última execução registrada) | `.ai/PROJECT_STATE.md` |
| Documentos oficiais | 6 (`Executive Report`, `Product Blueprint`, Documentação Técnica, `+Compras Funcional`, `+Compras UX`, `+Compras Data Model`) | `.ai/DOCUMENTATION_STRATEGY.md` |

Métricas sem dado oficial disponível não são exibidas com valor estimado — permanecem ausentes desta seção até existir fonte real.

## Decisões Recentes

| Data | Categoria | Resumo | Documento de origem |
|---|---|---|---|
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
