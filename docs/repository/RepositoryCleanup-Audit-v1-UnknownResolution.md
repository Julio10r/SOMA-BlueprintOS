# Repository Cleanup Audit v1 — Resolução de Itens UNKNOWN

Data: 2026-08-27
Continuação de: `docs/repository/RepositoryCleanup-Audit-v1.md`
Modo: **somente leitura**. Nenhum arquivo foi movido, renomeado, apagado, criado (exceto este relatório), commitado ou enviado (`git push`). Nenhum banco de dados ou API foi acessado. Todas as ações descritas abaixo são **propostas**, não execuções.

O item `.myNotes` já foi resolvido anteriormente (arquivo inexistente, nunca rastreado pelo git, gitignored) e não é reaberto aqui.

---

## 1. Os 4 arquivos `.ai/AUDITORIA_*` e `.ai/audit-visual-screenshots/`

### Evidência

- **Rastreamento git**: `git ls-files` para os 4 arquivos e a pasta retorna vazio — **nenhum é tracked**. `git status --porcelain` confirma os 5 caminhos como `??` (untracked).
- **Conteúdo e propósito** (lidos por completo):
  - `AUDITORIA_AGENTS_GUARDRAILS_SECURITY_LGPD_20260827.md` (931 linhas, 2026-08-27): diagnóstico da arquitetura de Agents/guardrails/segurança/LGPD — runtime agents reais, especialistas documentais, gaps de Policy Engine/Tool Gateway/AI Gateway.
  - `AUDITORIA_AI_FACTORY_CONTRATO_AGENTS_20260827.md` (702 linhas, 2026-08-27): mapeamento de como agents são criados/registrados hoje (`AgentFactory.cs` real vs. arquitetura-alvo de "AI Factory" documental), como briefing para continuidade em outro chat.
  - `AUDITORIA_COMPRAS_ESTADO_ATUAL.md` (371 linhas, 19/08/2026): auditoria funcional completa do +Compras (21 telas, classificação funcional/mock, achado crítico de DELETE físico em Parâmetros).
  - `AUDITORIA_VISUAL_UX_COMPLEMENTAR.md` (289 linhas, 2026-08-19): auditoria visual/UX complementar à anterior, mesma sessão de navegação Chrome DevTools MCP.
  - `audit-visual-screenshots/`: 39 arquivos `.png`.
- **Relação entre auditorias**: `AUDITORIA_VISUAL_UX_COMPLEMENTAR.md` referencia explicitamente `AUDITORIA_COMPRAS_ESTADO_ATUAL.md` no próprio texto ("Não repete a auditoria funcional/técnica já registrada em `.ai/AUDITORIA_COMPRAS_ESTADO_ATUAL.md`") — são pares intencionais da mesma campanha de auditoria (19/08/2026), enquanto as duas primeiras (20260827) são de uma campanha distinta e posterior sobre Agents/Factory.
- **Relação screenshots × auditorias — NÃO BATEM**: os nomes dos 39 PNGs (`S01-dashboard-1440.png` ... `S30-configuracao-erp-1024.png`, mais 4 arquivos `fornecedores-0N-*.png`) sugerem uma sessão de captura de tela real (S01–S30, com variantes 1440/1024). Porém, ambos os textos das auditorias que deveriam consumir essas imagens **afirmam explicitamente que nenhum screenshot pôde ser salvo em disco**:
  - `AUDITORIA_COMPRAS_ESTADO_ATUAL.md`, linha 314: *"Nenhum arquivo de screenshot pôde ser persistido em disco nesta auditoria: a ferramenta `mcp__chrome-devtools__take_screenshot` rejeitou todos os caminhos de destino testados [...] Como alternativa, toda a inspeção visual documentada [...] foi feita via `take_snapshot`"*.
  - `AUDITORIA_COMPRAS_ESTADO_ATUAL.md`, linha 363: repete a mesma afirmação nas conclusões.
  - Isso é uma **contradição não resolvida**: os 39 PNGs existem fisicamente em disco (confirmados via `find`) com nomes que combinam exatamente com a nomenclatura de telas das duas auditorias (S01-dashboard, S02-fornecedores-inicial, S04-perfis, etc.), mas o texto das próprias auditorias diz que a captura falhou. Duas hipóteses não verificáveis sem o autor original: (a) os PNGs foram gerados em uma sessão/tentativa posterior às citações de falha (as citações podem ter ficado desatualizadas no texto); ou (b) os PNGs vieram de outro mecanismo de captura (não o MCP citado) e o texto documenta apenas a limitação do `take_screenshot` MCP especificamente. Não há como confirmar qual, apenas por leitura estática — **este ponto é sinalizado como HUMAN_DECISION_REQUIRED** apenas para a pergunta "os PNGs são confiáveis/atuais o suficiente para anexar como evidência oficial", não para a classificação de destino do arquivo em si.
