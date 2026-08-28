# Auditoria de Reorganização Física do Repositório

**Data:** 2026-08-27
**Autor:** Agente de auditoria (somente leitura — nenhum arquivo movido, editado ou apagado)
**Objetivo:** Levantar evidência real do estado atual do repositório para embasar uma futura reorganização física em `agents/`, `applications/mais-compras/`, `shared/`, `tools/`, `scripts/`, `docs/`. Este documento não move nada; é insumo para decisão humana.

---

## 1. Árvore atual (nível de topo) com categoria

| Item | Arquivos rastreados (git) | Categoria | Observação |
|---|---|---|---|
| `agents/` | 15 | **AGENTS** | Política canônica + 8 manifests de agente |
| `backend/` | 689 | **APPLICATION_MAIS_COMPRAS** | Solução .NET completa do +Compras |
| `frontend/` | 205 | **APPLICATION_MAIS_COMPRAS** | SPA React do +Compras |
| `.ai/` | 197 | **OPERATIONS** (misto — ver §2) | Governança de processo, contexto, work orders |
| `resources/` | 130 | **SHARED** | Design system (multi-marca) + apresentações |
| `docs/` | 79 | **DOCUMENTATION** (misto — ver §2) | Documentação técnica, operações, auditorias |
| `tools/` | 15 | **TOOLING** | Scripts Node de governança de agentes |
| `scripts/` | 10 | **TOOLING/OPERATIONS** (misto) | Dev scripts + integração Linx/Wise + showcase collector |
| `mcp/` | 1 | **UNKNOWN** | Só contém `mcp/design-system/README.md`; propósito não fica claro pelo conteúdo (possível duplicata conceitual de `resources/design-system`) |
| `infrastructure/` | 1 | **UNKNOWN/TOOLING** | Só `docker/.env.example` rastreado; pastas `terraform/`, `nginx/`, `monitoring/`, `kubernetes/` existem no disco mas estão **vazias/não versionadas** |
| `README.md`, `CLAUDE.md`, `AGENTS.md`, `CHANGELOG.md`, `LICENSE`, `.editorconfig`, `.gitattributes`, `.env.example` | 1 cada | **DOCUMENTATION/TOOLING** | Arquivos de raiz, ficam na raiz por convenção |
| `.gitignore` | 1 | TOOLING | — |

### Árvores geradas (GENERATED) — não versionadas, confirmado via `git ls-files` (0 arquivos rastreados em todas)

| Pasta | Arquivos (aprox.) | Tamanho | Confirmação |
|---|---|---|---|
| `frontend/web/node_modules/` | 5.778 | 131 MB | `node_modules/` no `.gitignore` |
| `.venv/` | 787 | 11 MB | Python venv local, não rastreado |
| `**/bin/`, `**/obj/` (dentro de `backend/`) | 837 | — | `.gitignore:39-40` |
| `frontend/web/dist/` | — | — | `.gitignore:75-77` |
| `dist/` (raiz) | — | 528 KB | Documentado em `.ai/DECISIONS.md:121` como artefato gerado do Publication Engine; `.gitignore:47` (`/dist/`) |
| `downloads/` | — | 586 MB | `.gitignore:180`; saída de trabalho do coletor de showcase (planilhas, fotos, JSON de execução) |
| `_staging/` | 1 (`backend_full.tar.gz`, 203 KB, de 07/08) | 200 KB | Não rastreado, não referenciado por nenhum script/doc — **candidato a quarentena** |
| `.DS_Store` (várias pastas) | — | — | Artefato de macOS |

---

## 2. Auditoria item a item de `.ai/` e `docs/` (mistos)

### `.ai/` — subitem por subitem

