# Runbook — Carga e Integração Diária Linx/WISE

## Quando Usar

Use este runbook quando o Product Owner fornecer uma planilha de carga para `MB_PROD_EXTRA_WEB` e pedir a integração diária Linx/WISE.

Instrução curta recomendada:

```text
Executar integração diária Linx/WISE desta planilha.
```

Conhecimento canônico: [`.ai/context/linx-wise-daily-integration.md`](../../.ai/context/linx-wise-daily-integration.md).

## Pré-Requisitos

- `.env` local na raiz do projeto, ignorado pelo Git.
- `.venv` do projeto.
- `pyodbc`.
- `ODBC Driver 17 for SQL Server`.
- VPN ativa.
- Acesso ao SQL Server de produção.
- Planilha Excel anexada.

Nunca imprimir senha, `.env`, ou connection string.

## Gate Inicial

Antes de qualquer escrita, executar:

```sql
SELECT @@SERVERNAME AS servidor, DB_NAME() AS banco;
```

Continuar somente se:

- servidor: `SRV-SOMADB`
- banco: `SOMA`

## Gate de Campanha

Antes da integração WISE, perguntar obrigatoriamente:

```text
Qual ID_CAMPANHA devo utilizar nesta integração?
```

Não escolher campanha com base no legado ou em execuções anteriores.

## Fluxo Operacional

1. Ler a planilha.
2. Validar estrutura da planilha.
3. Validar globalmente `PRODUTOS` e `PRODUTO_CORES`.
4. Se houver produto ou produto/cor inexistente, parar antes de qualquer escrita.
5. Atualizar `MB_PROD_EXTRA_WEB` de forma incremental.
6. Garantir `PRODUTOS.ENVIA_ATACADO_INTERNET = 1`.
7. Verificar `PRODUTOS_PRECOS.CODIGO_TAB_PRECO = 'DL'`.
8. Perguntar `ID_CAMPANHA`.
9. Reconciliar a atividade no WISE somente para produtos aprovados e com `DL`: reativar os aprovados inativos e inativar com `DT_EXCLUSAO = GETDATE()` os registros ativos da campanha/universo fora do conjunto aprovado.
10. Validar por releitura a atividade da campanha.
11. Atualizar os saldos e grades somente dos produto/cores aprovados e ativos.
12. Validar por releitura os saldos e grades.
13. Gerar planilha processada.
14. Gerar relatórios.

## MB_PROD_EXTRA_WEB

Chave:

- `PRODUTO`
- `COR_PRODUTO`
- `DATA_LIMITE`

Mapeamento:

- `DATA -> DATA_LIMITE`
- `TAM_n -> EXn`
- `TOTAL ARGENTINA -> TOTAL` somente para validação.

`TOTAL` é computado. Nunca escrever `TOTAL`.

## Integração WISE — Estoque

Tabela:

```sql
[WISE_AZURE].[SOMA_LINX].[dbo].[WS_ESTOQUE_PRODUTOS]
```

Chave:

- `ID_CAMPANHA`
- `PRODUTO`
- `COR_PRODUTO`

Fonte correta:

```sql
FN_CONSULTA_SALDO_WEB_WISE('%','%',NULL,NULL)
```

Filtrar pela campanha informada pelo Product Owner e cruzar com os produto/cor aprovados.

Mapeamento:

- `SALDO_DISPONIVEL -> ESTOQUE`
- `D1..D16 -> ES1..ES16`
- `LIBERAR_GRADE_WEB -> LIBERAR_GRADE_WEB`
- `DT_EXCLUSAO = NULL` para aprovados ativos
- `DATA_PARA_TRANSFERENCIA = GETDATE()`
- `DT_INTEGRACAO = CAST(GETDATE() AS smalldatetime)`

Não usar o bloco legado hardcoded `ID_CAMPANHA = 99`.

## Inativação

Para a campanha informada:

- aprovado + DL: ativo, `DT_EXCLUSAO = NULL`
- na planilha sem DL: não integrar como ativo
- fora do conjunto aprovado da planilha: `DT_EXCLUSAO = GETDATE()`, se ainda ativo
- já inativo e deve continuar inativo: não atualizar novamente

