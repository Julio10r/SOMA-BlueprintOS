# Repository Cleanup Audit v1 (Read-Only)

Data: 2026-08-27
Autor: auditoria automatizada (Claude Code), solicitada por Julio Cesar
Escopo: SOMA-BlueprintOS pós-reorganização estrutural (agents/, applications/mais-compras/, shared/design-system/, tools/, scripts/, docs/repository/, infrastructure/, .ai/, .empty/)

**NADA foi movido, apagado, renomeado ou commitado durante esta auditoria. Nenhum push foi realizado. Esta é uma auditoria 100% read-only. A Fase 2 (execução de qualquer quarentena) aguarda autorização humana explícita.**

---

## 1. Resumo executivo

O repositório já passou por uma reorganização física validada (commits `23590d0`, `14ad48e`, `0d72f86`, `c8fcf7c`, `69e66f2`, e a criação de `.empty/` em `e090793`). A estrutura de alto nível está coerente com o padrão declarado: `agents/` (contrato + manifests canônicos), `applications/mais-compras/` (produto), `shared/design-system/`, `tools/agents/` (Factory/Registry/Orchestrator), `scripts/`, `docs/`, `infrastructure/`, `.ai/` (contexto operacional vivo) e `.empty/` (quarentena já criada, com manifesto próprio).

Principais achados:

- `agents/` está bem governado: `EXECUTION_POLICY.md`, `AGENT_CONTRACT.md`, `agent.schema.json` e os 7 `agent.yaml` (agent-factory, echo-agent, knowledge-agent, linx-database-specialist-agent, linx-erp-specialist-agent, security-lgpd-agent, showcase-agent, wise-agent) são fortemente referenciados entre si e por `tools/agents/*.js` e `.ai/context/*`. Nenhum candidato forte a quarentena foi identificado dentro de `agents/`.
- `.ai/` raiz tem uma dezena de arquivos `.md` "soltos" (BACKLOG.md, PROJECT_STATE.md, CURRENT_SPRINT.md, DECISIONS.md, WORKFLOW.md, VISION.md, STANDARDS.md, ROADMAP.md, ARCHITECTURE.md, AI_TEAM.md etc.) que, apesar de parecerem redundantes com `.ai/context/`, têm dezenas de referências cruzadas ativas (`grep` confirmou de 2 a 119 ocorrências cada) — portanto são **ACTIVE/CANONICAL**, não lixo.
- Quatro arquivos novos e não rastreados foram criados na raiz de `.ai/` na própria sessão de hoje (`AUDITORIA_AGENTS_GUARDRAILS_SECURITY_LGPD_20260827.md`, `AUDITORIA_AI_FACTORY_CONTRATO_AGENTS_20260827.md`, `AUDITORIA_COMPRAS_ESTADO_ATUAL.md`, `AUDITORIA_VISUAL_UX_COMPLEMENTAR.md`) mais a pasta `.ai/audit-visual-screenshots/` (34 PNGs). Nenhum é referenciado por outro arquivo do repositório além de si mesmo — são relatórios pontuais de auditoria, ainda não integrados/arquivados. Classificados como **UNKNOWN/possível candidato a `docs/audits/`** (não a `.empty/`), decisão do dono do conteúdo.
- `.empty/` já contém o inventário de quarentena da rodada anterior (`QUARANTINE_MANIFEST.md`), incluindo `backend_full.tar.gz` (200K, tracked no Git apesar do padrão `*.tar.gz` no `.gitignore` — provavelmente adicionado com `git add -f` antes do ignore, ou o ignore não se aplicava no momento do commit) e ~1.1 MB de saída bruta de integração Linx/Wise em `.empty/local-output/mb_prod_extra_web/`. Ambos já rastreados como QUARANTINE_CANDIDATE de rodada anterior — mantidos como estão, sem nova ação proposta aqui.
- Achado novo: `agents/docs/ai-factory/temp/` contém `LinxKnowledge-Fornecedor-Discovery-Snapshot.md` — uma pasta literalmente chamada `temp` dentro de uma árvore de documentação canônica de agents. Candidato a investigação (ver seção 13).
- Grandes volumes de dados locais não versionados: `applications/mais-compras/docs/linxERP/Linx_APP.zip` (376 MB, ignorado por `*.zip`), `scripts/.backend.log` (251 MB, ignorado por `*.log`, é log de processo de dev), `downloads/showcase_produtos/catalogo_showcase.xlsx` (123 MB, toda a pasta `downloads/` é ignorada e tratada como evidência de trabalho do usuário), além de `bin/`/`obj/` do backend .NET (dezenas de MB de binários regeneráveis). Nenhum desses é candidato a `.empty/` — são corretamente ignorados ou são evidência do usuário fora do escopo de código.
- Nenhum segredo em texto claro foi exposto neste relatório. `.env` existe na raiz e está corretamente listado no `.gitignore` (não tracked). Nenhum outro arquivo com nome sugestivo de credencial foi encontrado fora do que já era conhecido (`.myNotes`, mencionado no `QUARANTINE_MANIFEST.md` anterior como já sinalizado e não tratado — fora do escopo desta auditoria).