| Subpasta/arquivo | Categoria | Justificativa |
|---|---|---|
| `.ai/context/*.md` (14 arquivos: architecture, agents, tech-stack, testing, security, runtime, planner, observability, memory, knowledge, git-workflow, coding-standards, definition-of-done, linx-wise-daily-integration) | **OPERATIONS** (processo de engenharia, não específico do +Compras nem dos Agents) | Complementam `ARCHITECTURE.md`, `STANDARDS.md`, `AI_TEAM.md` na raiz de `.ai/`; `linx-wise-daily-integration.md` é fonte de verdade operacional citada pelo runbook e por 3 `agent.yaml` |
| `.ai/dashboard/*` (DASHBOARD_STATE.md, index.html, dashboard.css/js, README, UPDATE_COMMAND) | **TOOLING/OPERATIONS** | Dashboard de status de projeto, plataforma-agnóstico |
| `.ai/work-orders/**` (backlog, active, completed, superseded) | **APPLICATION_MAIS_COMPRAS** majoritariamente | Quase todos os work orders (`O1.*`, `B1`, `B2.*`) tratam do +Compras; alguns (`A1`–`A13`) tratam da fundação de Agents/arquitetura e são **AGENTS/SHARED** |
| `.ai/content/{client,engineering,executive}/*.md` | **APPLICATION_MAIS_COMPRAS** | Fonte do Publication Engine (`dist/`), conteúdo sobre o +Compras |
| `.ai/audit-visual-screenshots/*.png` (33 arquivos) | **APPLICATION_MAIS_COMPRAS** | Screenshots de telas do +Compras (fornecedores, filiais, RBAC, etc.) |
| `.ai/local-output/mb_prod_extra_web/**` | **TEMPORARY/QUARANTINE_CANDIDATE** | Saída bruta de execução de integração (CSVs/JSONs de produção Linx/Wise), não é código nem doc de referência — parece output operacional acumulado, não fonte de verdade |
| `.ai/sources/COMPRAS_INDIRETAS_SOURCES.md` | **APPLICATION_MAIS_COMPRAS** | — |
| `.ai/prompts/*.md` | **AGENTS/OPERATIONS** | Prompts reutilizáveis (novo agente, testes, refactor, consultar Wise) — mistura genérico e específico do domínio |
| `.ai/templates/*.md` | **OPERATIONS** | Templates de processo (refactor, release, épico, hotfix) — genéricos |
| `.ai/memory/*.md` (architecture, patterns, known_issues, completed_sprints, decisions) | **APPLICATION_MAIS_COMPRAS** (conteúdo atual é 100% sobre +Compras) | — |
| `.ai/ARCHITECTURE.md`, `PROJECT.md`, `STANDARDS.md`, `VISION.md`, `ROADMAP.md`, `BACKLOG.md`, `DECISIONS.md`, etc. (arquivos soltos na raiz de `.ai/`) | **APPLICATION_MAIS_COMPRAS** (a maioria fala do +Compras especificamente) — **AMBÍGUO**, ver §6 | `.ai/AI_TEAM.md`, `.ai/AI_AUTONOMY_POLICY.md`, `.ai/AI_BEHAVIOR.md` tratam de Agents/AI Factory (mais próximos de **AGENTS**) |

### `docs/` — subitem por subitem

| Subpasta | Categoria | Observação |
|---|---|---|
| `docs/agents/` + `docs/agents/ai-factory/` | **AGENTS** | Contém `Agents.md`, `AgentsCatalog.html`, `AgentsCatalog.generated.html` — candidatos diretos a `agents/docs/` |
| `docs/architecture/` (inclui `AIGovernance.md`) | **AGENTS/SHARED** | Referenciado por 2 `agent.yaml` (security-lgpd-agent, linx-database-specialist-agent) |
| `docs/operations/` (LinxWiseDailyIntegrationRunbook.md, WiseAgentRunbook.md, ShowcaseAgentRunbook.md, Operations.md) | **APPLICATION_MAIS_COMPRAS/OPERATIONS** | Runbooks referenciados por `agent.yaml` (`runbook_paths`) — ficam fora de `agents/`, mas o link é forte |
| `docs/backend/`, `docs/frontend/`, `docs/database/`, `docs/testing/` | **APPLICATION_MAIS_COMPRAS** | Documentação técnica específica do +Compras |
| `docs/product/`, `docs/executive/`, `docs/demo/`, `docs/releases/` | **APPLICATION_MAIS_COMPRAS** | — |
| `docs/audits/` | **APPLICATION_MAIS_COMPRAS/AGENTS** (misto) | Contém tanto auditorias de Agents (`AgentsV1-FinalCertification.md`) quanto do +Compras/Linx |
| `docs/linxERP/` | **APPLICATION_MAIS_COMPRAS** | — |
| `docs/assets/` | **SHARED/APPLICATION_MAIS_COMPRAS** | Não inspecionado em detalhe — verificar se contém apenas imagens do +Compras ou ativos genéricos |
| `docs/Product Blueprint.md`, `docs/Executive Report.md`, `docs/README.md` | **APPLICATION_MAIS_COMPRAS** | Linkados por `README.md` da raiz |

