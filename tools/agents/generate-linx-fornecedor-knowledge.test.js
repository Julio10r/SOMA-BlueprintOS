#!/usr/bin/env node

const assert = require("assert");
const fs = require("fs");
const path = require("path");
const crypto = require("crypto");
const { execFileSync } = require("child_process");
const { loadSource, render, sourcePath, outputPath } = require("./generate-linx-fornecedor-knowledge");

const repoRoot = path.resolve(__dirname, "../..");
const generatorPath = path.join(__dirname, "generate-linx-fornecedor-knowledge.js");

function sha256(text) {
  return crypto.createHash("sha256").update(text, "utf8").digest("hex");
}

// --- Ingestão real (não um segundo sistema paralelo): a fonte JSON existe e tem unidades ---
const source = loadSource();
assert(Array.isArray(source.units) && source.units.length >= 10, "fonte estruturada deve ter unidades de conhecimento reais");

// --- Ausência de duplicação: chaves são únicas (loadSource já lança se houver duplicata) ---
const keys = source.units.map((u) => u.key);
assert.equal(new Set(keys).size, keys.length, "nenhuma chave de conhecimento pode se repetir");

// --- Idempotência real: rodar o gerador duas vezes produz o MESMO arquivo (hash idêntico) ---
execFileSync("node", [generatorPath], { cwd: repoRoot });
const firstRun = fs.readFileSync(outputPath, "utf8");
const firstHash = sha256(firstRun);

execFileSync("node", [generatorPath], { cwd: repoRoot });
const secondRun = fs.readFileSync(outputPath, "utf8");
const secondHash = sha256(secondRun);

assert.equal(firstHash, secondHash, "gerar o artefato duas vezes a partir da mesma fonte deve produzir bytes idênticos (idempotência)");

// --- render() é uma função pura equivalente ao que foi escrito em disco ---
assert.equal(render(source), firstRun, "render(source) deve corresponder exatamente ao artefato gerado em disco");

// --- Recuperação de termos-chave do snapshot temporário original ---
const requiredTerms = [
  "LX_SEQUENCIAL",
  "CADASTRO_CLI_FOR",
  "LX_CADE",
  "p_RSV_INTEGRACAO_CADASTRO_FORNECEDOR",
  "NOME_CLIFOR",
];
for (const term of requiredTerms) {
  assert(firstRun.includes(term), `artefato gerado deve conter o termo recuperável '${term}'`);
}

// --- Recuperação por Agent Linx ERP (contexto funcional/arquitetura) ---
const erpAgentManifest = fs.readFileSync(path.join(repoRoot, "agents/linx-erp-specialist-agent/agent.yaml"), "utf8");
assert(
  erpAgentManifest.includes("agents/knowledge/linx-fornecedor-cnpj/linx-fornecedor-knowledge.generated.md"),
  "linx-erp-specialist-agent deve declarar o artefato de conhecimento gerado em context_paths",
);
assert(firstRun.includes("Fronteira do futuro Adapter Linx"), "conhecimento funcional (Adapter Linx) deve estar recuperável para o Linx ERP Specialist");
assert(firstRun.includes("DocumentoFiscal"), "decisão de DocumentoFiscal canônico deve estar recuperável para o Linx ERP Specialist");

// --- Recuperação por Agent Linx Banco/Database (schema/trigger/procedure) ---
const dbAgentManifest = fs.readFileSync(path.join(repoRoot, "agents/linx-database-specialist-agent/agent.yaml"), "utf8");
assert(
  dbAgentManifest.includes("agents/knowledge/linx-fornecedor-cnpj/linx-fornecedor-knowledge.generated.md"),
  "linx-database-specialist-agent deve declarar o artefato de conhecimento gerado em context_paths",
);
assert(firstRun.includes("11 triggers"), "conhecimento de schema/trigger deve estar recuperável para o Linx Database Specialist");
assert(firstRun.includes("LXI_CADASTRO_CLI_FOR"), "nome de trigger real deve estar recuperável para o Linx Database Specialist");

// --- Não duplicar fisicamente o mesmo conteúdo nos dois agents: fonte compartilhada única ---
assert.equal(
  (erpAgentManifest.match(/linx-fornecedor-knowledge\.generated\.md/g) || []).length,
  1,
  "linx-erp-specialist-agent deve referenciar o artefato compartilhado uma única vez, não duplicar o conteúdo",
);
assert.equal(
  (dbAgentManifest.match(/linx-fornecedor-knowledge\.generated\.md/g) || []).length,
  1,
  "linx-database-specialist-agent deve referenciar o artefato compartilhado uma única vez, não duplicar o conteúdo",
);
const generatedFileCount = fs
  .readdirSync(path.join(repoRoot, "agents/knowledge/linx-fornecedor-cnpj"))
  .filter((name) => name.endsWith(".generated.md")).length;
assert.equal(generatedFileCount, 1, "deve existir exatamente um artefato gerado compartilhado (fonte única, sem cópia física por agent)");

// --- Provenance: cada unidade rastreável até as fontes originais de discovery/arquitetura, nunca só o snapshot temporário ---
for (const unit of source.units) {
  assert(typeof unit.fonte === "string" && unit.fonte.length > 0, `unidade '${unit.key}' deve ter campo 'fonte'`);
  assert(
    unit.fonte.includes("docs/audits/Discovery-Fornecedor-CNPJ-Linx-Compras.md") ||
      unit.fonte.includes("docs/audits/Arquitetura-Fornecedor-CNPJ-Decisao.md"),
    `unidade '${unit.key}' deve ter proveniência rastreável até um dos dois documentos de auditoria canônicos, não apenas o snapshot temporário`,
  );
}
assert(
  !source.units.some((unit) => unit.fonte.includes("agents/docs/ai-factory/temp/")),
  "nenhuma unidade de conhecimento canônico deve depender do snapshot temporário como fonte de proveniência",
);

// --- Nenhum fallback para o snapshot temporário: o artefato gerado nunca referencia o caminho do snapshot ---
assert(
  !firstRun.includes("agents/docs/ai-factory/temp/"),
  "artefato canônico gerado não deve referenciar o snapshot temporário como fonte",
);

// --- knowledge.md aponta para o artefato canônico (mecanismo real, não apenas comentário/TODO) ---
const knowledgeMd = fs.readFileSync(path.join(repoRoot, ".ai/context/knowledge.md"), "utf8");
assert(
  knowledgeMd.includes("linx-fornecedor-knowledge.generated.md"),
  "knowledge.md deve linkar o artefato canônico gerado (padrão já usado para wise-knowledge.md/linx-wise-daily-integration.md)",
);

console.log(`OK — generate-linx-fornecedor-knowledge.test.js: ${source.units.length} unidades, idempotência e proveniência verificadas.`);
