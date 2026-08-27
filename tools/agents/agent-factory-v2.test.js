#!/usr/bin/env node

const assert = require("assert");
const crypto = require("crypto");
const fs = require("fs");
const os = require("os");
const path = require("path");
const { AgentFactoryV2, OPERATIONS, assertSafeAgentTarget, assertSafeOutput, escapeHtml, statusFromFindings } = require("./agent-factory-v2");
const { parseManifest, validateManifest, validateRepository } = require("./validate-agent-manifests");

const repoRoot = path.resolve(__dirname, "../..");
const factory = new AgentFactoryV2({ repoRoot, clock: () => new Date("2026-08-27T12:00:00.000Z") });
const records = factory.discover();
const knownIds = new Set(records.map((item) => item.manifest.id));
const echoRecord = records.find((item) => item.manifest.id === "echo-agent");

function clone(value) {
  return structuredClone(value);
}

function validateMutation(mutator) {
  const manifest = clone(echoRecord.manifest);
  mutator(manifest);
  return validateManifest(manifest, echoRecord.text, "fixture/agent.yaml", knownIds);
}

function manifestHashes() {
  return Object.fromEntries(factory.discover().map((item) => [
    item.relativePath,
    crypto.createHash("sha256").update(fs.readFileSync(item.manifestPath)).digest("hex"),
  ]));
}

assert.deepEqual(OPERATIONS, ["CREATE", "VALIDATE", "AUDIT", "UPDATE", "REGISTER", "CATALOG", "TEST", "SECURITY_CHECK"]);
assert.equal(records.length, 8, "Factory must discover all eight canonical manifests");
assert.equal(factory.validate().status, "PASS", "Valid repository must pass validation");

assert(validateMutation((manifest) => { delete manifest.governance; }).length > 0, "Invalid Agent must be rejected");
assert(validateMutation((manifest) => { manifest.capability_ownership["ai-runtime-echo"].responsible_agent_id = "missing-agent"; }).some((item) => item.includes("unknown Agent")));
assert(validateMutation((manifest) => { manifest.capability_ownership["ai-runtime-echo"].ownership = "invalid"; }).some((item) => item.includes("invalid ownership")));
assert(validateMutation((manifest) => { manifest.delegation.bypass_allowed = true; }).some((item) => item.includes("bypass_allowed")));
assert(validateMutation((manifest) => { manifest.connections.credential_policy.privilege_escalation_allowed = true; }).some((item) => item.includes("privilege_escalation_allowed")));

const duplicateRoot = fs.mkdtempSync(path.join(os.tmpdir(), "agent-factory-duplicate-"));
try {
  fs.mkdirSync(path.join(duplicateRoot, "agents/first"), { recursive: true });
  fs.mkdirSync(path.join(duplicateRoot, "agents/second"), { recursive: true });
  fs.copyFileSync(path.join(repoRoot, "agents/agent.schema.json"), path.join(duplicateRoot, "agents/agent.schema.json"));
  fs.writeFileSync(path.join(duplicateRoot, "agents/first/agent.yaml"), echoRecord.text);
  fs.writeFileSync(path.join(duplicateRoot, "agents/second/agent.yaml"), echoRecord.text);
  assert(validateRepository(duplicateRoot).errors.some((item) => item.includes("duplicate Agent id")));
} finally {
  fs.rmSync(duplicateRoot, { recursive: true, force: true });
}

const approvalMissing = clone(echoRecord.manifest);
approvalMissing.governance.can_execute_write = true;
approvalMissing.governance.approval_required_for = [];
assert.equal(factory.securityCheck(approvalMissing).status, "FAIL", "Write without approval must fail security check");

const beforeAudit = manifestHashes();
const audit = factory.audit();
assert.equal(audit.agents.length, 8);
assert(audit.agents.some((item) => item.findings.length > 0), "Audit must generate objective findings");
assert.deepEqual(manifestHashes(), beforeAudit, "AUDIT must not modify Agent files");
assert.equal(statusFromFindings([]), "PASS");
assert.equal(statusFromFindings([{ severity: "WARNING" }]), "WARN");
assert.equal(statusFromFindings([{ severity: "ERROR" }]), "FAIL");

const proposed = clone(echoRecord.manifest);
proposed.id = "proposed-agent";
proposed.name = "Proposed Agent";
proposed.version = "1.0.0";
proposed.capability_ownership = {
  "proposed-capability": {
    responsible_agent_id: "proposed-agent", ownership: "primary", delegation_required: true,
    direct_execution_by_others_allowed: false,
  },
};
assert.throws(() => factory.create({ manifest: proposed, capability_gap_evidence: "GAP-TEST", existing_agents_evaluated: ["echo-agent"] }), /explicit human approval/);
const createPreview = factory.create({
  manifest: proposed,
  capability_gap_evidence: "GAP-TEST",
  existing_agents_evaluated: ["echo-agent"],
  authorization: { approved: true, approved_by: "human-reviewer", approved_at: "2026-08-27T12:00:00Z" },
});
assert.equal(createPreview.applied, false);
assert.equal(fs.existsSync(path.join(repoRoot, "agents/proposed-agent/agent.yaml")), false);

const materialUpdate = clone(echoRecord.manifest);
materialUpdate.capability_ownership["new-sensitive-capability"] = {
  responsible_agent_id: "echo-agent", ownership: "primary", delegation_required: true,
  direct_execution_by_others_allowed: false,
};
assert.throws(() => factory.update({ agent_id: "echo-agent", manifest: materialUpdate }), /explicit human approval/);
const bypassUpdate = clone(echoRecord.manifest);
bypassUpdate.delegation.bypass_allowed = true;
assert.throws(() => factory.update({ agent_id: "echo-agent", manifest: bypassUpdate, authorization: { approved: true, approved_by: "human", approved_at: "2026-08-27T12:00:00Z" } }), /cannot enable bypass/);

assert.throws(() => assertSafeAgentTarget("agents/AGENT_CONTRACT.md"), /protected contract source/);
assert.throws(() => assertSafeAgentTarget("agents/echo-agent/knowledge.md"), /only mutate canonical Agent manifests/);
assert.equal(factory.register("agent-factory").status, "PASS");
assert.equal(factory.testPlan("agent-factory").status, "PASS");
assert.equal(factory.catalog().generated, false);
assert.equal(escapeHtml('<script data-test="x">'), "&lt;script data-test=&quot;x&quot;&gt;");
assert.throws(() => factory.catalog({ apply: true }), /explicit human approval/);
assert.throws(() => assertSafeOutput("../../outside.json", "audit"), /Unsafe/);
assert.throws(() => assertSafeOutput("docs/agents/not-json.json", "catalog"), /Unsupported/);
assert.equal(assertSafeOutput("docs/audits/audit.json", "audit"), "docs/audits/audit.json");
assert.equal(fs.existsSync(path.join(repoRoot, "docs/agents/AgentsCatalog.generated.html")), false);
assert(knownIds.has("agent-factory"), "Agent Factory must validate against its own contract");

console.log("PASS: Agent Factory v2 lifecycle, audit and safety tests");