- **Referências em outros arquivos do repo**: `grep -rl` pelos 4 nomes de arquivo e por `audit-visual-screenshots` em todo o repositório (exceto os próprios arquivos) encontra apenas:
  - `docs/repository/RepositoryReorganization-Audit.md`
  - `docs/repository/RepositoryCleanup-Audit-v1.md`
  Ou seja, os 4 arquivos **não foram incorporados a nenhum documento vivo, ADR, README ou implementação** além dos próprios relatórios de auditoria de repositório que já os catalogaram como UNKNOWN. Não há evidência de que o conteúdo tenha sido "absorvido" em outro lugar — a evidência que carregam (estado real do +Compras, gaps de guardrails/Agent Factory) existe **somente** nesses 4 arquivos.
- **Se são resultado final ou intermediário**: os 4 são relatórios de auditoria completos e autocontidos (não rascunhos truncados) — resultado final de cada sessão de auditoria, ainda não formalizados como documento canônico em `docs/`.

### Conclusão

Os 4 `.md` registram evidência de auditoria única (não duplicada em nenhum outro documento do repositório) e têm valor de referência contínuo (ex.: o achado crítico do DELETE físico em Parâmetros, os gaps de AI Factory/Agent Contract). Por padrão de projeto, `.ai/` é usado para contexto de trabalho de IA e não para relatórios de auditoria formal — o par `AUDITORIA_COMPRAS_ESTADO_ATUAL.md`/`AUDITORIA_VISUAL_UX_COMPLEMENTAR.md` e o par de Agents/AI Factory são análogos em natureza a outros relatórios já existentes em `docs/repository/` e (presumivelmente) `docs/audits/`. Recomenda-se `MOVE_TO_DOCS_AUDITS` para os 4 `.md`, preservando o conteúdo integralmente. A pasta de screenshots deve acompanhar o destino do texto que ela ilustra (as duas auditorias de 19/08), mas apenas depois de esclarecida a contradição sobre a origem real dos PNGs — por isso a pasta recebe `HUMAN_DECISION_REQUIRED` em vez de uma recomendação direta de movimentação.

---

## 2. `agents/docs/ai-factory/temp/LinxKnowledge-Fornecedor-Discovery-Snapshot.md`

### Evidência

- O próprio arquivo (460 linhas, lido por completo) se autodocumenta com precisão no cabeçalho:
  - `STATUS: TEMPORÁRIO — AGUARDANDO INGESTÃO NA MEMÓRIA DOS AGENTS LINX`.
  - `FINALIDADE`: preservar estruturadamente o conhecimento de discovery Linx/Fornecedor para ingestão futura em `LinxKnowledgeEntry`/`LinxKnowledgeRepository`, "quando o GAP de infraestrutura descrito na seção 12 for resolvido".
  - `NÃO É`: substituto da memória persistente `LinxKnowledgeEntry`; explicitamente diz que vive em local versionado pelo git "exatamente para não perder o conhecimento até que a ingestão real seja possível".
  - Cita como **documentos-fonte preservados intactos**: `docs/audits/Discovery-Fornecedor-CNPJ-Linx-Compras.md` e `docs/audits/Arquitetura-Fornecedor-CNPJ-Decisao.md`.
- **Busca por ingestão real**: `grep -rln "LinxKnowledgeEntry.Criar\|SeedLinxKnowledge\|LX_SEQUENCIAL"` em `backend/` não retornou nenhum arquivo — não há evidência de que o conteúdo estruturado do snapshot (as unidades de conhecimento sobre `LX_SEQUENCIAL`, `CADASTRO_CLI_FOR`, as 5 procedures analisadas, etc.) tenha sido efetivamente inserido/seedado na entidade `LinxKnowledgeEntry` do código-fonte. O gap de infraestrutura que o próprio arquivo menciona (seção 12, não lida integralmente nesta rodada, mas referenciada) permanece, ao que tudo indica, **aberto**.
- **Comparação com `.ai/context/`, `.ai/sources/`, `agents/`**: buscas pelos termos-chave do snapshot (`LX_SEQUENCIAL`, `CADASTRO_CLI_FOR`, `LX_CADE`, `p_RSV_INTEGRACAO_CADASTRO_FORNECEDOR`) não encontraram ocorrências fora do próprio arquivo e dos documentos-fonte citados em `docs/audits/`.