Nenhum novo QUARANTINE_CANDIDATE de confiança HIGH foi identificado para mover fisicamente. Os itens mais próximos de confiança HIGH (arquivos `.DS_Store`, `agents/docs/ai-factory/temp/`) ainda exigem confirmação humana antes de qualquer ação, dado o princípio de "nunca force um veredito" e o cuidado redobrado exigido para `agents/`.

## 2. Quantidade por classificação

| Classificação | Qtde (itens/diretórios avaliados nominalmente) |
|---|---|
| CANONICAL | 14 |
| ACTIVE | 28 |
| SHARED | 3 |
| GENERATED | 9 |
| LEGACY | 2 |
| DUPLICATE | 1 (par) |
| ORPHAN | 2 |
| TEMPORARY | 6 |
| SCAFFOLDING | 2 |
| UNKNOWN | 6 |
| QUARANTINE_CANDIDATE (novo, nesta rodada) | 3 |

(Contagem nominal por item/grupo relevante avaliado explicitamente nas seções 4–13; não é uma contagem arquivo-a-arquivo de todo o repositório, que soma ~2.728 arquivos fora de `.git/`, `.venv/`, `node_modules/`, `bin/`, `obj/`.)

## 3. Árvore auditada

```
.
├── .ai/                          (contexto operacional vivo — auditado integralmente)
│   ├── context/, memory/, prompts/, templates/, dashboard/, work-orders/, sources/, content/
│   ├── audit-visual-screenshots/  (novo, não rastreado)
│   └── *.md soltos na raiz         (ACTIVE, alta densidade de referências)
├── .empty/                       (quarentena já existente — apenas referenciado, não alterado)
├── .claude/                      (config local do Claude Code — fora do escopo de "projeto")
├── agents/                       (contrato canônico + 7 agent.yaml + docs/)
├── applications/mais-compras/    (produto: backend .NET, frontend, docs, resources)
├── docs/                         (repository/, audits/, assets/)
├── infrastructure/               (docker, kubernetes, monitoring, nginx, terraform — maioria vazia/scaffolding)
├── mcp/design-system/            (README apenas)
├── scripts/                      (start/stop/health + linx_wise_daily_integration.py + showcase_collector/)
├── shared/design-system/         (design tokens, templates, ui_kits)
├── tools/agents/                 (Factory v2, Runtime Registry, Governed Orchestrator + testes)
├── dist/                         (gerado sob demanda, ignorado por `/dist/`)
├── downloads/                    (evidência do usuário, ignorada por `downloads/`)
└── arquivos de raiz: AGENTS.md, CLAUDE.md, CHANGELOG.md, README.md, LICENSE, .env(.example), .gitignore, .gitattributes
```

