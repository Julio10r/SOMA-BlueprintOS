# Aprendizado das Procedures Linx WISE

## Escopo

As procedures abaixo foram lidas como fonte de conhecimento e nao foram executadas:

- dbo.PROC_INTEGRACAO_LINX_WISE_TB_AUXILIARES_COM_ESTOQUE: found=True, chars=23147, DELETE tokens=43, INSERT tokens=35, UPDATE tokens=0
- dbo.PROC_INTEGRACAO_LINX_WISE_PRODUTOS: found=True, chars=23513, DELETE tokens=15, INSERT tokens=26, UPDATE tokens=19
- dbo.PROC_INTEGRACAO_LINX_WISE_ESTOQUE: found=True, chars=13722, DELETE tokens=9, INSERT tokens=7, UPDATE tokens=4

## Ambiente Confirmado

- Servidor: SRV-SOMADB
- Banco: SOMA
- Linked Server encontrado no legado: `WISE_AZURE`
- Banco remoto encontrado no legado: `SOMA_LINX`

## Regras Confirmadas

- As tabelas `WS_*` usam as mesmas chaves logicas correspondentes do Linx, conforme Product Owner.
- Mesmo assim, a chave deve ser aplicada conforme cada fluxo das procedures.
- Para o fluxo relevante de estoque pronta entrega, o legado grava em `[WISE_AZURE].[SOMA_LINX].[dbo].[WS_ESTOQUE_PRODUTOS]`.
- A chave operacional observada nesse fluxo envolve `ID_CAMPANHA`, `PRODUTO` e `COR_PRODUTO`.
- `ID_CAMPANHA` encontrado no legado nao e autorizacao funcional para esta execucao.

## Campanhas Encontradas no Legado

- Literal observado no fluxo de estoque extra web: `99`.
- Fontes dinamicas observadas em outros fluxos: `WS_PRODUTOS_INTERNET.ID_CAMPANHA`, `GS_CAMPANHA_ATACADO.ID_CAMPANHA`, `GS_CAMPANHA_ATACADO_COLECOES.ID_CAMPANHA`.

Esses valores sao apenas conhecimento historico. A execucao remota esta parada ate o Product Owner informar explicitamente qual `ID_CAMPANHA` usar.

## Comportamento Destrutivo do Legado

As procedures contem estrategia ampla com `DELETE`, `INSERT`, backups temporarios e em alguns pontos limpeza/restauracao de tabelas remotas. Esse comportamento nao foi reproduzido e nao esta autorizado para esta carga.

## Mapeamento Relevante Identificado

Origem principal para estoque extra web:

- `MB_PROD_EXTRA_WEB`
- `PRODUTOS`
- vendas usadas para abatimento de saldo no legado, quando aplicavel

Destino relevante:

- `[WISE_AZURE].[SOMA_LINX].[dbo].[WS_ESTOQUE_PRODUTOS]`

Campos observados no fluxo legado:

- `LIBERAR_GRADE_WEB`
- `PRODUTO`
- `COR_PRODUTO`
- `ESTOQUE`
- `ES1` a `ES16`
- `DATA_PARA_TRANSFERENCIA`
- `ID_CAMPANHA`

Regra de estoque observada:

- `ESTOQUE` e grades sao derivados do saldo extra, com abatimento de vendas quando o legado encontra saldo vendido.
- Valores negativos sao limitados a zero.

## Estrategia Incremental Pretendida

A estrategia segura para esta execucao e:

1. Usar somente registros da planilha com produto e cor validados.
2. Excluir da integracao remota produtos sem tabela `DL`.
3. Aguardar `ID_CAMPANHA` autorizado pelo Product Owner.
4. Consultar o destino pela chave logica confirmada.
5. Inserir apenas ausentes, atualizar apenas divergentes, e nao apagar registros remotos.
6. Reconsultar o destino apos qualquer escrita para validar o resultado.

## Status Atual

- Blocos locais ja processados conforme `report.json`.
- Bloco 4 esta parado no gate humano de `ID_CAMPANHA`.
- Nenhuma nova escrita remota foi executada apos esta regra de gate.
- A tentativa anterior com four-part name falhou com `SQLNCLI11`/`7399`, foi revertida, e nao integrou linhas.
