# Showcase Collector

Implementação real e validada (2026-08-27, sessão FARM/LATAM: 418 produto/cor, 1193 fotos, 0 erros de download) do coletor de catálogo/grade/fotos do Showcase (Compuwise/WiseCommerce), parametrizada para qualquer marca/região disponível na sessão autenticada atual — nunca hardcoda marca.

Conhecimento canônico e passo a passo completo: [`.ai/context/showcase-knowledge.md`](../../.ai/context/showcase-knowledge.md) e [`docs/operations/ShowcaseAgentRunbook.md`](../../docs/operations/ShowcaseAgentRunbook.md). Este README é só o "como rodar"; não duplica as regras.

## Pré-requisito: contexto da sessão

Antes de rodar, extraia o contexto da sessão autenticada atual do Chrome (ver `showcase-knowledge.md`, seção "Como Extrair o Contexto da Sessão") e exporte:

```bash
export SHOWCASE_TOKEN="<token extraído de localStorage['0.soma|token']>"
export SHOWCASE_BRAND_ID="<brand_Id observado na rede>"
export SHOWCASE_COMPANY_ID="<company_Id>"
export SHOWCASE_DEPT_ID="<dept_Id>"
export SHOWCASE_COLLECTION_ID="<collection_Id>"
export SHOWCASE_CUSTOMER_ID="<customer_Id>"
export SHOWCASE_PRICELIST="<pricelist, ex.: DL>"
export SHOWCASE_PAYMENT="<payment, ex.: 60 DD>"
export SHOWCASE_ORDER_ID="<order_Id/orderId do carrinho da sessão>"
# opcional:
export SHOWCASE_COEFFICIENT="1"
export OUT_ROOT="/Users/juliocesar/Projects/SOMA-BlueprintOS/downloads/showcase_produtos"
```

Nenhum desses valores é fixo entre execuções — eles pertencem à sessão/conta logada no momento.

## Rodar

```bash
cd scripts/showcase_collector
npm install   # primeira vez apenas
node collect.js        # catálogo + grade + fotos, com checkpoint (coleta_showcase.csv)
node enrich.js          # opcional: adiciona LINHA/BASE/FABRIC por produto/cor
node build_excel.js     # gera catalogo_showcase.xlsx no layout validado
```

Reexecutar `collect.js` retoma sem baixar de novo fotos já marcadas `ok` no checkpoint.

## Saída

Em `$OUT_ROOT`:

- `fotos/` — imagens `{PRODUTO}_{COR}_{LETRA}.jpg`
- `catalogo_raw.json` — resposta bruta da paginação do catálogo
- `resultado_final.json` — catálogo processado (produto, cor, descrição, grade, estoque, fotos)
- `coleta_showcase.csv` — checkpoint de fotos (permite retomar)
- `erros.json` — erros não fatais
- `catalogo_showcase.xlsx` — planilha final (gerada por `build_excel.js`)