### Conclusão

Classificação: **A) conhecimento único e ainda necessário** — não foi incorporado (nem parcial nem totalmente) a nenhuma fonte canônica machine-readable (`LinxKnowledgeEntry` ainda não populado com esse conteúdo), e o próprio arquivo já documenta corretamente sua natureza transitória e sua relação de não-substituição com os documentos-fonte em `docs/audits/`. Isto **não** atende ao critério de QUARANTINE_CANDIDATE HIGH pedido no enunciado (que exige B ou D com evidência forte de substituto real) — não há substituto real ainda. Recomenda-se manter o arquivo onde está (`agents/docs/ai-factory/temp/`), pois seu propósito documentado (aguardar ingestão) continua válido e ele é a única cópia estruturada e pronta-para-conversão desse conhecimento.

---

## 3. `dist/`

### Evidência

- `git ls-files dist/` → **vazio** (nada tracked).
- `git check-ignore -v dist/` → `.gitignore:47:/dist/  dist/` — a pasta está explicitamente ignorada.
- `git status --porcelain -- dist/` → vazio (consistente com "ignorado", nada aparece nem como untracked porque o gitignore já cobre).
- Conteúdo: `dist/client/ClientGuide.{html,md,pdf}`, `dist/engineering/EngineeringGuide.{html,md,pdf}`, `dist/executive/ExecutiveReport.{html,md,pdf}` — 528 KB total.
- `dist/client/ClientGuide.md` contém no cabeçalho: `**Gerado em:** 2026-07-30 17:43 UTC` — confirma que é saída gerada por processo automatizado/manual, não fonte autoral.
- `docs/repository/solution-tree.md` (linha 166) documenta a relação: *"executive/ — Executive Blueprint — fonte autoral; html/pdf publicados em dist/executive/"* — confirmando que `dist/` é destino de publicação de artefatos derivados de fontes autorais que residem em outro lugar do repositório (não em `dist/`).
- Não foi localizado, nesta busca, um script versionado (`package.json`, `.csproj`, `.sh`) que gere esses arquivos automaticamente — o mecanismo de geração não está mapeado no código do repositório nesta auditoria, mas a evidência de conteúdo ("Gerado em") e a documentação em `solution-tree.md` já confirmam que é saída derivada e não fonte.

### Conclusão

`dist/` é untracked, ignorado, e seu conteúdo é saída publicada/derivada de fontes autorais documentadas em `solution-tree.md`. Nenhum arquivo-fonte único foi identificado ali. Classificação: **GENERATED/LOCAL_OUTPUT** — não deve ser proposto para `.empty/`, apenas mantido como saída local regenerável.

---

## 4. `scripts/.backend.log` e `.DS_Store` (todos os encontrados)

### Evidência

`find . -name ".backend.log" -o -name ".DS_Store"` (excluindo `node_modules`) encontrou:

| Arquivo | `git check-ignore -v` | `git status --porcelain` |
|---|---|---|
| `./scripts/.backend.log` | `.gitignore:99:*.log` | (untracked, coberto pelo ignore) |
| `./.DS_Store` | `.gitignore:11:.DS_Store` | idem |
| `./mcp/.DS_Store` | `.gitignore:11:.DS_Store` | idem |
| `./docs/.DS_Store` | `.gitignore:11:.DS_Store` | idem |
| `./applications/.DS_Store` | `.gitignore:11:.DS_Store` | idem |
| `./downloads/.DS_Store` | `.gitignore:180:downloads/` | idem |
| `./downloads/showcase_produtos/.DS_Store` | `.gitignore:180:downloads/` | idem |
| `./applications/mais-compras/frontend/.DS_Store` | `.gitignore:11:.DS_Store` | idem |
| `./applications/mais-compras/backend/.DS_Store` | `.gitignore:11:.DS_Store` | idem |
| `./applications/mais-compras/frontend/web/.DS_Store` | `.gitignore:11:.DS_Store` | idem |
| `./applications/mais-compras/frontend/web/src/.DS_Store` | `.gitignore:11:.DS_Store` | idem |
| `./.empty/local-output/mb_prod_extra_web/.DS_Store` | `.gitignore:11:.DS_Store` | idem |

