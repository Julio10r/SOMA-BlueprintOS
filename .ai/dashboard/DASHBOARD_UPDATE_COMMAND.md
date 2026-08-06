# Comando Permanente — `[atualizar dashboard]`

> Instrução operacional permanente. Registrada pela Work Order **"[Dashboard] Instituir o Comando Permanente [atualizar dashboard]"** (06/08/2026). Referenciada por `.ai/CLAUDE.md`. Não redesenha o Dashboard, não altera sua arquitetura visual, não cria abas/métricas/regras novas sem necessidade comprovada.

## 0. Gatilho

Ao receber exatamente `[atualizar dashboard]` ou `atualizar dashboard` (sem colchetes, como instrução isolada e inequívoca), executar integralmente esta rotina, sem pedir novamente instruções já registradas aqui e sem responder apenas com orientações — a rotina deve ser efetivamente executada.

## 1. Escopo permitido

Pode alterar: `.ai/CLAUDE.md`, `.ai/dashboard/README.md`, `.ai/dashboard/DASHBOARD_STATE.md`, este arquivo, HTML/CSS/JS do Dashboard (somente para refletir dados derivados ou corrigir falha comprovada na rotina), testes/scripts isolados de validação, o workflow n8n correspondente e a versão publicada.

Pode ler, sem editar automaticamente: `.ai/ROADMAP.md`, `.ai/BACKLOG.md`, `.ai/PROJECT_STATE.md`, `.ai/CURRENT_SPRINT.md`, `.ai/DECISIONS.md`, `.ai/DOCUMENTATION_STRATEGY.md`, `.ai/work-orders/`, `docs/product/`, `docs/`, código/testes/commits/tags, `dist/health/`.

Nunca altera automaticamente: ROADMAP, BACKLOG, PROJECT_STATE, CURRENT_SPRINT, DECISIONS, Work Orders, documentação funcional, código de negócio, banco, infraestrutura, escopo das Ondas. Inconsistência nessas fontes interrompe a rotina e é relatada — nunca corrigida silenciosamente.

Não realiza commit ou push por padrão.

## 2. Princípio arquitetural

Fluxo obrigatório: Documentação oficial → `DASHBOARD_STATE.md` → Dashboard HTML/CSS/JS → workflow n8n → publicação → validação da URL real. O Dashboard consome exclusivamente `DASHBOARD_STATE.md` — nunca interpreta documentos oficiais, calcula regra de negócio, ou inventa entregável/status/data/percentual. Toda consolidação acontece previamente no `DASHBOARD_STATE.md`.

## 3. Confirmação do ambiente

No início: `pwd`, `git rev-parse --show-toplevel`, `git branch --show-current`, `git rev-parse HEAD`, `git status --short`. Confirmar repositório SOMA-BlueprintOS correto. Alteração pendente não relacionada ao Dashboard: não descartar, não sobrescrever, registrar no relatório, continuar somente se seguro; senão, interromper.

## 4. Fontes oficiais (leitura obrigatória)

`.ai/ROADMAP.md`, `.ai/BACKLOG.md`, `.ai/PROJECT_STATE.md`, `.ai/CURRENT_SPRINT.md`, `.ai/DECISIONS.md`, `.ai/DOCUMENTATION_STRATEGY.md`, `.ai/dashboard/README.md`, `.ai/dashboard/DASHBOARD_STATE.md`, `.ai/work-orders/README.md` e `active/`/`completed/`, `docs/product/README.md`, `ComprasFuncional.md`, `ComprasUX.md`, `ComprasDataModel.md`. Consultar quando necessário: commits recentes, tags, código, testes, `dist/health/DocumentationHealth.md`.

## 5. Validação de consistência

Validar, no mínimo: Onda atual coerente entre ROADMAP/PROJECT_STATE/CURRENT_SPRINT; status coerentes com evidências; datas planejadas preservadas; Work Orders concluídas sustentadas por evidência; entregáveis sustentados por documentação/código; versão e tag corretas; contagens do Backlog; testes/builds somente com evidência registrada; ausência de nomenclaturas de status inválidas. Inconsistência relevante → interromper antes de tocar `DASHBOARD_STATE.md`, não atualizar n8n, não publicar, apresentar relatório com documentos conflitantes/valores/recomendação, aguardar decisão humana. Nunca escolher silenciosamente uma versão conflitante.

