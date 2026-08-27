#!/usr/bin/env node

const assert = require("assert");
const fs = require("fs");
const os = require("os");
const path = require("path");
const { RuntimeRegistry, safeProfiles } = require("./runtime-registry");
const { parseManifest } = require("./validate-agent-manifests");

const repoRoot = path.resolve(__dirname, "../..");
const sourceSchema = path.join(repoRoot, "agents/agent.schema.json");
const sourceManifest = path.join(repoRoot, "agents/echo-agent/agent.yaml");

function fixtureRoot(manifests) {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), "runtime-registry-"));
  fs.mkdirSync(path.join(root, "agents"), { recursive: true });
  fs.copyFileSync(sourceSchema, path.join(root, "agents/agent.schema.json"));
  for (const [directory, text] of Object.entries(manifests)) {
    fs.mkdirSync(path.join(root, "agents", directory), { recursive: true });
    fs.writeFileSync(path.join(root, "agents", directory, "agent.yaml"), text);
    const manifest = parseManifest(text, `${directory}/agent.yaml`);
    const declaredPaths = [
      ...manifest.implementation.code_paths, ...manifest.implementation.prompt_paths,
      ...manifest.implementation.context_paths, ...manifest.implementation.runbook_paths,
      ...manifest.implementation.script_paths, ...manifest.implementation.docs_paths,
      ...manifest.knowledge.memory_paths, ...manifest.tests.unit, ...manifest.tests.integration,
      ...manifest.tests.safety, ...manifest.tests.contract,
    ];
    for (const declaredPath of declaredPaths) {
      const target = path.join(root, declaredPath);
      fs.mkdirSync(path.dirname(target), { recursive: true });
      if (!fs.existsSync(target)) fs.writeFileSync(target, "fixture\n");
    }
  }
  return root;
}

function replaceIdentity(text, from, to) {
  return text
    .replace(`id: ${from}`, `id: ${to}`)
    .replace(`name: Echo Agent`, `name: ${to}`)
    .replaceAll(`responsible_agent_id: ${from}`, `responsible_agent_id: ${to}`);
}

const events = [];
const registry = new RuntimeRegistry({ repoRoot, observer: (event) => events.push(event) });
const discovered = registry.discoverAgents();
assert.equal(discovered.discovered_agents.length, 8);
assert.equal(new Set(discovered.discovered_agents.map((agent) => agent.id)).size, 8);
assert.equal(discovered.invalid_agents.length, 0);
assert(registry.listCapabilities().length > 8);

const linx = registry.resolveCapability("linx-database-analysis");
assert.equal(linx.primary_agent, "linx-database-specialist-agent");
assert.equal(linx.delegation_required, true);
assert(linx.cross_cutting_candidates.some((candidate) => candidate.agent_id === "security-lgpd-agent"));
assert.equal(linx.cross_cutting_agents.length, 0);
assert(linx.relationships.workflows.some((workflow) => workflow.includes("Linx/WISE")));
assert.equal(linx.connection_profiles["linx-erp-read-only"].access_intent, "read-only");
assert.equal("password" in linx.connection_profiles["linx-erp-read-only"], false);

const security = registry.resolveCapability("security-privacy-review");
assert.equal(security.status, "CAPABILITY_GAP", "Complementary ownership alone must not become primary");
assert.deepEqual(security.complementary_agents, ["security-lgpd-agent"]);

const gap = registry.resolveCapability("nonexistent-capability");
assert.equal(gap.status, "CAPABILITY_GAP");
assert.equal(gap.direct_bypass_allowed, false);
assert.equal(gap.capability_gap.direct_bypass_allowed, false);

const plan = registry.buildRoutingPlan(["linx-database-analysis", "wise-operational-analysis", "missing-write-capability"]);
assert.equal(plan.routes.length, 3);
assert.equal(plan.gaps.length, 1);
assert.equal(plan.execution_performed, false);
assert.equal(plan.authorization_granted, false);
assert.equal(plan.direct_bypass_allowed, false);

const showcase = registry.resolveCapability("showcase-read-only-collection");
assert.equal(showcase.runtime.implemented, false);
assert.equal(showcase.runtime.interface, null);
assert(showcase.relationships.workflows.length > 0);
assert.equal(registry.getAgent("showcase-agent").type, "operational");