Todos os 12 arquivos são confirmados untracked e cobertos por regras de `.gitignore` (`*.log`, `.DS_Store`, `downloads/`). São artefatos de runtime (log de backend local) e de sistema operacional macOS (Finder), inerentemente regeneráveis e sem valor de auditoria.

### Conclusão

Todos os 12 arquivos: **SAFE_LOCAL_DELETE** (nenhum foi apagado nesta auditoria — apenas classificação/proposta).

---

## 5. `.empty/backend_full.tar.gz`, `.empty/local-output/mb_prod_extra_web/`, `.empty/dot-dot-dot-dot-empty-file`

### Evidência

- `git ls-files .empty/` retornou apenas os arquivos de governança (`QUARANTINE_MANIFEST.md`, `README.md`, `dot-dot-dot-dot-empty-file`) e os arquivos dentro de `local-output/mb_prod_extra_web/` — ou seja, esses itens **são tracked pelo git** dentro de `.empty/` (diferente de `dist/`, que é ignorado). `backend_full.tar.gz` não apareceu na listagem parcial de `git ls-files .empty/` capturada (verificação adicional recomendada antes de qualquer ação, mas o `QUARANTINE_MANIFEST.md` o trata como item já movido/gerenciado).
- `.empty/README.md` (lido por completo): declara que a pasta é "quarentena de reorganização física", que nada ali deve ser referenciado por código/scripts/docs vivos, e que remoção definitiva **exige revisão humana explícita** — nunca deleção "de passagem".
- `.empty/QUARANTINE_MANIFEST.md` (lido por completo) documenta cada item:
  - `backend_full.tar.gz` (origem: `_staging/backend_full.tar.gz`) — "Backup/tarball órfão do backend, sem consumidor identificado". Substituto: `applications/mais-compras/backend/` (fonte viva). "Nenhuma referência ativa em código, scripts ou docs." Recomendação do próprio manifesto: "Provavelmente sim [seguro apagar depois], após confirmação do dono do backend de que o tarball não é necessário para nenhum processo de disaster recovery externo ao git."
  - `local-output/mb_prod_extra_web/**` (origem: `.ai/local-output/mb_prod_extra_web/**`) — "Saída bruta de execução de integração (CSV/JSON de precheck, execução e verificação Wise/Linx), gerada localmente, não é fonte de conhecimento canônica". Substituto: `.ai/context/`, `docs/operations/`. "Nenhuma referência ativa em código ou docs vivos." Recomendação: "Provavelmente sim, após confirmação do dono da integração Linx/Wise de que os dados não têm valor de auditoria retido."
  - `....` (arquivo vazio, nome literal, registrado no manifesto como `dot-dot-dot-dot-empty-file`) — "Arquivo vazio sem função identificável, provável artefato acidental." Recomendação do manifesto: "Sim — arquivo vazio sem conteúdo a preservar."
- Nenhum dos três itens foi encontrado referenciado ativamente em código, `agent.yaml` ou documentação viva fora do próprio manifesto (que os cataloga como quarentena, não como uso ativo).

### Conclusão

O manifesto já documenta origem, substituto e justificativa para cada item, mas em todos os três casos a recomendação do próprio manifesto ("provavelmente sim") está condicionada a **confirmação humana do dono da área** (backend / integração Linx-Wise) antes de qualquer deleção definitiva — isso não foi obtido nesta auditoria (somente leitura). Portanto:
- `backend_full.tar.gz`: **KEEP_QUARANTINED** até confirmação do dono do backend.
- `local-output/mb_prod_extra_web/`: **KEEP_QUARANTINED** até confirmação do dono da integração Linx/Wise.
- `dot-dot-dot-dot-empty-file`: arquivo vazio, sem função, sem dependência de terceiros para confirmar — **SAFE_FOR_PERMANENT_DELETE** (evidência forte e sem risco: 0 bytes, nome-artefato de erro de shell, sem qualquer referência ativa).

---

## MATRIZ FINAL