## 6. Cabeçalho do DASHBOARD_STATE

Atualizar Schema Version (só se estrutura mudar), Project Version, Generated At, Last Update (data/hora da execução), Status.

## 7. Foundation

Manter: Status Concluído, Progresso Técnico 100%, Peso Gerencial 20%, Contribuição ao MVP 20 pontos, Data Real 05/08/2026, observações relevantes — enquanto não houver mudança de fonte oficial.

## 8–9. Ondas e entregáveis

Por Onda: Nome, Objetivo, Resultado Esperado, Status, Progresso Técnico, Peso Gerencial, Contribuição ao MVP, Gate, Critério do Gate, datas (planejada/real/replanejada), entregáveis e seus status/percentual/observações/evidências.

Status de Onda permitidos: Planejado, Em desenvolvimento, Bloqueado, Concluído, Cancelado. Status de entregável permitidos: Planejado, Em desenvolvimento, Concluído. Nenhuma variação livre.

Evidência de classificação, nesta ordem: Work Order → PROJECT_STATE/CURRENT_SPRINT → BACKLOG → documentação funcional → código → testes → commit/tag. Não confundir estrutura documental criada, especificação completa, mock pronto, implementação real e homologação — registrar o estágio real.

## 10–11. Cálculo (sempre no DASHBOARD_STATE, nunca no HTML)

Entregável Concluído = 100%; Planejado = 0%; Em desenvolvimento = percentual individual documentado, ou 0 se não houver percentual confiável (progresso mínimo confirmado, nunca estimado). Percentual da Onda = soma do progresso comprovado dos entregáveis ÷ total de entregáveis. Arredondar só na apresentação.

**Percentual Global do MVP 1.0 = Σ (Peso Gerencial × Progresso Técnico)**, por componente (Foundation 20%, Onda 1 20%, Onda 2 20%, Onda 3 20%, Onda 4 10%, Onda 5 10%), somado em pontos mesmo quando o Status da Onda ainda é "Planejado" — sem gate por início formal. Atualizar tabela de contribuição, percentual preciso e arredondado, barra principal, Roadmap, Executive e métricas relacionadas. Não manter valores antigos por conveniência.

## 12. Datas e replanejamento

Baseline (imutável): Onda 1 03/08→14/08/2026; Onda 2 15/08→29/08/2026; Onda 3 30/08→13/09/2026; Onda 4 14/09→25/09/2026; Onda 5 26/09→05/10/2026. Ao concluir uma Onda: registrar Início/Fim Real, calcular desvio em dias corridos, recalcular Início/Fim Replanejados das Ondas seguintes, preservar as datas planejadas, atualizar Gantt e Resumo Executivo. Onda não terminada não recalcula as seguintes apenas pelo tempo transcorrido, salvo decisão explícita registrada.

## 13. Nomes oficiais

Preservar nomes vigentes, incluindo "Onda 5 — Go Live - MVP 1.0 funcional". Não renomear sem mudança oficial nas fontes.

## 14–17. Resumo Executivo, Últimas Entregas, Próximos Objetivos, Decisões

Resumo Executivo: Situação Atual, Últimas Entregas, Próximos Objetivos, Próximo Marco, Principais Riscos, Onda Atual, Percentual Global — texto curto (máx. 5 linhas no resumo principal), linguagem executiva, sem conteúdo inventado, destacando bloqueios reais. Últimas Entregas derivam de entregáveis/Work Orders concluídos, commits relevantes, mudanças documentais significativas (só relevantes). Próximos Objetivos/Marcos derivam de Onda atual, entregáveis em desenvolvimento, Gate, CURRENT_SPRINT, ROADMAP. Decisões Recentes: apenas relevantes e rastreáveis (Data, Categoria, Resumo, Documento de origem) — nunca inferências sem registro no repositório.

## 18. Métricas

Atualizar somente com evidência oficial (Work Orders, telas, APIs, entidades, integrações, agentes, testes, documentos, warnings, health, links inválidos). Sem dado confiável: manter ausente, nunca mostrar zero como medido, nunca inventar.

## 19. MVP 1.0 e MVP 1.1 (aba Executive)

