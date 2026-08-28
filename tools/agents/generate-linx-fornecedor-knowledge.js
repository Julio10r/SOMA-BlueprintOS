#!/usr/bin/env node

// Gerador determinístico e idempotente do artefato canônico de conhecimento Linx
// (domínio Fornecedor/CNPJ) consumido por linx-erp-specialist-agent e
// linx-database-specialist-agent.
//
// Fonte estruturada (entrada, editável): agents/knowledge/linx-fornecedor-cnpj/linx-fornecedor-knowledge.source.json
// Artefato gerado (saída, NUNCA editar à mão): agents/knowledge/linx-fornecedor-cnpj/linx-fornecedor-knowledge.generated.md
//
// Idempotência: rodar este script duas vezes seguidas, sem alterar o JSON de
// origem, produz o MESMO arquivo de saída byte a byte (nenhum timestamp de
// execução, nenhum valor não-determinístico entra no conteúdo gerado). Cada
// unidade de conhecimento tem uma `key` estável (kebab-case) usada como
// âncora `<!-- linx-knowledge-unit: <key> -->` — chave duplicada é um erro de
// geração (falha explícita), nunca ingestão silenciosa duplicada.
//
// Este script NÃO acessa banco de dados, rede, nem ambiente externo — opera
// apenas sobre o arquivo JSON local (consolidação offline de conhecimento já
// descoberto).

"use strict";

const fs = require("fs");
const path = require("path");

const repoRoot = path.resolve(__dirname, "../..");
const sourcePath = path.join(repoRoot, "agents/knowledge/linx-fornecedor-cnpj/linx-fornecedor-knowledge.source.json");
const outputPath = path.join(repoRoot, "agents/knowledge/linx-fornecedor-cnpj/linx-fornecedor-knowledge.generated.md");

// Tipos de origem legítimos para uma unidade de conhecimento, independente de
// aparecer ou não em dois documentos de auditoria distintos ("dupla
// proveniência" NÃO é mais exigida). O próprio snapshot temporário
// (`TEMP_DISCOVERY_SNAPSHOT`) é fonte legítima e suficiente quando o
// conhecimento vem de discovery documentado nele mesmo.
const VALID_SOURCE_TYPES = [
  "VFP_CODE_DISCOVERY",
  "DATABASE_PROCEDURE_DISCOVERY",
  "PRODUCT_OWNER_DECISION",
  "ARCHITECTURAL_DECISION",
  "TEMP_DISCOVERY_SNAPSHOT",
];

function loadSource() {
  const raw = fs.readFileSync(sourcePath, "utf8");
  const parsed = JSON.parse(raw);
  if (!Array.isArray(parsed.units) || parsed.units.length === 0) {
    throw new Error("Fonte estruturada sem unidades de conhecimento (units vazio ou ausente).");
  }
  const seen = new Set();
  for (const unit of parsed.units) {
    if (!unit.key || typeof unit.key !== "string") {
      throw new Error(`Unidade de conhecimento sem 'key' válida: ${JSON.stringify(unit).slice(0, 120)}`);
    }
    if (seen.has(unit.key)) {
      throw new Error(`Chave de conhecimento duplicada na fonte estruturada: '${unit.key}'. Ingestão idempotente exige chaves únicas.`);
    }
    seen.add(unit.key);
    if (!unit.sourceType || !VALID_SOURCE_TYPES.includes(unit.sourceType)) {
      throw new Error(
        `Unidade '${unit.key}' com 'sourceType' ausente ou inválido. Deve ser um de: ${VALID_SOURCE_TYPES.join(", ")}.`,
      );
    }
    if (!unit.fonte && !unit.source_ref) {
      throw new Error(`Unidade '${unit.key}' precisa de 'fonte' e/ou 'source_ref' para rastreabilidade.`);
    }
  }
  return parsed;
}

function renderList(items) {
  if (!items || items.length === 0) return "_nenhum_";
  return items.map((item) => `\`${item}\``).join(", ");
}

