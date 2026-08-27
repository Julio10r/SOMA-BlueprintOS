# Linx/WISE Daily Integration Knowledge

> Long-lived operational knowledge for the Linx Database Specialist and Linx ERP Specialist agents. This document consolidates confirmed learning from the production execution on 2026-08-24 and must be treated as reusable workflow knowledge, not as a transcript.

## Trigger Phrases

Use this workflow when the Product Owner provides an Excel spreadsheet and asks for one of:

- `Processar planilha de integração`
- `Executar integração diária Linx/WISE desta planilha`
- `Processar carga MB_PROD_EXTRA_WEB / WISE`

The agent must still confirm the production environment and must still ask the Product Owner for `ID_CAMPANHA` before remote WISE integration.

## Precedence

`DECISAO_PO`: For the trigger `Executar integração diária Linx/WISE desta planilha`, this document and [LinxWiseDailyIntegrationRunbook.md](../../docs/operations/LinxWiseDailyIntegrationRunbook.md) are the source of truth. The WISE Agent knowledge base is complementary only. If WISE Agent guidance conflicts with this daily workflow during a daily integration, this document and the daily runbook take precedence.

## Provenance Labels

Use these labels when extending this knowledge:

- `CONFIRMADO`: verified by production schema, procedure source, read-only query, or successful execution.
- `DECISAO_PO`: explicitly stated by the Product Owner.
- `LEGADO`: observed in legacy stored procedures; useful for understanding but not automatically authorized.
- `NAO_USAR`: attempted or legacy behavior that must not be used in the daily workflow.

## Confirmed Environment

- `CONFIRMADO`: Production environment must be checked before any write:
  - `SELECT @@SERVERNAME AS servidor, DB_NAME() AS banco`
  - Continue only when `servidor = SRV-SOMADB` and `banco = SOMA`.
- `CONFIRMADO`: Credentials are loaded from local `.env` and must never be printed.
- `CONFIRMADO`: Required variables:
  - `LINX_PROD_SERVER`
  - `LINX_PROD_DATABASE`
  - `LINX_PROD_USER`
  - `LINX_PROD_PASSWORD`
- `CONFIRMADO`: Use `.venv`, `pyodbc`, and `ODBC Driver 17 for SQL Server`.
- `NAO_USAR`: Never log password, connection string, or `.env` contents.

## Spreadsheet Contract

`CONFIRMADO` for the 2026-08-24 execution:

- Sheet: `Planilha1`
- Required columns:
  - `PRODUTO`
  - `COR_PRODUTO`
  - `DATA`
  - `TOTAL ARGENTINA`
  - `TAM_1` through `TAM_7`
- Confirmed mapping to `MB_PROD_EXTRA_WEB`:
  - `DATA -> DATA_LIMITE`
  - `TAM_1 -> EX1`
  - `TAM_2 -> EX2`
  - `TAM_3 -> EX3`
  - `TAM_4 -> EX4`
  - `TAM_5 -> EX5`
  - `TAM_6 -> EX6`
  - `TAM_7 -> EX7`
  - `TOTAL ARGENTINA` is validation-only against computed `TOTAL`.

Do not assume every future spreadsheet has exactly seven sizes if the sheet/schema changes. Detect `TAM_n` columns and map only confirmed columns to corresponding `EXn`. If the structure differs from the known contract, stop and ask for confirmation.

## Global Blocking Validation

Before any write:

- `CONFIRMADO`: every `PRODUTO` must exist in `PRODUTOS`.
- `CONFIRMADO`: every `PRODUTO + COR_PRODUTO` must exist in `PRODUTO_CORES`.

If any row fails:

- Stop the whole execution.
- Do not update `MB_PROD_EXTRA_WEB`.
- Do not update `PRODUTOS`.
- Do not integrate WISE.
- Generate a report listing invalid products/product-colors.

## MB_PROD_EXTRA_WEB

- `CONFIRMADO`: Logical key for this routine:
  - `PRODUTO`
  - `COR_PRODUTO`
  - `DATA_LIMITE`
- `CONFIRMADO`: `TOTAL` is a computed column (`is_computed = True`) calculated by SQL Server from `EX1..EX48`.
- `NAO_USAR`: Never include `TOTAL` in `INSERT`, `UPDATE`, or `SET`.
- `CONFIRMADO`: `TOTAL` is read-only validation against `TOTAL ARGENTINA`.

