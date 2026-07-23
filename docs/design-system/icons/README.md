# Ícones — Lucide

O BlueprintOS usa exclusivamente a família **Lucide** (fork open-source do Feather Icons) para todo ícone de UI, apresentação ou documentação visual. Ver regra em `docs/design-system/SKILL.md` e `docs/design-system/README.md` (seção de iconografia).

## Versão

- Família: **Lucide** (`lucide-static` / `lucide` npm package / CDN `unpkg.com/lucide`)
- Versão de referência do Design System: **latest** via CDN (`https://unpkg.com/lucide@latest`), sem pin de versão — os ícones usados (`network`, `target`, `trending-up`, setas, etc.) são estáveis e não sofrem breaking changes entre versões menores.
- Estilo: line-art, `stroke-width: 2`, `stroke-linecap: round`, `stroke-linejoin: round`, cor via `currentColor`, grade de 24×24 (renderizado em 14–16px nas interfaces).

## Origem

- Site oficial: https://lucide.dev
- Repositório: https://github.com/lucide-icons/lucide
- Licença: **ISC** (permissiva, permite uso comercial, redistribuição e modificação sem restrições).

## Processo de instalação

**Uso padrão (HTML/apresentações/artifacts) — via CDN:**

```html
<script src="https://unpkg.com/lucide@latest"></script>
<script>lucide.createIcons();</script>
```

**Uso em produção (React/TSX) — via npm:**

```bash
npm install lucide-react
```

```tsx
import { Network, Target, TrendingUp } from "lucide-react";
```

**SVG inline direto:** para PowerPoint e casos sem runtime JS, copie o `<svg>` diretamente de https://lucide.dev/icons/ (busque pelo nome do ícone, copie o SVG) e cole inline no arquivo, ajustando `stroke="currentColor"` para herdar a cor do texto.

> Este repositório **não versiona os arquivos SVG** dos ícones localmente (pasta `lucide/` mantida vazia/placeholder) — a família inteira tem centenas de ícones e o CDN/npm já garante consistência de versão. Se um ícone específico precisar ser versionado (ex.: para um asset PowerPoint que não pode depender de rede), salve o SVG individual em `lucide/<nome-do-icone>.svg` e documente aqui a lista de exceções.

## Convenções de uso

- Sempre `stroke-width="2"`, nunca preenchimento sólido (`fill="none"`).
- Cor sempre `currentColor` — herda `--text-primary` (`#1A1916`) ou a cor de contexto (ex.: branco sobre overlay escuro).
- Tamanho: 14–16px em UI/portal, 16–20px em apresentações/slides.
- Nunca misturar com emoji ou ícones de outra família (Font Awesome, Material Icons, etc.).
- Nomes de ícone usados devem ser os nomes oficiais do catálogo Lucide (kebab-case, ex.: `trending-up`, não inventar variações).
