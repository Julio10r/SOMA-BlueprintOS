# Apresentações — Padrões PowerPoint

Convenções oficiais para qualquer apresentação `.pptx` gerada a partir do Design System. Templates reais em `docs/design-system/templates/powerpoint/`.

## Resolução padrão

- **Widescreen 16:9**, dimensão nativa do Template Master: `12191695 EMU × 6858000 EMU` (≈ 13.33in × 7.5in / 33.87cm × 19.05cm).
- Nunca usar o formato 4:3 legado (10in × 7.5in).

## Grids e margens

- Margem externa: **0.6in** (≈ 5.5% da largura) em todos os lados, consistente com os placeholders do Template Master.
- Grid de conteúdo: 12 colunas para slides de cards/comparação (`Master-Section.pptx`), coluna única para slides de texto corrido (`Master-Content.pptx`).
- Espaçamento entre cards/blocos: mínimo **0.3in**, nunca encostar elementos na borda do slide.

## Espaçamentos

- Título → subtítulo: ~0.2in
- Section Title (DM Mono uppercase) → corpo: ~0.3in
- Entre marcos de timeline: espaçamento igual, nunca comprimido para caber texto extra — reduza o texto antes de reduzir o espaçamento.

## Exportação PDF

- Exportar sempre em **alta qualidade** (PowerPoint: Arquivo → Exportar → PDF → "Qualidade: Padrão" no mínimo, preferir "Ideal para impressão" quando houver fotografia full-bleed).
- Nome do PDF deve espelhar exatamente o nome do `.pptx` (ver Convenções de nomenclatura), trocando apenas a extensão.
- PDF é o artefato de entrega/compartilhamento; o `.pptx` é o fonte editável — ambos versionados juntos em `docs/presentations/`.

## Convenções de nomenclatura

- Apresentações finais vivem em `docs/presentations/<Nome da Apresentação>.pptx` (+ `.pdf` irmão).
- Documentos de processo de cada apresentação seguem o padrão `<Nome da Apresentação> <Etapa>.md`, ex.: `Executive Report Storyboard.md`, `Executive Report Design Mapping.md`, `Executive Report QA.md` — ver `.ai/PRESENTATION_WORKFLOW.md` para a lista completa de etapas.
- Templates e masters reutilizáveis (não apresentações finais) vivem em `docs/design-system/templates/powerpoint/`, com nomenclatura fixa:
  - `AZZAS-2154-Template.pptx` — template completo com os 6 masters em sequência.
  - `Master-Cover.pptx`, `Master-Section.pptx`, `Master-Content.pptx`, `Master-Timeline.pptx`, `Master-Architecture.pptx`, `Master-Closing.pptx` — um master por tipo de slide, para composição individual.

## Convenções gerais

- Território **Corporate / Brand** apenas (ver `docs/design-system/SKILL.md`) — nunca aplicar o tema do Portal GDT em apresentações.
- Fotografia editorial full-bleed reservada para Capa e Encerramento (masters `Master-Cover.pptx` / `Master-Closing.pptx`).
- Nenhuma apresentação nova deve reconstruir masters, ícones ou templates — sempre copiar e adaptar os arquivos existentes em `templates/powerpoint/`. Ver fluxo completo em `.ai/PRESENTATION_WORKFLOW.md`.
