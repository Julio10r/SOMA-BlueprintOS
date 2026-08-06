# DASHBOARD_STATE

> **Read Model oficial do projeto. Documento derivado. Não é fonte de verdade. Não editar manualmente.** Gerado a partir da leitura de `.ai/ROADMAP.md`, `.ai/BACKLOG.md`, `.ai/PROJECT_STATE.md`, `.ai/CURRENT_SPRINT.md`, `.ai/DOCUMENTATION_STRATEGY.md`, `.ai/DECISIONS.md` e `docs/product/`. Único consumível por qualquer Dashboard (HTML, React, Power BI, Grafana ou tecnologia futura) — nenhuma interface pode depender diretamente dos documentos do projeto. Qualquer edição manual será sobrescrita na próxima execução de `[atualizar dashboard]`.

## Cabeçalho

| Campo | Valor |
|---|---|
| Dashboard State | v1 |
| Schema Version | 2.0.0 |
| Project Version | `v0.9.0-blueprint-foundation` |
| Generated At | 05/08/2026 |
| Last Update | 05/08/2026 — ajuste de consistência gerencial (Onda 2 e pesos) |
| Status | Fundação concluída; MVP 1.0 replanejado; Onda 1 em desenvolvimento |

## Foundation

| Campo | Valor |
|---|---|
| Status | Concluído |
| Percentual | 100% |
| Peso no MVP | 20% |
| Data Planejada | — (concluída antes da existência desta política de datas) |
| Data Real | 05/08/2026 (merge em `main`, tag `v0.9.0-blueprint-foundation`) |
| Observações | Arquitetura, padrões, Publication Engine e governança de Work Orders — ver `.ai/ROADMAP.md` §"Fundação arquitetural" |

## Roadmap

| Campo | Onda 1 | Onda 2 | Onda 3 | Onda 4 | Onda 5 |
|---|---|---|---|---|---|
| Nome | Fundação Funcional | Cadastros | Processo de Compras | Integrações Operacionais | Go Live |
| Objetivo | Frontend navegável, Administração, blueprint de banco | Cadastros completos com sincronização ERP | Ciclo completo solicitação → pedido | ERP, Nota Fiscal, Pagamento | Homologação e estabilização |
| Peso no MVP | 20% | 20% | 20% | 10% | 10% |
| Percentual | 0% | 0% | 0% | 0% | 0% |
| Status | Em desenvolvimento | Planejado | Planejado | Planejado | Planejado |
| Gate | Frontend navegável aprovado | Cadastros homologados | Processo ponta a ponta funcionando | Integrações ERP/Fiscal/Pagamentos homologadas | Go Live aprovado |
| Critério do Gate | Produto navegável, Administração operável, blueprint de banco completo e aprovado | Todos os cadastros operáveis pelo frontend e sincronizados com o ERP | Ciclo solicitação→pedido operável de ponta a ponta, com aprovação e orçamento funcionando | Integrações operando ponta a ponta, sem alteração estrutural no ERP | Homologação aprovada, observabilidade/segurança mínimas operando, performance validada |
| Data Planejada | Pendente (preenchida somente na aprovação) | Pendente | Pendente | Pendente | Pendente |
| Data Real | — | — | — | — | — |
| Data Replanejada | — | — | — | — | — |
| Observações | — | Fornecedores já implementado tecnicamente antes do replanejamento (B1/B2/B2.1-B2.2), mas não antecipa o percentual da Onda — ver regra "Onda representa cronograma, não histórico" abaixo | Depende da Onda 2 | Depende da Onda 3 | Depende das Ondas 1-4 |

> **Regra: a Onda representa o cronograma oficial de entrega do MVP, não a ordem histórica em que funcionalidades foram desenvolvidas.** Uma funcionalidade tecnicamente implementada antes da aprovação formal de sua Onda (ex.: Fornecedores, implementado antes do replanejamento do MVP 1.0) continua registrada normalmente na documentação e no backlog, mas **não antecipa artificialmente o percentual da Onda** à qual foi posteriormente reclassificada. O percentual de uma Onda só reflete entregáveis concluídos **dentro da execução formal daquela Onda**.

