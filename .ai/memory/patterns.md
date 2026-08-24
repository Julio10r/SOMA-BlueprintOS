## A13 — Fluxo consultivo por caso de uso

- Controllers de produto validam e mapeiam HTTP, mas delegam a orquestração a um caso de uso Application.
- Identidade temporária de desenvolvimento é um adaptador substituível e não deve ser tratada como autenticação de produção.

# Padrão — Integração Diária Linx/WISE por Planilha

Quando o Product Owner pedir `Processar planilha de integração` ou `Executar integração diária Linx/WISE desta planilha`, consultar primeiro:

- `.ai/context/linx-wise-daily-integration.md`
- `docs/operations/LinxWiseDailyIntegrationRunbook.md`
- `.ai/prompts/processar-planilha-integracao-linx-wise.md`

Regras críticas persistidas:

- `ID_CAMPANHA` é gate humano obrigatório; nunca inferir do legado ou da execução anterior.
- `MB_PROD_EXTRA_WEB.TOTAL` é computado e nunca deve ser escrito.
- `PRODUTOS` e `PRODUTO_CORES` são validação global bloqueante antes de qualquer escrita.
- `ENVIA_ATACADO_INTERNET` deve ser corrigido para `1` e não bloqueia integração.
- Produtos sem `PRODUTOS_PRECOS.CODIGO_TAB_PRECO = 'DL'` não integram no WISE.
- `WS_ESTOQUE_PRODUTOS` deve usar `FN_CONSULTA_SALDO_WEB_WISE('%','%',NULL,NULL)`, filtrando a campanha informada e os produto/cor aprovados.
- Nunca executar as procedures legadas automaticamente, nem reproduzir `DELETE + INSERT` geral.