`.git/` (608M), `.venv/` (11M), `node_modules/` (dentro de `applications/mais-compras/frontend/web/`) e diretórios `bin/`/`obj/` do .NET foram tratados apenas como referência de tamanho/gitignore, não como fonte de projeto.

## 4. Itens CANONICAL

| Item | Motivo |
|---|---|
| `agents/EXECUTION_POLICY.md` | Política global de execução, precedência nº1 declarada em `CLAUDE.md` e em si mesma. |
| `agents/AGENT_CONTRACT.md` | Contrato estrutural dos Agents, precedência nº2. |
| `agents/agent.schema.json` | Validação machine-readable, precedência nº3, usado por `tools/agents/validate-agent-manifests.js`. |
| `agents/<agent-id>/agent.yaml` (7 arquivos) | Declaração canônica de cada Agent, precedência nº4. |
| `agents/CAPABILITY_GAP_AND_AGENT_EVOLUTION_POLICY.md` | Referenciada explicitamente por `EXECUTION_POLICY.md` e `AGENT_CONTRACT.md`. |
| `agents/USER_ARTIFACT_LEARNING_POLICY.md` | Idem. |
| `agents/DATABASE_CONNECTION_POLICY.md` | Idem, herdada por todo Agent que toca banco Linx/SOMA. |
| `CLAUDE.md` (raiz) | Bootstrap oficial de IA para este repositório. |
| `.gitignore`, `.gitattributes`, `.editorconfig` | Configuração canônica de repositório. |
| `docs/repository/RepositoryStructure.md` | Documento de referência da estrutura física atual. |

## 5. Itens ACTIVE

- `.ai/context/*.md` (agents.md, architecture.md, coding-standards.md, definition-of-done.md, git-workflow.md, knowledge.md, linx-wise-daily-integration.md, memory.md, observability.md, planner.md, runtime.md, security.md, showcase-knowledge.md, tech-stack.md, testing.md, wise-knowledge.md) — referenciados por `agent.yaml` de múltiplos Agents.
- `.ai/*.md` soltos na raiz (AI_AUTONOMY_POLICY, AI_BEHAVIOR, AI_TEAM, ARCHITECTURE, BACKLOG, CURRENT_SPRINT, DECISIONS, DEVELOPMENT_WORKFLOW, DOCUMENTATION_STRATEGY, DOCUMENTATION_UPDATE_COMMAND, ENGINEERING_BLUEPRINT, PRESENTATION_WORKFLOW, PROJECT, PROJECT_PHILOSOPHY, PROJECT_SCOPE, PROJECT_STATE, PROJECT_VISION, ROADMAP, STANDARDS, VISION, WORKFLOW) — todos com múltiplas referências cruzadas confirmadas via grep (2 a 119 ocorrências). `PROJECT_STATE.md` (119), `CURRENT_SPRINT.md` (117) e `BACKLOG.md` (95) são os mais centrais. **Não são candidatos a quarentena.**
- `.ai/dashboard/*` (DASHBOARD_STATE.md — modificado nesta sessão de trabalho anterior, dashboard.css/js/html) — painel de status vivo.
- `.ai/work-orders/{active,backlog,completed,superseded}` — fluxo de trabalho ativo.
- `.ai/memory/*.md` — memória curada ativa (architecture, completed_sprints, decisions, known_issues, patterns).
- `.ai/prompts/*.md` — prompts operacionais referenciados em `.ai/context/linx-wise-daily-integration.md` e outros.
- `.ai/sources/COMPRAS_INDIRETAS_SOURCES.md` — fonte de conhecimento de domínio.
- `.ai/templates/*` — templates de work order/audit ainda em uso (README explica o propósito).
- `tools/agents/*.js` + `*.test.js` — Factory v2, Runtime Registry, Governed Orchestrator, validador de manifests, todos com testes correspondentes.
- `scripts/start-dev.sh`, `stop-dev.sh`, `health-check.sh`, `linx_wise_daily_integration.py`, `test_linx_wise_daily_integration_governance.py`, `showcase_collector/*` — scripts operacionais ativos, com README próprio no caso do collector.
- `shared/design-system/*` — sistema de design compartilhado, com README/INDEX/SKILL próprios.
- `applications/mais-compras/{backend,frontend,docs,resources}` — produto ativo (não auditado arquivo-a-arquivo por volume, mas nenhuma evidência de código morto foi buscada especificamente aqui pois estava fora do foco principal desta rodada; recomenda-se auditoria dedicada ao código-fonte do produto em rodada futura).