---

## 3. Owners / responsabilidade inferida

| Área | Owner provável | Evidência |
|---|---|---|
| `agents/`, `tools/agents/`, `docs/agents/`, `docs/architecture/AIGovernance.md` | Time de Agents/AI Factory | Manifests, política de execução, testes de governança (`*.test.js`) |
| `backend/`, `frontend/`, `docs/backend/`, `docs/frontend/`, `docs/database/`, maior parte de `.ai/work-orders` | Time de +Compras (Procurement) | Namespace `BlueprintOS.*` de domínio de compras, pacote `@blueprintos/procurement-web` |
| `resources/design-system/`, `mcp/design-system/` | Plataforma/Design (multi-marca, referencia AZZAS 2154, GDT) | `resources/design-system/SKILL.md` descreve uso para múltiplos portais, não só +Compras |
| `scripts/linx_wise_daily_integration.py`, `scripts/showcase_collector/` | Time de integração ERP (Linx/Wise) — usado tanto por Agents (`linx-erp-specialist-agent`, `wise-agent`, `showcase-agent`) quanto por operação do +Compras | `agent.yaml` `script_paths` |
| `.ai/` (governança de processo) | Plataforma/Processo de engenharia | Framework de work orders, templates, dashboard — usado por todos os times |
| `infrastructure/` | Plataforma/Infra (hoje quase vazio, não versionado) | Só `docker/.env.example` rastreado |

---

## 4. Proposta de destino (tabela path atual → path proposto)