| PATH | CLASSIFICAÇÃO FINAL | EVIDÊNCIA | SUBSTITUTO | AÇÃO RECOMENDADA | CONFIANÇA |
|---|---|---|---|---|---|
| `.ai/AUDITORIA_AGENTS_GUARDRAILS_SECURITY_LGPD_20260827.md` | Relatório de auditoria final, untracked, sem duplicata | `git ls-files` vazio; `grep` só encontra referência em relatórios de repo cleanup; conteúdo lido por completo | Nenhum — evidência única | MOVE_TO_DOCS_AUDITS | ALTA |
| `.ai/AUDITORIA_AI_FACTORY_CONTRATO_AGENTS_20260827.md` | Relatório de auditoria final, untracked, sem duplicata | idem acima | Nenhum — evidência única | MOVE_TO_DOCS_AUDITS | ALTA |
| `.ai/AUDITORIA_COMPRAS_ESTADO_ATUAL.md` | Relatório de auditoria final, untracked, sem duplicata | idem acima; referenciado por `AUDITORIA_VISUAL_UX_COMPLEMENTAR.md` como par | Nenhum — evidência única | MOVE_TO_DOCS_AUDITS | ALTA |
| `.ai/AUDITORIA_VISUAL_UX_COMPLEMENTAR.md` | Relatório de auditoria final, untracked, sem duplicata | idem acima | Nenhum — evidência única | MOVE_TO_DOCS_AUDITS | ALTA |
| `.ai/audit-visual-screenshots/` (39 PNGs) | Evidência visual de origem incerta — contradiz o texto das próprias auditorias que dizem "nenhum screenshot pôde ser persistido" | Nomes S01–S30 batem com telas citadas nas auditorias, mas texto das auditorias (linhas 314 e 363 de `AUDITORIA_COMPRAS_ESTADO_ATUAL.md`) afirma falha de persistência do MCP de screenshot | Possivelmente nenhum — necessita confirmação humana de origem/data dos PNGs | HUMAN_DECISION_REQUIRED | MÉDIA |
| `agents/docs/ai-factory/temp/LinxKnowledge-Fornecedor-Discovery-Snapshot.md` | A) conhecimento único, ainda não ingerido | Nenhuma ocorrência de ingestão real em `LinxKnowledgeEntry`/backend; termos-chave não aparecem fora do próprio arquivo e de `docs/audits/*` (documentos-fonte já citados pelo próprio snapshot) | Nenhum ainda (`LinxKnowledgeEntry` não populado) | KEEP | ALTA |
| `dist/` (client/engineering/executive — html/md/pdf) | Saída gerada, ignorada, não fonte | `git check-ignore` confirma `/dist/` ignorado; `git ls-files` vazio; conteúdo com timestamp "Gerado em"; `solution-tree.md` documenta como destino de publicação de fonte autoral externa | Fontes autorais em outros diretórios (ex.: `executive/` conforme `solution-tree.md`) | KEEP (GENERATED/LOCAL_OUTPUT, não mover) | ALTA |
| `scripts/.backend.log` | Log de runtime, untracked, ignorado (`*.log`) | `git check-ignore` confirmado | — | SAFE_LOCAL_DELETE | ALTA |
| `.DS_Store` (12 ocorrências, ver lista completa na Seção 4) | Artefato de SO, untracked, ignorado (`.DS_Store` / `downloads/`) | `git check-ignore` confirmado para todas as 12 ocorrências | — | SAFE_LOCAL_DELETE | ALTA |
| `.empty/backend_full.tar.gz` | Quarentena documentada, sem consumidor ativo, mas pendente de confirmação do dono | `QUARANTINE_MANIFEST.md`: "Nenhuma referência ativa"; recomendação condicionada a "confirmação do dono do backend" | `applications/mais-compras/backend/` (fonte viva) | KEEP_QUARANTINED | ALTA |
| `.empty/local-output/mb_prod_extra_web/` | Quarentena documentada, saída bruta não canônica, pendente de confirmação do dono | `QUARANTINE_MANIFEST.md`: "Nenhuma referência ativa"; recomendação condicionada a "confirmação do dono da integração Linx/Wise" | `.ai/context/`, `docs/operations/` | KEEP_QUARANTINED | ALTA |
| `.empty/dot-dot-dot-dot-empty-file` | Arquivo vazio (0 bytes), sem função, sem dependência externa | `QUARANTINE_MANIFEST.md`: "Arquivo vazio sem função identificável"; 0 bytes confirmado | — | SAFE_FOR_PERMANENT_DELETE | ALTA |

---

**Nada foi movido, alterado, commitado ou enviado (push) durante esta auditoria. Toda "AÇÃO RECOMENDADA" listada acima é uma proposta para decisão e execução humana posterior, não uma execução realizada por este relatório.**

REPOSITORY_CLEANUP_AUDIT_V1_UNKNOWN_RESOLUTION = COMPLETED
