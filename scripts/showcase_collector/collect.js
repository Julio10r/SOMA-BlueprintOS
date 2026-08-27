// Coletor Showcase (multi-marca/região) — catálogo, grade e fotos.
// Somente leitura: usa a mesma API que o site já usa, com o token e o contexto
// (brand_Id/company_Id/dept_Id/collection_Id/customer_Id/order_Id/...) da sessão autenticada atual.
//
// NADA aqui é FARM-específico. Todo contexto de marca/região vem de variáveis de ambiente
// extraídas da sessão do Chrome no início de CADA execução — ver
// .ai/context/showcase-knowledge.md ("Como Extrair o Contexto da Sessão") e
// docs/operations/ShowcaseAgentRunbook.md.
//
// Validado originalmente em 2026-08-27 (sessão FARM/LATAM, 418 produto/cor, 1193 fotos, 0 erros).
// Este arquivo é a implementação REAL que funcionou naquela execução, apenas parametrizada
// para não fixar marca/região — o algoritmo não foi redesenhado.
'use strict';
const fs = require('fs');
const path = require('path');

const OUT_ROOT = process.env.OUT_ROOT || '/Users/juliocesar/Projects/SOMA-BlueprintOS/downloads/showcase_produtos';
const FOTOS_DIR = path.join(OUT_ROOT, 'fotos');
const CHECKPOINT_CSV = path.join(OUT_ROOT, 'coleta_showcase.csv');
const CATALOG_JSON = path.join(OUT_ROOT, 'catalogo_raw.json');
const ERRORS_JSON = path.join(OUT_ROOT, 'erros.json');

const TOKEN = process.env.SHOWCASE_TOKEN;
if (!TOKEN) { console.error('Faltou SHOWCASE_TOKEN (extraído de localStorage["0.soma|token"] na sessão autenticada).'); process.exit(1); }

// Contexto da sessão — NUNCA hardcodar marca/região aqui. Todos os valores vêm de variáveis de
// ambiente extraídas da sessão atual do Chrome (ver showcase-knowledge.md). Nenhum default de
// marca é assumido: se faltar uma variável obrigatória, o script para e explica o que falta.
const REQUIRED_ENV = [
  'SHOWCASE_BRAND_ID', 'SHOWCASE_COMPANY_ID', 'SHOWCASE_DEPT_ID', 'SHOWCASE_COLLECTION_ID',
  'SHOWCASE_CUSTOMER_ID', 'SHOWCASE_PRICELIST', 'SHOWCASE_PAYMENT', 'SHOWCASE_ORDER_ID',
];
const missing = REQUIRED_ENV.filter((k) => !process.env[k]);
if (missing.length) {
  console.error(`Faltou contexto de sessão: ${missing.join(', ')}. Extraia da sessão autenticada atual (ver .ai/context/showcase-knowledge.md) — nunca hardcode marca/região.`);
  process.exit(1);
}

const API = process.env.SHOWCASE_API_BASE || 'https://wiseapi-gruposoma.azurewebsites.net/service.asmx';
const COMMON = {
  brand_Id: process.env.SHOWCASE_BRAND_ID,
  company_Id: process.env.SHOWCASE_COMPANY_ID,
  dept_Id: process.env.SHOWCASE_DEPT_ID,
  collection_Id: process.env.SHOWCASE_COLLECTION_ID,
  customer_Id: process.env.SHOWCASE_CUSTOMER_ID,
  pricelist: process.env.SHOWCASE_PRICELIST,
  payment: process.env.SHOWCASE_PAYMENT,
  order_Id: process.env.SHOWCASE_ORDER_ID,
  coefficient: process.env.SHOWCASE_COEFFICIENT || '1',
};