| Path atual | Path proposto | Justificativa |
|---|---|---|
| `agents/AGENT_CONTRACT.md`, `EXECUTION_POLICY.md`, `DATABASE_CONNECTION_POLICY.md`, `USER_ARTIFACT_LEARNING_POLICY.md`, `CAPABILITY_GAP_AND_AGENT_EVOLUTION_POLICY.md`, `agent.schema.json`, `README.md`, `<agent-id>/` | *(mantém — já na raiz correta)* | Confirmado como política canônica, estrutura-alvo já atendida |
| `docs/agents/Agents.md`, `AgentsCatalog.html`, `AgentsCatalog.generated.html`, `docs/agents/ai-factory/` | `agents/docs/` | Estrutura-alvo pede `agents/docs/`; hoje há 8 `agent.yaml` referenciando `docs/agents/Agents.md` via `docs_paths` — **todos precisam de atualização em bloco** |
| `docs/architecture/AIGovernance.md` | `agents/docs/AIGovernance.md` (ou `shared/architecture/`) | Referenciado só por `agent.yaml` de Agents — decisão: mover para `agents/docs/` se for específico de governança de Agents |
| `tools/agents/*` | `agents/tools/` (ou manter `tools/` na raiz só para ferramentas cross-app) | Hoje só existe conteúdo de Agents em `tools/`; se `tools/` vai virar genérico para múltiplas apps, mover para dentro de `agents/` evita ambiguidade |
| `backend/` (tudo) | `applications/mais-compras/backend/` | Confirmado 100% específico do +Compras (namespace `BlueprintOS.*`, sem referências genéricas encontradas) |
| `frontend/` (tudo) | `applications/mais-compras/frontend/` | Idem — pacote `@blueprintos/procurement-web` |
| `docs/backend/`, `docs/frontend/`, `docs/database/`, `docs/testing/`, `docs/product/`, `docs/executive/`, `docs/demo/`, `docs/releases/`, `docs/linxERP/`, `docs/Product Blueprint.md`, `docs/Executive Report.md` | `applications/mais-compras/docs/` | Documentação específica do domínio de compras |
| `docs/operations/*.md` | `applications/mais-compras/docs/operations/` (mantendo referência cruzada para `agents/`) | Runbooks operam sobre integrações do +Compras, mas são citados em `agent.yaml`; recomendação: manter um link/redirect ou atualizar `runbook_paths` |
| `.ai/work-orders/**` (O1.*, B1, B2.*) | `applications/mais-compras/.ai/work-orders/` (ou manter raiz se `.ai/` continuar sendo o "cérebro" cross-app) | Maioria do conteúdo é específico do +Compras — decisão de produto necessária (ver §6) |
| `.ai/work-orders/**` (A1–A13, A10) | `agents/` ou `shared/` (fundação de Agents/arquitetura) | Tratam da fundação de Agents, não do domínio de compras |
| `.ai/memory/*`, `.ai/content/*`, `.ai/audit-visual-screenshots/*`, `.ai/sources/*` | `applications/mais-compras/.ai/` | Conteúdo 100% sobre o +Compras |
| `.ai/context/*.md`, `.ai/templates/*`, `.ai/dashboard/*`, `.ai/prompts/*` (genéricos) | `shared/knowledge/` ou raiz `.ai/` mantida como cross-app | Processo de engenharia genérico, não específico do +Compras |
| `resources/design-system/`, `mcp/design-system/` | `shared/design-system/` | Confirmado multi-marca (AZZAS 2154, GDT) — não é exclusivo do +Compras |
| `resources/presentations/` | `applications/mais-compras/resources/presentations/` (roadmap do +Compras) ou `shared/` se genérico | Conteúdo (`+COMPRAS Strategic Roadmap.*`) é específico do +Compras |
| `scripts/start-dev.sh`, `stop-dev.sh`, `health-check.sh` | `applications/mais-compras/scripts/` | Fazem `dotnet run --project backend/...` / `cd frontend/web` — **hardcoded para o layout atual, precisam reescrita se movidos** |
| `scripts/linx_wise_daily_integration.py`, `test_linx_wise_daily_integration_governance.py` | Permanece em `scripts/` na raiz (cross-app: referenciado por 3 `agent.yaml`) ou `applications/mais-compras/scripts/` com atualização de `script_paths` | Decisão de produto — script é operação do +Compras mas orquestrado por Agents |
| `scripts/showcase_collector/` | `applications/mais-compras/scripts/showcase_collector/` (ou manter, referenciado por `showcase-agent`) | — |
| `infrastructure/` | `shared/architecture/` ou `applications/mais-compras/infrastructure/` | Hoje quase vazio (só `docker/.env.example` rastreado) — baixo risco, mas subpastas vazias (`terraform/`, `nginx/`, `monitoring/`, `kubernetes/`) sugerem intenção de infra cross-app → `shared/` é mais coerente |
| `mcp/` | Consolidar com `shared/design-system/` (ver §6 — possível duplicata) | `mcp/design-system/README.md` é o único arquivo; propósito não está claro sem mais contexto |

---

## 5. Dependências de paths encontradas (evidência concreta)

### 5.1 Backend (.csproj / .sln)
- `backend/BlueprintOS.sln` referencia todos os projetos via paths relativos `src\...\*.csproj` e `tests\...\*.csproj` — **internos a `backend/`, não cruzam a fronteira**.
- `ProjectReference` entre projetos `.csproj` usam `..\BlueprintOS.X\...` — todos internos a `backend/src/`.
- `backend/tests/BlueprintOS.IntegrationTests/BlueprintOS.IntegrationTests.csproj` e `BlueprintOS.UnitTests.csproj` referenciam `..\..\src\...` — internos a `backend/`.
- **Conclusão:** mover `backend/` inteiro para `applications/mais-compras/backend/` preservando a estrutura interna **não quebra nenhum path de build** (nenhuma referência escapa da pasta `backend/`).

