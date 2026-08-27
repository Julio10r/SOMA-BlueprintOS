// Enriquecimento opcional: busca o campo LINHA/BASE/FABRIC (endpoint `products`) para cada
// produto/cor já coletado por collect.js. Mesma sessão/contexto — não hardcoda marca/região.
// Validado em 2026-08-27 (418 chamadas extra, ~120ms de cadência, sem erro).
'use strict';
const fs = require('fs');
const path = require('path');

const OUT_ROOT = process.env.OUT_ROOT || '/Users/juliocesar/Projects/SOMA-BlueprintOS/downloads/showcase_produtos';
const TOKEN = process.env.SHOWCASE_TOKEN;
if (!TOKEN) { console.error('Faltou SHOWCASE_TOKEN'); process.exit(1); }

const REQUIRED_ENV = [
  'SHOWCASE_BRAND_ID', 'SHOWCASE_COMPANY_ID', 'SHOWCASE_DEPT_ID', 'SHOWCASE_COLLECTION_ID',
  'SHOWCASE_CUSTOMER_ID', 'SHOWCASE_PRICELIST', 'SHOWCASE_PAYMENT', 'SHOWCASE_ORDER_ID',
];
const missing = REQUIRED_ENV.filter((k) => !process.env[k]);
if (missing.length) { console.error(`Faltou contexto de sessão: ${missing.join(', ')}`); process.exit(1); }

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

function qs(p) { return Object.entries(p).map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(v)}`).join('&'); }
function sleep(ms) { return new Promise((r) => setTimeout(r, ms)); }

async function apiGet(pathName, params, tries = 3) {
  const url = `${API}/${pathName}?${qs(params)}`;
  for (let a = 1; a <= tries; a++) {
    try {
      const res = await fetch(url, { headers: { authorization: `Bearer ${TOKEN}`, accept: 'application/json' } });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const text = await res.text();
      if (text.trim().startsWith('<')) throw new Error('sessao expirada');
      return JSON.parse(text);
    } catch (e) { if (a === tries) throw e; await sleep(400 * a); }
  }
}

async function main() {
  const data = JSON.parse(fs.readFileSync(path.join(OUT_ROOT, 'resultado_final.json'), 'utf8'));
  let done = 0;
  for (const item of data) {
    try {
      const detail = await apiGet('products', { ...COMMON, product_Id: item.produto, color_Id: item.cor });
      const d = Array.isArray(detail) ? detail[0] : null;
      if (d) {
        item.line = d.line || '';
        item.base = d.base || '';
        item.fabric = d.fabric || '';
      }
    } catch (e) {
      item.enrichError = String(e.message || e);
    }
    done++;
    if (done % 25 === 0) console.log(`enrich: ${done}/${data.length}`);
    await sleep(120);
  }
  fs.writeFileSync(path.join(OUT_ROOT, 'resultado_final.json'), JSON.stringify(data, null, 2));
  console.log('enrich concluido');
}
main().catch((e) => { console.error(e); process.exit(1); });