function qs(params) {
  return Object.entries(params).map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(v)}`).join('&');
}

async function apiGet(pathName, params, tries = 3) {
  const url = `${API}/${pathName}?${qs(params)}`;
  for (let attempt = 1; attempt <= tries; attempt++) {
    try {
      const res = await fetch(url, {
        headers: {
          authorization: `Bearer ${TOKEN}`,
          accept: 'application/json, text/plain, */*',
        },
      });
      if (res.status === 401 || res.status === 403) {
        throw new SessionExpiredError(`HTTP ${res.status} em ${pathName}`);
      }
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const text = await res.text();
      // A API do Showcase ocasionalmente devolve HTML de login quando a sessão expira.
      if (text.trim().startsWith('<')) throw new SessionExpiredError('Resposta HTML (provável expiração de sessão)');
      return JSON.parse(text);
    } catch (err) {
      if (err instanceof SessionExpiredError) throw err;
      if (attempt === tries) throw err;
      await sleep(500 * attempt);
    }
  }
}

class SessionExpiredError extends Error {}

function sleep(ms) { return new Promise((r) => setTimeout(r, ms)); }

async function fetchFullCatalog() {
  const pageSize = 24;
  let page = 1;
  let totalPages = 1;
  const items = new Map(); // key produto_cor -> vitrine row

  while (page <= totalPages) {
    const data = await apiGet('showcase', {
      size: '', color: '', Base: '', Line: '', Line2: '', Line3: '', Line4: '',
      bestSellers: 'false', ...COMMON, category: '', fabric: '', family: '', subFamily: '',
      favorites: 'false', gender: '', ignoreNoImages: '', keyword: '', orderId: COMMON.order_Id,
      order_by: '1', page_number: String(page), print: '', style: '', subcategories: '',
      subcategory: '', subcollection: '', soldOut: 'false', product_deliveries: '',
      profile: 'Customer', tag: 'false', language: 'PORTUGUES', comprados: 'false', filial: '',
      griffe: '', page_size: String(pageSize), TagFiltro: '', categoriesProducts: '',
      colecoesCompradas: '', deliveriesProducts: '', productType: '', isGriffe: 'false',
      distinct: 'true',
    });
    const vitrine = data.vitrine || [];
    if (vitrine.length === 0) break;
    totalPages = vitrine[0].totalPages || totalPages;
    for (const row of vitrine) {
      const key = `${row.product_Id}_${row.color_Id}`;
      if (!items.has(key)) items.set(key, row);
    }
    console.log(`[catalogo] pagina ${page}/${totalPages} — itens únicos até agora: ${items.size}`);
    page++;
    await sleep(300);
  }
  return Array.from(items.values());
}

// As imagens vivem em blob storage sob uma pasta por marca:
//   https://wiseimagessoma.blob.core.windows.net/soma/imagens/{MARCA}/produtos/{PRODUTO}-{COR}-{N}.jpg
// A pasta {MARCA} é DESCOBERTA a partir da própria URL imageShowcase que a API já devolveu para
// aquele produto/cor no catálogo — nunca hardcodada. Itens sem nenhuma URL imageShowcase no
// catálogo genuinamente não têm foto cadastrada (confirmado em 2026-08-27: 94/418 casos) — não são
// erro de coleta, e são pulados sem tentar advinhar uma pasta.
function extractImageBase(row) {
  const anyUrl = row.imageShowcase || row.imageShowcase_Back || row.imageShowcase_Look;
  if (!anyUrl) return null;
  const match = anyUrl.match(/\/imagens\/([^/]+)\/produtos\//);
  return match ? `https://wiseimagessoma.blob.core.windows.net/soma/imagens/${match[1]}/produtos` : null;
}

async function probeImages(base, produto, cor, maxN = 12) {
  const found = [];
  let n = 1;
  let misses = 0;
  while (n <= maxN && misses < 2) {
    const url = `${base}/${produto}-${cor}-${n}.jpg`;
    try {
      const res = await fetch(url, { method: 'HEAD' });
      if (res.ok) {
        found.push({ n, url, contentLength: res.headers.get('content-length') });
        misses = 0;
      } else {
        misses++;
      }
    } catch (e) {
      misses++;
    }
    n++;
  }
  return found;
}

async function downloadImage(url, destPath) {
  const res = await fetch(url);
  if (!res.ok) throw new Error(`HTTP ${res.status} ao baixar imagem`);
  const buf = Buffer.from(await res.arrayBuffer());
  fs.writeFileSync(destPath, buf);
  return buf.length;
}

const LETTERS = 'abcdefghijklmnopqrstuvwxyz';

function loadCheckpoint() {
  const map = new Map();
  if (!fs.existsSync(CHECKPOINT_CSV)) return map;
  const lines = fs.readFileSync(CHECKPOINT_CSV, 'utf8').split('\n').filter(Boolean);
  for (let i = 1; i < lines.length; i++) {
    const [produto, cor, ordem_foto, url, arquivo_local, status] = lines[i].split(',');
    map.set(`${produto}_${cor}_${ordem_foto}`, { status, arquivo_local });
  }
  return map;
}