### 5.2 Frontend (package.json / tsconfig / vite.config)
- `frontend/web/package.json`: sem paths absolutos ou `workspaces` apontando para fora de `frontend/web/`.
- `frontend/web/tsconfig.json`: `"include": ["src"]`, sem `paths`/`baseUrl` customizados.
- `frontend/web/vite.config.ts`: importa apenas `./src/core/viteProxyRules` (relativo interno) e `http://127.0.0.1:5262` (URL de rede, não path de arquivo).
- Imports TS com `../../../` encontrados em 10 arquivos (`CadastroFornecedor.tsx`, `SupplierComparison.tsx`, `FornecedorTable.tsx`, `FornecedorDetalhePage.tsx`, `PedidosPage.tsx`, `UnidadeNegocioTable.tsx`, `ErpConfiguracaoPage.tsx`, `RegraOrcamentariaTable.tsx`, e 2 testes) — todos **internos a `frontend/web/src/`** (voltam no máximo até `src/shared/` ou `src/core/`), nenhum escapa para fora de `frontend/web/`.
- **Conclusão:** mover `frontend/` para `applications/mais-compras/frontend/` preservando estrutura interna **não quebra imports TS**.

### 5.3 Scripts com paths hardcoded (RISCO REAL)
- `.myNotes` (não rastreado, mas usado por dev): `dotnet build backend/BlueprintOS.sln`, `dotnet run --project backend/src/BlueprintOS.Api/BlueprintOS.Api.csproj`, `cd frontend/web`.
- `scripts/start-dev.sh`, `scripts/stop-dev.sh`, `scripts/health-check.sh` — não lidos linha a linha nesta auditoria, mas pelo padrão do `.myNotes` e pela existência de `.backend.pid`/`.frontend.pid`/`.backend.log`/`.frontend.log` em `scripts/`, é quase certo que contenham `backend/` e `frontend/web` hardcoded. **Precisam ser abertos e ajustados antes de mover.**

### 5.4 Manifests de Agent (`agents/*/agent.yaml`) — dependência mais crítica encontrada
Todos os 8 manifests referenciam paths absolutos do repositório em `code_paths`, `runbook_paths`, `script_paths`, `docs_paths`, `test_paths`, `memory_paths`:

| Agente | Referencia `backend/...` | Referencia `docs/agents/Agents.md` | Referencia `docs/operations/*` | Referencia `scripts/...` | Referencia `tools/agents/...` |
|---|---|---|---|---|---|
| linx-erp-specialist-agent | sim (4 code_paths + 2 test_paths) | sim | sim (LinxWiseDailyIntegrationRunbook.md) | sim | sim |
| knowledge-agent | sim (5 code_paths) | sim | não | não | sim |
| agent-factory | não | referencia `AgentsCatalog.generated.html` | não | não | sim (3x) |
| wise-agent | não (code_paths vazio) | sim | sim (2 runbooks) | sim | sim |
| security-lgpd-agent | sim (5 code_paths) | sim | não | não | sim |
| linx-database-specialist-agent | sim (5 code_paths) | sim | sim | sim | sim |
| showcase-agent | não | sim | sim (2 runbooks) | sim (5 script_paths) | sim |
| echo-agent | sim (5 code_paths) | sim | não | não | sim |

**Todos os 8 manifests também referenciam `docs/architecture/AIGovernance.md` (security-lgpd e linx-database) e `tools/agents/validate-agent-manifests.js` (todos).**

- **RISCO CRÍTICO:** mover `backend/` para `applications/mais-compras/backend/` invalida `code_paths` e `test_paths` em 5 dos 8 manifests (linx-erp-specialist, knowledge-agent, security-lgpd, linx-database-specialist, echo-agent). Mover `docs/agents/*` para `agents/docs/` invalida `docs_paths` em 6 manifests. Mover `scripts/` invalida `script_paths`/`runbook_paths` em 5 manifests.
- Esses paths não são apenas documentação: fazem parte do **contrato de governança certificado (Agents v1)** — alterá-los sem atualizar os manifests quebra a rastreabilidade que a certificação depende.