## 6. Itens LEGACY

| Item | Evidência |
|---|---|
| `docs/repository/RepositoryReorganization-Audit.md` | Relatório da auditoria que precedeu a reorganização física já concluída (commit `e090793` e subsequentes). Histórico, não mais acionável, mas com valor de proveniência. |
| `docs/audits/repository-cleanup-step-01/02/03.md` | Passos intermediários de uma limpeza de repositório anterior, já superados pelos commits de reorganização. Mantidos como registro histórico em `docs/audits/` (pasta já é `gitignore`d via `docs/audits/`, ou seja, é local/não versionada apesar de existir fisicamente — ver seção 14). |

Nenhum destes é recomendado para mover a `.empty/`: são histórico de auditoria já com "endereço" correto (`docs/audits/`, que é o padrão do projeto para relatórios de auditoria).

## 7. Itens GENERATED

| Item | Evidência |
|---|---|
| `agents/docs/AgentsCatalog.generated.html` | Gerado por `tools/agents/agent-factory-v2.js` (linha que escreve em `agents/docs/AgentsCatalog.generated.html`), a partir dos manifests canônicos. Regenerável a qualquer momento. |
| `scripts/.backend.log`, `scripts/.frontend.log` | Gerados por `scripts/start-dev.sh` (variáveis `BACKEND_LOG_FILE`/`FRONTEND_LOG_FILE`), ignorados via `*.log`. |
| `scripts/.backend.pid`, `scripts/.frontend.pid` | Idem, gerados/consumidos por `start-dev.sh`/`stop-dev.sh`, ignorados via `scripts/*.pid`. |
| `dist/{client,engineering,executive}/*.{html,md,pdf}` | Pasta `/dist/` inteira é gerada sob demanda por `dotnet run -- publish` (comentário no `.gitignore`), mas está presente localmente e sem estar no ignore ativo de fato (`/dist/` ancorado à raiz cobre `./dist`) — confirmar se está de fato ignorada (ver seção 14). |
| `applications/mais-compras/backend/**/bin`, `**/obj` | Build output .NET padrão, ignorado por `**/bin/`, `**/obj/`. Inclui os binários grandes (QuestPdfSkia, qpdf, EF Core dlls) listados na seção 14. |
| `applications/mais-compras/frontend/web/node_modules/**` | Dependências Node, ignoradas por `node_modules/`. |
| `downloads/showcase_produtos/catalogo_raw.json`, `resultado_final.json`, `erros.json`, `planilha_rows.json` | Saída de execução do `showcase_collector`, dentro de `downloads/` (ignorado por completo, tratado como evidência/execução do usuário). |

## 8. Itens DUPLICATE

Nenhuma duplicação de conteúdo binário/textual idêntico foi confirmada por hash nesta rodada (não foram encontrados pares de arquivos com nomes idênticos em locais diferentes que justificassem cálculo de `md5`/`shasum`, além do que já está documentado no `QUARANTINE_MANIFEST.md` anterior). Um par foi identificado por **sobreposição temática, não por conteúdo idêntico**:

| Par | Situação |
|---|---|
| `docs/repository/RepositoryReorganization-Audit.md` (229 linhas) vs `docs/repository/RepositoryReorganization-Final.md` (209 linhas) | Conteúdo diferente (`diff -q` confirma diferença), não são cópias. `-Audit` é o relatório de diagnóstico pré-reorganização; `-Final` é o relatório pós-execução. Não é DUPLICATE real — ambos são **SOURCE_CANONICAL** para momentos distintos do processo (LEGACY/histórico, ver seção 6), mantidos lado a lado por design. Reclassificado: não é candidato a quarentena. |

