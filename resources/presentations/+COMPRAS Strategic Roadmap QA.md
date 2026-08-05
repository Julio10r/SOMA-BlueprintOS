# +COMPRAS — Strategic Roadmap — QA (v1.0)

Revisão automatizada via python-pptx + renderização de página (LibreOffice → PDF → PNG) para os 16 slides.

## Checklist

- **(a) Estouro de texto/cards** — verificado programaticamente (nenhuma shape fora dos limites do slide, margem 0.6in respeitada em todas) e visualmente via renderização de cada página; nenhum texto ultrapassa bordas de card/placeholder. As 8 páginas de fase usam 14 linhas de sprint dentro de uma caixa de ~4.25in de altura, com folga confortável.
- **(b) 8 páginas de fase com layout idêntico** — confirmado: mesmo grid (kicker DM Mono → H1 Inter Tight → linha Objetivo → faixa Status/Timeline → lista de 7 sprints), reaproveitando sempre o Master-Content.pptx.
- **(c) Cores e fontes consistentes com os tokens** — Inter Tight (títulos), DM Sans (corpo/UI), DM Mono (labels/kickers/meta), cor de texto `#1A1916` (nunca `#000000` puro em texto — o único preto puro do arquivo é o overlay de fundo herdado dos masters de Capa/Encerramento, que já vinha assim no Design System). Status usa os tokens semânticos existentes: `--execucao` (#4A90D9) para "em andamento" e `--aguardando` (#9B6CC8) para "planejado".
- **(d) Numeração de slides e rodapé** — adicionado rodapé "+COMPRAS · Executive Edition · Confidential" + paginação "NN / 16" em todos os slides de conteúdo (2–15). Capa e Encerramento (slides 1 e 16) seguem o padrão dos próprios masters full-bleed, sem rodapé sobreposto à fotografia, consistente com o Design System.
- **(e) Margem 0.6in respeitada** — validado programaticamente; nenhum elemento colado na borda.

## Correções feitas durante a geração

- O `Master-Cover.pptx` continha relacionamentos órfãos (`slide2.xml`–`slide6.xml`) não referenciados no `sldIdLst`, remanescentes do arquivo de origem. Isso causava colisão de nomes de partes ao adicionar novos slides (zip com nomes duplicados, arquivo potencialmente corrompido). Corrigido removendo os relacionamentos órfãos antes de montar o deck final.
- Slide de Timeline geral (8 marcos) e Arquitetura (5 camadas) exigiram duplicar o padrão de cartão+seta do master (originalmente com 3–4 elementos) — reaproveitando exatamente a mesma forma/estilo, apenas redimensionando a largura para caber no grid.
- Slide de Arquitetura: o master é um fluxo horizontal (não um diagrama de camadas empilhado). Mantido o layout horizontal do master (conforme instrução de não reconstruir estilos), ordenando as camadas da base ao topo da esquerda para a direita.

## Exportação

- PDF gerado com sucesso via `soffice --headless --convert-to pdf` (LibreOffice disponível em `/opt/homebrew/bin/soffice`): `resources/presentations/+COMPRAS Strategic Roadmap.pdf`.

## Pendências

Nenhuma pendência crítica identificada. Recomenda-se uma revisão humana final de conteúdo (números/datas) antes de publicação externa.
