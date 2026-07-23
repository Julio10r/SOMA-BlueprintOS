# Product Blueprint — Storyboard

> Storyboard completo dos 11 slides do Product Blueprint, a partir do conteúdo aprovado em `Product Blueprint.md` (`docs/presentations/`).
> Segue exatamente o mesmo padrão visual do `Executive Report Storyboard.md` — mesmos tokens, mesmos componentes, mesma sequência de masters do Design System (`docs/design-system/templates/powerpoint/`).
> Território: **Corporate / Brand** (`docs/design-system/SKILL.md`).

---

## Design System aplicado

Idêntico ao Executive Report — nenhuma cor, fonte, ícone, fotografia ou componente fora do inventário já usado:

- **Cores**: `--brand-ink` #000000, `--brand-paper` #FFFFFF, texto `--text-primary` #1A1916. Apoio: `--brand-blue` #3B80D9. Status: `--warn`/`--warn-bg`, `--aprovado`/`--aprovado-bg`.
- **Tipografia**: Display — Inter Tight 700 (Capa/Conclusão). Corpo — DM Sans 300–600. Section Title/rótulos — DM Mono uppercase.
- **Ícones**: Lucide, stroke 2px, `currentColor`.
- **Fotografia**: `assets/brand-photography/editorial-desert-duo.jpeg` (Capa), `assets/brand-photography/movement-bw-diptych.jpeg` (Conclusão) — os mesmos dois arquétipos do Executive Report, sem novas fotografias.
- **Logotipo**: `assets/logos/azzas-2154-mark-watermark.png` (Capa), `assets/logos/rumo-a-2154-watermark.png` (Conclusão).
- **Masters PowerPoint reutilizados** (`docs/design-system/templates/powerpoint/`): `Master-Cover.pptx`, `Master-Section.pptx` (3 Cards, e variante 2 Cards), `Master-Content.pptx` (Notice + lista), `Master-Timeline.pptx` (3 marcos), `Master-Architecture.pptx` (4 etapas), `Master-Closing.pptx`.

---

## Slide 1 — Capa

**Master**: `Master-Cover.pptx`

### Layout
Full-bleed com fotografia editorial + overlay `--brand-ink`; marca d'água discreta no canto superior; título/subtítulo/linha de apoio em branco, terço inferior — idêntico ao Slide 1 do Executive Report.

