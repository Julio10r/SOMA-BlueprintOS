# Product Blueprint — Design Mapping

> Mapa de rastreabilidade entre `Product Blueprint Storyboard.md` e os arquivos reais do Design System (`docs/design-system/`), incluindo os Masters PowerPoint oficiais (`docs/design-system/templates/powerpoint/`, Sprint B2.1).
> Território: **Corporate / Brand** (`docs/design-system/SKILL.md`).
> Base: `Product Blueprint.md` (conteúdo aprovado), `Product Blueprint Storyboard.md` (layout aprovado), `Executive Report Design Mapping.md` (referência de formato e de reutilização de componentes).

---

## Tabela consolidada por slide

| # | Slide | Master PowerPoint | Componentes (preview/) | Assets | Tokens de cor | Tipografia |
|---|---|---|---|---|---|---|
| 1 | Capa | `Master-Cover.pptx` | `preview/brand-logo.html`, `preview/brand-photography.html` | `assets/brand-photography/editorial-desert-duo.jpeg`; `assets/logos/azzas-2154-mark-watermark.png` | `--brand-ink`, `--brand-paper` | Display — Inter Tight 700 |
| 2 | Visão Geral | `Master-Section.pptx` (3 Cards) | `preview/component-cards.html` (3×) | — | `--brand-paper`, `--border`, `--text-primary` | Section Title DM Mono; corpo DM Sans |
| 3 | Problema Resolvido | `Master-Content.pptx` (`warn`) | `preview/component-notice-boxes.html` | — | `--warn`/`--warn-bg` | Section Title DM Mono; corpo DM Sans |
| 4 | Objetivos | `Master-Section.pptx` (3 Cards) | `preview/component-cards.html` (3×) | — | `--brand-paper`, `--border`, `--text-primary` | Section Title DM Mono; corpo DM Sans |
| 5 | Arquitetura Simplificada | `Master-Timeline.pptx` (3 marcos) | `preview/component-timeline.html` | — | `--text-primary`, `--brand-blue` | Section Title DM Mono; rótulos DM Mono |
| 6 | Funcionalidades | `Master-Section.pptx` (2 Cards) | `preview/component-cards.html` (2×, largura dobrada) | — | `--brand-paper`, `--border`, `--text-primary` | Section Title DM Mono; corpo DM Sans |
| 7 | Jornada do Usuário | `Master-Architecture.pptx` (4 etapas) | `preview/component-timeline.html` (variante 4 marcos) | — | `--text-primary`, `--border` | Section Title DM Mono; rótulos DM Mono |
| 8 | Roadmap | `Master-Timeline.pptx` (3 marcos) | `preview/component-timeline.html` | — | `--text-primary`, `--border` | Section Title DM Mono; rótulos DM Mono uppercase |
| 9 | Benefícios | `Master-Section.pptx` (3 Cards) | `preview/component-cards.html` (3×) | — | `--brand-paper`, `--border`, `--text-primary` | Section Title DM Mono; corpo DM Sans |
| 10 | Perguntas Frequentes | `Master-Content.pptx` (`ok`) | `preview/component-notice-boxes.html` | — | `--aprovado`/`--aprovado-bg` | Section Title DM Mono; corpo DM Sans |
| 11 | Conclusão | `Master-Closing.pptx` | `preview/brand-logo.html`, `preview/brand-photography.html` | `assets/brand-photography/movement-bw-diptych.jpeg`; `assets/logos/rumo-a-2154-watermark.png` | `--brand-ink`, `--brand-paper` | Display — Inter Tight 700 |

Todos os tokens de cor existem em `docs/design-system/colors_and_type.css`. Toda tipografia referencia `docs/design-system/fonts.css`. Todos os masters existem em `docs/design-system/templates/powerpoint/` (Sprint B2.1) — nenhum foi recriado ou alterado.

---

## Justificativas por slide

- **Slide 1 e 11**: reaproveitam exatamente o par Capa/Conclusão do Executive Report — mesmas duas fotografias editoriais, mesmos dois assets de logo, único uso permitido de full-bleed no Design System.
- **Slides 2, 4, 9**: três slides de 3 Cards com o mesmo grid — repetição estrutural intencional, idêntica ao padrão dos Slides 2/5/9 do Executive Report.
- **Slide 3**: Notice box `warn` — o problema é uma constatação de atenção, não uma falha do sistema (mesma regra semântica do Slide 3 do Executive Report).
- **Slide 5**: Timeline de 3 marcos reaproveitada para uma cadeia conceitual (Comprador → Agente → Recomendação), não cronológica — mesmo uso do componente no Slide 4 do Executive Report.
- **Slide 6**: variante de 2 Cards largos (Implementadas / Planejadas) — mesma variante usada no Slide 10 do Executive Report (Próximos Passos), reaproveitada aqui para separar o que já existe do que está no roadmap.
- **Slide 7**: usa o master `Master-Architecture.pptx` (4 etapas) para a Jornada do Usuário, condensando os 5 passos do conteúdo aprovado em 4 marcos, para caber exatamente na anatomia de 4 etapas já existente — nenhum master novo foi criado para isso.
- **Slide 8**: Roadmap do produto (5 fases no conteúdo aprovado) condensado em 3 marcos conceituais (Fundação / Conhecimento e Automação / Escala), reaproveitando o mesmo master de Timeline de 3 marcos do Slide 5 — evita introduzir uma variante de Timeline com mais marcos.
- **Slide 10**: Notice box `ok` — a mensagem central ("apoia, não substitui") é uma afirmação positiva de escopo, mesma variante usada no Slide 8 do Executive Report.

---

## Lista consolidada de masters utilizados

| Master | Slides |
|---|---|
| `Master-Cover.pptx` | 1 |
| `Master-Section.pptx` | 2, 4, 6, 9 |
| `Master-Content.pptx` | 3, 10 |
| `Master-Timeline.pptx` | 5, 8 |
| `Master-Architecture.pptx` | 7 |
| `Master-Closing.pptx` | 11 |

## Lista consolidada de assets utilizados

| Arquivo | Slides |
|---|---|
| `assets/brand-photography/editorial-desert-duo.jpeg` | 1 |
| `assets/brand-photography/movement-bw-diptych.jpeg` | 11 |
| `assets/logos/azzas-2154-mark-watermark.png` | 1 |
| `assets/logos/rumo-a-2154-watermark.png` | 11 |

---

## Componentes do Design System não utilizados nesta apresentação

Mesma justificativa herdada do `Executive Report Design Mapping.md`: componentes de dashboard operacional (`component-barchart.html`, `component-gauge-circular.html`, etc.), de domínio de curadoria/avaliação (`component-ranking-table.html`, `component-podium-top3.html`, etc.) e de navegação de portal (`component-sidebar.html`, `component-filters.html`, etc.) não se aplicam a uma apresentação institucional estática de produto. Nenhum componente novo foi criado ou inventado para o Product Blueprint.

---

## Confirmação de reuso

Nenhum elemento do Design System foi alterado nesta sprint. Todos os 6 masters usados já existiam em `docs/design-system/templates/powerpoint/` antes desta apresentação (Sprint B2.1). O Product Blueprint apenas populou os placeholders existentes com o conteúdo aprovado — nenhum slide-master, ícone, fonte ou token de cor novo foi criado.
