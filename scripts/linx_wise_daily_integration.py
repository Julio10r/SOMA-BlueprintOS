#!/usr/bin/env python3
import argparse
import csv
import json
import os
import shutil
from datetime import datetime
from pathlib import Path

import openpyxl
import pyodbc


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / ".ai" / "local-output" / "mb_prod_extra_web" / "current"


def load_env():
    env_path = ROOT / ".env"
    if not env_path.exists():
        raise SystemExit(".env not found")
    for line in env_path.read_text().splitlines():
        line = line.strip()
        if not line or line.startswith("#") or "=" not in line:
            continue
        k, v = line.split("=", 1)
        os.environ.setdefault(k.strip(), v.strip().strip('"').strip("'"))


def conn():
    load_env()
    required = ["LINX_PROD_SERVER", "LINX_PROD_DATABASE", "LINX_PROD_USER", "LINX_PROD_PASSWORD"]
    missing = [k for k in required if not os.environ.get(k)]
    if missing:
        raise SystemExit(f"missing env vars: {', '.join(missing)}")
    cs = (
        "DRIVER={ODBC Driver 17 for SQL Server};"
        f"SERVER={os.environ['LINX_PROD_SERVER']};"
        f"DATABASE={os.environ['LINX_PROD_DATABASE']};"
        f"UID={os.environ['LINX_PROD_USER']};"
        f"PWD={os.environ['LINX_PROD_PASSWORD']};"
        "TrustServerCertificate=yes;"
    )
    return pyodbc.connect(cs, autocommit=False, timeout=30)


def read_rows(path: Path, data_limite: str):
    wb = openpyxl.load_workbook(path, data_only=True)
    ws = wb[wb.sheetnames[0]]
    headers = [str(c.value).strip() if c.value is not None else "" for c in ws[1]]
    idx = {h: i for i, h in enumerate(headers)}
    required = ["PRODUTO", "COR_PRODUTO", "TOTAL"]
    missing = [h for h in required if h not in idx]
    if missing:
        raise SystemExit(f"missing spreadsheet columns: {missing}")
    tam_cols = [h for h in headers if h.startswith("TAM_")]
    if not tam_cols:
        raise SystemExit("no TAM_n columns found")
    rows = []
    errors = []
    seen = set()
    for rnum, row in enumerate(ws.iter_rows(min_row=2, values_only=True), start=2):
        if all(v is None for v in row):
            continue
        produto = str(row[idx["PRODUTO"]]).strip()
        cor = str(row[idx["COR_PRODUTO"]]).strip().zfill(4)
        vals = {}
        total_calc = 0
        for h in tam_cols:
            n = int(h.split("_", 1)[1])
            val = row[idx[h]]
            val = int(val or 0)
            vals[f"EX{n}"] = val
            total_calc += val
        total = int(row[idx["TOTAL"]] or 0)
        key = (produto, cor, data_limite)
        if key in seen:
            errors.append({"row": rnum, "erro": "DUPLICATE_KEY", "produto": produto, "cor": cor})
        seen.add(key)
        if total != total_calc:
            errors.append({"row": rnum, "erro": "TOTAL_DIVERGENTE", "produto": produto, "cor": cor, "total": total, "calculado": total_calc})
        rows.append({"row": rnum, "PRODUTO": produto, "COR_PRODUTO": cor, "DATA_LIMITE": data_limite, "TOTAL_PLANILHA": total, **vals})
    return rows, tam_cols, errors


def write_csv(path, rows, fields=None):
    path.parent.mkdir(parents=True, exist_ok=True)
    if fields is None and rows:
        fields = list(rows[0].keys())
    with path.open("w", newline="") as f:
        w = csv.DictWriter(f, fieldnames=fields or [])
        w.writeheader()
        w.writerows(rows)


def make_temp(cur, rows):
    cur.execute("IF OBJECT_ID('tempdb..#carga') IS NOT NULL DROP TABLE #carga")
    ex_cols = sorted([k for k in rows[0] if k.startswith("EX")], key=lambda x: int(x[2:]))
    cur.execute(
        "CREATE TABLE #carga (PRODUTO varchar(30) NOT NULL, COR_PRODUTO varchar(20) NOT NULL, DATA_LIMITE date NOT NULL, "
        + ", ".join(f"{c} int NOT NULL" for c in ex_cols)
        + ", TOTAL_PLANILHA int NOT NULL, PRIMARY KEY (PRODUTO, COR_PRODUTO, DATA_LIMITE))"
    )
    cols = ["PRODUTO", "COR_PRODUTO", "DATA_LIMITE", *ex_cols, "TOTAL_PLANILHA"]
    placeholders = ",".join("?" for _ in cols)
    cur.fast_executemany = True
    cur.executemany(
        f"INSERT INTO #carga ({','.join(cols)}) VALUES ({placeholders})",
        [[r[c] for c in cols] for r in rows],
    )
    return ex_cols


