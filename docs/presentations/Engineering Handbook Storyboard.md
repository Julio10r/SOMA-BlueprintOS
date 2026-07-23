# Engineering Handbook — Storyboard

> Storyboard completo dos 11 slides do Engineering Handbook, a partir do conteúdo aprovado em `Engineering Handbook.md` (`docs/presentations/`).
> Segue exatamente o mesmo padrão visual do `Executive Report Storyboard.md` — mesmos tokens, mesmos componentes, mesma sequência de masters do Design System (`docs/design-system/templates/powerpoint/`).
> Território: **Corporate / Brand** (`docs/design-system/SKILL.md`).

---

## Design System aplicado

Idêntico ao Executive Report — nenhuma cor, fonte, ícone, fotografia ou componente fora do inventário já usado:

- **Cores**: `--brand-ink` #000000, `--brand-paper` #FFFFFF, texto `--text-primary` #1A1916. Apoio: `--brand-blue` #3B80D9. Status: `--aprovado`/`--aprovado-bg`.
- **Tipografia**: Display — Inter Tight 700 (Capa/Conclusão). Corpo — DM Sans 300–600. Section Title/rótulos — DM Mono uppercase.
- **Ícones**: Lucide, stroke 2px, `currentColor`.
- **Fotografia**: `assets/brand-photography/editorial-desert-duo.jpeg` (Capa), `assets/brand-photography/movement-bw-diptych.jpeg` (Conclusão) — os mesmos dois arquétipos do Executive Report.
- **Logotipo**: `assets/logos/azzas-2154-mark-watermark.png` (Capa), `assets/logos/rumo-a-2154-watermark.png` (Conclusão).
- **Masters PowerPoint reutilizados**: `Master-Cover.pptx`, `Master-Section.pptx` (3 Cards, e variante 2 Cards), `Master-Content.pptx` (Notice + lista), `Master-Timeline.pptx` (3 marcos), `Master-Architecture.pptx` (4 etapas), `Master-Closing.pptx`.

---

## Slide 1 — Capa

**Master**: `Master-Cover.pptx`

### Layout
Full-bleed com fotografia editorial + overlay `--brand-ink`; marca d'água discreta; título/subtítulo/linha de apoio em branco, terço inferior.