Behavior:

- Existing row with matching mapped grade: classify `OK_SEM_ATUALIZACAO`, no write.
- Existing row with divergent `EXn`: update only divergent `EXn` columns.
- Missing row: insert only writable required fields; never insert `TOTAL`.
- Never delete from `MB_PROD_EXTRA_WEB` in this workflow.
- After writing, re-read rows and validate:
  - key fields
  - mapped `EXn`
  - computed `TOTAL`

## PRODUTOS.ENVIA_ATACADO_INTERNET

- `CONFIRMADO`: For every product in the spreadsheet, ensure `PRODUTOS.ENVIA_ATACADO_INTERNET = 1`.
- If already `1`: classify `JA_HABILITADO`, no write.
- If different from `1`: update only this field for that product, classify `HABILITADO_NESTA_EXECUCAO`.
- `DECISAO_PO`: This field is not an integration blocker because the workflow corrects it before integration.
- `NAO_USAR`: Never update other fields in `PRODUTOS` in this routine.

## PRODUTOS_PRECOS / DL

- `CONFIRMADO`: For each product, check existence of `PRODUTOS_PRECOS.CODIGO_TAB_PRECO = 'DL'`.
- `DL_OK`: product can proceed to WISE eligibility if the other conditions pass.
- `SEM_TABELA_DL`: product is not integrated remotely.
- `DECISAO_PO`: Absence of `DL` does not block `MB_PROD_EXTRA_WEB` or `ENVIA_ATACADO_INTERNET`.
- `NAO_USAR`: Never `INSERT`, `UPDATE`, or `DELETE` in `PRODUTOS_PRECOS` for this routine.
- `DECISAO_PO`: In `WS_PRODUTOS_PRECOS`, the daily routine only needs to verify `CODIGO_TAB_PRECO = 'DL'`; do not reproduce the broad legacy price-table export.

## Legacy Procedures Studied

`LEGADO`: These procedures are knowledge sources only:

- `dbo.PROC_INTEGRACAO_LINX_WISE_TB_AUXILIARES_COM_ESTOQUE`
- `dbo.PROC_INTEGRACAO_LINX_WISE_PRODUTOS`
- `dbo.PROC_INTEGRACAO_LINX_WISE_ESTOQUE`

Rules:

- `NAO_USAR`: Do not execute these procedures automatically.
- `NAO_USAR`: Do not reproduce their broad `DELETE + INSERT` campaign reload strategy.
- `NAO_USAR`: Ignore hardcoded `ID_CAMPANHA = 99` or other hardcoded filters outside the current Product Owner-approved scope.
- `CONFIRMADO`: Procedure source is useful to identify mappings, keys, destination tables, and legacy business semantics.

## ID_CAMPANHA Gate

- `DECISAO_PO`: The agent must never choose `ID_CAMPANHA`.
- Always stop and ask:
  - `Qual ID_CAMPANHA devo utilizar nesta integração?`
- Legacy values or previous executions are context only, never authorization.
- Once the Product Owner provides `ID_CAMPANHA`, use it consistently for that execution.
- Ask again only if a conflict appears or the scope requires more than one campaign.

## WISE Destination

- `CONFIRMADO`: Linked Server: `WISE_AZURE`.
- `CONFIRMADO`: Remote database/schema: `[SOMA_LINX].[dbo]`.
- `DECISAO_PO`: `WS_*` tables use the same logical keys as the corresponding Linx tables unless procedure/schema evidence contradicts this.
- `CONFIRMADO`: Four-part name writes succeeded for targeted `UPDATE` operations against WISE tables.
- `CONFIRMADO`: A previous four-part `INSERT` attempt into `WS_ESTOQUE_PRODUTOS` failed with provider `SQLNCLI11` / SQL Server `7399`; this is useful diagnostic knowledge, not the preferred path.

## WS_ESTOQUE_PRODUTOS

### Key and Scope

- `CONFIRMADO`: Operational key used in this workflow:
  - `ID_CAMPANHA`
  - `PRODUTO`
  - `COR_PRODUTO`
- Always restrict every remote operation by Product Owner-approved `ID_CAMPANHA`.
- Never affect another campaign.

### Correct Source of Stock

