# Executive Report — Storyboard

> Storyboard completo dos 11 slides do Executive Report (Sprint B1), a partir do conteúdo aprovado em `Executive Report.md` e da revisão em `Executive Review.md`.
> Este documento define layout, hierarquia, fluxo de leitura, componentes e wireframe — não é o PowerPoint (Sprint B2) nem gera arquivos `.pptx`.
> Segue rigorosamente o Design System oficial da AZZAS 2154 (`docs/design-system/SKILL.md`, `README.md`, `colors_and_type.css`), território **Corporate / Brand** (apresentações e comunicação institucional).

---

## Design System aplicado nesta sprint

Este storyboard usa exclusivamente tokens e ativos reais de `docs/design-system/`:

- **Cores de marca** (`colors_and_type.css`, seção 1): preto `--brand-ink #000000`, branco `--brand-paper #FFFFFF` como cores principais; apoio em azul `--brand-blue #3B80D9`, azul-céu `--brand-sky #6EB6E4`, azul-profundo `--brand-deep #274566`, verde-azulado `--brand-teal #539193`, areia `--brand-sand #B7A696`, íris `--brand-iris #7A8FE4`. Texto sempre `--text-primary #1A1916` (quase-preto), nunca preto puro.
- **Tipografia**: Display/títulos — **Inter Tight 700**, tracking apertado (`-0.02em`), line-height 1.05 (`--font-display`). Corpo — **DM Sans** 300–600 (`--font`). Numérico, IDs e Section Titles — **DM Mono** 300–500, uppercase, letter-spacing `0.07em` (`--mono`).
- **Ícones**: SVG line-art estilo Lucide, stroke 2px, `currentColor`, 14–16px. Sem emoji, sem preenchimento sólido.
- **Fotografia**: editorial full-bleed, `assets/brand-photography/` — `editorial-desert-duo.jpeg` (capa institucional, crepúsculo/sépia), `movement-bw-diptych.jpeg` (editorial/abstrato, P&B). Nunca ilustração colorida ou SVG decorativo de produto.
- **Logotipo**: `assets/logos/azzas-2154-mark-black.png` (wordmark principal) e `azzas-2154-mark-watermark.png` / `rumo-a-2154-watermark.png` (marca d'água sobre fotografia ou fundo escuro).
- **Componentes de referência** (`preview/`): Notice box (`component-notice-boxes.html`), KPI hero card (`component-kpi-hero.html`), Cards — anatomy (`component-cards.html`), Timeline / audit trail (`component-timeline.html`), Badges de status (`component-badges-status.html`), Brand photography (`brand-photography.html`), Brand wordmark (`brand-logo.html`).
- **Fonte-mestre para o PowerPoint**: `assets/pptx-media/` e `Template PPT Azzas_04092024.pptx` — a base oficial de capa, índice e conteúdo a ser reaproveitada na Sprint B2.

Nenhuma cor, fonte, ícone ou fotografia fora deste inventário é usada neste storyboard.

---

## Slide 1 — Capa

### Objetivo
Apresentar o projeto e criar impacto institucional imediato, antes de qualquer conteúdo.

### Mensagem Principal
Este é o SOMA BlueprintOS: a plataforma inteligente de IA da SOMA.

### Layout
- Full-bleed: fotografia editorial ocupa 100% do slide, com overlay sólido translúcido em `--brand-ink` (preto) para legibilidade do texto — único uso de gradiente permitido pelo Design System (overlay sobre fotografia).
- Wordmark AZZAS 2154 em marca d'água (`azzas-2154-mark-watermark.png`), discreto, canto superior.
- Título e subtítulo em branco (`--brand-paper`), alinhados à esquerda, no terço inferior do slide (mesma composição do template PPT oficial).

### Fluxo Visual
1. Fotografia editorial (impacto imediato)
2. Título (SOMA BlueprintOS)
3. Subtítulo e linha de apoio

### Componentes do Design System
- Brand photography (full-bleed)
- Brand wordmark (marca d'água)
- Display type (Inter Tight 700)

### Ícones
Não utilizar. Capa é fotografia + tipografia, sem elementos de UI.

### Fotografia
**Utilizar** — `assets/brand-photography/editorial-desert-duo.jpeg` (crepúsculo/sépia), indicada no Design System como referência de capa institucional.

### Palavras em Destaque
"SOMA BlueprintOS" — em Inter Tight 700, branco, sobre o overlay.

### Observações de Design
Overlay em `rgba(0,0,0,0.40–0.50)` sobre a fotografia para garantir contraste do texto branco, seguindo o padrão de blur/overlay do sistema (uso econômico, nunca decorativo). Marca d'água nunca compete em contraste com o título.

### Wireframe

```
+------------------------------------------------+
|[foto editorial full-bleed, overlay escuro]      |
|                                     [marca d'água]
|                                                  |
|                                                  |
|                                                  |
|  SOMA BlueprintOS                               |
|  Plataforma Inteligente para Gestão Corporativa |
|  Fundação tecnológica do +Compras               |
+------------------------------------------------+
```

---

## Slide 2 — Resumo Executivo

### Objetivo
Entregar, em poucos segundos de leitura, o essencial do projeto: o quê, por quê e valor.

### Mensagem Principal
O BlueprintOS é a base de IA da SOMA — e o +Compras é sua primeira prova de valor.

### Layout
- Fundo `--brand-paper` (branco). Section Title em DM Mono uppercase no topo ("RESUMO EXECUTIVO").
- Três Cards (`component-cards.html`, radius `--radius` 12px, border 1px `--border`), mesma largura, mesma altura: "O que é", "Objetivo estratégico", "Valor para o negócio".

### Fluxo Visual
1. Section Title
2. Card 1 → Card 2 → Card 3

### Componentes do Design System
- Section Title (DM Mono, uppercase, `--tracking-caps`)
- Cards — anatomy (3, mesmo peso)

### Ícones
Utilizar — Lucide, stroke 2px, 16px, cor `--text-primary`: `network` (O que é), `target` (Objetivo estratégico), `trending-up` (Valor para o negócio).

### Fotografia
Não utilizar.

### Palavras em Destaque
"base própria de IA", "decisões de compra mais consistentes" — peso 600 em DM Sans.

### Observações de Design
Cards com padding 14–20px (`--space-4`–`--space-5`), radius 12px, sem status color na borda (não é um card de status/fila).

### Wireframe

```
+------------------------------------------------+
|  RESUMO EXECUTIVO                               |
|                                                  |
|  +-------------+ +-------------+ +-------------+|
|  | (o) O que é | | (o) Objetivo| |(o)  Valor   ||
|  |             | | estratégico | |para negócio ||
|  +-------------+ +-------------+ +-------------+|
+------------------------------------------------+
```

---

## Slide 3 — O Problema Atual

### Objetivo
Justificar, com clareza, por que este investimento é necessário agora.

### Mensagem Principal
O conhecimento de compras hoje depende de pessoas, não de processo — e isso é frágil.

### Layout
- Section Title no topo ("O PROBLEMA ATUAL").
- Notice box (`component-notice-boxes.html`, variante `warn`) com a frase-síntese do problema.
- Lista simples de 3 pontos abaixo, em DM Sans, sem componente de card.

### Fluxo Visual
1. Section Title
2. Notice box (síntese)
3. Três pontos de apoio

### Componentes do Design System
- Section Title
- Notice box — variante `warn` (`--warn #E09B3D` / `--warn-bg #FDF3E3`)

### Ícones
Utilizar — ícone `alert-triangle` (Lucide) dentro do Notice box, único ícone do slide.

### Fotografia
Não utilizar.

### Palavras em Destaque
"experiência individual", "se perde"

### Observações de Design
O Notice box usa a dupla cor + bg pastel padrão do sistema (`--warn` / `--warn-bg`), nunca vermelho de erro (`--rejeitado`) — o problema é um alerta de atenção, não uma falha.

### Wireframe

```
+------------------------------------------------+
|  O PROBLEMA ATUAL                               |
|                                                  |
|  [!] "O conhecimento de compras depende de      |
|       pessoas, não de processo."                |
|                                                  |
|  - Negociações dependem da experiência individual|
|  - Histórico de fornecedores fica disperso       |
|  - Conhecimento se perde com rotatividade        |
+------------------------------------------------+
```

---

## Slide 4 — A Visão do BlueprintOS

### Objetivo
Apresentar o conceito central da plataforma e sua cadeia de valor.

### Mensagem Principal
Um investimento, duas entregas: o +Compras agora, a plataforma para sempre.

### Layout
- Section Title ("A VISÃO DO BLUEPRINTOS").
- Timeline / audit trail (`component-timeline.html`) horizontal com 3 pontos conectados: Programa → BlueprintOS → +Compras.
- Linha de 4 ícones com rótulo curto abaixo (AI First, Multiagentes, Modular, Reutilizável).

### Fluxo Visual
1. Section Title
2. Timeline Programa → BlueprintOS → +Compras
3. Linha de conceitos-chave

### Componentes do Design System
- Section Title
- Timeline / audit trail (adaptada para 3 marcos conceituais)
- Ícones com rótulo (padrão de `component-badges-meta.html`)

### Ícones
Utilizar — Lucide 16px, `--text-primary`: `cpu` (AI First), `users` (Multiagentes), `box` (Modular), `refresh-cw` (Reutilizável).

### Fotografia
Não utilizar.

### Palavras em Destaque
"BlueprintOS", "sem recomeçar do zero"

### Observações de Design
A Timeline central deve ter maior peso visual (tipografia DM Mono nos rótulos dos 3 marcos) que a linha de 4 ícones abaixo, que é apoio.

### Wireframe

```
+------------------------------------------------+
|  A VISÃO DO BLUEPRINTOS                         |
|                                                  |
|   [Programa] --> [BlueprintOS] --> [+Compras]   |
|                                                  |
|   (o)          (o)          (o)          (o)    |
|  AI First   Multiagentes   Modular   Reutilizável|
+------------------------------------------------+
```

---

## Slide 5 — Princípios da Plataforma

### Objetivo
Deixar claros os critérios que orientam toda decisão técnica da plataforma.

### Mensagem Principal
Toda decisão técnica segue três compromissos: inteligência, crescimento e confiança.

### Layout
- Section Title ("PRINCÍPIOS DA PLATAFORMA").
- Três Cards (mesma anatomia do Slide 2), um por bloco: "AI First e Multiagentes", "Modular e Escalável", "Segura, observável e integrável".

### Fluxo Visual
1. Section Title
2. Card 1 → Card 2 → Card 3

### Componentes do Design System
- Section Title
- Cards — anatomy (3)

### Ícones
Utilizar — Lucide 16px: `sparkles` (AI First e Multiagentes), `layers` (Modular e Escalável), `shield-check` (Segura, observável e integrável).

### Fotografia
Não utilizar.

### Palavras em Destaque
"AI First", "Modular", "Confiança"

### Observações de Design
Mesmo grid e alinhamento dos Cards do Slide 2 e do Slide 9, para consistência estrutural entre os três slides de 3 Cards.

### Wireframe

```
+------------------------------------------------+
|  PRINCÍPIOS DA PLATAFORMA                       |
|                                                  |
|  +-------------+ +-------------+ +-------------+|
|  |(o) AI First | | (o) Modular | |(o)  Segura, ||
|  |e Multiagentes| | e Escalável| |observável e ||
|  |             | |             | |  integrável ||
|  +-------------+ +-------------+ +-------------+|
+------------------------------------------------+
```

---

## Slide 6 — Arquitetura de Alto Nível

### Objetivo
Situar, sem jargão técnico, os blocos que compõem a plataforma.

### Mensagem Principal
A plataforma segue um fluxo simples: entrada, inteligência, dados e conexão com o mundo corporativo.

### Layout
- Section Title ("ARQUITETURA DE ALTO NÍVEL").
- Timeline / audit trail horizontal com 4 marcos conceituais: Entrada → Inteligência → Dados → Conexão. Sem caixas de sistema, sem nomes de tecnologia visíveis.

### Fluxo Visual
1. Section Title
2. Bloco 1 → Bloco 2 → Bloco 3 → Bloco 4

### Componentes do Design System
- Section Title
- Timeline / audit trail (4 marcos, mesmo componente do Slide 4 e 7 — consistência entre os três slides de linha do tempo)

### Ícones
Utilizar — Lucide 16px: `log-in` (Entrada), `cpu` (Inteligência), `database` (Dados), `share-2` (Conexão).

### Fotografia
Não utilizar.

### Palavras em Destaque
"simples", "rastreabilidade"

### Observações de Design
Nenhum nome de tecnologia (SQL Server, ASP.NET etc.) aparece no slide — fica reservado às notas do apresentador, se necessário. Este é o slide com maior risco de parecer técnico demais; a regra é 4 blocos e nada além disso.

### Wireframe

```
+------------------------------------------------+
|  ARQUITETURA DE ALTO NÍVEL                      |
|                                                  |
|  [Entrada] -> [Inteligência] -> [Dados] -> [Conexão]
+------------------------------------------------+
```

---

## Slide 7 — Roadmap

### Objetivo
Apresentar a evolução planejada da plataforma em grandes etapas.

### Mensagem Principal
Da fundação à operação real, em seis etapas claras.

### Layout
- Section Title ("ROADMAP").
- Timeline / audit trail horizontal com 6 marcos (Epic A–F), rótulo curto em DM Mono abaixo de cada.

### Fluxo Visual
1. Section Title
2. Marco A → B → C → D → E → F

### Componentes do Design System
- Section Title
- Timeline / audit trail (6 marcos)
- Badge de status (`component-badges-status.html`) no marco em andamento

### Ícones
Utilizar — Lucide 16px, um por marco, mesma família: `flag` (Fundação), `store` (Portal +Compras), `cpu` (Inteligência Operacional), `share-2` (Integração Corporativa), `bar-chart-2` (Analytics e IA), `rocket` (Produção).

### Fotografia
Não utilizar.

### Palavras em Destaque
Nome de cada épico (A a F), em DM Mono uppercase.

### Observações de Design
O marco Epic A recebe o Badge de status "Em andamento" (`--novo` / `--novo-bg`, dupla cor + bg pastel padrão); os demais ficam em contorno neutro (`--border`), sem preenchimento — antecipa o Slide 8 sem repetir texto.

### Wireframe

```
+------------------------------------------------+
|  ROADMAP                                        |
|                                                  |
|  (A)--(B)--(C)--(D)--(E)--(F)                   |
| FUND. PORTAL INTEL. INTEGR. ANALYT. PROD.        |
|  [em andamento]                                 |
+------------------------------------------------+
```

---

## Slide 8 — Situação Atual

### Objetivo
Mostrar, de forma honesta, onde o projeto está hoje.

### Mensagem Principal
A inteligência que apoia o comprador sênior já existe e funciona.

### Layout
- Section Title ("SITUAÇÃO ATUAL").
- Notice box variante `ok` (`--aprovado` / `--aprovado-bg`) com a entrega mais relevante (capacidade de IA para o comprador sênior).
- KPI hero card (`component-kpi-hero.html`) para o status da Epic A ("Epic A — em andamento").
- Linha de apoio menor, em `--text-secondary`, com a base institucional já entregue.

### Fluxo Visual
1. Section Title
2. Notice box — capacidade de IA já entregue
3. KPI hero card — status da Epic A
4. Linha de apoio — base institucional

### Componentes do Design System
- Section Title
- Notice box — variante `ok`
- KPI hero card

### Ícones
Utilizar — `check-circle` no Notice box; ícone de progresso (`loader`) no KPI hero card.

### Fotografia
Não utilizar.

### Palavras em Destaque
"já existe e funciona", "comprador sênior"

### Observações de Design
Ordem de leitura intencional: a capacidade de IA (Notice box `ok`, prova concreta) vem antes da base institucional — decisão herdada da revisão executiva (Sprint B0.1), que identificou risco de o projeto parecer "só documentação" se a ordem original fosse mantida.

### Wireframe

```
+------------------------------------------------+
|  SITUAÇÃO ATUAL                                 |
|                                                  |
|  [✓] Motor de negociação e memória de           |
|      fornecedores já apoiam o comprador sênior  |
|                                                  |
|  [KPI: Epic A — em andamento]                   |
|                                                  |
|  Base institucional: conhecimento organizacional |
|  e documentação oficial já entregues             |
+------------------------------------------------+
```

---

## Slide 9 — Benefícios Esperados

### Objetivo
Mostrar o impacto esperado por dimensão.

### Mensagem Principal
O retorno aparece em três frentes: negócio, tecnologia e governança.

### Layout
- Section Title ("BENEFÍCIOS ESPERADOS").
- Três Cards (mesma anatomia dos Slides 2 e 5): Negócio, Tecnologia, Governança e IA.

### Fluxo Visual
1. Section Title
2. Card Negócio → Card Tecnologia → Card Governança e IA

### Componentes do Design System
- Section Title
- Cards — anatomy (3)

### Ícones
Utilizar — Lucide 16px: `trending-up` (Negócio), `layers` (Tecnologia), `shield` (Governança e IA).

### Fotografia
Não utilizar.

### Palavras em Destaque
"decisões mais consistentes", "sem retrabalho", "conhecimento preservado"

### Observações de Design
Evitar qualquer menção a artefatos internos (ADRs, documentação automatizada) no texto visível do slide — benefício em linguagem de resultado, não de processo, conforme já ajustado na revisão executiva.

### Wireframe

```
+------------------------------------------------+
|  BENEFÍCIOS ESPERADOS                           |
|                                                  |
|  +-------------+ +-------------+ +-------------+|
|  | (o) Negócio | |(o)Tecnologia| |(o) Governança||
|  |             | |             | |    e IA     ||
|  +-------------+ +-------------+ +-------------+|
+------------------------------------------------+
```

---

## Slide 10 — Próximos Passos

### Objetivo
Mostrar a evolução imediata do projeto.

### Mensagem Principal
O caminho segue em duas etapas claras: consolidar agora, expandir em seguida.

### Layout
- Section Title ("PRÓXIMOS PASSOS").
- Duas Cards lado a lado, largura igual: "Curto Prazo" e "Médio Prazo".

### Fluxo Visual
1. Section Title
2. Card esquerda (Curto Prazo) → Card direita (Médio Prazo)

### Componentes do Design System
- Section Title
- Cards — anatomy (2, largura dobrada em relação aos slides de 3 Cards)

### Ícones
Utilizar — `arrow-right-circle` (Curto Prazo), `compass` (Médio Prazo).

### Fotografia
Não utilizar.

### Palavras em Destaque
"Curto Prazo", "Médio Prazo"

### Observações de Design
Manter apenas as duas frases já validadas na revisão executiva — não reintroduzir itens já entregues (Product Blueprint, Engineering Handbook etc.), removidos por inconsistência factual na Sprint B0.1.

### Wireframe

```
+------------------------------------------------+
|  PRÓXIMOS PASSOS                                |
|                                                  |
|  +-----------------+   +-----------------+      |
|  | (o) Curto Prazo |   | (o) Médio Prazo |      |
|  |  Concluir a     |   |  Portal         |      |
|  |  Fundação       |   |  +Compras e     |      |
|  |                 |   |  Intel. Operac. |      |
|  +-----------------+   +-----------------+      |
+------------------------------------------------+
```

---

## Slide 11 — Conclusão

### Objetivo
Encerrar reforçando o valor estratégico do projeto e gerando confiança na continuidade do investimento.

### Mensagem Principal
O BlueprintOS já entrega valor hoje — e é a base de tudo o que vem depois.

### Layout
- Full-bleed, ecoando a Capa (fecha o arco visual da apresentação): fotografia editorial P&B (`movement-bw-diptych.jpeg`) com overlay `--brand-ink`.
- Marca d'água "RUMO A 2154" (`rumo-a-2154-watermark.png`), grande, discreta atrás do texto.
- Frase de prova concreta em destaque máximo (Inter Tight 700, branco); frase de visão de futuro abaixo, peso menor.

### Fluxo Visual
1. Frase de prova concreta (destaque máximo)
2. Frase de visão de futuro (apoio)

### Componentes do Design System
- Brand photography (full-bleed, P&B)
- Brand wordmark (marca d'água "RUMO A 2154")
- Display type

### Ícones
Não utilizar — fechamento tipográfico, mesmo espírito de sobriedade da Capa.

### Fotografia
**Utilizar** — `assets/brand-photography/movement-bw-diptych.jpeg` (editorial/abstrato, P&B), com overlay `--brand-ink` para contraste do texto branco.

### Palavras em Destaque
"já entrega valor real hoje", "sem recomeçar do zero"

### Observações de Design
Este slide ecoa visualmente o Slide 1 (mesma composição full-bleed + overlay + marca d'água), fechando o arco da apresentação. A frase de prova concreta vem primeiro — não a de visão — para terminar em fato, não em promessa (decisão da revisão executiva).

### Wireframe

```
+------------------------------------------------+
|[foto editorial P&B full-bleed, overlay escuro]  |
|                        [marca d'água RUMO A 2154]
|                                                  |
|     O BlueprintOS já entrega valor real hoje    |
|         através do apoio ao comprador sênior    |
|                                                  |
|            -------------------------            |
|                                                  |
|      É a base para o +Compras e os próximos     |
|         produtos de IA da SOMA crescerem        |
+------------------------------------------------+
```

---

# Guia de Diagramação

## Princípios de Composição

- Um assunto por slide — nenhum slide deste storyboard tem mais de 3 blocos de conteúdo simultâneos.
- Espaço em branco como elemento ativo (regra do sistema: "o suporte gráfico é o espaço em branco e o bege/branco") — reservar ao menos 30–40% da área do slide sem conteúdo.
- Hierarquia por tamanho e peso tipográfico, cor reforça mas não substitui hierarquia.
- Repetição estrutural: Slides 2, 5 e 9 (3 Cards) usam exatamente o mesmo grid; Slides 4, 6 e 7 (Timeline) usam o mesmo componente horizontal.
- Fotografia editorial reservada a Capa e Conclusão — os únicos dois slides "sem grid de conteúdo", full-bleed.

## Hierarquia Tipográfica

| Nível | Uso | Fonte / Token |
|---|---|---|
| Hero/Display | Título da Capa e da Conclusão | Inter Tight 700, `--t-hero` (clamp 48–120px), `--tracking-tight`, `--lh-tight` |
| H1 | Título executivo de página | Inter Tight 700 / `--t-h1` 28px |
| Section Title | Cabeçalho de cada slide de conteúdo (Slides 2–10) | DM Mono, uppercase, `--t-section` 10–11px, `--tracking-caps` 0.07em |
| H3 | Rótulo de Card / KPI / marco de Timeline | DM Sans 600, `--t-h3` 18px |
| Body | Texto de apoio, Notice box | DM Sans 400, `--t-body` 14px, `--lh-base` |
| Caption | Rodapé, legendas | DM Sans, `--t-caption` 12px, `--text-secondary` |

## Uso de Cores

- Cores principais: `--brand-ink #000000` e `--brand-paper #FFFFFF` — fundo e texto de base em todos os slides de conteúdo.
- Texto de corpo sempre `--text-primary #1A1916` (quase-preto), nunca `#000000` puro.
- Um destaque de apoio por slide, escolhido entre `--brand-blue #3B80D9`, `--brand-teal #539193`, `--brand-sand #B7A696` ou `--brand-iris #7A8FE4` — nunca mais de uma cor de apoio por slide além dos neutros.
- Status (Slides 7 e 8): dupla cor + bg pastel padrão do sistema (`--novo`/`--novo-bg` para "em andamento"; `--aprovado`/`--aprovado-bg` para "concluído/entregue").
- Sombras sempre derivadas de `rgba(26,25,22,x)` (`--shadow-1`/`--shadow-2` em Cards) — nunca preto puro, nunca glow.

## Uso de Ícones

- Biblioteca: Lucide (via CDN `unpkg.com/lucide@latest` ou inline SVG), stroke 2px, `currentColor`, 16px em Cards/Notice box, 14px em rótulos menores.
- Terminações arredondadas (`stroke-linecap: round`, `stroke-linejoin: round`), sem preenchimento sólido.
- Nunca emoji, nunca Heroicons/Material Icons/Font Awesome.
- Nomes de ícone específicos por slide estão listados em cada seção "Ícones" acima — usar exatamente esses, para consistência entre storyboard e arquivo final.

## Uso de Fotografias

- Reservada a Capa (`editorial-desert-duo.jpeg`) e Conclusão (`movement-bw-diptych.jpeg`) — os dois arquétipos editoriais indicados pelo Design System para abertura e fechamento institucional.
- Sempre full-bleed, sempre com overlay sólido translúcido em `--brand-ink` para contraste de texto branco — nunca gradiente colorido, nunca duotone, nunca filtro.
- Nenhum outro slide usa fotografia — os slides de conteúdo (2 a 10) são resolvidos por tipografia, Cards, Notice box e Timeline, conforme a "vibe" documentada do sistema (sóbrio, sem exagero visual).

## Espaçamentos

- Escala base de 4px (`--space-1` a `--space-10`). Padding interno de Card: `--space-4`–`--space-5` (16–20px).
- Margem externa de slide uniforme (mesma distância entre conteúdo e borda em todos os 11 slides).
- Espaçamento entre Cards igual em todos os slides que usam esse componente (Slides 2, 5, 9): `--space-6` (24px).

## Alinhamentos

- Section Title sempre na mesma posição/alinhamento nos Slides 2 a 10 — Capa e Conclusão (1 e 11) são as exceções, centralizadas/full-bleed.
- Cards e marcos de Timeline sempre alinhados entre si na base e no topo, mesma altura dentro de um mesmo slide.

## Grid Recomendado

- Slides de conteúdo (2–10): grid de 12 colunas — 3 Cards = 4 colunas cada; 2 Cards (Slide 10) = 6 colunas cada; Timeline de 4 ou 6 marcos = colunas iguais distribuídas.
- Capa e Conclusão (1, 11): fora do grid de colunas — composição full-bleed com texto ancorado ao terço inferior, replicando o template PPT oficial.
- Margem de segurança (safe area) idêntica em todos os slides.

## Recomendações para PowerPoint

- Usar `docs/design-system/assets/pptx-media/` e o `Template PPT Azzas_04092024.pptx` como base real de slide-master — não recriar capa/índice do zero.
- Criar um slide-master por tipo de layout identificado neste storyboard: (1) Full-bleed + overlay (Slides 1, 11); (2) 3 Cards (Slides 2, 5, 9); (3) Timeline (Slides 4, 6, 7); (4) Notice box + KPI (Slide 8); (5) 2 Cards (Slide 10); (6) Notice box + lista (Slide 3) — 6 mestres para os 11 slides.
- Nomear os placeholders do slide-master pelos mesmos nomes de componente usados neste storyboard (Section Title, Card, Notice box, Timeline, KPI hero card, Badge de status), para rastreabilidade entre este documento e o arquivo `.pptx`.
- Importar as fontes reais (Inter Tight, DM Sans, DM Mono — `fonts.css`) no PowerPoint antes de iniciar a diagramação; não usar Arial (fonte original do template PPTX, já substituída oficialmente pelo Design System).
- Usar os arquivos de `assets/logos/` e `assets/brand-photography/` diretamente — não recriar ou gerar novas variações de logo/fotografia.
