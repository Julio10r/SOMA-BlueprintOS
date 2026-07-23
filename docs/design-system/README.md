# AZZAS 2154 — GDT Design System

Sistema de design unificado para a **AZZAS 2154**, cobrindo dois territórios:

1. **Corporate / Brand** — apresentações, capas, comunicação institucional
   (linguagem da marca-mãe AZZAS, derivada do template oficial de PPT).
2. **GDT — Gestão de Demandas de Tecnologia** — portais internos de tecnologia
   (curadoria, avaliação, execução, dashboards executivos).

A AZZAS 2154 é a holding criada em 2024 a partir da fusão entre **Grupo Soma** e **Arezzo&Co**,
hoje a maior plataforma de moda da América Latina. O número **2154** referencia a visão
de longo prazo do grupo. A família GDT são as ferramentas que orquestram a tecnologia
por trás dessas marcas — todas precisam respirar o mesmo idioma visual.

## Materiais de origem

| Arquivo | O que é |
|---|---|
| `uploads/GDT-BASE-TEMPLATE.html` | Template HTML "showcase" com todos os componentes do GDT v4.x |
| `uploads/GDT-DESIGN-SYSTEM.md`   | Documentação oficial do GDT (tokens, layout, componentes, UX) |
| `uploads/Template PPT Azzas_04092024.pptx` | Template corporativo oficial AZZAS — capas, fotografia editorial, paleta institucional |

*Não há repositório Figma ou GitHub anexado. Caso existam, anexá-los via menu Import
nos enriquece automaticamente o sistema.*

---

## Index

| Arquivo / pasta | Conteúdo |
|---|---|
| `README.md`                | Este arquivo — fundamentos, voz, motivos visuais, iconografia |
| `SKILL.md`                 | Front-matter pronto para uso como Claude Code Skill |
| `colors_and_type.css`      | **Fonte única** de tokens — cores, fontes, raios, sombras, espaçamento, escalas tipográficas |
| `fonts.css`                | Import Google Fonts (DM Sans, DM Mono, Inter Tight) |
| `assets/logos/`            | Wordmarks e monogramas AZZAS 2154 em variantes (black, watermark, vertical, stacked) |
| `assets/brand-photography/`| Fotos editoriais oficiais — uso em capas, heroes, slides de abertura |
| `assets/pptx-media/`       | Extração bruta do PPT (mantida para rastreabilidade) |
| `preview/`                 | Cards estáticos do Design System (aba Design System) |
| `ui_kits/portal-gdt/`      | UI kit clicável da SHELL GDT — auth OTP + tabs + Curadoria + Decisão + Acompanhamento |
| `slides/`                  | Slides exemplo seguindo o template PPT (capa, índice, conteúdo, etc.) |
| `portal-juridico/`         | Refit do Portal Jurídico com o design system (autossuficiente) |
| `ANALYSIS.md`              | Gap analysis entre o portal-GDT real e o design system |

---

## CONTENT FUNDAMENTALS — voz e copy

A escrita combina **formalidade corporativa em português brasileiro** (fundo
holding) com a **frieza objetiva de um sistema interno bem feito**.

### Idioma
- **PT-BR é primeira-classe.** Toda a UI dos portais é em português ("Em Avaliação", "Aprovar", "Rejeitar"). Acentos preservados. Sem inglês cosmético — `Dashboard` é uma exceção tolerada como termo técnico.
- **Voz de marca AZZAS é mais lacônica.** No template institucional: títulos curtos em caixa-alta (`CAPA DE APRESENTAÇÃO`, `ÍNDICE`, `OBRIGADO`), com longos parágrafos descritivos abaixo.

### Tom
- **Você** quando o sistema fala com o usuário ("Tem certeza que deseja aprovar esta demanda?"). Nunca "tu", nunca "vocês". Sem gírias.
- **Verbo no imperativo direto** para ações ("Aprovar", "Salvar", "Cancelar") — nunca "Aprove", nunca "Clique aqui para aprovar".
- **Mensagens factuais, não animadas.** "Demanda aprovada com sucesso." e não "🎉 Aprovado!". O sistema é um instrumento sério.

### Casing
- **Headings:** Sentence case (`Showcase de componentes`, `Pipeline executivo`).
- **Section titles em DM Mono:** UPPERCASE com letter-spacing largo (`PIPELINE EXECUTIVO`, `DADOS DA CAMPANHA`).
- **Botões:** Title Case curta (`Salvar`, `Novo Item`, `Em Avaliação`).
- **Profile badges:** UPPERCASE (`ADMIN`, `APROVADOR`).
- **IDs:** sempre `GDT-042` (prefixo + número, monoespaçado).