MVP 1.0: objetivo, percentual, Onda atual, Ondas e objetivos, status, prazos, marco final. MVP 1.1 (escopo adiado, só altera se fonte oficial mudar): ESG, Portal de Fornecedores, Marketplace, Analytics avançado, Previsão de Demanda, Previsão de Preços, Jurídico, Compliance, Gestão de Riscos.

## 20. Dashboard HTML

Após validar o `DASHBOARD_STATE.md`: regenerar/atualizar HTML/CSS/JS preservando todas as abas e funcionalidades; Roadmap como página inicial; Executive como segunda aba; atualizar barras, percentuais, entregáveis, datas, Gantt, resumos, métricas; ocultar campos vazios; nunca exibir "N/D", "Sem dado", "Pendente" ou explicações de ausência. Não redesenhar sem necessidade.

## 21. Gráfico de Gantt

Sempre atualizar Foundation + 5 Ondas, datas planejadas/reais/replanejadas, barra Planejada, barra Realizada proporcional ao Progresso Técnico, marcador da data atual, status e percentuais. Garantir que caiba integralmente no card (posicionamento percentual/flex, não pixels fixos), nomes legíveis, responsivo em Desktop/Notebook/Tablet. Legenda "Replanejado" só aparece quando houver dados replanejados.

## 22. Validações locais (antes do n8n)

Checklist: DASHBOARD_STATE consistente; Percentual Global recalculado; barras coerentes; datas corretas; Gantt correto; todas as Ondas e entregáveis presentes; status válidos; Roadmap abre por padrão; Executive em segundo; Backlog com busca/filtros; Fluxo de Compras funciona; Arquitetura aparece; responsividade; navegação por teclado; campos vazios ocultos. Executar scripts/testes isolados existentes. Falha nos testes → não atualizar n8n, não publicar; corrigir só se a falha for do Dashboard/Read Model; se decorrer de fontes inconsistentes, interromper e relatar.

## 23. Atualização do n8n

Somente após validação local aprovada: localizar o workflow correto, atualizar somente o node/artefato responsável pelo Dashboard, preservar a URL atual, não alterar outros workflows/nodes, preservar forma segura de restaurar a versão anterior, publicar/ativar. Nunca afirmar sucesso sem evidência real da operação.

## 24. Validação publicada

Acessar a URL real e validar: carregamento, Roadmap inicial, Executive em segundo, percentual global, barras, Gantt, entregáveis, datas, abas, busca/filtros, Fluxo de Compras, Arquitetura, responsividade, paridade com a versão local. Falha → restaurar/manter versão anterior, não considerar concluído, relatar detalhadamente.

## 25. Histórico do dia (no relatório)

Percentual anterior e novo; entregáveis que mudaram de status; Onda atual; alterações em datas; novo desvio (se houver); decisões adicionadas; métricas alteradas; resumo da evolução desde a última atualização. Não criar `HISTORY.md` nesta etapa, salvo se já oficialmente adotado.

## 26. Git

Por padrão: sem commit, sem push, sem alterar arquivos fora do escopo; alterações ficam prontas para revisão. Sem mudança de conteúdo/artefato: não criar alteração artificial — apenas validar publicação e relatar "sem mudanças materiais".

## 27. Critério de conclusão

Só concluída quando: fontes lidas; consistência validada; DASHBOARD_STATE atualizado; cálculos atualizados; Dashboard local validado; workflow n8n atualizado; publicação concluída; URL real validada; relatório apresentado. Nunca considerar concluído apenas por arquivos locais terem sido modificados.

## 28. Relatório padrão

Sempre apresentar: ambiente e branch; fontes lidas; consistência encontrada; mudanças no DASHBOARD_STATE; percentual anterior e novo; status das Ondas; entregáveis que mudaram; datas/replanejamento; métricas atualizadas; resultado dos testes locais; resultado da atualização do n8n; URL publicada; resultado da validação publicada; arquivos alterados; `git diff --check`; `git status --short`; riscos/pendências; sugestão de mensagem de commit.

## 29. Manutenção desta instrução

Alterações a este documento seguem o mesmo escopo permitido do comando (`.ai/CLAUDE.md`, `.ai/dashboard/README.md`, `.ai/dashboard/DASHBOARD_STATE.md`, este arquivo). Mudanças estruturais na rotina exigem Work Order explícita do Product Owner — nunca alteradas silenciosamente durante uma execução cotidiana do comando.
