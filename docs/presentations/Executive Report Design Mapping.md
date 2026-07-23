# Executive Report — Design Mapping

> Mapa de rastreabilidade entre o `Executive Report Storyboard.md` (Sprint B1) e os arquivos reais do Design System oficial (`docs/design-system/`). Para cada slide, lista exatamente quais componentes, assets, tokens e templates foram empregados, com o caminho de arquivo — nenhum item aqui é inventado ou substituído por descrição genérica.
> Território: **Corporate / Brand** (`docs/design-system/SKILL.md`).
> Base: `Executive Report.md` (conteúdo aprovado), `Executive Review.md` (revisão aprovada), `Executive Report Storyboard.md` (layout aprovado nesta sprint).

---

## Tabela consolidada por slide

| # | Slide | Componentes (preview/) | Assets | Tokens de cor | Tipografia |
|---|---|---|---|---|---|
| 1 | Capa | `preview/brand-logo.html` (wordmark), `preview/brand-photography.html` (padrão de uso de foto full-bleed) | `assets/brand-photography/editorial-desert-duo.jpeg`; `assets/logos/azzas-2154-mark-watermark.png` | `--brand-ink` (overlay), `--brand-paper` (texto) | Display — Inter Tight 700 (`type-display.html`) |
| 2 | Resumo Executivo | `preview/component-cards.html` (3×) | — | `--brand-paper` (fundo), `--border`, `--text-primary` | Section Title — DM Mono (`type-mono.html`); corpo — DM Sans (`type-ui.html`) |
| 3 | O Problema Atual | `preview/component-notice-boxes.html` (variante `warn`) | — | `--warn` / `--warn-bg` | Section Title DM Mono; corpo DM Sans |
| 4 | A Visão do BlueprintOS | `preview/component-timeline.html` (3 marcos), `preview/component-badges-meta.html` (rótulos de ícone) | — | `--text-primary`, um destaque de apoio (`--brand-blue`) | Section Title DM Mono; rótulos DM Mono |
| 5 | Princípios da Plataforma | `preview/component-cards.html` (3×) | — | `--brand-paper`, `--border`, `--text-primary` | Section Title DM Mono; corpo DM Sans |
| 6 | Arquitetura de Alto Nível | `preview/component-timeline.html` (4 marcos) | — | `--text-primary`, `--border` | Section Title DM Mono; rótulos DM Mono |
| 7 | Roadmap | `preview/component-timeline.html` (6 marcos), `preview/component-badges-status.html` (badge "em andamento") | — | `--novo` / `--novo-bg` (marco ativo), `--border` (marcos neutros) | Section Title DM Mono; rótulos DM Mono uppercase |
| 8 | Situação Atual | `preview/component-notice-boxes.html` (variante `ok`), `preview/component-kpi-hero.html` | — | `--aprovado` / `--aprovado-bg`, `--text-secondary` | Section Title DM Mono; corpo DM Sans; H3 nos rótulos do KPI |
| 9 | Benefícios Esperados | `preview/component-cards.html` (3×) | — | `--brand-paper`, `--border`, `--text-primary` | Section Title DM Mono; corpo DM Sans |
| 10 | Próximos Passos | `preview/component-cards.html` (2×, largura dobrada) | — | `--brand-paper`, `--border`, `--text-primary` | Section Title DM Mono; corpo DM Sans |
| 11 | Conclusão | `preview/brand-logo.html` (wordmark/marca d'água), `preview/brand-photography.html` (padrão full-bleed) | `assets/brand-photography/movement-bw-diptych.jpeg`; `assets/logos/rumo-a-2154-watermark.png` | `--brand-ink` (overlay), `--brand-paper` (texto) | Display — Inter Tight 700 |

Todos os tokens de cor acima existem em `docs/design-system/colors_and_type.css`. Toda tipografia referencia `docs/design-system/fonts.css` e os previews `type-display.html`, `type-mono.html`, `type-ui.html`.

---

## Detalhamento por slide

### Slide 1 — Capa
- **Componente**: `docs/design-system/preview/brand-logo.html` — anatomia da marca d'água sobre fundo escuro/fotografia.
- **Asset fotográfico**: `docs/design-system/assets/brand-photography/editorial-desert-duo.jpeg`.
- **Asset de logo**: `docs/design-system/assets/logos/azzas-2154-mark-watermark.png`.
- **Tokens**: `--brand-ink` (overlay translúcido sobre a foto), `--brand-paper` (título/subtítulo em branco).
- **Justificativa**: o Design System reserva fotografia editorial full-bleed exclusivamente para abertura/fechamento institucional (`SKILL.md`, seção "Corporate / Brand"); a marca d'água em vez do wordmark sólido evita competir em contraste com o título.

### Slide 2 — Resumo Executivo
- **Componente**: `docs/design-system/preview/component-cards.html`, instanciado 3 vezes com pesos iguais.
- **Tokens**: `--brand-paper` (fundo), `--border` (borda do card), `--text-primary` (texto).
- **Justificativa**: os 3 blocos de conteúdo aprovados ("O que é", "Objetivo estratégico", "Valor para o negócio") mapeiam 1:1 para a anatomia de Cards já existente — não há necessidade de tabela, lista ou componente novo.

### Slide 3 — O Problema Atual
- **Componente**: `docs/design-system/preview/component-notice-boxes.html`, variante `warn`.
- **Tokens**: `--warn` / `--warn-bg` (`colors_and_type.css`, linha 70).
- **Justificativa**: o problema é um alerta de atenção, não uma falha do sistema — a variante `warn` (âmbar) é semanticamente correta; `rejeitado` (vermelho) seria um uso indevido do token de erro.

### Slide 4 — A Visão do BlueprintOS
- **Componentes**: `docs/design-system/preview/component-timeline.html` (3 marcos: Programa → BlueprintOS → +Compras); `docs/design-system/preview/component-badges-meta.html` (padrão de ícone + rótulo curto para os 4 conceitos de apoio).
- **Justificativa**: a cadeia de valor aprovada em `Executive Report.md` (Slide 4, speaker notes) é sequencial por natureza — o componente Timeline já existe para expressar exatamente essa relação, sem necessidade de diagrama customizado.

### Slide 5 — Princípios da Plataforma
- **Componente**: `docs/design-system/preview/component-cards.html`, mesma anatomia dos Slides 2 e 9 — repetição estrutural intencional.
- **Justificativa**: a revisão executiva (`Executive Review.md`) já consolidou 7 princípios em 3 blocos por afinidade; 3 Cards reaproveita o mesmo grid do Resumo Executivo, reforçando consistência visual entre slides de mesma densidade de conteúdo.

### Slide 6 — Arquitetura de Alto Nível
- **Componente**: `docs/design-system/preview/component-timeline.html`, mesmo componente dos Slides 4 e 7 — 4 marcos (Entrada → Inteligência → Dados → Conexão).
- **Justificativa**: `Executive Review.md` identifica este como o slide de maior risco técnico; reaproveitar a Timeline (já usada para conceitos, não apenas cronologia) evita introduzir um diagrama de arquitetura com caixas/setas fora do Design System.

### Slide 7 — Roadmap
- **Componentes**: `docs/design-system/preview/component-timeline.html` (6 marcos, Epic A–F); `docs/design-system/preview/component-badges-status.html` (badge no marco em andamento).
- **Tokens**: `--novo` / `--novo-bg` (marco ativo), `--border` (marcos neutros).
- **Justificativa**: o roadmap é literalmente uma linha do tempo de épicos — uso mais direto possível do componente Timeline; o Badge de status antecipa o Slide 8 sem duplicar texto.

### Slide 8 — Situação Atual
- **Componentes**: `docs/design-system/preview/component-notice-boxes.html` (variante `ok`); `docs/design-system/preview/component-kpi-hero.html`.
- **Tokens**: `--aprovado` / `--aprovado-bg`.
- **Justificativa**: a entrega mais relevante (motor de IA para o comprador sênior) é um fato consolidado — a variante `ok` do Notice box comunica isso corretamente; o KPI hero card expressa o status "em andamento" da Epic A como um indicador único e legível, evitando texto corrido.

### Slide 9 — Benefícios Esperados
- **Componente**: `docs/design-system/preview/component-cards.html`, mesma anatomia dos Slides 2 e 5.
- **Justificativa**: 3 dimensões de benefício (Negócio, Tecnologia, Governança e IA), já consolidadas na revisão executiva, mapeiam para o mesmo grid de 3 Cards — terceira repetição estrutural do mesmo componente na apresentação, reforçando previsibilidade de leitura.

### Slide 10 — Próximos Passos
- **Componente**: `docs/design-system/preview/component-cards.html`, 2 instâncias de largura dobrada.
- **Justificativa**: apenas 2 blocos de conteúdo (Curto Prazo, Médio Prazo) após a correção factual da revisão executiva — o mesmo componente Card, em variante de 2 colunas ao invés de 3, mantém a linguagem visual sem introduzir um componente diferente para um caso de conteúdo menor.

### Slide 11 — Conclusão
- **Componente**: `docs/design-system/preview/brand-logo.html`; `docs/design-system/preview/brand-photography.html`.
- **Asset fotográfico**: `docs/design-system/assets/brand-photography/movement-bw-diptych.jpeg`.
- **Asset de logo**: `docs/design-system/assets/logos/rumo-a-2154-watermark.png`.
- **Tokens**: `--brand-ink` (overlay), `--brand-paper` (texto).
- **Justificativa**: ecoa a Capa (mesma composição full-bleed + overlay + marca d'água), fechando o arco visual da apresentação — segunda e última aplicação de fotografia editorial, conforme reservado pelo Design System a abertura/fechamento.

---

## Lista consolidada de componentes utilizados

| Arquivo | Slides |
|---|---|
| `preview/brand-logo.html` | 1, 11 |
| `preview/brand-photography.html` | 1, 11 |
| `preview/component-cards.html` | 2, 5, 9, 10 |
| `preview/component-notice-boxes.html` | 3, 8 |
| `preview/component-timeline.html` | 4, 6, 7 |
| `preview/component-badges-meta.html` | 4 |
| `preview/component-badges-status.html` | 7 |
| `preview/component-kpi-hero.html` | 8 |

## Lista consolidada de assets utilizados

| Arquivo | Slides |
|---|---|
| `assets/brand-photography/editorial-desert-duo.jpeg` | 1 |
| `assets/brand-photography/movement-bw-diptych.jpeg` | 11 |
| `assets/logos/azzas-2154-mark-watermark.png` | 1 |
| `assets/logos/rumo-a-2154-watermark.png` | 11 |

## Tokens/arquivos de referência

- Cores: `docs/design-system/colors_and_type.css`
- Tipografia: `docs/design-system/fonts.css`, `preview/type-display.html`, `preview/type-mono.html`, `preview/type-ui.html`
- Espaçamento: `preview/spacing-scale.html`, `preview/spacing-radii.html`, `preview/spacing-shadows.html`

---

## Componentes do Design System não utilizados nesta apresentação

Componentes relevantes existentes em `preview/` que não aparecem no Executive Report, com justificativa da não-utilização:

- `component-summary-strip.html`, `component-stats.html`, `component-gauge-circular.html`, `component-barchart.html`, `component-hbar-comparison.html`, `component-heatmap.html` — componentes de dashboard operacional (dados quantitativos, séries, comparações numéricas). O Executive Report é uma narrativa qualitativa de projeto (problema → visão → roadmap → conclusão); não há, no conteúdo aprovado em `Executive Report.md`, nenhuma métrica numérica que justifique um gráfico ou gauge.
- `component-ranking-table.html`, `component-table.html`, `component-podium-top3.html`, `component-medals.html` — específicos do domínio de curadoria/avaliação de fornecedores (rankings, pontuação, pódio). Fora do escopo de uma apresentação executiva institucional.
- `component-sidebar.html`, `component-sidebar-variants.html`, `component-page-tabs.html`, `component-lookup-tabs.html`, `component-filters.html`, `component-form-selectors.html`, `component-inputs.html`, `component-search-hero.html`, `component-auth-otp.html` — componentes de navegação e interação de portal web (GDT Portal), incompatíveis com um documento estático de apresentação.
- `component-status-funnel.html`, `component-status-stepper.html`, `component-step-progress.html`, `component-pulsing-trilha.html`, `component-history-list.html`, `component-pendencia.html`, `component-sla-alert-row.html`, `component-avaliacao-comment.html`, `component-cur-card.html`, `component-area-strip.html`, `component-area-modal.html`, `component-categoria-grid.html`, `component-filas-em-uso.html`, `component-method-param.html`, `component-info-card.html` — componentes operacionais do território GDT Portal (filas, SLA, avaliação, curadoria), não aplicáveis ao território Corporate/Brand desta apresentação.
- `component-modal-toast.html`, `component-loading-states.html`, `component-states.html` — estados de interface (feedback transiente, carregamento) sem equivalente em um documento estático.
- `component-buttons.html` — não há call-to-action interativo em uma apresentação; os "botões" do sistema são elementos de UI de portal, não de slide.
- `component-avatar-mini.html` — não há atribuição de usuário/autor individual em nenhum slide do conteúdo aprovado.

---

## Recomendações para PowerPoint

Herdadas e detalhadas a partir da seção "Recomendações para PowerPoint" do `Executive Report Storyboard.md`:

1. **Base de slide-master**: usar `docs/design-system/assets/pptx-media/` e `Template PPT Azzas_04092024.pptx` como fonte real de capa, índice e grid — não recriar layout do zero em branco.
2. **6 masters, 11 slides**: criar um master por padrão de layout identificado neste mapeamento —
   - Master 1 "Full-bleed + overlay" → Slides 1, 11
   - Master 2 "3 Cards" → Slides 2, 5, 9
   - Master 3 "Timeline" → Slides 4, 6, 7 (variando 3/4/6 marcos no mesmo master)
   - Master 4 "Notice box + KPI" → Slide 8
   - Master 5 "2 Cards" → Slide 10
   - Master 6 "Notice box + lista" → Slide 3
3. **Nomeação de placeholders**: nomear cada placeholder do slide-master com o mesmo nome de componente usado neste documento (Card, Notice box, Timeline, KPI hero card, Badge de status, Brand photography, Brand wordmark) — garante rastreabilidade direta entre este mapeamento e o arquivo `.pptx` final.
4. **Fontes reais**: importar Inter Tight, DM Sans e DM Mono (`docs/design-system/fonts.css`) no PowerPoint antes de iniciar a diagramação; a Arial do template original já foi oficialmente substituída pelo Design System e não deve ser usada.
5. **Assets diretos**: usar os arquivos de `assets/logos/` e `assets/brand-photography/` listados na tabela consolidada diretamente — não gerar, recortar ou recriar variações de logo ou fotografia.
6. **Ícones**: importar o set Lucide (via `unpkg.com/lucide@latest` ou SVG inline) nomeado slide a slide conforme a seção "Ícones" do storyboard — não substituir por Heroicons, Material Icons, Font Awesome ou emoji.
7. **Validação final antes da Sprint B2**: abrir cada preview HTML referenciado nesta tabela ao lado do slide equivalente do `.pptx` e confirmar visualmente a paridade de cor, tipografia, espaçamento e raio de borda antes de considerar a diagramação concluída.