assert(events.some((event) => event.event === "registry.discovery.started"));
assert(events.some((event) => event.event === "registry.discovery.completed"));
assert(events.some((event) => event.event === "registry.routing.resolved"));
assert(events.some((event) => event.event === "registry.routing.gap"));
assert(events.every((event) => !JSON.stringify(event).match(/prompt|sql|password|token|cookie|secret|credential/i)));
assert.deepEqual(safeProfiles({ logical: { environment: "test", access_intent: "read-only", classification: "Internal", username: "blocked", password: "blocked" } }), {
  logical: { environment: "test", access_intent: "read-only", classification: "Internal" },
});

const sourceText = fs.readFileSync(sourceManifest, "utf8");
const before = fs.readFileSync(sourceManifest, "utf8");
const conflictRoot = fixtureRoot({
  "echo-agent": sourceText,
  "second-agent": replaceIdentity(sourceText, "echo-agent", "second-agent"),
});
try {
  const conflictRegistry = new RuntimeRegistry({ repoRoot: conflictRoot });
  const conflict = conflictRegistry.resolveCapability("ai-runtime-echo");
  assert.equal(conflict.status, "ROUTING_CONFLICT");
  assert.equal(conflict.conflicts[0].conflicting_agents.length, 2);
  assert.equal(conflict.direct_bypass_allowed, false);
} finally { fs.rmSync(conflictRoot, { recursive: true, force: true }); }

const complementaryRoot = fixtureRoot({
  "echo-agent": sourceText,
  "second-agent": replaceIdentity(sourceText, "echo-agent", "second-agent")
    .replace("ownership: primary", "ownership: complementary"),
});
try {
  const complementaryRegistry = new RuntimeRegistry({ repoRoot: complementaryRoot });
  const route = complementaryRegistry.resolveCapability("ai-runtime-echo");
  assert.equal(route.status, "ROUTING_RESOLVED");
  assert.deepEqual(route.complementary_agents, ["second-agent"]);
} finally { fs.rmSync(complementaryRoot, { recursive: true, force: true }); }

const inactiveRoot = fixtureRoot({ "echo-agent": sourceText.replace("status: active", "status: deprecated") });
try {
  const route = new RuntimeRegistry({ repoRoot: inactiveRoot }).resolveCapability("ai-runtime-echo");
  assert.equal(route.status, "CAPABILITY_GAP");
} finally { fs.rmSync(inactiveRoot, { recursive: true, force: true }); }

const inactiveConflictRoot = fixtureRoot({
  "echo-agent": sourceText,
  "second-agent": replaceIdentity(sourceText, "echo-agent", "second-agent").replace("status: active", "status: deprecated"),
});
try {
  const inactiveConflictRegistry = new RuntimeRegistry({ repoRoot: inactiveConflictRoot });
  assert.equal(inactiveConflictRegistry.resolveCapability("ai-runtime-echo").status, "ROUTING_RESOLVED");
  assert.equal(inactiveConflictRegistry.detectConflicts().length, 0);
} finally { fs.rmSync(inactiveConflictRoot, { recursive: true, force: true }); }

const invalidRoot = fixtureRoot({ "echo-agent": sourceText.replace("bypass_allowed: false", "bypass_allowed: true") });
try {
  const result = new RuntimeRegistry({ repoRoot: invalidRoot }).discoverAgents();
  assert.equal(result.discovered_agents.length, 0);
  assert.equal(result.invalid_agents.length, 1);
} finally { fs.rmSync(invalidRoot, { recursive: true, force: true }); }

const unknownReferenceRoot = fixtureRoot({ "echo-agent": sourceText.replace("upstream_agents: []", "upstream_agents:\n    - missing-agent") });
try {
  const result = new RuntimeRegistry({ repoRoot: unknownReferenceRoot }).discoverAgents();
  assert.equal(result.discovered_agents.length, 0);
  assert(result.invalid_agents[0].errors.some((error) => error.includes("unknown Agent")));
} finally { fs.rmSync(unknownReferenceRoot, { recursive: true, force: true }); }

const missingSchemaRoot = fixtureRoot({ "echo-agent": sourceText });
try {
  fs.rmSync(path.join(missingSchemaRoot, "agents/agent.schema.json"));
  const result = new RuntimeRegistry({ repoRoot: missingSchemaRoot }).discoverAgents();
  assert.equal(result.discovered_agents.length, 0);
  assert(result.invalid_agents[0].errors.some((error) => error.includes("agent.schema.json is missing")));
} finally { fs.rmSync(missingSchemaRoot, { recursive: true, force: true }); }

assert.equal(fs.readFileSync(sourceManifest, "utf8"), before, "Registry must not mutate canonical manifests");
console.log("PASS: Runtime Registry v1 discovery, routing, gaps, conflicts and safety tests");
