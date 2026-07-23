# Engineering Handbook — Design Mapping

> Mapa de rastreabilidade entre `Engineering Handbook Storyboard.md` e os arquivos reais do Design System (`docs/design-system/`), incluindo os Masters PowerPoint oficiais (`docs/design-system/templates/powerpoint/`, Sprint B2.1).
> Território: **Corporate / Brand** (`docs/design-system/SKILL.md`).
> Base: `Engineering Handbook.md` (conteúdo aprovado), `Engineering Handbook Storyboard.md` (layout aprovado), `Executive Report Design Mapping.md` (referência de formato e de reutilização de componentes).

---

## Tabela consolidada por slide

| # | Slide | Master PowerPoint | Componentes (preview/) | Assets | Tokens de cor | Tipografia |
|---|---|---|---|---|---|---|
| 1 | Capa | `Master-Cover.pptx` | `preview/brand-logo.html`, `preview/brand-photography.html` | `assets/brand-photography/editorial-desert-duo.jpeg`; `assets/logos/azzas-2154-mark-watermark.png` | `--brand-ink`, `--brand-paper` | Display — Inter Tight 700 |
| 2 | Arquitetura | `Master-Section.pptx` (3 Cards) | `preview/component-cards.html` (3×) | — | `--brand-paper`, `--border`, `--text-primary` | Section Title DM Mono; corpo DM Sans |
| 3 | Regras de Arquitetura | `Master-Content.pptx` (`warn`) | `preview/component-notice-boxes.html` | — | `--warn`/`--warn-bg` | Section Title DM Mono; corpo DM Sans |
| 4 | Stack | `Master-Section.pptx` (3 Cards) | `preview/component-cards.html` (3×) | — | `--brand-paper`, `--border`, `--text-primary` | Section Title DM Mono; corpo DM Sans |
| 5 | Organização do Projeto | `Master-Timeline.pptx` (3 marcos) | `preview/component-timeline.html` | — | `--text-primary`, `--brand-blue` | Section Title DM Mono; rótulos DM Mono |
| 6 | Convenções | `Master-Section.pptx` (2 Cards) | `preview/component-cards.html` (2×, largura dobrada) | — | `--brand-paper`, `--border`, `--text-primary` | Section Title DM Mono; corpo DM Sans |
| 7 | Estrutura das Pastas | `Master-Architecture.pptx` (4 etapas) | `preview/component-timeline.html` (variante 4 marcos) | — | `--text-primary`, `--border` | Section Title DM Mono; rótulos DM Mono |
| 8 | Testes e Qualidade | `Master-Timeline.pptx` (3 marcos) | `preview/component-timeline.html` | — | `--text-primary`, `--aprovado` | Section Title DM Mono; rótulos DM Mono |
| 9 | Ambiente e Deploy | `Master-Section.pptx` (3 Cards) | `preview/component-cards.html` (3×) | — | `--brand-paper`, `--border`, `--text-primary` | Section Title DM Mono; corpo DM Sans |
| 10 | Git Flow | `Master-Content.pptx` (`ok`) | `preview/component-notice-boxes.html` | — | `--aprovado`/`--aprovado-bg` | Section Title DM Mono; corpo DM Sans |
| 11 | Conclusão | `Master-Closing.pptx` | `preview/brand-logo.html`, `preview/brand-photography.html` | `assets/brand-photography/movement-bw-diptych.jpeg`; `assets/logos/rumo-a-2154-watermark.png` | `--brand-ink`, `--brand-paper` | Display — Inter Tight 700 |

Todos os tokens de cor existem em `docs/design-system/colors_and_type.css`. Toda tipografia referencia `docs/design-system/fonts.css`. Todos os masters existem em `docs/design-system/templates/powerpoint/` (Sprint B2.1) — nenhum foi recriado ou alterado.

---

## Justificativas por slide

- **Slide 1 e 11**: reaproveitam exatamente o par Capa/Conclusão do Executive Report — mesmas duas fotografias editoriais, mesmos dois assets de logo.
- **Slides 2, 4, 9**: três slides de 3 Cards com o mesmo grid — Arquitetura, Stack e Ambiente/Deploy têm a mesma densidade de conteúdo (3 blocos), mesma anatomia do Executive Report.
- **Slide 3**: Notice box `warn` — a regra de Contracts é uma restrição crítica que nenhum código pode violar, semanticamente equivalente ao alerta de atenção do Slide 3 do Executive Report.
- **Slide 5**: Timeline de 3 marcos reaproveitada para a cadeia de camadas (Domain → Application → Infrastructure/Api), mesmo uso conceitual (não cronológico) do componente já validado no Executive Report e no Product Blueprint.
- **Slide 6**: variante de 2 Cards largos (Código / Governança) — mesma variante usada no Slide 10 do Executive Report e no Slide 6 do Product Blueprint, reaproveitada para separar dois grupos de convenções.
- **Slide 7**: usa `Master-Architecture.pptx` (4 etapas) para a Estrutura das Pastas (backend/ → docs/ → .ai/ → infrastructure/) — mapeamento direto 1:1, sem necessidade de master novo.
- **Slide 8**: Testes e Qualidade reaproveita o mesmo master de Timeline de 3 marcos do Slide 5, para a prioridade de cobertura (Application → Domain → Integration).
- **Slide 10**: Notice box `ok` — a regra "main nunca recebe commit direto" é uma afirmação de processo consolidado, mesma variante usada no Slide 8 do Executive Report e no Slide 10 do Product Blueprint.

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

Mesma justificativa herdada do `Executive Report Design Mapping.md`: componentes de dashboard operacional, de domínio de curadoria/avaliação e de navegação de portal não se aplicam a uma apresentação institucional estática de handbook técnico. Nenhum componente novo foi criado ou inventado para o Engineering Handbook.

---

## Confirmação de reuso

Nenhum elemento do Design System foi alterado nesta sprint. Todos os 6 masters usados já existiam em `docs/design-system/templates/powerpoint/` antes desta apresentação (Sprint B2.1). O Engineering Handbook apenas populou os placeholders existentes com o conteúdo aprovado — nenhum slide-master, ícone, fonte ou token de cor novo foi criado.