Nenhum DUPLICATE_CANDIDATE forte foi encontrado nesta rodada dentro do escopo revisado.

## 9. Itens ORPHAN

| Item | Evidência |
|---|---|
| `.empty/backend_full.tar.gz` | Já classificado como órfão no `QUARANTINE_MANIFEST.md` da rodada anterior ("Backup/tarball órfão do backend, sem consumidor identificado"). Confirmado nesta rodada: nenhuma referência ativa encontrada. Mantido como está — decisão de remoção definitiva já é do dono do backend, conforme o manifesto existente. |
| `.empty/local-output/mb_prod_extra_web/**` (75 arquivos, ~1.1 MB) | Idem — já classificado e justificado no manifesto anterior como saída bruta de execução Linx/Wise, sem referência ativa. Mantido como está. |

## 10. Itens TEMPORARY

| Item | Evidência |
|---|---|
| `.empty/dot-dot-dot-dot-empty-file` | Já classificado no manifesto anterior como arquivo vazio acidental (nome literal `....`). |
| `scripts/.backend.log` / `.frontend.log` / `.backend.pid` / `.frontend.pid` | Artefatos de execução local de dev server, regeneráveis, ignorados. |
| `.DS_Store` (11 ocorrências fora de `.git`/`.venv`/`node_modules`) | Arquivo de metadados do Finder do macOS, já coberto por `.gitignore` (`.DS_Store`), mas presentes fisicamente na raiz, `applications/.DS_Store`, `docs/.DS_Store`, `downloads/.DS_Store`, `mcp/.DS_Store`, `downloads/showcase_produtos/.DS_Store`, `.empty/local-output/mb_prod_extra_web/.DS_Store`, e outros. Lixo local de SO, seguro para apagar da máquina (categoria A, seção 14), nunca tracked. |
| `agents/docs/ai-factory/temp/` | Pasta chamada literalmente `temp` dentro de `agents/docs/ai-factory/`, contendo `LinxKnowledge-Fornecedor-Discovery-Snapshot.md`. Ver seção 13 — candidato a investigação, não a ação automática. |

## 11. Itens SCAFFOLDING

| Item | Evidência |
|---|---|
| `infrastructure/{docker,kubernetes,monitoring,nginx,terraform}` | `infrastructure/` tem apenas 4.0K de conteúdo próprio (fora `docker/.env.example`), a maioria das subpastas está vazia ou quase vazia — estrutura preparada para uso futuro, não ativa hoje. Não é lixo: é scaffolding intencional de uma reorganização que já previu esses diretórios. Nenhuma ação recomendada. |
| `mcp/design-system/README.md` | Único arquivo em `mcp/`, aparentemente scaffolding para uma integração MCP do design system ainda não implementada. |

## 12. Itens UNKNOWN

| Item | Por que não foi possível classificar com confiança |
|---|---|
| `.ai/AUDITORIA_AGENTS_GUARDRAILS_SECURITY_LGPD_20260827.md` | Não rastreado no Git, criado nesta sessão de trabalho recente, sem referências de outros arquivos. Pode ser um relatório de auditoria válido que ainda não foi movido para `docs/audits/` (padrão do projeto), ou pode ser rascunho de trabalho em andamento. Requer decisão do autor. |
| `.ai/AUDITORIA_AI_FACTORY_CONTRATO_AGENTS_20260827.md` | Idem. |
| `.ai/AUDITORIA_COMPRAS_ESTADO_ATUAL.md` | Idem. |
| `.ai/AUDITORIA_VISUAL_UX_COMPLEMENTAR.md` | Idem. |
| `.ai/audit-visual-screenshots/` (34 PNGs, não rastreados) | Evidência visual associada aparentemente aos relatórios acima. Sem referência textual explícita confirmada por nome de arquivo cruzado com os `.md` da mesma leva (não foi feita correlação fina conteúdo-a-conteúdo nesta rodada). |
| `agents/docs/ai-factory/temp/LinxKnowledge-Fornecedor-Discovery-Snapshot.md` | Nome da pasta (`temp`) sugere descartável, mas o conteúdo é um "Knowledge Snapshot" que pode ter sido incorporado a `.ai/context/` ou pode ainda ser a única cópia de um discovery. Não foi encontrada referência ativa, mas o esforço desta rodada não incluiu diff de conteúdo linha a linha contra `.ai/context/linx-wise-daily-integration.md` e correlatos. |