### Componentes
Brand photography (full-bleed), Brand wordmark (marca d'água), Display type (Inter Tight 700).

### Ícones
Não utilizar.

### Fotografia
`assets/brand-photography/editorial-desert-duo.jpeg`.

### Wireframe
```
+------------------------------------------------+
|[foto editorial full-bleed, overlay escuro]      |
|                                     [marca d'água]
|  +Compras                                       |
|  Product Blueprint                              |
|  O produto de IA para Compras, construído       |
|  sobre o BlueprintOS                            |
+------------------------------------------------+
```

---

## Slide 2 — Visão Geral

**Master**: `Master-Section.pptx` (3 Cards)

### Layout
Section Title "VISÃO GERAL" + 3 Cards de mesmo peso: "O que é", "Problema que resolve", "Como funciona".

### Componentes
Section Title (DM Mono), Cards — anatomy (3).

### Ícones
Lucide 16px: `network` (O que é), `alert-circle` (Problema que resolve), `cpu` (Como funciona).

### Wireframe
```
+------------------------------------------------+
|  VISÃO GERAL                                    |
|  +-------------+ +-------------+ +-------------+|
|  |(o) O que é  | |(o) Problema | |(o) Como     ||
|  |             | | que resolve | | funciona    ||
|  +-------------+ +-------------+ +-------------+|
+------------------------------------------------+
```

---

## Slide 3 — Problema Resolvido

**Master**: `Master-Content.pptx` (Notice + lista), variante `warn`

### Layout
Section Title "PROBLEMA RESOLVIDO" + Notice box `warn` com a síntese do problema + lista de 3 pontos de apoio.

### Componentes
Section Title, Notice box — variante `warn` (`--warn`/`--warn-bg`).

### Ícones
`alert-triangle` no Notice box.

### Wireframe
```
+------------------------------------------------+
|  PROBLEMA RESOLVIDO                             |
|  [!] "Decisões de compra dependem fortemente    |
|       da experiência individual do comprador."  |
|  - Grande volume de negociações, histórico disperso
|  - Decisões dependem da experiência individual  |
|  - Conhecimento se perde entre negociações       |
+------------------------------------------------+
```

---

## Slide 4 — Objetivos

**Master**: `Master-Section.pptx` (3 Cards)

### Layout
Section Title "OBJETIVOS" + 3 Cards: "Apoiar decisões", "Reter conhecimento", "Reduzir tempo".

### Componentes
Section Title, Cards — anatomy (3), mesmo grid do Slide 2.

### Ícones
`target` (Apoiar decisões), `archive` (Reter conhecimento), `clock` (Reduzir tempo).

### Wireframe
```
+------------------------------------------------+
|  OBJETIVOS                                      |
|  +-------------+ +-------------+ +-------------+|
|  |(o) Apoiar   | |(o) Reter    | |(o) Reduzir  ||
|  |  decisões   | | conhecimento| |   tempo     ||
|  +-------------+ +-------------+ +-------------+|
+------------------------------------------------+
```

---

## Slide 5 — Arquitetura Simplificada

**Master**: `Master-Timeline.pptx` (3 marcos)

### Layout
Section Title "ARQUITETURA SIMPLIFICADA" + Timeline horizontal: Comprador → Agente de IA → Recomendação; texto de apoio abaixo citando Memória de Negociação e Conhecimento Organizacional.

### Componentes
Section Title, Timeline / audit trail (3 marcos).

### Ícones
`user` (Comprador), `cpu` (Agente de IA), `check-circle` (Recomendação).

### Wireframe
```
+------------------------------------------------+
|  ARQUITETURA SIMPLIFICADA                       |
|  [Comprador] --> [Agente de IA] --> [Recomendação]
|  - Alimentada por Memória de Negociação e        |
|    Conhecimento Organizacional                   |
+------------------------------------------------+
```

---

## Slide 6 — Funcionalidades

**Master**: `Master-Section.pptx` (2 Cards, largura dobrada — mesma variante do Slide 10 do Executive Report)

### Layout
Section Title "FUNCIONALIDADES" + 2 Cards largos: "Implementadas hoje", "Planejadas".

### Componentes
Section Title, Cards — anatomy (2, largura dobrada).

### Ícones
`check-circle` (Implementadas hoje), `compass` (Planejadas).

### Wireframe
```
+------------------------------------------------+
|  FUNCIONALIDADES                                |
|  +-----------------+   +-----------------+      |
|  |(o) Implementadas|   |(o) Planejadas   |      |
|  |  hoje           |   |                 |      |
|  +-----------------+   +-----------------+      |
+------------------------------------------------+
```

---

## Slide 7 — Jornada do Usuário

**Master**: `Master-Architecture.pptx` (4 etapas)

### Layout
Section Title "JORNADA DO USUÁRIO" + 4 etapas conectadas: Abrir negociação → Consultar histórico → Receber recomendação → Registrar resultado.

### Componentes
Section Title, componente de 4 etapas conectadas (mesma anatomia da Arquitetura de Alto Nível do Executive Report).

### Ícones
`play` (Abrir negociação), `search` (Consultar histórico), `check-circle` (Receber recomendação), `save` (Registrar resultado).

### Wireframe
```
+------------------------------------------------+
|  JORNADA DO USUÁRIO                             |
|  [Abrir]->[Consultar]->[Receber]->[Registrar]   |
|  Hoje: consulta e recomendação já existem;       |
|  abertura e registro dependem do portal (roadmap)|
+------------------------------------------------+
```

---

## Slide 8 — Roadmap

**Master**: `Master-Timeline.pptx` (3 marcos)

### Layout
Section Title "ROADMAP" + Timeline horizontal: Fundação → Conhecimento e Automação → Escala.

### Componentes
Section Title, Timeline / audit trail (3 marcos), mesmo componente do Slide 5.

### Ícones
`flag` (Fundação), `cpu` (Conhecimento e Automação), `bar-chart-2` (Escala).

### Wireframe
```
+------------------------------------------------+
|  ROADMAP                                        |
|  [Fundação] --> [Conhecimento e Automação] --> [Escala]
+------------------------------------------------+
```

---

## Slide 9 — Benefícios

**Master**: `Master-Section.pptx` (3 Cards)

### Layout
Section Title "BENEFÍCIOS" + 3 Cards: "Decisões apoiadas por dados", "Conhecimento preservado", "Plataforma sem retrabalho".

### Componentes
Section Title, Cards — anatomy (3), mesmo grid dos Slides 2 e 4.

### Ícones
`trending-up`, `archive`, `layers`.

### Wireframe
```
+------------------------------------------------+
|  BENEFÍCIOS                                     |
|  +-------------+ +-------------+ +-------------+|
|  |(o) Decisões | |(o) Conhecim.| |(o) Plataforma||
|  | apoiadas    | | preservado  | | sem retrab. ||
|  +-------------+ +-------------+ +-------------+|
+------------------------------------------------+
```

---

## Slide 10 — Perguntas Frequentes

**Master**: `Master-Content.pptx` (Notice + lista), variante `ok`

### Layout
Section Title "PERGUNTAS FREQUENTES" + Notice box `ok` com a mensagem-síntese ("apoia, não substitui") + lista de 2 pontos de apoio (estágio atual, segurança de dados).

### Componentes
Section Title, Notice box — variante `ok`.

### Ícones
`check-circle` no Notice box.

### Wireframe
```
+------------------------------------------------+
|  PERGUNTAS FREQUENTES                           |
|  [✓] O +Compras apoia a decisão do comprador —  |
|      não a substitui.                           |
|  - Produto em construção: IA pronta, portal no roadmap
|  - Segurança de dados no módulo de Identidade    |
+------------------------------------------------+
```

---

## Slide 11 — Conclusão

**Master**: `Master-Closing.pptx`

### Layout
Full-bleed, ecoando a Capa: fotografia P&B + overlay + marca d'água "RUMO A 2154"; frase de prova concreta em destaque, linha de visão abaixo.

### Componentes
Brand photography (full-bleed, P&B), Brand wordmark (marca d'água), Display type.

### Fotografia
`assets/brand-photography/movement-bw-diptych.jpeg`.

### Wireframe
```
+------------------------------------------------+
|[foto editorial P&B full-bleed, overlay escuro]  |
|                        [marca d'água RUMO A 2154]
|  O +Compras já apoia o comprador sênior hoje    |
|  -------------------------                      |
|  É o primeiro produto construído sobre o        |
|  BlueprintOS                                    |
+------------------------------------------------+
```

---

## Guia de Diagramação

Idêntico ao `Executive Report Storyboard.md` — mesma hierarquia tipográfica, mesmo uso de cores, ícones (Lucide), fotografias (reservadas a Capa/Conclusão), espaçamentos (escala de 4px) e grid de 12 colunas. Nenhuma convenção nova foi criada nesta sprint; todas herdadas do storyboard do Executive Report e dos masters reais em `docs/design-system/templates/powerpoint/`.