### Componentes
Brand photography (full-bleed), Brand wordmark (marca d'água), Display type.

### Wireframe
```
+------------------------------------------------+
|[foto editorial full-bleed, overlay escuro]      |
|                                     [marca d'água]
|  Engineering Handbook                           |
|  SOMA BlueprintOS                               |
|  Guia de arquitetura e desenvolvimento para     |
|  engenheiros                                    |
+------------------------------------------------+
```

---

## Slide 2 — Arquitetura

**Master**: `Master-Section.pptx` (3 Cards)

### Layout
Section Title "ARQUITETURA" + 3 Cards: "Modular Monolith", "Clean Architecture", "DDD pragmático".

### Ícones
`box` (Modular Monolith), `layers` (Clean Architecture), `sparkles` (DDD pragmático).

### Wireframe
```
+------------------------------------------------+
|  ARQUITETURA                                    |
|  +-------------+ +-------------+ +-------------+|
|  |(o) Modular  | |(o) Clean    | |(o) DDD      ||
|  |  Monolith   | | Architecture| | pragmático  ||
|  +-------------+ +-------------+ +-------------+|
+------------------------------------------------+
```

---

## Slide 3 — Regras de Arquitetura

**Master**: `Master-Content.pptx` (Notice + lista), variante `warn`

### Layout
Section Title "REGRAS DE ARQUITETURA" + Notice box `warn` (regra de Contracts) + lista de 2 regras adicionais.

### Ícones
`alert-triangle` no Notice box.

### Wireframe
```
+------------------------------------------------+
|  REGRAS DE ARQUITETURA                          |
|  [!] Módulos se comunicam apenas via Contracts  |
|  - Domain não referencia nenhuma outra camada    |
|  - Nenhuma regra de negócio em Api/Infrastructure|
+------------------------------------------------+
```

---

## Slide 4 — Stack

**Master**: `Master-Section.pptx` (3 Cards)

### Layout
Section Title "STACK" + 3 Cards: "Backend", "Dados e Autenticação", "Infraestrutura".

### Ícones
`terminal` (Backend), `database` (Dados e Autenticação), `server` (Infraestrutura).

### Wireframe
```
+------------------------------------------------+
|  STACK                                          |
|  +-------------+ +-------------+ +-------------+|
|  |(o) Backend  | |(o) Dados e  | |(o) Infra-   ||
|  |             | | Autenticação| | estrutura   ||
|  +-------------+ +-------------+ +-------------+|
+------------------------------------------------+
```

---

## Slide 5 — Organização do Projeto

**Master**: `Master-Timeline.pptx` (3 marcos)

### Layout
Section Title "ORGANIZAÇÃO DO PROJETO" + Timeline horizontal: Domain → Application → Infrastructure/Api.

### Ícones
`box` (Domain), `layers` (Application), `server` (Infrastructure/Api).

### Wireframe
```
+------------------------------------------------+
|  ORGANIZAÇÃO DO PROJETO                         |
|  [Domain] --> [Application] --> [Infra/Api]     |
|  - Core concentra contratos e modelos dos módulos|
+------------------------------------------------+
```

---

## Slide 6 — Convenções

**Master**: `Master-Section.pptx` (2 Cards, largura dobrada)

### Layout
Section Title "CONVENÇÕES" + 2 Cards largos: "Código", "Governança".

### Ícones
`code` (Código), `shield-check` (Governança).

### Wireframe
```
+------------------------------------------------+
|  CONVENÇÕES                                     |
|  +-----------------+   +-----------------+      |
|  |(o) Código       |   |(o) Governança   |      |
|  +-----------------+   +-----------------+      |
+------------------------------------------------+
```

---

## Slide 7 — Estrutura das Pastas

**Master**: `Master-Architecture.pptx` (4 etapas)

### Layout
Section Title "ESTRUTURA DAS PASTAS" + 4 etapas: backend/ → docs/ → .ai/ → infrastructure/.

### Ícones
`folder` para as 4 etapas.

### Wireframe
```
+------------------------------------------------+
|  ESTRUTURA DAS PASTAS                           |
|  [backend/]->[docs/]->[.ai/]->[infrastructure/] |
|  Texto de apoio: dist/ é gerado, não versionado  |
+------------------------------------------------+
```

---

## Slide 8 — Testes e Qualidade

**Master**: `Master-Timeline.pptx` (3 marcos)

### Layout
Section Title "TESTES E QUALIDADE" + Timeline: Application → Domain → Integration (prioridade de cobertura); texto de apoio com o número atual de testes.

### Ícones
`check-circle` nos 3 marcos.

### Wireframe
```
+------------------------------------------------+
|  TESTES E QUALIDADE                             |
|  [Application] --> [Domain] --> [Integration]   |
|  - 167 testes unitários + 1 integração, 100% OK  |
+------------------------------------------------+
```

---

## Slide 9 — Ambiente e Deploy

**Master**: `Master-Section.pptx` (3 Cards)

### Layout
Section Title "AMBIENTE E DEPLOY" + 3 Cards: "Ambiente local", "Deploy atual", "Reservado para escala".

### Ícones
`terminal` (Ambiente local), `upload-cloud` (Deploy atual), `server` (Reservado para escala).

### Wireframe
```
+------------------------------------------------+
|  AMBIENTE E DEPLOY                              |
|  +-------------+ +-------------+ +-------------+|
|  |(o) Ambiente | |(o) Deploy   | |(o) Reservado||
|  |  local      | | atual       | | para escala ||
|  +-------------+ +-------------+ +-------------+|
+------------------------------------------------+
```

---

## Slide 10 — Git Flow

**Master**: `Master-Content.pptx` (Notice + lista), variante `ok`

### Layout
Section Title "GIT FLOW" + Notice box `ok` ("main nunca recebe commit direto") + lista de 2 pontos (branches, commits/PR).

### Ícones
`git-branch` no Notice box.

### Wireframe
```
+------------------------------------------------+
|  GIT FLOW                                       |
|  [✓] main nunca recebe commit direto            |
|  - feature/, bugfix/, hotfix/, release/          |
|  - Commits tipo: descrição; PR com checklist     |
+------------------------------------------------+
```

---

## Slide 11 — Conclusão

**Master**: `Master-Closing.pptx`

### Layout
Full-bleed, ecoando a Capa: fotografia P&B + overlay + marca d'água "RUMO A 2154"; frase de prova concreta em destaque, linha de visão abaixo.

### Fotografia
`assets/brand-photography/movement-bw-diptych.jpeg`.

### Wireframe
```
+------------------------------------------------+
|[foto editorial P&B full-bleed, overlay escuro]  |
|                        [marca d'água RUMO A 2154]
|  167 testes, 100% passando, build sem warnings  |
|  -------------------------                      |
|  A base para o próximo módulo do BlueprintOS    |
+------------------------------------------------+
```

---

## Guia de Diagramação

Idêntico ao `Executive Report Storyboard.md` — mesma hierarquia tipográfica, mesmo uso de cores, ícones (Lucide), fotografias (reservadas a Capa/Conclusão), espaçamentos (escala de 4px) e grid de 12 colunas. Nenhuma convenção nova foi criada nesta sprint; todas herdadas do storyboard do Executive Report e dos masters reais em `docs/design-system/templates/powerpoint/`.
