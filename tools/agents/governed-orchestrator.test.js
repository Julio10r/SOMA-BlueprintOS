#!/usr/bin/env node

const assert = require("assert");
const crypto = require("crypto");
const fs = require("fs");
const path = require("path");
const { GovernedOrchestrator } = require("./governed-orchestrator");
const { RuntimeRegistry } = require("./runtime-registry");

const repoRoot = path.resolve(__dirname, "../..");
const manifestPaths = fs.readdirSync(path.join(repoRoot, "agents"), { withFileTypes: true })
  .filter((entry) => entry.isDirectory())
  .map((entry) => path.join(repoRoot, "agents", entry.name, "agent.yaml"))
  .filter(fs.existsSync);
const hashes = () => Object.fromEntries(manifestPaths.map((file) => [file, crypto.createHash("sha256").update(fs.readFileSync(file)).digest("hex")]));
const before = hashes();
const events = [];
let registryCalls = 0;
const registry = new RuntimeRegistry({ repoRoot });
const originalResolve = registry.resolveCapability.bind(registry);
registry.resolveCapability = (capability) => { registryCalls += 1; return originalResolve(capability); };
const orchestrator = new GovernedOrchestrator({ registry, observer: (event) => events.push(event) });

function base(overrides = {}) {
  return {
    request_id: "REQ-TEST-001",
    requested_by: "test-subject-id",
    environment: "Development",
    system: "Showcase",
    resource_type: "ApiEndpoint",
    resource: "catalog",
    operation_intent: "READ",
    requested_capabilities: ["showcase-read-only-collection"],
    fields: [],
    filter_summary: null,
    expected_affected_rows: 100,
    purpose: "collection",
    data_classifications: ["Confidential"],
    contains_personal_data: false,
    contains_sensitive_personal_data: false,
    contains_secrets: false,
    reversibility: "Reversible",
    ...overrides,
  };
}

const readOnly = orchestrator.orchestrate(base());
assert.equal(readOnly.execution_status, "READ_ONLY_PLAN");
assert.equal(readOnly.primary_agents[0], "showcase-agent");
assert.deepEqual(readOnly.cross_cutting_agents, []);
assert.equal(readOnly.routes[0].enforcement_status, "DOCUMENTAL");
assert(readOnly.workflows.some((workflow) => workflow.includes("Showcase")));
assert.equal(readOnly.execution_performed, false);
assert.equal(readOnly.approval_granted, false);
assert.equal(readOnly.direct_bypass_allowed, false);
assert(registryCalls > 0, "Orchestrator must reuse Runtime Registry");

const derivedShowcase = orchestrator.orchestrate(base({ requested_capabilities: [] }));
assert.deepEqual(derivedShowcase.capabilities, ["showcase-read-only-collection"]);
assert.equal(derivedShowcase.capability_source, "DETERMINISTIC_RULE");

const update = orchestrator.orchestrate(base({
  environment: "Production",
  system: "SOMA/Linx",
  resource_type: "DatabaseTable",
  resource: "X",
  operation_intent: "UPDATE",
  requested_capabilities: ["linx-database-analysis", "soma-database-write"],
  filter_summary: "approved product identifiers",
  expected_affected_rows: 25,
  purpose: "architectural test",
  data_classifications: ["Internal"],
  reversibility: "PartiallyReversible",
  runbook_reference: "logical-runbook-reference",
  workflow_reference: "Linx/WISE daily integration",
  connection_profile: "linx-erp-read-only",
}));
assert.equal(update.execution_status, "BLOCKED_CAPABILITY_GAP");
assert.equal(update.routes[0].primary_agent, "linx-database-specialist-agent");
assert(update.capability_gaps.some((gap) => gap.requested_capability === "soma-database-write"));
assert.deepEqual(update.cross_cutting_agents, ["security-lgpd-agent"]);
assert.equal(update.sensitive_action_detected, true);
assert.equal(update.action_proposal_required, true);
assert.equal(update.approval_required_candidate, true);
assert.equal(update.approval_granted, false);
assert.equal(update.direct_bypass_allowed, false);
assert.equal(update.runbook_reference, "logical-runbook-reference");
assert(update.workflows.includes("Linx/WISE daily integration"));
assert(update.connection_profiles.includes("linx-erp-read-only"));
assert.equal(update.credential_resolution_required, true);

const wise = orchestrator.orchestrate(base({
  system: "WISE",
  resource_type: "DatabaseTable",
  resource: "WS_ESTOQUE_PRODUTOS",
  operation_intent: "ANALYZE",
  requested_capabilities: [],
  purpose: "operational analysis",
  data_classifications: ["Internal"],
}));
assert.deepEqual(wise.capabilities, ["wise-operational-analysis"]);
assert.equal(wise.primary_agents[0], "wise-agent");
assert(wise.workflows.some((workflow) => workflow.includes("Linx/WISE")));
assert.deepEqual(wise.cross_cutting_agents, []);