function renderUnit(unit) {
  const lines = [];
  lines.push(`<!-- linx-knowledge-unit: ${unit.key} -->`);
  lines.push(`### ${unit.assunto}`);
  lines.push("");
  lines.push(`- **Chave**: \`${unit.key}\``);
  lines.push(`- **Especialista**: ${unit.especialista}`);
  lines.push(`- **Categoria**: ${unit.categoria}`);
  if (unit.entidade_linx) lines.push(`- **Entidade Linx**: \`${unit.entidade_linx}\``);
  if (unit.tabela) lines.push(`- **Tabela**: \`${unit.tabela}\``);
  if (unit.procedure) lines.push(`- **Procedure**: \`${unit.procedure}\``);
  if (unit.campos && unit.campos.length > 0) lines.push(`- **Campos**: ${renderList(unit.campos)}`);
  lines.push(`- **Proveniência**: ${unit.proveniencia}`);
  lines.push(`- **Confiança**: ${unit.confianca}`);
  if (unit.sourceType) lines.push(`- **Tipo de origem (sourceType)**: ${unit.sourceType}`);
  if (unit.gapType) lines.push(`- **Classificação de gap (gapType)**: ${unit.gapType}`);
  lines.push("");
  lines.push(unit.conteudo);
  lines.push("");
  lines.push(`- **Fonte**: ${unit.fonte}`);
  if (unit.source_ref) lines.push(`- **Referência de origem (source_ref)**: ${unit.source_ref}`);
  if (unit.restricoes) lines.push(`- **Restrições/observações**: ${unit.restricoes}`);
  if (unit.tags && unit.tags.length > 0) lines.push(`- **Tags**: ${renderList(unit.tags)}`);
  lines.push("");
  return lines.join("\n");
}

function render(source) {
  const header = [
    "# Conhecimento Linx Persistido — Fornecedor / CNPJ",
    "",
    "> **ARQUIVO GERADO — NÃO EDITAR À MÃO.** Gerado deterministicamente por " +
      "`tools/agents/generate-linx-fornecedor-knowledge.js` a partir de " +
      "`agents/knowledge/linx-fornecedor-cnpj/linx-fornecedor-knowledge.source.json`. " +
      "Para atualizar o conhecimento, edite o JSON de origem e rode o gerador novamente " +
      "(`node tools/agents/generate-linx-fornecedor-knowledge.js`) — a regeneração é " +
      "idempotente: mesma fonte produz o mesmo arquivo.",
    "",
    `Domínio: \`${source.domain}\`. Descoberto em: ${source.discovery_date}.`,
    "",
    "Consumido por (`agent.yaml` `implementation.context_paths`): `linx-erp-specialist-agent`, `linx-database-specialist-agent`.",
    "",
    "## Proveniência das fontes originais",
    "",
    "Cada unidade abaixo referencia sua fonte original — este arquivo NUNCA é a fonte",
    "primária, é uma consolidação recuperável. As fontes primárias completas são:",
    "",
    ...source.sources.map((s) => `- \`${s.path}\` (\`${s.id}\`)`),
    "",
    "## Rótulos de Proveniência (mesma convenção de `.ai/context/linx-wise-daily-integration.md`)",
    "",
    "- `Descoberto`: fato lido diretamente do schema/procedure/banco (Linx Database Specialist).",
    "- `Inferido`: interpretação funcional ainda não confirmada por especialista humano Visual Linx (Linx ERP Specialist).",
    "- `Validado`/`Aprovado`: promoção formal, exclusiva do fluxo `LinxKnowledgeEntry.Promover` com RBAC dedicado — nenhuma unidade deste arquivo foi promovida além de Descoberto/Inferido.",
    "",
    "## Unidades de Conhecimento",
    "",
  ];
  const body = source.units.map(renderUnit);
  return header.join("\n") + "\n" + body.join("\n");
}

function main() {
  const source = loadSource();
  const rendered = render(source);
  const previous = fs.existsSync(outputPath) ? fs.readFileSync(outputPath, "utf8") : null;
  fs.writeFileSync(outputPath, rendered, "utf8");
  const unchanged = previous === rendered;
  console.log(
    `Gerado ${outputPath} com ${source.units.length} unidade(s) de conhecimento. ` +
      (unchanged ? "Idempotente: conteúdo idêntico à execução anterior." : "Conteúdo atualizado."),
  );
}

if (require.main === module) {
  main();
}

module.exports = { loadSource, render, sourcePath, outputPath };
