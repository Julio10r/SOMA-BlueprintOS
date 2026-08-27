#!/usr/bin/env node

const assert = require("assert");
const { GovernedOrchestrator } = require("./governed-orchestrator");
const { invokeGovernedPlanCli } = require("./governance-bridge");

const orchestrator = new GovernedOrchestrator();

const input = {
  request_id: "REQ-BRIDGE-E2E-001",
  requested_by: "subject-requester-001",
  environment: "Production",
  system: "SOMA/Linx",
  resource_type: "DatabaseTable",
  resource: "PRODUTOS",
  operation_intent: "UPDATE",
  requested_capabilities: ["soma-database-write-proposal"],
  fields: ["ENVIA_ATACADO_INTERNET"],
  reversibility: "Reversible",
  filter_summary: "validated fictional set",
  expected_affected_rows: 417,
  purpose: "validated integration",
  data_classifications: ["Internal"],
  connection_profile: "linx-erp-governed-write",
};

const bridgeResult = orchestrator.buildActionProposalPayload(input);
assert.equal(bridgeResult.eligible, true, "orchestrator plan must be READY_FOR_GOVERNANCE for this fixture");

const { exitCode, response } = invokeGovernedPlanCli(bridgeResult.payload);
assert.equal(exitCode, 0, `governed-plan CLI must exit 0, got stderr-visible failure: ${JSON.stringify(response)}`);
assert.equal(response.requestId, "REQ-BRIDGE-E2E-001");
assert.equal(response.proposalBuild.succeeded, true);
assert.equal(response.policyDecision.status, "RequiresApproval");
assert.equal(response.policyDecision.riskClassification, "Yellow");
assert(response.approvalRequest, "an Update proposal must produce an approval request");
assert.equal(response.liveExecution, "BLOCKED");

// A malformed enum in the payload must surface as a real cross-process failure,
// not a silently-defaulted "success".
const badResult = invokeGovernedPlanCli({ ...bridgeResult.payload, operationIntent: "NOT_A_REAL_INTENT" });
assert.equal(badResult.exitCode, 1);
assert.equal(badResult.response.error, "INVALID_ENUM_VALUE");

// A non-eligible orchestrator plan must never reach the bridge at all.
const nonEligible = orchestrator.buildActionProposalPayload({ ...input, requested_capabilities: [] });
assert.equal(nonEligible.eligible, false);

// Green read-only path through the same cross-process CLI: Allowed, no approval request.
const readOnlyResult = orchestrator.buildActionProposalPayload({
  ...input,
  operation_intent: "READ",
  requested_capabilities: ["soma-database-read-proposal"],
  fields: [],
  reversibility: "Reversible",
  connection_profile: "linx-erp-governed-read",
});
assert.equal(readOnlyResult.eligible, false, "READ alone is not action-proposal-eligible by orchestrator rules");
// The read-only capability is exercised directly against the .NET side instead (see
// GovernedWriteStackTests.ReadOnly_Adapter_Allows_Select_Without_Approval_When_Policy_Green),
// because the Orchestrator's requiresActionProposal() only forces a proposal for
// mutating/export/sensitive intents — a plain READ never reaches READY_FOR_GOVERNANCE by design,
// so there is nothing eligible for this bridge to carry for that case.

console.log("PASS: JS orchestrator -> governed-plan CLI -> .NET GovernedPlanBridge end-to-end offline bridge test");