### 5.5 `docs/agents/AgentsCatalog.html`
Contém referências textuais hardcoded (não são links `<a href>` verificados, mas texto/paths citados) a:
`backend/src/BlueprintOS.Application/...`, `backend/src/BlueprintOS.Core/Agents/*.cs`, `docs/agents/Agents.md`, `docs/architecture/AIGovernance.md`, `docs/operations/*.md`, `scripts/linx_wise_daily_integration.py`, `scripts/showcase_collector/`. Mesma exposição de risco do item 5.4 — é um artefato derivado dos manifests, deve ser regenerado após qualquer movimentação de paths.

### 5.6 Links Markdown relativos (`.ai/context/**`, `docs/operations/**`)
- `.ai/context/*.md` usam `../ARQUIVO.md` para apontar para a raiz de `.ai/` (ex.: `[AI_TEAM.md](../AI_TEAM.md)`) — **quebram se `.ai/context/` for movido sem mover `.ai/` inteiro junto**.
- `.ai/context/linx-wise-daily-integration.md` linka `../../docs/operations/LinxWiseDailyIntegrationRunbook.md` — **cruza a fronteira `.ai/` → `docs/`**, quebra se qualquer um dos dois lados mover isoladamente.
- `.ai/context/showcase-knowledge.md` e `wise-knowledge.md` linkam `../../scripts/showcase_collector/` e `../../backend/src/BlueprintOS.Application/Knowledge/Linx/LinxSpecialistAgents.cs` — **cruzam para `scripts/` e `backend/`**.
- `docs/operations/*.md` linkam de volta `../../.ai/context/*.md` — mesma fronteira, sentido inverso.
- **Conclusão:** existe uma malha de links cruzados entre `.ai/context/`, `docs/operations/` e `backend/`/`scripts/` que precisa ser tratada como grafo único ao mover qualquer um dos três.

### 5.7 CI (`.github/workflows`)
- Pasta existe mas está **vazia** (nenhum arquivo). **Sem risco de quebra de CI hoje** — mas também significa que não há proteção automatizada (lint/test) que detectaria path quebrado após a reorganização.

### 5.8 Testes com paths hardcoded
- Não foram encontrados paths de arquivo hardcoded (fora de `ProjectReference`) nos `.csproj` de teste além dos já listados em 5.1. Fixtures de teste não foram auditadas arquivo a arquivo nesta rodada (fora do escopo de tempo) — recomenda-se grep dedicado por `AppContext.BaseDirectory`, `Path.Combine(".."...)` antes de mover `backend/tests/`.

### 5.9 Recursos/assets (design system)
- `resources/design-system/preview/*.html` e `colors_and_type.css`/`fonts.css` não foram checados por referências absolutas nesta rodada; como são previews HTML standalone, risco de path quebrado é baixo, mas não confirmado por grep.
- Nenhuma referência de `backend/` ou `frontend/` a `resources/` foi encontrada (grep vazio) — **design system hoje não é consumido programaticamente pelo +Compras**, é usado via skill/Claude, reforçando que é genuinamente compartilhado e não acoplado ao código do +Compras.

---

## 6. Itens ambíguos (UNKNOWN) que precisam de decisão humana

