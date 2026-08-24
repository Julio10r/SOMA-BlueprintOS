# Prompt — Processar Planilha de Integração Linx/WISE

Use quando o Product Owner anexar uma planilha e pedir:

```text
Processar planilha de integração
```

ou:

```text
Executar integração diária Linx/WISE desta planilha.
```

## Instrução para o Agent

Siga o workflow canônico em:

- `.ai/context/linx-wise-daily-integration.md`
- `docs/operations/LinxWiseDailyIntegrationRunbook.md`

Resumo obrigatório:

1. Ler e validar a planilha.
2. Confirmar produção com `SELECT @@SERVERNAME, DB_NAME()`.
3. Validar globalmente `PRODUTOS` e `PRODUTO_CORES`.
4. Parar se houver produto ou produto/cor inexistente.
5. Atualizar `MB_PROD_EXTRA_WEB` sem escrever `TOTAL`.
6. Garantir `PRODUTOS.ENVIA_ATACADO_INTERNET = 1`.
7. Verificar `PRODUTOS_PRECOS.CODIGO_TAB_PRECO = 'DL'`.
8. Perguntar ao Product Owner o `ID_CAMPANHA`.
9. Integrar WISE incrementalmente para aprovados com `DL`.
10. Para `WS_ESTOQUE_PRODUTOS`, usar `FN_CONSULTA_SALDO_WEB_WISE('%','%',NULL,NULL)`.
11. Validar WISE por releitura.
12. Gerar planilha processada e relatórios.

## Nunca Fazer

- Nunca escolher `ID_CAMPANHA`.
- Nunca executar procedures legadas automaticamente.
- Nunca executar `DELETE` ou `TRUNCATE`.
- Nunca escrever em `MB_PROD_EXTRA_WEB.TOTAL`.
- Nunca escrever em `PRODUTOS_PRECOS`.
- Nunca alterar outra campanha.
- Nunca imprimir segredo.