function appendCheckpoint(row) {
  const header = 'produto,cor,ordem_foto,url,arquivo_local,status,data_download,erro\n';
  if (!fs.existsSync(CHECKPOINT_CSV)) fs.writeFileSync(CHECKPOINT_CSV, header);
  const line = [row.produto, row.cor, row.ordem_foto, row.url, row.arquivo_local, row.status, row.data_download, (row.erro || '').replace(/,/g, ';')].join(',') + '\n';
  fs.appendFileSync(CHECKPOINT_CSV, line);
}

async function main() {
  fs.mkdirSync(FOTOS_DIR, { recursive: true });
  const checkpoint = loadCheckpoint();
  const errors = [];

  console.log('[1/8] Catálogo: iniciando paginação completa...');
  const catalog = await fetchFullCatalog();
  fs.writeFileSync(CATALOG_JSON, JSON.stringify(catalog, null, 2));
  console.log(`[2/8] Catálogo mapeado: ${catalog.length} produto/cor únicos.`);

  const results = [];
  let processed = 0;
  let photosDownloaded = 0;
  let errorCount = 0;

  for (const row of catalog) {
    const produto = row.product_Id;
    const cor = row.color_Id;
    try {
      // grade / estoque
      let stock = [];
      try {
        stock = await apiGet('stock', {
          ...COMMON, product_Id: produto, colorId: cor,
        });
      } catch (e) {
        if (e instanceof SessionExpiredError) throw e;
        errors.push({ produto, cor, etapa: 'stock', erro: String(e.message || e) });
      }

      // fotos: probe pelo padrão de URL, com a pasta de marca descoberta a partir do próprio item
      const imageBase = extractImageBase(row);
      const imgs = imageBase ? await probeImages(imageBase, produto, cor) : [];
      const fotoFiles = [];
      let ordem = 0;
      for (const img of imgs) {
        const letra = LETTERS[ordem] || `x${ordem}`;
        const filename = `${produto}_${cor}_${letra}.jpg`;
        const destPath = path.join(FOTOS_DIR, filename);
        const cpKey = `${produto}_${cor}_${ordem}`;
        const already = checkpoint.get(cpKey);
        let status = 'ok';
        let erro = '';
        if (already && already.status === 'ok' && fs.existsSync(destPath) && fs.statSync(destPath).size > 0) {
          fotoFiles.push(filename);
        } else {
          try {
            const size = await downloadImage(img.url, destPath);
            if (size === 0) throw new Error('arquivo vazio');
            fotoFiles.push(filename);
            photosDownloaded++;
          } catch (e) {
            status = 'erro';
            erro = String(e.message || e);
            errorCount++;
            errors.push({ produto, cor, etapa: 'download_foto', erro });
          }
          appendCheckpoint({
            produto, cor, ordem_foto: ordem, url: img.url, arquivo_local: `fotos/${filename}`,
            status, data_download: new Date().toISOString(), erro,
          });
          await sleep(150);
        }
        ordem++;
      }

      results.push({
        produto,
        descricao: row.product_name,
        cor,
        descCor: row.colorDescription,
        gender: row.gender,
        category: row.category_name,
        subcategory: row.subcategory_name,
        grid: row.grid,
        price: row.price,
        composition: row.composition,
        stock,
        fotos: fotoFiles,
      });
    } catch (e) {
      if (e instanceof SessionExpiredError) {
        console.error('SESSAO EXPIRADA. Parando coleta. Faça login novamente e reexecute.');
        fs.writeFileSync(ERRORS_JSON, JSON.stringify(errors, null, 2));
        fs.writeFileSync(path.join(OUT_ROOT, 'resultado_parcial.json'), JSON.stringify(results, null, 2));
        process.exit(2);
      }
      errorCount++;
      errors.push({ produto, cor, etapa: 'geral', erro: String(e.message || e) });
    }
    processed++;
    if (processed % 10 === 0 || processed === catalog.length) {
      console.log(`Processados: ${processed}/${catalog.length} | Fotos baixadas: ${photosDownloaded} | Erros: ${errorCount}`);
    }
  }

  fs.writeFileSync(path.join(OUT_ROOT, 'resultado_final.json'), JSON.stringify(results, null, 2));
  fs.writeFileSync(ERRORS_JSON, JSON.stringify(errors, null, 2));
  console.log(`[6/8] Coleta finalizada. Total produto/cor: ${results.length}. Fotos baixadas: ${photosDownloaded}. Erros: ${errorCount}`);
}

main().catch((e) => {
  console.error('Falha fatal:', e);
  process.exit(1);
});