Nunca executar `DELETE` físico.

## Ordem de Reconciliação e Saldo

Para cada campanha, executar nesta ordem:

1. Para cada produto/cor aprovado: inserir o registro quando não existir na campanha; reativar quando existir com `DT_EXCLUSAO` preenchida.
2. Inativar os registros ativos fora do conjunto aprovado.
3. Reler e confirmar que o conjunto ativo coincide com o conjunto aprovado.
4. Somente então atualizar `ESTOQUE`, `ES1..ES16`, `LIBERAR_GRADE_WEB`, `DATA_PARA_TRANSFERENCIA` e `DT_INTEGRACAO`.

Essa separação evita atualizar saldo de registro que deva permanecer inativo e torna a validação de campanha independente da validação de estoque.

## Produto, Cor, Preço e Barras

- `WS_PRODUTOS`, `WS_PRODUTO_CORES`, `WS_PRODUTOS_BARRA` e `WS_PROP_PRODUTOS`: para o conjunto aprovado, inserir quando a chave não existir e atualizar somente os campos divergentes quando existir.
- `WS_PRODUTOS_PRECOS`: para este workflow diário, verificar somente `CODIGO_TAB_PRECO = 'DL'`. Para cada produto aprovado, localizar o registro remoto da campanha com `CODIGO_TAB_PRECO = 'DL'`: quando ausente, registrar como pendência; quando existir, comparar `PRODUTOS_PRECOS.PRECO1` com `WS_PRODUTOS_PRECOS.PRECO1`. Quando diferente, atualizar somente `WS_PRODUTOS_PRECOS.PRECO1`, restringindo por `ID_CAMPANHA`, `PRODUTO` e `CODIGO_TAB_PRECO = 'DL'`.

Não executar tabelas auxiliares amplas de campanha/rede sem novo escopo aprovado.

## Comandos Proibidos

Não executar:

- procedures legadas automaticamente
- `DELETE`
- `TRUNCATE`
- carga completa
- `UPDATE` sem `WHERE`
- alteração de outra campanha
- escrita em `PRODUTOS_PRECOS`
- escrita em `PRODUTO_CORES`
- escrita direta em `MB_PROD_EXTRA_WEB.TOTAL`

## Saídas

Diretório local de relatórios:

```text
.ai/local-output/mb_prod_extra_web/
```

Este diretório deve permanecer ignorado pelo Git.

Arquivos recomendados:

- `report.json`
- `integracao_execucao_summary.json`
- `integrados.csv`
- `pendentes_integracao.csv`
- `sem_tabela_dl.csv`
- `erros_integracao.csv`
- `aprendizado_procedures.md`
- `processada.xlsx`

## Atualização da Planilha

Gerar nova cópia, sem sobrescrever a original.

Colunas mínimas:

- `STATUS_INTEGRACAO`
- `DETALHE_INTEGRACAO`

Colunas recomendadas:

- `STATUS_VALIDACAO`
- `STATUS_MB_PROD_EXTRA_WEB`
- `STATUS_ENVIA_ATACADO`
- `STATUS_TABELA_DL`
- `STATUS_WISE`
- `DATA_PROCESSAMENTO`

## Comunicação com o Product Owner

Informar progresso por etapa:

- planilha lida
- validação Linx concluída
- delta de `MB_PROD_EXTRA_WEB`
- `ENVIA_ATACADO_INTERNET`
- `DL`
- campanha recebida
- integração WISE em andamento
- validação pós-integração
- planilha processada
- relatório final

Em erro, informar etapa, impacto e status de commit/rollback.

## Validação Final

Antes de encerrar:

- confirmar ambiente
- confirmar campanha usada
- confirmar zero divergências nos aprovados
- confirmar contagens de inativados/reativados/atualizados
- confirmar que não houve `DELETE`/`TRUNCATE`/procedure legada
- confirmar caminho da planilha processada e relatórios