- `CONFIRMADO`: The active stock source in `PROC_INTEGRACAO_LINX_WISE_ESTOQUE` is:
  - `FN_CONSULTA_SALDO_WEB_WISE('%','%',NULL,NULL)`
- `CONFIRMADO`: For the successful correction on 2026-08-24, the set-based source worked with:
  - `FN_CONSULTA_SALDO_WEB_WISE('%','%',NULL,NULL)`
  - filter `ID_CAMPANHA = <PO value>`
  - join against approved `PRODUTO + COR_PRODUTO`.
- `CONFIRMADO`: Calling the function with `NULL,NULL,NULL,NULL` returned zero rows in this environment and must not be used as the default.

### Mapping

Map the function result to `WS_ESTOQUE_PRODUTOS`:

- `ID_CAMPANHA -> ID_CAMPANHA`
- `LIBERAR_GRADE_WEB -> LIBERAR_GRADE_WEB`
- `PRODUTO -> PRODUTO`
- `COR_PRODUTO -> COR_PRODUTO`
- `SALDO_DISPONIVEL -> ESTOQUE`
- `D1 -> ES1`
- `D2 -> ES2`
- `D3 -> ES3`
- `D4 -> ES4`
- `D5 -> ES5`
- `D6 -> ES6`
- `D7 -> ES7`
- `D8 -> ES8`
- `D9 -> ES9`
- `D10 -> ES10`
- `D11 -> ES11`
- `D12 -> ES12`
- `D13 -> ES13`
- `D14 -> ES14`
- `D15 -> ES15`
- `D16 -> ES16`
- `DATA_PARA_TRANSFERENCIA = GETDATE()`
- `DT_INTEGRACAO = CAST(GETDATE() AS smalldatetime)`
- `DT_EXCLUSAO = NULL` for approved active rows.

### Activity Rules

- Approved spreadsheet row with `DL_OK`: must be active in WISE.
  - If the `ID_CAMPANHA + PRODUTO + COR_PRODUTO` key does not exist, insert it with the expected stock/grade values and `DT_EXCLUSAO = NULL`.
  - If `DT_EXCLUSAO IS NOT NULL`, reactivate with `DT_EXCLUSAO = NULL`.
  - Update only divergent stock/grade fields.
- Product in spreadsheet with `SEM_TABELA_DL`: do not integrate as active.
- Active remote row in the campaign/universe that is not in the spreadsheet-approved set: mark `DT_EXCLUSAO = GETDATE()`.
- If a row is already inactive and should remain inactive, do not rewrite `DT_EXCLUSAO`.
- Never physically delete from `WS_ESTOQUE_PRODUTOS`.

### Required Execution Order

`DECISAO_PO`: Reconcile activity before updating stock values:

1. Insert approved product/color rows missing from the campaign, or reactivate approved rows that are inactive.
2. Inactivate active campaign rows outside the approved set.
3. Re-read and validate that the active campaign set matches the approved set.
4. Only then update `ESTOQUE`, `ES1..ES16`, `LIBERAR_GRADE_WEB`, `DATA_PARA_TRANSFERENCIA`, and `DT_INTEGRACAO` for approved active rows.

This ordering is mandatory for future daily executions.

### Confirmed Successful Execution Pattern

The safe correction pattern used successfully:

1. Build `#aprov_pc` from approved product/color rows.
2. Build `#expected` from `FN_CONSULTA_SALDO_WEB_WISE('%','%',NULL,NULL)` filtered by `ID_CAMPANHA` and joined to `#aprov_pc`.
3. Abort if `#expected` count differs from approved product/color count.
4. Reconcile activity first: insert missing approved rows, reactivate approved inactive rows, then inactivate active rows outside `#expected`.
5. Re-read the remote active set and validate it before touching stock fields.
6. `UPDATE [WISE_AZURE].[SOMA_LINX].[dbo].[WS_ESTOQUE_PRODUTOS]` joined to `#expected` only for stock and grade fields.
7. Update only rows where `LIBERAR_GRADE_WEB`, `ESTOQUE`, or `ES1..ES16` differ.
8. Re-read remote rows and assert zero mismatches.

## Product-Dependent WISE Tables

`CONFIRMADO` from the 2026-08-24 execution:

- `DECISAO_PO`: `WS_PRODUTOS`, `WS_PRODUTO_CORES`, `WS_PRODUTOS_BARRA`, and `WS_PROP_PRODUTOS` use incremental synchronization for the approved set: insert missing keys; update only divergent fields for existing keys.
- `DECISAO_PO`: For each approved product, locate the remote `WS_PRODUTOS_PRECOS` row for the current campaign where `CODIGO_TAB_PRECO = 'DL'`. When the remote price is missing, report it as pending and do not insert it. When it exists, compare `PRODUTOS_PRECOS.PRECO1` with `WS_PRODUTOS_PRECOS.PRECO1`; if different, update only the remote `PRECO1`, restricted by `ID_CAMPANHA`, `PRODUTO`, and `CODIGO_TAB_PRECO = 'DL'`.
- `CONFIRMADO`: `WS_PROP_PRODUTOS` had no expected rows for property `00717` in the 2026-08-24 execution.

Do not execute broad auxiliary table reloads from the procedures unless a future, explicit Product Owner-approved workflow is created for that scope.

## Spreadsheet Output

After each execution, create a processed copy of the original workbook. Do not destructively overwrite the original.

Minimum result columns:

- `STATUS_INTEGRACAO`
- `DETALHE_INTEGRACAO`

Recommended additional columns:

- `STATUS_VALIDACAO`
- `STATUS_MB_PROD_EXTRA_WEB`
- `STATUS_ENVIA_ATACADO`
- `STATUS_TABELA_DL`
- `STATUS_WISE`
- `DATA_PROCESSAMENTO`

Preserve workbook formatting, tabs, row order, and original data whenever technically possible.

## Reporting

Generate reports under an ignored local output directory, for example:

- `.ai/local-output/mb_prod_extra_web/`

Required report content:

- spreadsheet counts
- product/product-color validation
- `MB_PROD_EXTRA_WEB` classifications
- `ENVIA_ATACADO_INTERNET` classifications
- `DL_OK` / `SEM_TABELA_DL`
- `ID_CAMPANHA` used
- WISE integration counts:
  - already OK
  - updated
  - inserted
  - reactivated
  - inactivated
  - sem DL
  - errors
- commit/rollback status per step
- processed workbook path
- report paths

## Product Owner Updates

Send short progress updates per stage or meaningful batch, not per row:

- Spreadsheet read and row counts.
- Global Linx validation result.
- `MB_PROD_EXTRA_WEB` delta.
- `ENVIA_ATACADO_INTERNET` result.
- `DL` result.
- `ID_CAMPANHA` gate.
- WISE integration started/completed.
- Post-validation completed.
- Processed workbook generated.

If an error occurs, report:

- stage
- nature of problem
- impact
- commit/rollback status
- whether execution can continue

## Safety Rules

Never:

- print secrets
- version `.env`
- execute the legacy procedures automatically
- execute `DELETE` or `TRUNCATE`
- run broad campaign reloads
- update without a restrictive `WHERE`
- write `MB_PROD_EXTRA_WEB.TOTAL`
- write `PRODUTOS_PRECOS`
- modify `PRODUTO_CORES`
- choose `ID_CAMPANHA`
- affect another campaign
- rely on `ID_CAMPANHA = 99` hardcoded legacy blocks for this daily workflow

## Conceptual Self-Test

A new agent using this file must be able to answer:

1. Validate `PRODUTOS` and `PRODUTO_CORES` before any write.
2. Missing product or product/color blocks the whole execution.
3. `ENVIA_ATACADO_INTERNET` is corrected to `1` and does not block integration.
4. Missing `DL` blocks only remote integration for that product.
5. `MB_PROD_EXTRA_WEB` is updated by key `PRODUTO + COR_PRODUTO + DATA_LIMITE`.
6. `TOTAL` is computed and must never be written.
7. `ID_CAMPANHA` must be requested from the Product Owner.
8. WISE integration is incremental and restricted to approved product/color rows and the approved campaign.
9. Rows outside the approved set are inactivated with `DT_EXCLUSAO`, not deleted.
10. Already inactive rows are not touched just to refresh `DT_EXCLUSAO`.
11. Remote validation is by re-reading destination and comparing exact relevant fields.
12. The processed workbook is a new copy with status/detail columns.
13. The Product Owner receives stage-level progress updates.
14. Reports are generated under ignored local output.
15. Legacy broad deletes/truncates/procedure executions are forbidden.