def rows_as_dicts(cur):
    cols = [d[0] for d in cur.description]
    return [dict(zip(cols, row)) for row in cur.fetchall()]


def generate_processed_workbook(args, summary, sem_dl):
    src = Path(args.xlsx)
    dest = OUT / f"{src.stem} - processada-campanha-{args.id_campanha}-{datetime.now():%Y%m%d-%H%M%S}.xlsx"
    shutil.copy2(src, dest)
    wb = openpyxl.load_workbook(dest)
    ws = wb[wb.sheetnames[0]]
    start = ws.max_column + 1
    extra = [
        "STATUS_GERAL",
        "STATUS_INTEGRACAO",
        "DETALHE_INTEGRACAO",
        "STATUS_VALIDACAO",
        "STATUS_MB_PROD_EXTRA_WEB",
        "STATUS_ENVIA_ATACADO",
        "STATUS_TABELA_DL",
        "STATUS_WISE",
        "DATA_PROCESSAMENTO",
    ]
    for i, h in enumerate(extra, start=start):
        ws.cell(1, i).value = h

    no_dl = {r["PRODUTO"] for r in sem_dl}
    status = summary.get("status", "unknown")
    if status == "success":
        integration_status = "INTEGRADO"
        detail = "Carga diária Linx/WISE executada"
        wise_default = "OK"
    elif status == "success_linx_only":
        integration_status = "LINX_OK_WISE_NAO_EXECUTADO"
        detail = "Carga Linx executada; WISE não executado nesta fase"
        wise_default = "NAO_EXECUTADO"
    elif status == "rolled_back":
        integration_status = "NAO_INTEGRADO"
        detail = f"Execução revertida: {summary.get('error', 'ver relatório')}"
        wise_default = "NAO_EXECUTADO"
    else:
        integration_status = "PARCIAL"
        detail = f"Execução parcial: {summary.get('error', 'ver relatório')}"
        wise_default = "PENDENTE_VALIDACAO"

    processed_at = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    for r in range(2, ws.max_row + 1):
        produto = str(ws.cell(r, 1).value).strip()
        table_status = "SEM_DL" if produto in no_dl else "DL_OK"
        wise_status = "NAO_INTEGRADO_SEM_DL" if produto in no_dl else wise_default
        row_statuses = [integration_status, "OK", "OK", "OK", table_status, wise_status]
        status_geral = "Sucesso" if all(
            value in {"INTEGRADO", "OK", "DL_OK"} for value in row_statuses
        ) else "Erro"
        values = [
            status_geral,
            integration_status,
            detail,
            "OK",
            "OK",
            "OK",
            table_status,
            wise_status,
            processed_at,
        ]
        for i, v in enumerate(values, start=start):
            ws.cell(r, i).value = v
    wb.save(dest)
    summary["processed_workbook"] = str(dest)
    return dest


