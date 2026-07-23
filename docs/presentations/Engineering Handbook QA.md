# Engineering Handbook — QA

Relatório de verificação da implementação da apresentação PowerPoint
"Engineering Handbook" (AZZAS 2154 / SOMA BlueprintOS). Conteúdo e design já
decididos em `Engineering Handbook.md` e `Engineering Handbook Storyboard.md`;
nada foi inventado nesta etapa. Gerada por reuso direto dos Masters oficiais em
`docs/design-system/templates/powerpoint/` (Sprint B2.1) — mesmo padrão visual
do `Executive Report.pptx`.

## Checklist

| Item | Status | Observação |
|---|---|---|
| 11 slides no Engineering Handbook | PASSOU | Ordem e conteúdo conforme `Engineering Handbook Storyboard.md`: Capa, Arquitetura, Regras de Arquitetura, Stack, Organização do Projeto, Convenções, Estrutura das Pastas, Testes e Qualidade, Ambiente e Deploy, Git Flow, Conclusão. |
| Reuso dos Masters oficiais | PASSOU | Cada slide foi gerado clonando o slide real de `Master-Cover.pptx`, `Master-Section.pptx`, `Master-Content.pptx`, `Master-Timeline.pptx`, `Master-Architecture.pptx` ou `Master-Closing.pptx` — nenhum shape/master foi recriado do zero. |
| Variante de 2 Cards (Slide 6) | PASSOU | Reaproveita `Master-Section.pptx`, removendo o 3º card e redistribuindo a largura entre os 2 cards restantes (Código / Governança). |
| Notice box — variantes de cor | PASSOU | Slide 3 usa `warn` (`--warn` #E09B3D / `--warn-bg` #FDF3E3, herdado do master); Slide 10 usa `ok` (`--aprovado` #2D6A4F / `--aprovado-bg` #D8F3DC), recolorido programaticamente a partir dos mesmos tokens do Design System. |
| Assets corretos (fotografias e watermarks) | PASSOU | Capa e Conclusão usam exatamente as mesmas 2 fotografias e 2 marcas d'água do Executive Report, herdadas diretamente do `Master-Cover.pptx`/`Master-Closing.pptx`. |
| Tipografia | RESSALVA | Nomes de fonte gravados como "Inter Tight", "DM Sans", "DM Mono" (herdados dos masters). Essas fontes não estão instaladas no ambiente de build/conversão; o PDF usa fallback do sistema. Mesma ressalva já registrada em `Executive Report QA.md`. |
| Grid e espaçamento | PASSOU | Slides de 3 Cards e Timeline usam exatamente a geometria original dos masters, sem alteração de proporção. |
| Sem componentes inventados | PASSOU | Apenas os 6 tipos de slide já existentes no Design System (Cover, Section 3/2-Cards, Content, Timeline, Architecture, Closing) foram usados. |
| Sem assets externos | PASSOU | Nenhuma chamada de rede feita; apenas imagens já embutidas nos arquivos `Master-*.pptx`. |
| Design System não alterado | PASSOU | Nenhum arquivo em `docs/design-system/` foi modificado nesta sprint; os masters foram apenas lidos e clonados. |

## Masters utilizados

| Master | Slides |
|---|---|
| `Master-Cover.pptx` | 1 |
| `Master-Section.pptx` | 2, 4, 6, 9 |
| `Master-Content.pptx` | 3, 10 |
| `Master-Timeline.pptx` | 5, 8 |
| `Master-Architecture.pptx` | 7 |
| `Master-Closing.pptx` | 11 |

## Observações

1. **Geração**: mesmo script Python (`python-pptx`) usado no Product Blueprint, que clona o slide de cada Master real e substitui apenas o texto dos placeholders pelo conteúdo aprovado em `Engineering Handbook.md`.
2. **Exportação PDF**: `soffice --headless --convert-to pdf "Engineering Handbook.pptx"` (LibreOffice já instalado no ambiente). PDF gerado com sucesso, refletindo os 11 slides.
3. **Verificação visual**: Capa e Notice box `ok` (Slide 10) renderizados como imagem a partir do PDF e conferidos visualmente antes da entrega.
4. **Ambiente de build**: mesmo ambiente Python do Product Blueprint; script de geração mantido fora do repositório (scratchpad da sessão), não versionado.
