# Runbook — WISE Agent (Consulta ao Ambiente WISE)

## Quando Usar

Use este runbook quando o Product Owner pedir uma consulta ao ambiente WISE — campanha, saldo/estoque, estrutura `WS_*`, ou relacionamento Showcase ↔ WISE.

Instrução curta recomendada:

```text
Peça ao Agent WISE para buscar os saldos desses produtos.
```

ou:

```text
Use o Agent WISE para consultar a campanha 54.
```

Conhecimento canônico: [`.ai/context/wise-knowledge.md`](../../.ai/context/wise-knowledge.md).

Para a rotina de **escrita**/sincronização diária Linx/WISE (`MB_PROD_EXTRA_WEB`, `WS_ESTOQUE_PRODUTOS`), use em vez disso [LinxWiseDailyIntegrationRunbook.md](./LinxWiseDailyIntegrationRunbook.md) — este runbook cobre apenas consulta/leitura.

Quando uma tarefa pedir integração real, o WISE Agent participa apenas como especialista de interpretação/reconciliação do destino WISE. A validação de elegibilidade ERP pertence ao Agent Linx e ao runbook diário.

## Pré-Requisitos

- Para consulta SQL direta: mesmo mecanismo já usado pela rotina diária Linx/WISE — `.env` local (`LINX_PROD_SERVER`, `LINX_PROD_DATABASE`, `LINX_PROD_USER`, `LINX_PROD_PASSWORD`), `.venv`, `pyodbc`, `ODBC Driver 17 for SQL Server`, VPN ativa — **ou** um servidor MCP de SQL Server conectado à sessão atual. Se nenhum dos dois estiver disponível, informar o Product Owner e oferecer a via alternativa (API do Showcase) quando aplicável, em vez de travar a tarefa.
- Para consulta ao Showcase: sessão de navegador autenticada pelo Product Owner (login manual, nunca preenchido pelo agente) e Chrome DevTools MCP.
- Nunca imprimir senha, `.env`, connection string ou token.

## Gate Inicial (consulta SQL direta)

Antes de qualquer leitura sensível, executar:

```sql
SELECT @@SERVERNAME AS servidor, DB_NAME() AS banco;
```

Continuar somente se:

- servidor: `SRV-SOMADB`
- banco: `SOMA`

O WISE é alcançado a partir dessa conexão via Linked Server de quatro partes: `[WISE_AZURE].[SOMA_LINX].[dbo].[TABELA]`.

## Gate de Campanha

Antes de qualquer consulta restrita a uma campanha, se `ID_CAMPANHA` não tiver sido informado na tarefa, perguntar obrigatoriamente:

```text
Qual ID_CAMPANHA devo utilizar nesta consulta?
```

Não escolher campanha com base em execuções anteriores ou valores de exemplo deste runbook.

## Fluxo Operacional — Consulta de Saldo por Campanha

1. Confirmar ambiente (`SELECT @@SERVERNAME, DB_NAME()`).
2. Confirmar `ID_CAMPANHA` com o Product Owner, se ainda não informado.
3. Consultar:
   ```sql
   SELECT *
   FROM [WISE_AZURE].[SOMA_LINX].[DBO].WS_ESTOQUE_PRODUTOS
   WHERE ID_CAMPANHA = '<ID_CAMPANHA>'
     AND DT_EXCLUSAO IS NULL
   ```
4. Interpretar `DT_EXCLUSAO IS NULL` como registro ativo.
5. Ao cruzar com produtos do Showcase ou de uma planilha, usar a chave `PRODUTO + COR_PRODUTO` (nunca descrição).
6. Responder citando a fonte e classificando o conhecimento usado (`CONFIRMADO`/`INFERIDO`/`AINDA_NAO_MAPEADO`, ver `.ai/context/wise-knowledge.md`).

## Fluxo Operacional — Consulta ao Showcase (quando não há acesso SQL disponível)

1. Abrir o Showcase no Chrome via MCP; parar no login e aguardar confirmação do Product Owner.
2. Após confirmação, extrair o token de sessão (`localStorage['0.soma|token']`) apenas para uso em memória durante a tarefa — nunca gravar em arquivo versionado.
3. Usar os endpoints confirmados em `wise-knowledge.md` (`showcase`, `products`, `productColors`, `stock`) com cadência controlada (não disparar centenas de requisições simultâneas).
4. Se a sessão expirar (respostas HTML de login ou HTTP 401/403), parar e pedir novo login ao Product Owner — nunca tentar contornar autenticação.

