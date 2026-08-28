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

// --- Provenance (política atual, sem exigência de dupla fonte): toda unidade precisa de
// sourceType válido e de referência de origem rastreável (fonte e/ou source_ref). O
// snapshot temporário é fonte legítima e suficiente por si só (TEMP_DISCOVERY_SNAPSHOT,
// VFP_CODE_DISCOVERY, PRODUCT_OWNER_DECISION) quando o conhecimento se origina de
// discovery de código VFP, procedure/artefato técnico, decisão do PO ou discovery
// documentado no próprio snapshot -- não é mais descartado por não aparecer em dois
// documentos de auditoria distintos. ---
const VALID_SOURCE_TYPES = [
  "VFP_CODE_DISCOVERY",
  "DATABASE_PROCEDURE_DISCOVERY",
  "PRODUCT_OWNER_DECISION",
  "ARCHITECTURAL_DECISION",
  "TEMP_DISCOVERY_SNAPSHOT",
];
for (const unit of source.units) {
  assert(
    typeof unit.sourceType === "string" && VALID_SOURCE_TYPES.includes(unit.sourceType),
    `unidade '${unit.key}' deve ter 'sourceType' válido (um de: ${VALID_SOURCE_TYPES.join(", ")})`,
  );
  const ref = (unit.fonte || "") + (unit.source_ref || "");
  assert(ref.length > 0, `unidade '${unit.key}' deve ter 'fonte' e/ou 'source_ref' para rastreabilidade`);
}

// --- Cobertura: pelo menos uma unidade real para cada sourceType usado nesta rodada ---
for (const type of VALID_SOURCE_TYPES) {
  assert(
    source.units.some((u) => u.sourceType === type),
    `deve existir ao menos uma unidade com sourceType '${type}'`,
  );
}

// --- Unidades cuja proveniência é só o snapshot (TEMP_DISCOVERY_SNAPSHOT/VFP_CODE_DISCOVERY/
// PRODUCT_OWNER_DECISION apontando só para o snapshot) devem existir e ser recuperáveis --
// é exatamente o cenário que a política antiga (dupla proveniência obrigatória) rejeitava. ---
const snapshotOnlyUnits = source.units.filter((u) => {
  const ref = (u.fonte || "") + (u.source_ref || "");
  const mentionsAudits =
    ref.includes("docs/audits/Discovery-Fornecedor-CNPJ-Linx-Compras.md") ||
    ref.includes("docs/audits/Arquitetura-Fornecedor-CNPJ-Decisao.md");
  return ref.includes("agents/docs/ai-factory/temp/") && !mentionsAudits;
});
assert(
  snapshotOnlyUnits.length > 0,
  "deve existir ao menos uma unidade cuja única proveniência é o snapshot temporário (política de proveniência única legítima)",
);
assert(
  snapshotOnlyUnits.every((u) =>
    ["VFP_CODE_DISCOVERY", "DATABASE_PROCEDURE_DISCOVERY", "PRODUCT_OWNER_DECISION", "ARCHITECTURAL_DECISION", "TEMP_DISCOVERY_SNAPSHOT"].includes(
      u.sourceType,
    ),
  ),
  "toda unidade com proveniência só-snapshot deve ter sourceType classificado explicitamente",
);

// --- Gaps técnicos (ex.: §12/§9) preservam gapType quando aplicável, nunca viram regra de negócio ---
const gapUnits = source.units.filter((u) => u.gapType);
assert(gapUnits.length > 0, "deve haver ao menos uma unidade classificada com gapType (ARCHITECTURE_GAP/IMPLEMENTATION_CONSTRAINT/KNOWN_LIMITATION)");
for (const u of gapUnits) {
  assert(
    ["ARCHITECTURE_GAP", "IMPLEMENTATION_CONSTRAINT", "KNOWN_LIMITATION"].includes(u.gapType),
    `unidade '${u.key}' tem gapType '${u.gapType}' fora do conjunto permitido`,
  );
}

// --- Decisões do PO preservadas como conhecimento válido, nunca rebaixadas a mera hipótese ---
const poUnits = source.units.filter((u) => u.sourceType === "PRODUCT_OWNER_DECISION");
assert(poUnits.length >= 2, "deve haver ao menos duas unidades de decisão do PO (13-C e 13-D do snapshot)");
for (const u of poUnits) {
  assert(u.proveniencia === "Validado" || u.proveniencia === "Aprovado", `decisão do PO '${u.key}' não deve ser rebaixada a Descoberto/Inferido`);
}

// --- Descobertas de código VFP preservam procedure/comportamento inferido e confiança ---
const vfpUnits = source.units.filter((u) => u.sourceType === "VFP_CODE_DISCOVERY");
assert(vfpUnits.length >= 2, "deve haver ao menos duas unidades de descoberta de código VFP (13-A/13-A.1/13-B do snapshot)");
for (const u of vfpUnits) {
  assert(typeof u.confianca === "string" && u.confianca.length > 0, `unidade VFP '${u.key}' deve preservar grau de confiança`);
}

// --- Artefato gerado deve expor sourceType/gapType/source_ref quando presentes (recuperável pelos Agents) ---
assert(firstRun.includes("Tipo de origem (sourceType)"), "artefato gerado deve expor o tipo de origem de cada unidade");
assert(firstRun.includes("Referência de origem (source_ref)") || firstRun.includes("agents/docs/ai-factory/temp/"), "artefato gerado deve expor referência de origem rastreável, inclusive ao snapshot quando aplicável");

// --- Nenhum fallback SILENCIOSO: o snapshot pode ser citado como fonte legítima agora,
// mas apenas via fonte/source_ref explícito de uma unidade real, nunca como comentário solto ---
assert(
  source.units.some((u) => (u.fonte || "").includes("agents/docs/ai-factory/temp/") || (u.source_ref || "").includes("agents/docs/ai-factory/temp/")),
  "o snapshot temporário deve aparecer como proveniência explícita de ao menos uma unidade real (não é mais proibido, é fonte legítima)",
);

// --- knowledge.md aponta para o artefato canônico (mecanismo real, não apenas comentário/TODO) ---
const knowledgeMd = fs.readFileSync(path.join(repoRoot, ".ai/context/knowledge.md"), "utf8");
assert(
  knowledgeMd.includes("linx-fornecedor-knowledge.generated.md"),
  "knowledge.md deve linkar o artefato canônico gerado (padrão já usado para wise-knowledge.md/linx-wise-daily-integration.md)",
);

console.log(`OK — generate-linx-fornecedor-knowledge.test.js: ${source.units.length} unidades, idempotência e proveniência verificadas.`);