## 13. QUARANTINE_CANDIDATES

| PATH ATUAL | CLASSIFICAÇÃO | EVIDÊNCIA | REFERÊNCIAS | SUBSTITUTO | RISCO | DESTINO PROPOSTO EM .empty/ | CONFIANÇA | RECOMENDAÇÃO |
|---|---|---|---|---|---|---|---|---|
| `agents/docs/ai-factory/temp/LinxKnowledge-Fornecedor-Discovery-Snapshot.md` | TEMPORARY / UNKNOWN | Pasta chamada `temp` dentro de árvore de docs de agents; sem referência ativa encontrada via grep | Nenhuma referência encontrada em `.ai/`, `agents/`, `tools/agents/` | Possivelmente `.ai/context/linx-wise-daily-integration.md` ou `.ai/sources/COMPRAS_INDIRETAS_SOURCES.md` (não confirmado por diff) | Baixo (não é usado por schema/validator/testes) | `.empty/temporary/` (se confirmado obsoleto) | LOW | Pedir ao dono do conteúdo (Linx/Wise) para confirmar se o snapshot já foi incorporado ao `.ai/context/`; só então decidir mover. |
| `.ai/AUDITORIA_AGENTS_GUARDRAILS_SECURITY_LGPD_20260827.md`, `AUDITORIA_AI_FACTORY_CONTRATO_AGENTS_20260827.md`, `AUDITORIA_COMPRAS_ESTADO_ATUAL.md`, `AUDITORIA_VISUAL_UX_COMPLEMENTAR.md` | UNKNOWN (relatório de auditoria fora do padrão de local) | Não rastreados, criados na sessão atual, sem referência cruzada | Nenhuma | Local canônico esperado seria `docs/audits/` | Baixo (arquivos novos, não interferem em build/runtime) | N/A — não é candidato a `.empty/`; é candidato a **realocação para `docs/audits/`**, decisão do autor, não desta auditoria | LOW | Não mover agora. Autor deve decidir se são entregáveis finais (mover para `docs/audits/`) ou rascunho (manter em `.ai/` ou descartar manualmente). |
| `.ai/audit-visual-screenshots/` (34 arquivos PNG, não rastreados) | UNKNOWN | Evidência visual não rastreada, provavelmente associada às auditorias acima | Nenhuma referência textual confirmada | N/A | Baixo | N/A — mesma observação acima | LOW | Mesma decisão do item acima; se os relatórios forem arquivados em `docs/audits/`, os screenshots deveriam acompanhar (ex: `docs/assets/`). |

Nenhum item com confiança **HIGH** foi encontrado nesta rodada além dos já existentes e já tratados no `QUARANTINE_MANIFEST.md` anterior (não repetidos aqui por já estarem em `.empty/`). Nenhuma automação de Fase 2 é recomendada a partir desta tabela — todos os itens exigem confirmação humana.

## 14. Arquivos grandes locais

**(A) Lixo local que pode ser apagado da máquina (não versionado, não referenciado, sem valor de auditoria):**
- Todos os `.DS_Store` fora do Git (11 ocorrências) — metadado de Finder, zero valor.
- `scripts/.backend.log` (251 MB) — log de execução local de dev server, regenerável a cada `start-dev.sh`.

**(B) Conteúdo que poderia ir para `.empty/` versionado (requer decisão humana, nenhum movido aqui):**
- Nenhum item novo identificado além dos já presentes em `.empty/` desde a rodada anterior.