## Entregáveis

### Onda 1 — Fundação Funcional

| Entregável | Status | Percentual | Observações |
|---|---|---|---|
| Frontend navegável | Planejado | — | — |
| Administração (Unidade de Negócio, Usuários, Perfis, Permissões, IdP, Configuração ERP, Workflow, Aprovação, Controle Orçamentário, Feature Flags) | Planejado | — | — |
| Blueprint completo do banco | Planejado | — | — |

### Onda 2 — Cadastros

| Entregável | Status | Percentual | Observações |
|---|---|---|---|
| Fornecedores | Concluído | 100% | B1, B2, B2.1, B2.1.1, B2.1.2, B2.1.3, B2.2 — implementado antes da aprovação formal da Onda 2; registrado normalmente, mas **não conta para o percentual da Onda** (ver regra acima) |
| Materiais | Planejado | — | — |
| Serviços | Planejado | — | — |
| Categorias | Planejado | — | — |
| Compradores | Planejado | — | — |
| Centros de Custo | Planejado | — | — |

> Percentual da Onda 2 = **0%**, apesar de o entregável Fornecedores constar Concluído acima — a Onda 2 ainda não foi formalmente aprovada nem iniciada em execução.

### Onda 3 — Processo de Compras

| Entregável | Status | Percentual | Observações |
|---|---|---|---|
| Solicitação | Planejado | — | — |
| Cotação | Planejado | — | — |
| Negociação por IA | Planejado | — | Estratégia/memória de negociação existem em código, sem produto de Onda 3 concluído |
| Workflow | Planejado | — | — |
| Controle Orçamentário | Planejado | — | — |
| Aprovação | Planejado | — | — |
| Pedido | Planejado | — | — |

### Onda 4 — Integrações Operacionais

| Entregável | Status | Percentual | Observações |
|---|---|---|---|
| ERP | Em desenvolvimento | — | Descoberta/sincronização de fornecedores implementada; itens/pedidos pendentes |
| Nota Fiscal | Planejado | — | — |
| Pagamento | Planejado | — | — |

### Onda 5 — Go Live

| Entregável | Status | Percentual | Observações |
|---|---|---|---|
| Homologação | Planejado | — | — |
| Observabilidade | Planejado | — | — |
| Performance | Planejado | — | — |
| Segurança | Planejado | — | — |
| Estabilização | Planejado | — | — |

## Percentual Global do MVP 1.0

**Fórmula oficial:** Percentual Global = Σ (Peso da Onda/Foundation × Percentual concluído da Onda/Foundation)

| Componente | Peso | Percentual | Contribuição |
|---|---|---|---|
| Foundation | 20% | 100% | 20,0 |
| Onda 1 | 20% | 0% | 0,0 |
| Onda 2 | 20% | 0% | 0,0 |
| Onda 3 | 20% | 0% | 0,0 |
| Onda 4 | 10% | 0% | 0,0 |
| Onda 5 | 10% | 0% | 0,0 |
| **Total** | **100%** | — | **20%** |

Esta é a origem oficial da barra principal de qualquer Dashboard. Nenhum Dashboard recalcula este valor — ele apenas lê a linha "Total" acima.

## Resumo Executivo

> Gerado automaticamente — nunca editado manualmente.

**Situação Atual:** Fundação arquitetural concluída e publicada (tag `v0.9.0-blueprint-foundation`). MVP 1.0 replanejado sob a estratégia Frontend First, com 5 Ondas, pesos e Gates definidos. Onda 1 em desenvolvimento; demais Ondas planejadas. Progresso global do MVP: 20% (apenas Foundation concluída; nenhuma Onda contribui percentual ainda — ver regra "Onda representa cronograma, não histórico").

**Últimas Entregas:**
- Unificação do Publication Engine (`DocsPublisher`) e reorganização de `docs/`/`resources/` (ADR-0019).
- Merge da fundação em `main`, tag `v0.9.0-blueprint-foundation`.
- Replanejamento oficial para o MVP 1.0 (5 Ondas, versão 1.1 definida).
- Criação de `docs/product/` e de `.ai/dashboard/` como Read Model do projeto.

