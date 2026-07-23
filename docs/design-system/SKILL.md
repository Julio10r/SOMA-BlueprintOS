---
name: azzas-2154-design
description: Use this skill to generate well-branded interfaces and assets for AZZAS 2154 (the GDT internal portals and the corporate/editorial brand), either for production or throwaway prototypes/mocks/etc. Contains essential design guidelines, colors, type, fonts, assets, and UI kit components for prototyping.
user-invocable: true
---

Read the `README.md` file within this skill, and explore the other available files.

If creating visual artifacts (slides, mocks, throwaway prototypes, etc), copy assets out and create static HTML files for the user to view. If working on production code, you can copy assets and read the rules here to become an expert in designing with this brand.

If the user invokes this skill without any other guidance, ask them what they want to build or design, ask some questions, and act as an expert designer who outputs HTML artifacts _or_ production code, depending on the need.

### Two surfaces, one system

This brand has two coexisting territories you must distinguish:

1. **Corporate / Brand** — editorial, presentations, marketing. Sourced from `Template PPT Azzas_04092024.pptx`. Black + white + warm tan/blue accents, full-bleed editorial photography, big display type. Use assets in `assets/logos/` and `assets/brand-photography/`.
2. **GDT Portal** — internal technology demand-management apps (`portal-curadoria`, `portal-avaliacao`, `portal-execucao`, …). Warm bege background (`#F7F6F3`), DM Sans + DM Mono, restrained status palette, dashboard-density layouts. Reference: `uploads/GDT-BASE-TEMPLATE.html`.

Both surfaces import from `colors_and_type.css` and share the same `--text-primary` and tone-of-voice.

### Hard rules

- All copy in **Portuguese (PT-BR)**. No emoji. No gradient backgrounds. No SVG illustrations of products — use the real editorial photography.
- Text color is `#1A1916` (quase-preto), NOT `#000000`. Shadows are derived from that color (`rgba(26,25,22,x)`).
- Icons are Lucide-style SVG line-art, stroke 2px, currentColor.
- The "2154" mark and "RUMO A 2154" are core brand expressions — feel free to use them as large watermarks on covers.