1. **`mcp/design-system/README.md`** — conteúdo mínimo (1 arquivo), propósito não fica claro se é duplicata de `resources/design-system/` ou uma integração MCP separada e intencional. Decisão: consolidar ou manter distinto?
2. **`.ai/` como um todo** — hoje mistura conteúdo genérico de processo (templates, dashboard, prompts) com conteúdo 100% específico do +Compras (work orders O1.*/B1/B2.*, memory, content). A estrutura-alvo não prevê `.ai/` explicitamente. Decisão necessária: `.ai/` vira parte de `applications/mais-compras/`, continua na raiz como orquestrador cross-app, ou é dividido item a item conforme tabela do §2?
3. **`docs/operations/*.md`** — são runbooks operacionais do +Compras/ERP mas citados como `runbook_paths` em manifests de Agents. Ficam em `applications/mais-compras/docs/operations/` (conforme sugestão do enunciado) ou em `agents/docs/` (por serem parte do contrato de execução dos agentes)?
4. **`docs/architecture/AIGovernance.md`** — é doc de arquitetura geral (`docs/architecture/`) mas só é referenciado por manifests de Agents. Vai para `agents/docs/` ou `shared/architecture/`?
5. **`infrastructure/`** — hoje quase vazio (subpastas `terraform/`, `docker/`, `nginx/`, `monitoring/`, `kubernetes/` existem mas não têm arquivos rastreados além de `docker/.env.example`). É intenção futura para todas as aplicações (`shared/`) ou específico do +Compras?
6. **`docs/assets/`** — não inspecionado em detalhe; precisa verificação manual para saber se contém apenas ativos do +Compras ou algo reaproveitável.
7. **`scripts/linx_wise_daily_integration.py`** e **`scripts/showcase_collector/`** — orquestrados por Agents (`agent.yaml`) mas operam sobre dados do +Compras/ERP Linx. Ficam em `scripts/` (raiz, cross-app) ou dentro de `applications/mais-compras/scripts/`?
8. **`resources/presentations/`** — hoje contém apenas material do roadmap do +Compras; poderia ficar em `applications/mais-compras/resources/` mas fisicamente está ao lado do design system compartilhado.

---

## 7. Itens candidatos a `.empty/` (quarentena) — apenas listagem, nada movido

| Item | Evidência concreta de obsolescência/duplicação |
|---|---|
| `_staging/backend_full.tar.gz` | Tarball de 203 KB, datado de 07/08/2026, não rastreado pelo git, não referenciado por nenhum script, doc ou manifest encontrado via grep. Parece um backup manual esquecido. |
| `backend/backend/tests/BlueprintOS.UnitTests/Application/Governance/` | Diretório duplicado dentro de `backend/` (path `backend/backend/...`) — não rastreado pelo git, não faz parte da solução (`BlueprintOS.sln` não referencia `backend/backend/`). Parece resíduo de uma cópia/extração acidental. |
| `.ai/local-output/mb_prod_extra_web/**` | Dezenas de CSVs/JSONs de execução de integração de produção (`integracao_execucao_*.json`, `erros_integracao.csv`, etc.) — são saídas operacionais de uma execução específica, não código nem documentação de referência; se precisam ser preservados como evidência de auditoria, deveriam ir para um local de "audit trail", não para `.ai/local-output/` misturado com o restante do contexto de engenharia. |
| `....` (arquivo de 0 bytes na raiz) | Nome sem sentido, 0 bytes, criado em 24/08 — parece artefato acidental de terminal/editor. |
| `.DS_Store` (múltiplas pastas: raiz, `resources/`, `mcp/`, `docs/agents/`, `.ai/local-output/mb_prod_extra_web/`, `downloads/`) | Artefato de macOS Finder, sem valor, deveria estar 100% coberto por `.gitignore` (já está — não rastreado, mas continua sendo ruído físico no disco). |
| `.myNotes` | Contém credencial em texto claro (usuário/senha de e-mail). Não rastreado pelo git (`.gitignore:156`), mas por prudência deveria ser removido do disco de qualquer máquina compartilhada — **não é um problema de reorganização de pastas, é um risco de segurança independente**. Sinalizado aqui apenas para conhecimento, não como ação desta auditoria. |
| `downloads/` (586 MB) | Maior consumidor de espaço do repositório local; é saída de execução do `showcase_collector`, não fonte. Já ignorado pelo git; candidato a limpeza local, não a `.empty/` versionado. |

---

## 8. Riscos identificados (resumo consolidado)

