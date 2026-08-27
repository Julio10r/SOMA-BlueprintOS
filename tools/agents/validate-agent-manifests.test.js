#!/usr/bin/env node

const assert = require("assert");
const fs = require("fs");
const path = require("path");
const { parseManifest, validateManifest } = require("./validate-agent-manifests");

const repoRoot = path.resolve(__dirname, "../..");
const sourcePath = path.join(repoRoot, "agents/echo-agent/agent.yaml");
const sourceText = fs.readFileSync(sourcePath, "utf8");
const knownAgentIds = new Set([
  "echo-agent", "knowledge-agent", "security-lgpd-agent", "linx-erp-specialist-agent",
  "linx-database-specialist-agent", "wise-agent", "showcase-agent",
  "agent-factory",
]);

function cloneBase() {
  return structuredClone(parseManifest(sourceText, "fixture"));
}

function expectRejected(name, mutate, extraText = "") {
  const manifest = cloneBase();
  mutate(manifest);
  const errors = validateManifest(manifest, `${sourceText}\n${extraText}`, `fixture:${name}`, knownAgentIds);
  assert(errors.length > 0, `${name} should be rejected`);
}

expectRejected("password", () => {}, "DB_PASSWORD=obviously-fake-value");
expectRejected("token", () => {}, "ACCESS_TOKEN=obviously-fake-token-value");
expectRejected("privilege-escalation", (manifest) => {
  manifest.connections.credential_policy.privilege_escalation_allowed = true;
});
expectRejected("unknown-agent-reference", (manifest) => {
  manifest.capability_ownership["ai-runtime-echo"].responsible_agent_id = "missing-agent";
});
expectRejected("direct-bypass", (manifest) => {
  manifest.delegation.bypass_allowed = true;
});
expectRejected("direct-execution-by-others", (manifest) => {
  manifest.capability_ownership["ai-runtime-echo"].direct_execution_by_others_allowed = true;
});
expectRejected("sensitive-agent-without-governance", (manifest) => {
  delete manifest.governance;
});

console.log("PASS: 7 negative Agent Contract v1.1 validation scenarios rejected");