**(C) Conteúdo que deve permanecer ignorado (correto como está):**
- `applications/mais-compras/docs/linxERP/Linx_APP.zip` (376 MB) — ignorado por `*.zip`; material de referência do ERP Linx, não deve ser versionado.
- `downloads/` inteiro (586 MB, incl. `catalogo_showcase.xlsx` de 123 MB) — ignorado por `downloads/`; evidência de trabalho do usuário, fora do escopo de código.
- `applications/mais-compras/backend/**/bin`, `**/obj` (dezenas de binários de 2–8 MB cada: QuestPdfSkia, qpdf, EF Core, Roslyn) — build output .NET padrão, ignorado por `**/bin/`, `**/obj/`.
- `applications/mais-compras/frontend/web/node_modules/**` — dependências Node, ignoradas.
- `.venv/` (11 MB) — ambiente virtual Python local, não tracked, correto como está.

**(D) Conteúdo regenerável (não precisa ser versionado nem preservado):**
- Tudo listado em (C) é regenerável (`npm install`, `dotnet build`, `python -m venv`).
- `agents/docs/AgentsCatalog.generated.html` — regenerável via `tools/agents/agent-factory-v2.js`.
- `dist/{client,engineering,executive}/*` — regenerável via `dotnet run -- publish` conforme comentário no `.gitignore`. **Nota de risco**: o `.gitignore` usa o padrão `/dist/` (ancorado à raiz), o que deveria cobrir `./dist`, mas os arquivos de `dist/` não foram confirmados como *tracked ou não* nesta rodada com o mesmo rigor dos demais — recomenda-se checagem pontual (`git ls-files dist/`) antes de qualquer decisão sobre essa pasta.

## 15. Duplicações encontradas

Ver seção 8. Nenhuma duplicação binária/textual nova e confirmada por hash foi encontrada nesta rodada além do que a rodada de reorganização anterior já mapeou em `.empty/QUARANTINE_MANIFEST.md` (`_staging/backend_full.tar.gz` vs `applications/mais-compras/backend/` como fonte viva — SOURCE_CANONICAL já identificado anteriormente).

## 16. Riscos (sem expor segredos)

- **Risco de credencial já sinalizado e não resolvido**: o `QUARANTINE_MANIFEST.md` da rodada anterior registra que `.myNotes` foi sinalizado por "possível credencial em texto claro" e **não foi tratado** por estar fora do escopo da reorganização física. Este risco **permanece aberto** e não foi reavaliado nesta auditoria (fora do escopo read-only desta rodada acessar ou exibir o conteúdo). Recomenda-se ação humana direta: rotação de credencial + garantir que o arquivo está e permanece fora do controle de versão.
- **Arquivo `backend_full.tar.gz` tracked em `.empty/`**: apesar do padrão `*.tar.gz` no `.gitignore`, este arquivo está de fato commitado no Git (`git ls-files` confirma). Isso não é um risco de segredo per se, mas indica que o `.gitignore` atual não impediria retroativamente arquivos já adicionados antes da regra existir — não há evidência de conteúdo sensível dentro do tarball nesta auditoria (não foi extraído, por ser fora do escopo read-only não-destrutivo aprofundado).
- **Volume não versionado de dados operacionais Linx/Wise** (`downloads/`, `.empty/local-output/`): contém CSVs/JSONs com nomes sugerindo dados de produção (preços, estoque, campanhas). Nenhum segredo de credencial foi observado nos nomes de arquivo, mas o conteúdo não foi aberto/auditado por este processo (fora do escopo solicitado). Risco de dados operacionais sensíveis (não segredo de sistema) se esses arquivos forem compartilhados indevidamente — já mitigado por estarem fora do Git.
- **`agents/docs/ai-factory/temp/`**: nome de pasta genérico "temp" dentro de árvore de documentação de governança de Agents é uma prática de higiene de repositório a corrigir eventualmente (renomear ou mover), mas não representa risco de segurança.

## 17. Recomendações