1. **Governança de Agents quebra silenciosamente**: os 8 `agent.yaml` e o `AgentsCatalog.html`/`.generated.html` têm dezenas de `code_paths`/`docs_paths`/`script_paths`/`runbook_paths` hardcoded apontando para `backend/`, `docs/agents/`, `docs/architecture/`, `docs/operations/`, `scripts/`, `tools/agents/`. Qualquer movimentação dessas pastas sem atualizar todos os manifests em conjunto invalida a certificação Agents v1 (os paths deixam de existir, mas nada nos manifests indicaria isso automaticamente sem revalidação).
2. **Malha de links cruzados `.ai/context/` ↔ `docs/operations/` ↔ `backend/`/`scripts/`**: links Markdown relativos (`../../`) formam um grafo que atravessa as três fronteiras propostas (agents / applications / shared). Mover qualquer nó isoladamente quebra links nos outros.
3. **Build .NET e frontend são internamente coesos** (baixo risco): nenhuma referência de path escapa de `backend/` nem de `frontend/`, então mover essas duas pastas inteiras para `applications/mais-compras/` como unidades atômicas é seguro para build/testes — desde que os scripts de dev (`scripts/start-dev.sh` etc. e `.myNotes`) sejam atualizados junto.
4. **Scripts de dev hardcoded**: `scripts/start-dev.sh`/`stop-dev.sh`/`health-check.sh` (não lidos linha a linha, mas fortemente indicado por `.myNotes` e pelos `.pid`/`.log` presentes) provavelmente contêm `backend/BlueprintOS.sln`, `backend/src/BlueprintOS.Api/...`, `frontend/web` hardcoded — precisam ser abertos e corrigidos antes/durante a movimentação.
5. **Nenhuma proteção de CI**: `.github/workflows/` está vazio, então não há pipeline automatizado que capturaria path quebrado (build .NET, testes, lint frontend) após a reorganização — a validação terá que ser manual (`dotnet build`, `npm run build`, `npm test`, `node tools/agents/validate-agent-manifests.js`).
6. **`.ai/` não tem fronteira clara** entre conteúdo cross-app e conteúdo específico do +Compras — mover sem uma decisão explícita (§6.2) corre o risco de espalhar o mesmo diretório em dois lugares de forma inconsistente.
7. **Duplicação física não intencional** (`backend/backend/`) pode confundir uma migração automatizada (ex. um script de `git mv backend/ applications/mais-compras/backend/` replicaria a duplicata) — deve ser resolvida/limpa antes de qualquer `git mv` em massa.

---

## 9. Contagem-resumo por categoria (itens de topo/subpasta relevante avaliados nesta auditoria)

- **AGENTS**: 3 (agents/ raiz completo, tools/agents/, docs/agents/ + docs/architecture/AIGovernance.md)
- **APPLICATION_MAIS_COMPRAS**: 689+205 arquivos em backend/+frontend/, mais a maior parte de .ai/ (work-orders O1*/B1/B2*, memory, content, sources, audit-visual-screenshots) e docs/{backend,frontend,database,testing,product,executive,demo,releases,linxERP}
- **SHARED**: resources/design-system/, mcp/design-system/ (candidato a consolidar)
- **TOOLING**: tools/agents/, scripts/ (dev scripts)
- **OPERATIONS**: .ai/context/, .ai/templates/, .ai/dashboard/, docs/operations/, scripts/linx_wise_daily_integration.py, scripts/showcase_collector/
- **DOCUMENTATION**: docs/ (arquivos de topo), README/CLAUDE/AGENTS.md de raiz
- **GENERATED**: node_modules, bin/obj, dist/, frontend/web/dist/, .venv/
- **TEMPORARY**: downloads/, .ai/local-output/
- **UNKNOWN**: mcp/, infrastructure/, docs/assets/ (8 itens listados no §6)
- **QUARANTINE_CANDIDATE**: 6 itens listados no §7 (_staging/, backend/backend/, .ai/local-output/mb_prod_extra_web/, "....", .DS_Store, .myNotes)

---

*Fim do relatório. Nenhum arquivo existente foi movido, editado ou apagado durante esta auditoria — apenas este documento foi criado.*