### Emoji & ícones decorativos
- **Sem emoji.** O sistema usa **ícones SVG line-art** (stroke 2px, currentColor). Único caractere unicode tolerado: o ponto colorido (`●`) ao lado de status em listas.

### Vibe
> "Um software corporativo que respeita o trabalho de quem usa." Visual editorial, sem exagero. Os portais devem se sentir como **um caderno de couro bege em cima da mesa**: organizados, sóbrios, com toques de cor apenas onde a semântica exige.

### Exemplos reais (extraídos)
- Empty state: *"Nenhum resultado — Não foram encontradas demandas com os filtros selecionados."*
- Toast de sucesso: *"Demanda aprovada com sucesso!"*
- Form hint: *"Selecione a área que originou a demanda"*
- Modal de confirmação: *"Tem certeza que deseja aprovar esta demanda? Essa ação não pode ser desfeita."*
- Slide PPT institucional: *"A campanha 'Elegância Sustentável' da Arezzo é uma celebração da moda consciente e sofisticada."*

---

## VISUAL FOUNDATIONS

### Paleta — duas camadas que se sobrepõem
- **Base do portal (GDT):** bege quente `#F7F6F3`, branco `#FFFFFF`, bordas `#E2E0DB`. Texto `#1A1916` (quase-preto, *não* preto puro — esta nuance é deliberada e a base de TODAS as sombras: `rgba(26,25,22,x)`).
- **Marca AZZAS (PPT):** preto `#000000` + branco `#FFFFFF` como cores principais; **apoio** em azuis (`#3B80D9`, `#6EB6E4`, `#274566`, `#A0C6ED`), verde-azulado `#539193`, areia `#B7A696`, íris `#7A8FE4`.
- **Status semânticos** (válidos em ambos os territórios): azul = novo, laranja = avaliação, verde = aprovado, vermelho = rejeitado, roxo = aguardando. **Cada status sempre vem em par** (cor + bg pastel).

### Tipografia
- **Display (capas, heros, hero KPIs):** Inter Tight 700, tracking apertado (-0.02em), line-height 1.05. Substituto leal ao wordmark AZZAS — ver "Substituições de fonte" abaixo.
- **UI corpo:** **DM Sans** em pesos 300–600. 13–14px é o tamanho dominante. Letter-spacing -0.01em a -0.02em em títulos.
- **Numérico / meta / IDs / section titles:** **DM Mono** em 300–500. Section titles em UPPERCASE 10–11px com letter-spacing 0.07em. KPIs grandes (24–30px) também em DM Mono.

### Backgrounds
- **Sem gradientes coloridos.** O fundo é sempre bege liso (`--bg`) ou branco (`--surface`). **Único** lugar onde gradiente é tolerado: overlay sobre fotografia editorial (preto translúcido para legibilidade).
- **Fotografia editorial em full-bleed** é o recurso visual mais forte do brand — usado em slides de capa, hero sections de páginas institucionais. Sempre fotos profissionais; **nunca** ilustração colorida.
- **Sem patterns repetitivos, sem texturas, sem grão.** O suporte gráfico é o **espaço em branco** e o bege.

### Animação
- **Discreta.** Padrão: `all 150ms ease` para hover/focus.
- **Bounce sutil** (`cubic-bezier(0.34, 1.56, 0.64, 1)`) é usado APENAS em entrada de modal e toast — sutil, ~200–300ms.
- **Progress bars / score fills:** `width 600ms cubic-bezier(0.4, 0, 0.2, 1)`.
- **Skeletons:** shimmer por mudança de opacidade (0.4 ↔ 0.9) em loop de 1.2s.
- **Sem parallax, sem efeitos de scroll, sem animações decorativas.**

### Hover states
- **Cards:** borda muda de `--border` para `--border-hover` + ganho de `--shadow-1` + `translateY(-1px)`.
- **Botão primário:** background `#1A1916` → `#2D2C29` (um tom mais claro).
- **Nav items:** background transparente → `--bg` + texto `--text-secondary` → `--text-primary`.
- **Filtros pill:** borda muda para `--border-hover`, texto fica primário.

### Press / active states
- Componentes não "encolhem". Em vez disso, o item ativo permanece destacado com `background: var(--bg)` e `font-weight: 500` (nav items, filter buttons mudam para `--accent` background + texto branco).