**Próximos Objetivos:**
- Aprovação formal da Onda 1 pelo Product Owner.
- Especificação da funcionalidade de Login em `docs/product/ComprasFuncional.md`/`ComprasUX.md`.
- Blueprint completo do banco.

**Próximo Marco:** Gate da Onda 1 — "Frontend navegável aprovado".

**Principais Riscos:**
- Nenhuma Onda tem Data Planejada registrada ainda — cronograma real depende da aprovação do Product Owner.
- `.ai/content/{executive,client,engineering}/` e três documentos institucionais em `docs/` permanecem como pendências de limpeza já registradas em `.ai/BACKLOG.md` (não bloqueiam a Onda 1).

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
| Testes | 239 unitários + 5 integração aprovados (última execução registrada) | `.ai/PROJECT_STATE.md` |
| Documentos oficiais | 6 (`Executive Report`, `Product Blueprint`, Documentação Técnica, `+Compras Funcional`, `+Compras UX`, `+Compras Data Model`) | `.ai/DOCUMENTATION_STRATEGY.md` |

Métricas sem dado oficial disponível não são exibidas com valor estimado — permanecem ausentes desta seção até existir fonte real.

## Decisões Recentes

| Data | Categoria | Resumo | Documento de origem |
|---|---|---|---|
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

**Percentual Global do MVP = Σ (Peso Gerencial × percentual concluído)** de cada componente — pesos concluídos contam integralmente; componentes em desenvolvimento contribuem proporcionalmente ao seu percentual interno, calculado exclusivamente pelos entregáveis pertencentes à execução formal daquela Onda (ver "Regra: a Onda representa o cronograma..." acima e seção "Percentual Global do MVP 1.0" para o cálculo vigente). Esta é a origem oficial da barra principal do Dashboard.

## Política dos percentuais

Todo percentual é derivado por cálculo a partir da documentação oficial — nunca preenchido manualmente quando puder ser calculado.

| Indicador | Fórmula | Origem |
|---|---|---|
| Percentual da Foundation | Binário: 100% quando `.ai/ROADMAP.md` registra "concluída" | `.ai/ROADMAP.md` |
| Percentual de uma Onda | (entregáveis com status Concluído, ponderados por seu Percentual quando informado) ÷ (total de entregáveis da Onda) | Tabela "Entregáveis" acima |
| Percentual Global do MVP | Σ (peso do componente × percentual do componente) — ver política dos pesos | `.ai/ROADMAP.md` + tabela de Entregáveis |
| Percentual de Backlog concluído | (Work Orders com status Concluído) ÷ (total do catálogo de 56) | `.ai/BACKLOG.md` |

Quando um percentual não puder ser calculado por falta de dado real, o campo permanece "—" (traço), nunca um valor estimado.

## Política dos status

**Ondas:** Planejado, Em desenvolvimento, Bloqueado, Concluído, Cancelado.

**Entregáveis:** Planejado, Em desenvolvimento, Concluído.

Nenhuma outra nomenclatura é permitida em nenhum dos dois níveis.

## Política das datas

Cada Onda possui Data Planejada, Data Real e Data Replanejada. A **Data Planejada representa a baseline do projeto e nunca é alterada** após seu primeiro registro; é preenchida somente quando a execução da Onda é formalmente aprovada. A Data Real é registrada ao término efetivo da Onda. A Data Replanejada é recalculada para as Ondas restantes sempre que uma Onda termina, com base no desvio observado.

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

> Os passos 9–11 descrevem o comportamento oficial quando existir um Dashboard publicado via n8n; nesta etapa nenhum Dashboard HTML, workflow n8n ou publicação real foi criado, alterado ou executado — apenas o comportamento é especificado para uso futuro.

## Responsabilidade do Dashboard (HTML ou tecnologia futura)

O Dashboard possui responsabilidade **exclusivamente visual**. Ele não interpreta documentação, não calcula indicadores, não cria regras de negócio, não infere estados. Toda informação exibida deve existir previamente neste documento. Sempre que um novo indicador for necessário, a ordem é sempre: (1) atualizar `DASHBOARD_STATE.md`, (2) atualizar o Dashboard — nunca o contrário.