## Fluxo Conceitual — Integração Autorizada de Planilha

Não executar integração a partir deste runbook. Quando o Product Owner pedir algo como "Use o Agent WISE para integrar uma nova planilha de produtos na campanha 54", redirecionar para [LinxWiseDailyIntegrationRunbook.md](./LinxWiseDailyIntegrationRunbook.md) e raciocinar nesta ordem:

1. Agent Linx valida o ERP:
   - existência de `PRODUTO` em `PRODUTOS`;
   - existência de `PRODUTO + COR_PRODUTO` em `PRODUTO_CORES`;
   - `PRODUTOS.ENVIA_ATACADO_INTERNET = 1` ou correção autorizada pelo fluxo diário;
   - `PRODUTOS_PRECOS.CODIGO_TAB_PRECO = 'DL'` para elegibilidade WISE.
2. Agent Linx atualiza `MB_PROD_EXTRA_WEB` quando aplicável:
   - `DATA -> DATA_LIMITE`;
   - `TAM_n -> EXn`;
   - `TOTAL ARGENTINA` apenas como validação contra `TOTAL`;
   - nunca escrever em `MB_PROD_EXTRA_WEB.TOTAL`, que é computado.
3. Agent WISE interpreta o destino `[WISE_AZURE].[SOMA_LINX].[dbo].[WS_ESTOQUE_PRODUTOS]` com chave `ID_CAMPANHA + PRODUTO + COR_PRODUTO`.
4. Para a campanha informada pelo Product Owner, reconciliar atividade antes de saldo:
   - reativar aprovados inativos com `DT_EXCLUSAO = NULL`;
   - inativar ativos fora do conjunto aprovado com `DT_EXCLUSAO = GETDATE()`;
   - validar por releitura que o conjunto ativo coincide com o conjunto aprovado;
   - somente depois atualizar `ESTOQUE`, `ES1..ES16`, `LIBERAR_GRADE_WEB`, `DATA_PARA_TRANSFERENCIA` e `DT_INTEGRACAO`.

Números históricos, como os `446` produto/cores ativos observados em uma execução da campanha `54`, são apenas referência de execução passada; a regra é comparar a cardinalidade da execução corrente contra o conjunto validado da planilha.

## Segurança

- Comportamento padrão: **somente leitura**.
- Nunca executar `INSERT`, `UPDATE`, `DELETE`, `TRUNCATE`, `MERGE`, `ALTER`, `DROP`, `CREATE` ou procedure de escrita automaticamente.
- Se uma tarefa exigir alteração no WISE, seguir [LinxWiseDailyIntegrationRunbook.md](./LinxWiseDailyIntegrationRunbook.md) (se for a rotina diária já coberta) ou explicar a alteração proposta, mostrar os registros afetados via `SELECT` prévio, e aguardar autorização explícita antes de qualquer escrita.
- Nunca executar automaticamente as procedures legadas `dbo.PROC_INTEGRACAO_LINX_WISE_TB_AUXILIARES_COM_ESTOQUE`, `dbo.PROC_INTEGRACAO_LINX_WISE_PRODUTOS` ou `dbo.PROC_INTEGRACAO_LINX_WISE_ESTOQUE`; elas são referência de regra legada, não rotina atual autorizada.

## Erros Conhecidos

- `INSERT` remoto por Linked Server em `[WISE_AZURE].[SOMA_LINX].[dbo].[WS_ESTOQUE_PRODUTOS]` já apresentou erro provider `SQLNCLI11` / SQL Server `7399` em tentativa anterior. Registrar o contexto e investigar antes de repetir; não tratar como limitação universal, mas não assumir que `INSERT` remoto é caminho seguro sem validação.
- Updates direcionados e restritos por campanha/chave já foram usados com sucesso para `DT_EXCLUSAO` e saldo/grade, sempre com validação por releitura.

## Autoteste Conceitual

Ver seção "Autoteste Conceitual" em [`.ai/context/wise-knowledge.md`](../../.ai/context/wise-knowledge.md) — um WISE Agent que carregou o conhecimento canônico deve responder a essas sete perguntas sem depender do chat original.