def run(args):
    OUT.mkdir(parents=True, exist_ok=True)
    data_limite = datetime.strptime(args.data_limite, "%d/%m/%Y").strftime("%Y-%m-%d")
    rows, tam_cols, sheet_errors = read_rows(Path(args.xlsx), data_limite)
    write_csv(OUT / "sheet_validation_errors.csv", sheet_errors)
    if sheet_errors:
        raise SystemExit(f"spreadsheet validation failed: {len(sheet_errors)} errors")

    cn = conn()
    cur = cn.cursor()
    summary = {"data_limite": data_limite, "id_campanha": args.id_campanha, "rows": len(rows)}
    linx_committed = False
    sem_dl = []
    try:
        env = rows_as_dicts(cur.execute("SELECT @@SERVERNAME AS servidor, DB_NAME() AS banco"))[0]
        summary["environment"] = env
        if env["servidor"] != "SRV-SOMADB" or env["banco"] != "SOMA":
            raise SystemExit(f"environment gate failed: {env}")

        ex_cols = make_temp(cur, rows)
        missing_products = rows_as_dicts(cur.execute("""
            SELECT c.PRODUTO FROM (SELECT DISTINCT PRODUTO FROM #carga) c
            WHERE NOT EXISTS (SELECT 1 FROM PRODUTOS p WHERE p.PRODUTO = c.PRODUTO)
            ORDER BY c.PRODUTO
        """))
        missing_colors = rows_as_dicts(cur.execute("""
            SELECT c.PRODUTO, c.COR_PRODUTO FROM (SELECT DISTINCT PRODUTO, COR_PRODUTO FROM #carga) c
            WHERE NOT EXISTS (SELECT 1 FROM PRODUTO_CORES pc WHERE pc.PRODUTO = c.PRODUTO AND pc.COR_PRODUTO = c.COR_PRODUTO)
            ORDER BY c.PRODUTO, c.COR_PRODUTO
        """))
        write_csv(OUT / "missing_products.csv", missing_products)
        write_csv(OUT / "missing_product_colors.csv", missing_colors)
        summary["missing_products"] = len(missing_products)
        summary["missing_product_colors"] = len(missing_colors)
        if missing_products or missing_colors:
            cn.rollback()
            summary["status"] = "blocked_validation"
            (OUT / "final_report.json").write_text(json.dumps(summary, indent=2, default=str))
            raise SystemExit("global product/product-color validation failed")

        diff = " OR ".join([f"ISNULL(m.{c},0) <> c.{c}" for c in ex_cols])
        set_clause = ", ".join([f"m.{c} = c.{c}" for c in ex_cols])
        insert_cols = ["PRODUTO", "COR_PRODUTO", "DATA_LIMITE", *ex_cols]
        cur.execute(f"""
            UPDATE m SET {set_clause}
            FROM MB_PROD_EXTRA_WEB m
            JOIN #carga c ON c.PRODUTO=m.PRODUTO AND c.COR_PRODUTO=m.COR_PRODUTO AND c.DATA_LIMITE=m.DATA_LIMITE
            WHERE {diff}
        """)
        summary["mb_updated"] = cur.rowcount
        cur.execute(f"""
            INSERT INTO MB_PROD_EXTRA_WEB ({','.join(insert_cols)})
            SELECT {','.join('c.' + c for c in insert_cols)}
            FROM #carga c
            WHERE NOT EXISTS (
              SELECT 1 FROM MB_PROD_EXTRA_WEB m
              WHERE m.PRODUTO=c.PRODUTO AND m.COR_PRODUTO=c.COR_PRODUTO AND m.DATA_LIMITE=c.DATA_LIMITE
            )
        """)
        summary["mb_inserted"] = cur.rowcount
        cur.execute("""
            UPDATE p SET ENVIA_ATACADO_INTERNET = 1
            FROM PRODUTOS p
            JOIN (SELECT DISTINCT PRODUTO FROM #carga) c ON c.PRODUTO=p.PRODUTO
            WHERE ISNULL(p.ENVIA_ATACADO_INTERNET,0) <> 1
        """)
        summary["envia_atacado_updated"] = cur.rowcount

        sem_dl = rows_as_dicts(cur.execute("""
            SELECT DISTINCT c.PRODUTO
            FROM #carga c
            WHERE NOT EXISTS (
              SELECT 1 FROM PRODUTOS_PRECOS pp
              WHERE pp.PRODUTO = c.PRODUTO AND pp.CODIGO_TAB_PRECO = 'DL'
            )
            ORDER BY c.PRODUTO
        """))
        write_csv(OUT / "products_without_dl.csv", sem_dl)
        summary["products_without_dl"] = len(sem_dl)

        cur.execute("IF OBJECT_ID('tempdb..#aprov_pc') IS NOT NULL DROP TABLE #aprov_pc")
        cur.execute("""
            SELECT DISTINCT c.PRODUTO, c.COR_PRODUTO
            INTO #aprov_pc
            FROM #carga c
            WHERE EXISTS (
              SELECT 1 FROM PRODUTOS_PRECOS pp
              WHERE pp.PRODUTO = c.PRODUTO AND pp.CODIGO_TAB_PRECO = 'DL'
            )
        """)
        cur.execute("IF OBJECT_ID('tempdb..#expected') IS NOT NULL DROP TABLE #expected")
        cur.execute("""
            SELECT TOP 0 f.*
            INTO #expected
            FROM FN_CONSULTA_SALDO_WEB_WISE('%','%',NULL,NULL) f
        """)
        cur.execute(f"""
            INSERT INTO #expected
            SELECT f.*
            FROM FN_CONSULTA_SALDO_WEB_WISE('%','%',NULL,NULL) f
            JOIN #aprov_pc a ON a.PRODUTO=f.PRODUTO AND a.COR_PRODUTO=f.COR_PRODUTO
            WHERE f.ID_CAMPANHA = {int(args.id_campanha)}
        """)
        counts = rows_as_dicts(cur.execute("""
            SELECT
              (SELECT COUNT(*) FROM #aprov_pc) AS aprovados,
              (SELECT COUNT(*) FROM #expected) AS expected
        """))[0]
        summary["wise_expected_counts"] = counts
        if counts["aprovados"] != counts["expected"]:
            missing_expected = rows_as_dicts(cur.execute("""
                SELECT a.PRODUTO, a.COR_PRODUTO
                FROM #aprov_pc a
                WHERE NOT EXISTS (SELECT 1 FROM #expected e WHERE e.PRODUTO=a.PRODUTO AND e.COR_PRODUTO=a.COR_PRODUTO)
                ORDER BY a.PRODUTO, a.COR_PRODUTO
            """))
            write_csv(OUT / "wise_missing_expected.csv", missing_expected)
            raise RuntimeError("WISE expected stock source does not cover all approved product/colors")

        missing_remote = rows_as_dicts(cur.execute("""
            SELECT e.PRODUTO, e.COR_PRODUTO
            FROM #expected e
            WHERE NOT EXISTS (
              SELECT 1 FROM [WISE_AZURE].[SOMA_LINX].[dbo].[WS_ESTOQUE_PRODUTOS] w
              WHERE w.ID_CAMPANHA=e.ID_CAMPANHA AND w.PRODUTO=e.PRODUTO AND w.COR_PRODUTO=e.COR_PRODUTO
            )
            ORDER BY e.PRODUTO, e.COR_PRODUTO
        """))
        write_csv(OUT / "wise_missing_remote_rows.csv", missing_remote)
        summary["wise_missing_remote_rows"] = len(missing_remote)
        if missing_remote:
            raise RuntimeError("WISE has missing remote rows; this script does not perform linked-server INSERT")

        if args.linx_only:
            cn.commit()
            linx_committed = True
            summary["wise_skipped"] = True
            summary["status"] = "success_linx_only"
            return

        cn.commit()
        linx_committed = True
        cn.autocommit = True

        # Reactivate existing approved rows, then inactivate active rows outside the approved set.
        cur.execute("""
            UPDATE w SET DT_EXCLUSAO = NULL
            FROM [WISE_AZURE].[SOMA_LINX].[dbo].[WS_ESTOQUE_PRODUTOS] w
            JOIN #expected e ON e.ID_CAMPANHA=w.ID_CAMPANHA AND e.PRODUTO=w.PRODUTO AND e.COR_PRODUTO=w.COR_PRODUTO
            WHERE w.ID_CAMPANHA = ? AND w.DT_EXCLUSAO IS NOT NULL
        """, args.id_campanha)
        summary["wise_reactivated"] = cur.rowcount
        cur.execute("""
            UPDATE w SET DT_EXCLUSAO = GETDATE()
            FROM [WISE_AZURE].[SOMA_LINX].[dbo].[WS_ESTOQUE_PRODUTOS] w
            WHERE w.ID_CAMPANHA = ? AND w.DT_EXCLUSAO IS NULL
              AND NOT EXISTS (SELECT 1 FROM #expected e WHERE e.ID_CAMPANHA=w.ID_CAMPANHA AND e.PRODUTO=w.PRODUTO AND e.COR_PRODUTO=w.COR_PRODUTO)
        """, args.id_campanha)
        summary["wise_inactivated"] = cur.rowcount

        mismatch_active = rows_as_dicts(cur.execute("""
            SELECT 'MISSING_ACTIVE' AS tipo, e.PRODUTO, e.COR_PRODUTO
            FROM #expected e
            WHERE NOT EXISTS (
              SELECT 1 FROM [WISE_AZURE].[SOMA_LINX].[dbo].[WS_ESTOQUE_PRODUTOS] w
              WHERE w.ID_CAMPANHA=e.ID_CAMPANHA AND w.PRODUTO=e.PRODUTO AND w.COR_PRODUTO=e.COR_PRODUTO AND w.DT_EXCLUSAO IS NULL
            )
            UNION ALL
            SELECT 'EXTRA_ACTIVE' AS tipo, w.PRODUTO, w.COR_PRODUTO
            FROM [WISE_AZURE].[SOMA_LINX].[dbo].[WS_ESTOQUE_PRODUTOS] w
            WHERE w.ID_CAMPANHA=? AND w.DT_EXCLUSAO IS NULL
              AND NOT EXISTS (SELECT 1 FROM #expected e WHERE e.ID_CAMPANHA=w.ID_CAMPANHA AND e.PRODUTO=w.PRODUTO AND e.COR_PRODUTO=w.COR_PRODUTO)
        """, args.id_campanha))
        write_csv(OUT / "wise_activity_mismatches.csv", mismatch_active)
        summary["wise_activity_mismatches"] = len(mismatch_active)
        if mismatch_active:
            raise RuntimeError("WISE active-set validation failed")

        grade_diff = " OR ".join([f"ISNULL(w.ES{i},0) <> ISNULL(e.D{i},0)" for i in range(1, 17)])
        grade_set = ", ".join([f"w.ES{i} = e.D{i}" for i in range(1, 17)])
        cur.execute(f"""
            UPDATE w SET
              w.LIBERAR_GRADE_WEB = e.LIBERAR_GRADE_WEB,
              w.ESTOQUE = e.SALDO_DISPONIVEL,
              {grade_set},
              w.DATA_PARA_TRANSFERENCIA = GETDATE(),
              w.DT_INTEGRACAO = CAST(GETDATE() AS smalldatetime),
              w.DT_EXCLUSAO = NULL
            FROM [WISE_AZURE].[SOMA_LINX].[dbo].[WS_ESTOQUE_PRODUTOS] w
            JOIN #expected e ON e.ID_CAMPANHA=w.ID_CAMPANHA AND e.PRODUTO=w.PRODUTO AND e.COR_PRODUTO=w.COR_PRODUTO
            WHERE w.ID_CAMPANHA = ?
              AND (ISNULL(w.LIBERAR_GRADE_WEB,'') <> ISNULL(e.LIBERAR_GRADE_WEB,'')
                   OR ISNULL(w.ESTOQUE,0) <> ISNULL(e.SALDO_DISPONIVEL,0)
                   OR {grade_diff})
        """, args.id_campanha)
        summary["wise_stock_updated"] = cur.rowcount

        post = rows_as_dicts(cur.execute("""
            SELECT e.PRODUTO, e.COR_PRODUTO
            FROM #expected e
            JOIN [WISE_AZURE].[SOMA_LINX].[dbo].[WS_ESTOQUE_PRODUTOS] w
              ON e.ID_CAMPANHA=w.ID_CAMPANHA AND e.PRODUTO=w.PRODUTO AND e.COR_PRODUTO=w.COR_PRODUTO
            WHERE w.DT_EXCLUSAO IS NOT NULL OR ISNULL(w.ESTOQUE,0) <> ISNULL(e.SALDO_DISPONIVEL,0)
        """))
        write_csv(OUT / "wise_post_mismatches.csv", post)
        summary["wise_post_mismatches"] = len(post)
        if post:
            raise RuntimeError("WISE stock validation failed")

        cn.commit()
        summary["status"] = "success"
    except Exception as e:
        if not linx_committed:
            cn.rollback()
            summary["status"] = "rolled_back"
        else:
            summary["status"] = "linx_committed_wise_failed"
        summary["error"] = str(e)
        raise
    finally:
        if "status" in summary:
            generate_processed_workbook(args, summary, sem_dl)
        (OUT / "final_report.json").write_text(json.dumps(summary, indent=2, default=str))
        cn.close()
    print(json.dumps(summary, indent=2, default=str))


if __name__ == "__main__":
    p = argparse.ArgumentParser()
    p.add_argument("--xlsx", required=True)
    p.add_argument("--data-limite", required=True)
    p.add_argument("--id-campanha", type=int, required=True)
    p.add_argument("--linx-only", action="store_true")
    run(p.parse_args())
