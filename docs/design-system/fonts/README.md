# Fontes — Inter Tight, DM Sans, DM Mono

O Design System oficial da AZZAS 2154 usa três famílias tipográficas, todas hospedadas no Google Fonts e carregadas via `docs/design-system/fonts.css`. Ver tokens de aplicação em `docs/design-system/colors_and_type.css` e exemplos em `docs/design-system/preview/type-display.html`, `type-mono.html`, `type-ui.html`.

## Inter Tight — Display

- **Uso**: títulos grandes, capas, encerramentos (`--font-display`). Peso 700 (bold), tracking apertado (`-0.02em`), line-height 1.05.
- **Origem**: https://fonts.google.com/specimen/Inter+Tight
- **Licença**: SIL Open Font License 1.1 (livre, uso comercial, embed e redistribuição permitidos).

## DM Sans — Corpo / UI

- **Uso**: corpo de texto, labels de UI, botões (`--font`). Pesos 300–600.
- **Origem**: https://fonts.google.com/specimen/DM+Sans
- **Licença**: SIL Open Font License 1.1.

## DM Mono — Numérico / Meta

- **Uso**: Section Titles, IDs, dados numéricos, uppercase com letter-spacing (`--mono`). Pesos 300–500.
- **Origem**: https://fonts.google.com/specimen/DM+Mono
- **Licença**: SIL Open Font License 1.1.

## Instalação

Carregamento padrão via `@import` do Google Fonts, já configurado em `docs/design-system/fonts.css`:

```css
@import url("https://fonts.googleapis.com/css2?family=DM+Sans:ital,wght@0,300;0,400;0,500;0,600;0,700;1,300;1,400&family=DM+Mono:ital,wght@0,300;0,400;0,500;1,300&family=Inter+Tight:wght@400;500;600;700;800&display=swap");
```

Basta importar `fonts.css` em qualquer HTML/artifact do sistema:

```html
<link rel="stylesheet" href="/docs/design-system/fonts.css">
```

Para **PowerPoint**, as três famílias devem estar instaladas localmente na máquina que gera/edita o `.pptx` (fontes do sistema, não CSS) — baixe os arquivos `.ttf`/`.otf` diretamente do Google Fonts nos links acima e instale via Font Book (macOS) ou Configurações de Fontes (Windows). Não versionamos os arquivos de fonte neste repositório: o Google Fonts já é a fonte de verdade e a licença OFL permite redistribuição, mas manter os binários fora do repo evita duplicação e problemas de atualização de versão.

## Fallback

Cada família tem uma pilha de fallback de sistema, definida em `colors_and_type.css`:

- `--font-display`: `"Inter Tight", "Helvetica Neue", Arial, sans-serif`
- `--font`: `"DM Sans", "Helvetica Neue", Arial, sans-serif`
- `--mono`: `"DM Mono", "SF Mono", Consolas, monospace`

Em PowerPoint, se a fonte não estiver instalada na máquina de destino, o PowerPoint substitui automaticamente pela fonte fallback do tema — por isso o Template Master (`templates/powerpoint/`) já embute os nomes de fonte corretos nos placeholders, e recomenda-se sempre abrir/editar com as três famílias instaladas.

## Plataformas suportadas

- **Web / HTML / Artifacts**: via `@import` do Google Fonts — funciona em qualquer navegador moderno, sem instalação.
- **macOS / Windows (PowerPoint, edição local)**: requer instalação manual dos arquivos de fonte baixados do Google Fonts.
- **Geração automatizada de PPTX** (scripts/python-pptx): a fonte é apenas referenciada por nome no XML do `.pptx` — não requer que a máquina que *gera* o arquivo tenha a fonte instalada, apenas a máquina que *abre/edita* visualmente.
