#!/usr/bin/env node

const assert = require("assert");
const fs = require("fs");
const path = require("path");
const { parseManifest } = require("./validate-agent-manifests");

const repoRoot = path.resolve(__dirname, "../..");
const read = (relativePath) => fs.readFileSync(path.join(repoRoot, relativePath), "utf8");
const manifestText = read("agents/wise-agent/agent.yaml");
const manifest = parseManifest(manifestText, "agents/wise-agent/agent.yaml");
const script = read("scripts/linx_wise_daily_integration.py");
const dailyPrompt = read(".ai/prompts/processar-planilha-integracao-linx-wise.md");
const dailyRunbook = read("applications/mais-compras/docs/operations/LinxWiseDailyIntegrationRunbook.md");

assert.equal(manifest.governance.read_only, true);
assert.equal(manifest.governance.can_execute_write, false);
assert.equal(manifest.governance.can_execute_destructive_operation, false);
assert.equal(manifest.delegation.bypass_allowed, false);
assert.equal(manifest.connections.credential_policy.least_privilege, true);
assert.equal(manifest.connections.credential_policy.privilege_escalation_allowed, false);
assert(manifest.governance.requires_action_proposal_for.some((item) => item.toLowerCase().includes("escrita")));
assert(manifest.governance.approval_required_for.length > 0);
assert.equal(manifest.governance.enforcement_status, "DOCUMENTAL");

const trigger = "Executar integração diária Linx/WISE desta planilha";
assert(dailyPrompt.includes(trigger));
assert(dailyRunbook.includes(trigger));
assert(dailyRunbook.includes("fonte de verdade operacional"));
assert(dailyRunbook.includes("tem precedência"));

for (const variable of ["LINX_PROD_SERVER", "LINX_PROD_DATABASE", "LINX_PROD_USER", "LINX_PROD_PASSWORD"]) {
  assert(script.includes(`os.environ['${variable}']`), `${variable} must come from the local environment`);
}
assert(!/LINX_PROD_(?:USER|PASSWORD)\s*=\s*["'][^"']+["']/.test(script), "No user or password literal may be assigned in the script");
assert(script.includes("pyodbc.connect(cs"));
assert(script.includes("this script does not perform linked-server INSERT"));

console.log("PASS: WISE Agent offline safety invariants");
