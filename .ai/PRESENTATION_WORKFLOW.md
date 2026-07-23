# PRESENTATION_WORKFLOW.md

> Fluxo oficial para criação de qualquer apresentação no SOMA BlueprintOS. Nenhuma apresentação deve começar do zero — todo o fluxo abaixo reutiliza os assets consolidados em `docs/design-system/` (Sprint B2.1).

Antes de iniciar, todo agente deve consultar, nesta ordem:

1. `docs/design-system/SKILL.md`
2. `docs/design-system/INDEX.md`
3. `docs/design-system/templates/powerpoint/`

---

## 1. Conteúdo

- Levantar e redigir o conteúdo bruto da apresentação (mensagens-chave, dados, narrativa).
- Salvar em `docs/presentations/<Nome da Apresentação>.md`.
- Sem preocupação com layout/visual nesta etapa — foco em precisão factual e clareza da mensagem.

## 2. Executive Review

- Revisão crítica do conteúdo: corta redundância, ajusta tom executivo, valida números e claims.
- Salvar em `docs/presentations/<Nome da Apresentação> Executive Review.md`.
- Qualquer correção factual encontrada aqui deve refletir de volta no documento de Conteúdo.

## 3. Storyboard

- Para cada slide aprovado: objetivo, mensagem principal, layout, fluxo visual, componentes do Design System a usar, ícones, fotografia, wireframe em texto.
- Deve referenciar exclusivamente tokens/componentes existentes em `docs/design-system/` (cores, tipografia, `preview/*.html`, `assets/`).
- Salvar em `docs/presentations/<Nome da Apresentação> Storyboard.md`.
- Ver `docs/presentations/Executive Report Storyboard.md` como referência de formato.

## 4. Design Mapping

- Mapa de rastreabilidade slide-a-slide: quais componentes, assets, tokens de cor e tipografia de `docs/design-system/` foram efetivamente usados, com caminho de arquivo e justificativa.
- Nenhum item genérico ou inventado — sempre apontar para o arquivo real.
- Salvar em `docs/presentations/<Nome da Apresentação> Design Mapping.md`.
- Ver `docs/presentations/Executive Report Design Mapping.md` como referência de formato.

## 5. Geração PowerPoint

- Partir sempre de um dos masters em `docs/design-system/templates/powerpoint/` (`Master-Cover.pptx`, `Master-Section.pptx`, `Master-Content.pptx`, `Master-Timeline.pptx`, `Master-Architecture.pptx`, `Master-Closing.pptx`) ou do `AZZAS-2154-Template.pptx` completo.
- Copiar o(s) master(s) necessário(s), duplicar slides conforme a quantidade definida no Storyboard, e preencher os placeholders com o conteúdo aprovado.
- Nunca recriar slide masters, fontes, ícones ou paleta do zero — apenas popular os masters existentes.
- Resultado: `docs/presentations/<Nome da Apresentação>.pptx`.
- Seguir padrões de resolução, grid, margens e espaçamento em `docs/design-system/presentations/README.md`.

## 6. Exportação PDF

- Exportar o `.pptx` final para PDF em alta qualidade.
- Resultado: `docs/presentations/<Nome da Apresentação>.pdf`, nome espelhado ao `.pptx`.

## 7. QA

- Checklist de revisão: ortografia, consistência de tokens de cor/tipografia, alinhamento com o Storyboard e o Design Mapping, legibilidade de texto sobre fotografia, presença de watermark/logo corretos, paginação.
- Salvar em `docs/presentations/<Nome da Apresentação> QA.md`.
- Ver `docs/presentations/Executive Report QA.md` como referência de formato.

## 8. Git

- Etapa de versionamento controlada pelo usuário/Maestro — nunca automática dentro do fluxo de geração de conteúdo.
- `git add` dos arquivos `.md`, `.pptx` e `.pdf` da apresentação.
- Commit descrevendo a apresentação entregue (ex.: `docs: gera <Nome da Apresentação> em PowerPoint`).

## 9. Publicação

- Compartilhamento do PDF/PPTX final com os stakeholders pelo canal apropriado (Teams, e-mail, repositório).
- Nunca publicar antes do QA (etapa 7) estar concluído e aprovado.

---

## Regra geral

Sempre reutilizar Template Master, Masters individuais, ícones (Lucide) e fontes (`docs/design-system/fonts/`) documentados no Design System. Nenhuma apresentação futura deve reconstruir esses elementos — apenas consumi-los. Em caso de necessidade de um master novo (tipo de slide inexistente), a criação do master deve passar por uma sprint de Design System, nunca ser embutida ad-hoc dentro da geração de uma apresentação específica.