const piiExport = orchestrator.orchestrate(base({
  environment: "Production",
  system: "FictitiousSystem",
  resource_type: "FileExport",
  resource: "fictional-export",
  operation_intent: "EXPORT",
  requested_capabilities: ["fictional-pii-export"],
  expected_affected_rows: 20000,
  purpose: "governance test",
  data_classifications: ["PersonalData"],
  contains_personal_data: true,
}));
assert.deepEqual(piiExport.cross_cutting_agents, ["security-lgpd-agent"]);
assert.equal(piiExport.action_proposal_required, true);
assert.equal(piiExport.execution_status, "BLOCKED_CAPABILITY_GAP");

const destructive = orchestrator.orchestrate(base({
  environment: "Production",
  system: "FictitiousSystem",
  resource_type: "DatabaseTable",
  resource: "fictional-table",
  operation_intent: "TRUNCATE",
  requested_capabilities: ["fictional-destructive-operation"],
  purpose: "governance test",
  data_classifications: ["Internal"],
  reversibility: "Irreversible",
}));
assert.deepEqual(destructive.cross_cutting_agents, ["security-lgpd-agent"]);
assert.equal(destructive.action_proposal_required, true);
assert.equal(destructive.execution_status, "BLOCKED_CAPABILITY_GAP");
assert.equal(destructive.execution_performed, false);

const insufficient = orchestrator.orchestrate(base({
  requested_capabilities: [], system: "Unmapped", operation_intent: "UNKNOWN", purpose: "", data_classifications: [],
}));
assert.equal(insufficient.execution_status, "BLOCKED_CONTEXT_GAP");
assert(insufficient.context_gaps.some((gap) => gap.code === "CAPABILITY_RESOLUTION_CONTEXT_GAP"));

const unknownClassification = orchestrator.orchestrate(base({ data_classifications: ["Unknown"] }));
assert.deepEqual(unknownClassification.cross_cutting_agents, ["security-lgpd-agent"]);
assert.equal(unknownClassification.action_proposal_required, true);

const complementaryRegistry = {
  resolveCapability: (capability) => ({
    requested_capability: capability, status: "ROUTING_RESOLVED", routing_resolved: true,
    primary_agent: "primary-agent", complementary_agents: ["complementary-agent"],
    cross_cutting_candidates: [], capability_gap: null, conflicts: [], relationships: { workflows: [] },
    connection_profiles: {},
  }),
};
const complementaryPlan = new GovernedOrchestrator({ registry: complementaryRegistry }).orchestrate(base());
assert.deepEqual(complementaryPlan.complementary_agents, ["complementary-agent"]);

const conflictRegistry = {
  resolveCapability: (capability) => ({
    requested_capability: capability, status: "ROUTING_CONFLICT", routing_resolved: false,
    primary_agent: null, complementary_agents: [], cross_cutting_candidates: [], capability_gap: null,
    conflicts: [{ type: "ROUTING_CONFLICT", capability }], relationships: { workflows: [] }, connection_profiles: {},
  }),
};
const conflictPlan = new GovernedOrchestrator({ registry: conflictRegistry }).orchestrate(base());
assert.equal(conflictPlan.execution_status, "BLOCKED_ROUTING_CONFLICT");

const exportContextGap = orchestrator.orchestrate(base({ operation_intent: "EXPORT", expected_affected_rows: null }));
assert.equal(exportContextGap.execution_status, "BLOCKED_CONTEXT_GAP");
assert(exportContextGap.context_gaps.some((gap) => gap.code === "CROSS_CUTTING_EXPORT_IMPACT_UNKNOWN"));

assert(events.some((event) => event.event === "orchestrator.plan.started"));
assert(events.some((event) => event.event === "orchestrator.capability.resolved"));
assert(events.some((event) => event.event === "orchestrator.capability.gap"));
assert(events.some((event) => event.event === "orchestrator.crosscutting.resolved"));
assert(events.some((event) => event.event === "orchestrator.context.gap"));
assert(events.some((event) => event.event === "orchestrator.plan.completed"));
assert(events.every((event) => !JSON.stringify(event).match(/prompt|sql|cpf|email|password|token|cookie|secret|credential/i)));
assert.deepEqual(hashes(), before, "Orchestrator must not mutate canonical manifests");
console.log("PASS: Governed Orchestrator v1 context, routing, cross-cutting and safety tests");