### Borders, shadows
- **Bordas 1px sólidas** em tons quentes (`--border` #E2E0DB) — **nunca** 2px decorativas, **nunca** coloridas como acento isolado (a "left-border colorida 4px" só aparece como **indicador de status** em cards — não como ornamento).
- **Sombras** todas derivam de `rgba(26,25,22,x)` — a cor do texto. Há 4 níveis declarados (`--shadow-1`…`--shadow-4`); o nível 0 (flat) é o padrão.
- **Sem inner shadows.** Sem glow.

### Capsule vs protection gradient
- **Capsules (pill 100px radius)** para: filtros ativos, badges de status, search box, nav-badges. É a forma característica do GDT.
- **Sem gradient overlays de proteção** — em cima de fotografias usa-se apenas um overlay sólido translúcido se necessário.

### Layout rules
- **Header height:**
  - `--header-h: 56px` para a **SHELL** (single-page com tab-nav embutida no header)
  - `--header-h-portal: 64px` para **portal standalone** (sem tab-nav, com logo + label + user-chip)
- **Sidebar widths** (três larguras coexistem):
  - `240px` para listas densas (curadoria), `200px` para avaliação, `220px` para admin.
  - Escondida em <768px.
- **Main content** com padding 24–32px, max-width orgânico (não constrange a 1200px — usa toda a área disponível).
- **Header full-width** que cobre tanto sidebar quanto main (`grid-column: 1 / -1`).

### Sidebar — duas variantes (use a apropriada para o contexto)
- **"Quieta"** (curadoria, avaliação, acompanhamento): active = `background: var(--bg)`. Texto fica primary, sem inversão. Para portais densos onde a lista do main é o foco visual.
- **"Afirmativa"** (admin): active = `background: var(--accent)` + texto branco. Para portais de configuração onde a sidebar é a navegação principal.
- Card de referência: `preview/component-sidebar-variants.html`.

### Tab navigation no header da SHELL
A SHELL principal (`index.html`) usa **tabs horizontais no header**, não sidebar. Cada tab carrega um portal em `<iframe>` com `?embedded=1`. O módulo admin tem cor especial (`--aguardando` ou `--brand-iris`) para diferenciar.

### Transparência e blur
- **Modal overlay:** `rgba(26,25,22,0.40)` + `backdrop-filter: blur(2px)` (use `--modal-overlay` e `--backdrop-blur`). Blur sutil, não agressivo — a intenção é reduzir o ruído do contexto, não esconder.
- **Único uso de blur no sistema.** Lema: "blur é caro, use com economia."
- **Sem cards translúcidos sobre conteúdo.** Cards são sempre opacos.

### Cor da imagem
- **Editorial:** fotos quentes ao crepúsculo (deserto cor-de-rosa, céu profundo), pretos puros, peles iluminadas — **paleta cinematográfica**, não saturada.
- **Diptychs B&W** são parte da linguagem (ver `movement-bw-diptych.jpeg`).
- Quando houver foto em UI (avatares, thumbnails), preservar o **brilho natural** — sem filtros, sem duotone.

### Corner radii
- Cards: 12px (`--radius`)
- Inputs / botões / badges grandes: 8px (`--radius-sm`)
- Tags pequenas / nivel-badges: 4px (`--radius-xs`)
- Filtros, status badges, search, nav-badges: 100px (pill)

### Card anatomy
- Background branco, border 1px `--border`, radius 12px, padding 14–20px.
- **Hover:** border-hover + sombra 1 + translateY(-1px).
- **Status indicator:** `border-left: 4px solid var(--status)` — o único caso em que coloração entra na borda de card.
- **Section header dentro do card:** section-title mono uppercase 10–11px.

### Filas SLA (curadoria, admin)
Quando agrupar itens por urgência, use o sistema de filas A/B/C/D documentado nos tokens:
- **Fila A — Urgente** (`--fila-a` #C0392B): SLA 1d, alertas críticos
- **Fila B — Rápida** (`--fila-b` #E09B3D): SLA 2d, curadoria expressa
- **Fila C — Analítica** (`--fila-c` #1A5276): SLA 5d, demanda discutida
- **Fila D — Aguardando** (`--fila-d` #6B6860): SLA pausado, espera externa

Cada fila tem a tripla (`--fila-x`, `--fila-x-bg`, `--fila-x-border`) para colorir header da seção + tag + borda. Card de referência: `preview/colors-filas-sla.html`.

### Logo no header de portal
- **Use sempre o PNG oficial** `assets/logos/azzas-2154-mark-black.png` em **22px de altura**.
- Sobre fundo escuro: aplique `filter: invert(1); opacity: .95`.
- **Não substitua** por SVG-texto, monogramas ad-hoc ou icônes improvisados — mesmo nas SHELLs internas. Card de referência: `preview/brand-logo-header.html`.

### Header v2 — padrão do Portal Jurídico (em produção)
A partir de mai/2026, o Portal Jurídico adota o **Header v2**, que substitui o nav central por breadcrumb + user-chip com dropdown:

- **Esquerda**: `[LOGO PNG] | Portal Jurídico (link pra home) · [Nome do módulo]` (via `.logo-suffix`)
- **Centro**: vazio (sem `<nav>` central)
- **Direita**: user-chip pill clicável que abre dropdown com Administração (apenas se admin) + Sair (em `--rejeitado`)

**Anti-padrões a evitar:** `<nav>` central com links de módulos, SVG-texto inline pra logo, label "Portal Jurídico" estático não-clicável, avatar + nome + ícone solto. Card de referência: `preview/component-header-v2.html`.

### Substituições de fonte (FLAG ao usuário)
O template PPT original declara que a apresentação foi feita em **Arial**. Para uso digital,
substituí por **Inter Tight** (display, mais próxima do peso/proporção do wordmark AZZAS)
e mantive **DM Sans / DM Mono** já estabelecidas no GDT.

> ⚠ **Ação sugerida ao usuário:** se houver uma fonte corporativa oficial (mesmo
> que seja Arial puro), envie o arquivo `.ttf`/`.otf` e atualizamos o `fonts.css`.

---

## ICONOGRAPHY

### Sistema
**Ícones SVG inline, stroke 2px, currentColor, 14×14 ou 16×16px** — o estilo é
**Lucide / Feather-like**: traços limpos, terminações arredondadas (`stroke-linecap: round`,
`stroke-linejoin: round`), sem preenchimento sólido. Os ícones que já existem
no `GDT-BASE-TEMPLATE.html` (grid, file, settings, search, plus, lupa…) usam exatamente esse padrão.

**Vinculação CDN:** preferimos **Lucide via CDN** (`https://unpkg.com/lucide@latest`)
quando precisamos de um ícone novo — ele é exato em traço, peso e proporção
ao que o GDT já usa.

```html
<!-- Lucide CDN, opcional -->
<script src="https://unpkg.com/lucide@latest"></script>
<script>lucide.createIcons();</script>
<!-- ou inline diretamente do site lucide.dev -->
```

> Substituição declarada: o codebase **não tem icon font próprio nem sprite**. Como
> os SVGs inline são o padrão, escolhemos Lucide como família coerente. **Não usamos
> Heroicons, Material Icons, Font Awesome, nem emoji.**

### Emoji
**Não.** Em momento algum o sistema usa emoji para representar conceitos.
Status é cor + texto, nunca emoji + texto.

### Unicode como ícone
**Apenas o bullet circular `●`** (ou um `<span>` com border-radius 50%) como indicador de cor de status em listas de navegação.

### Logotipos & marcas (`assets/logos/`)
- `azzas-2154-mark-black.png` — wordmark completo "AZZAS 2154" em preto sobre branco. **Logo institucional principal.**
- `azzas-2154-mark-watermark.png` — mesma marca em cinza muito claro (`#EFEFEF`-ish). Para overlay em fotografia, watermarks em capas escuras.
- `azzas-wordmark-black.png` — apenas "AZZAS" (sem 2154). Usar em contextos onde já existe a referência 2154 visualmente perto.
- `2154-stacked-black.png` — número "2154" empilhado vertical, em peso bold. **Uso decorativo / monograma**.
- `2154-numerals-black.png` — "2154" em grid 2×2, linhas finas. Variação minimalista.
- `azzas-vertical-black.png` — "AZZAS" rotacionado 90° (uso em laterais de slide / spine de revista).
- `A2154-monogram-light.png` — lockup "A • 21 / 54". Uso em watermarks/backgrounds editoriais (cor cinza claro).
- `rumo-a-2154-watermark.png` — "RUMO A 2154" como watermark institucional grande.

### Fotografia (`assets/brand-photography/`)
- `editorial-desert-duo.jpeg` — duas modelos em vestidos pretos no deserto. **Crepúsculo / sépia.** Capa institucional.
- `floral-gowns-duo.png` — vestidos florais bordados, fundo claro. **Coleção / produto.**
- `skater-sky.jpeg` — homem em movimento contra céu azul profundo. **Lifestyle / energia.**
- `movement-bw-diptych.jpeg` — diptych preto-e-branco de dança. **Editorial / abstrato.**

Estas quatro imagens cobrem os arquétipos editoriais da marca: capa solene, produto,
lifestyle e abstrato. Quando criar slides ou hero sections, escolha uma delas como
ponto de partida — não gere imagens novas.
