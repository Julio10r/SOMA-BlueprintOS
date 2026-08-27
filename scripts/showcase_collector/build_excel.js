// Gera catalogo_showcase.xlsx a partir de resultado_final.json + fotos/ já baixadas.
// Layout validado em 2026-08-27 (com correção de PRECO_VENDA — não é FOB, o Showcase não expõe FOB).
// Não redesenhar sem necessidade real: este é o layout que o Product Owner validou.
'use strict';
const fs = require('fs');
const path = require('path');
const ExcelJS = require('exceljs');

const OUT_ROOT = process.env.OUT_ROOT || '/Users/juliocesar/Projects/SOMA-BlueprintOS/downloads/showcase_produtos';
const FOTOS_DIR = path.join(OUT_ROOT, 'fotos');

async function main() {
  const data = JSON.parse(fs.readFileSync(path.join(OUT_ROOT, 'resultado_final.json'), 'utf8'));
  const errorsRaw = fs.existsSync(path.join(OUT_ROOT, 'erros.json'))
    ? JSON.parse(fs.readFileSync(path.join(OUT_ROOT, 'erros.json'), 'utf8'))
    : [];

  // Determina o maior número de tamanhos entre todos os itens (para colunas TAM_1..TAM_N)
  let maxSizes = 0;
  for (const item of data) maxSizes = Math.max(maxSizes, (item.stock || []).length);
  maxSizes = Math.max(maxSizes, 8);

  const wb = new ExcelJS.Workbook();
  wb.creator = 'Showcase Agent';
  wb.created = new Date(0); // determinístico

  // ===== Aba principal: Catalogo =====
  const ws = wb.addWorksheet('Catalogo');
  const baseCols = [
    { header: 'FOTO', key: 'foto', width: 14 },
    { header: 'PROD', key: 'prod', width: 12 },
    { header: 'COR_PROD', key: 'cor', width: 12 },
    { header: 'CHAVE', key: 'chave', width: 16 },
    { header: 'DESC_PRODUTO', key: 'descProduto', width: 32 },
    { header: 'DESC_COR_PRODUTO', key: 'descCor', width: 26 },
    { header: 'GRU', key: 'gru', width: 14 },
    { header: 'GRUP', key: 'grup', width: 14 },
    { header: 'LINHA', key: 'linha', width: 14 },
    { header: 'COMPOSICAO', key: 'composicao', width: 30 },
    { header: 'PRECO_VENDA', key: 'preco', width: 12 },
    { header: 'GRADE', key: 'grade', width: 20 },
  ];
  const tamCols = Array.from({ length: maxSizes }, (_, i) => ({ header: `TAM_${i + 1}`, key: `tam${i + 1}`, width: 8 }));
  ws.columns = [...baseCols, ...tamCols];
  ws.getRow(1).font = { bold: true };
  ws.getRow(1).alignment = { vertical: 'middle', horizontal: 'center' };

  const ROW_HEIGHT = 60;

  for (const item of data) {
    const stock = item.stock || [];
    const gradeDesc = stock.length ? stock.map((s) => s.size).join('-') : (item.grid || '');
    const rowData = {
      prod: item.produto,
      cor: item.cor,
      chave: `${item.produto}-${item.cor}`,
      descProduto: item.descricao,
      descCor: item.descCor,
      gru: item.category,
      grup: item.subcategory,
      linha: item.line || '',
      composicao: item.composition || '',
      preco: item.price || '',
      grade: gradeDesc,
    };
    stock.forEach((s, i) => { rowData[`tam${i + 1}`] = s.quantity; });
    const row = ws.addRow(rowData);
    row.height = ROW_HEIGHT;

    const primeiraFoto = (item.fotos || [])[0];
    if (primeiraFoto) {
      const imgPath = path.join(FOTOS_DIR, primeiraFoto);
      if (fs.existsSync(imgPath)) {
        try {
          const ext = path.extname(imgPath).slice(1).toLowerCase();
          const imgId = wb.addImage({ filename: imgPath, extension: ext === 'jpg' ? 'jpeg' : ext });
          ws.addImage(imgId, {
            tl: { col: 0.05, row: row.number - 1 + 0.05 },
            ext: { width: 60, height: 78 },
          });
        } catch (e) {
          // imagem inválida/corrompida — ignora, mantém a linha
        }
      }
    }
  }

  // ===== Aba Fotos =====
  const wsFotos = wb.addWorksheet('Fotos');
  wsFotos.columns = [
    { header: 'PRODUTO', key: 'produto', width: 12 },
    { header: 'COR', key: 'cor', width: 12 },
    { header: 'ORDEM', key: 'ordem', width: 8 },
    { header: 'ARQUIVO', key: 'arquivo', width: 30 },
    { header: 'URL_ORIGINAL', key: 'url', width: 70 },
    { header: 'STATUS', key: 'status', width: 10 },
  ];
  wsFotos.getRow(1).font = { bold: true };
  const letters = 'abcdefghijklmnopqrstuvwxyz';
  for (const item of data) {
    (item.fotos || []).forEach((filename, idx) => {
      wsFotos.addRow({
        produto: item.produto,
        cor: item.cor,
        ordem: letters[idx] || idx,
        arquivo: `fotos/${filename}`,
        url: '',
        status: 'ok',
      });
    });
  }

  // ===== Aba Erros =====
  const wsErros = wb.addWorksheet('Erros');
  wsErros.columns = [
    { header: 'PRODUTO', key: 'produto', width: 12 },
    { header: 'COR', key: 'cor', width: 12 },
    { header: 'ETAPA', key: 'etapa', width: 20 },
    { header: 'ERRO', key: 'erro', width: 50 },
  ];
  wsErros.getRow(1).font = { bold: true };
  for (const e of errorsRaw) wsErros.addRow(e);

  // ===== Aba Resumo =====
  const wsResumo = wb.addWorksheet('Resumo');
  const produtosUnicos = new Set(data.map((d) => d.produto)).size;
  const totalFotos = data.reduce((acc, d) => acc + (d.fotos || []).length, 0);
  const semFoto = data.filter((d) => !d.fotos || d.fotos.length === 0).length;
  const semStock = data.filter((d) => !d.stock || d.stock.length === 0).length;

  wsResumo.columns = [{ header: 'Métrica', key: 'k', width: 40 }, { header: 'Valor', key: 'v', width: 30 }];
  wsResumo.getRow(1).font = { bold: true };
  wsResumo.addRows([
    { k: 'Produtos únicos', v: produtosUnicos },
    { k: 'Produto/cores encontrados (Showcase)', v: data.length },
    { k: 'Fotos encontradas/baixadas', v: totalFotos },
    { k: 'Produto/cores sem foto no Showcase', v: semFoto },
    { k: 'Produto/cores sem estoque retornado', v: semStock },
    { k: 'Erros registrados', v: errorsRaw.length },
    { k: 'Fonte de estoque', v: 'API Showcase (endpoint stock) — enriquecer com WISE Agent quando SQL disponível' },
  ]);

  const xlsxPath = path.join(OUT_ROOT, 'catalogo_showcase.xlsx');
  await wb.xlsx.writeFile(xlsxPath);
  console.log('Excel gerado em:', xlsxPath);
}

main().catch((e) => { console.error(e); process.exit(1); });