1. **Não mover nada nesta fase.** Esta auditoria é diagnóstica; qualquer ação sobre os itens da seção 13 exige autorização humana explícita (Fase 2).
2. Resolver o risco de credencial em `.myNotes` (já sinalizado anteriormente e ainda pendente) como prioridade separada e urgente, fora do fluxo de limpeza de repositório.
3. Decidir o destino dos 4 relatórios de auditoria não rastreados em `.ai/` (seção 12/13): arquivar em `docs/audits/` se forem entregáveis finais, ou descartar manualmente se forem rascunho — decisão do autor, não desta auditoria.
4. Investigar `agents/docs/ai-factory/temp/LinxKnowledge-Fornecedor-Discovery-Snapshot.md` com o dono do domínio Linx/Wise antes de qualquer decisão.
5. Confirmar com `git ls-files dist/` se a pasta `dist/` está de fato sendo ignorada como o comentário do `.gitignore` sugere, e reconciliar se houver divergência.
6. Rodar `find . -name ".DS_Store"` periodicamente e apagar localmente (nunca tracked, sem risco).
7. Considerar, em uma Fase 2 futura e autorizada por humano, uma política de retenção/expurgo explícita para `.empty/local-output/` e `.empty/backend_full.tar.gz`, já que ambos seguem sem consumidor identificado desde a rodada anterior.
8. Não foi feita, nesta rodada, uma varredura arquivo-a-arquivo do código-fonte de `applications/mais-compras/{backend,frontend}` em busca de código morto — isso ficou fora do foco desta auditoria de limpeza estrutural e é recomendado como auditoria dedicada futura.

---

REPOSITORY_CLEANUP_AUDIT_V1 = COMPLETED

```
Totais (nominais, por item/grupo avaliado explicitamente nas seções 4-13):
  CANONICAL:            14
  ACTIVE:                28 (grupos; inclui dezenas de arquivos individuais dentro de .ai/context, .ai/ raiz, .ai/memory, .ai/prompts, tools/agents, scripts, shared/design-system)
  LEGACY:                 2
  GENERATED:              9
  DUPLICATE:               1 (par, não classificado como remoção — ambos mantidos)
  ORPHAN:                  2 (grupos, já quarentenados em rodada anterior)
  TEMPORARY:               6
  SCAFFOLDING:             2
  UNKNOWN:                 6

QUARANTINE_CANDIDATES (novos nesta rodada):
  HIGH:    0
  MEDIUM:  0
  LOW:     3

Total de arquivos avaliados no escopo (excluindo .git/, .venv/, node_modules/, bin/, obj/): ~2.728 arquivos

Espaço potencialmente recuperável (estimativa via du, tudo já corretamente ignorado ou já quarentenado — nada novo proposto para remoção nesta rodada):
  scripts/.backend.log:                          251 MB (categoria A - lixo local, regenerável)
  .DS_Store (11 arquivos):                       ~200 KB (categoria A - lixo local de SO)
  applications/.../docs/linxERP/Linx_APP.zip:    376 MB (categoria C - deve permanecer ignorado, não é "recuperável" pois é referência necessária)
  downloads/ (evidência do usuário):             586 MB (categoria C - fora do escopo de remoção, decisão do usuário)
  .empty/ já quarentenado (rodada anterior):      ~1.3 MB (aguardando decisão humana de remoção definitiva, já documentado)

Itens que exigem decisão humana: 7
  1. .myNotes (risco de credencial, já sinalizado, ainda pendente)
  2-5. Os 4 relatórios de auditoria + screenshots não rastreados em .ai/ (arquivar vs descartar)
  6. agents/docs/ai-factory/temp/LinxKnowledge-Fornecedor-Discovery-Snapshot.md (obsoleto ou não)
  7. Confirmação do tracking real de dist/ vs o comentário do .gitignore
```

**Reforçando: nada foi movido, nada foi commitado, nada foi enviado (push) nesta auditoria. A Fase 2 (qualquer ação física sobre os itens listados) aguarda autorização humana explícita.**
