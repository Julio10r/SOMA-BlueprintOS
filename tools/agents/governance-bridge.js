#!/usr/bin/env node

// The real process boundary for WAVE A: takes the payload produced by
// GovernedOrchestrator.buildActionProposalPayload() (tools/agents/governed-orchestrator.js)
// and hands it, as JSON over stdin, to the `governed-plan` CLI command exposed by
// BlueprintOS.Api (applications/mais-compras/backend/src/BlueprintOS.Api/Governance/GovernedPlanCliHandler.cs),
// which converts it into a real ActionProposal via GovernedPlanBridge and runs it
// through the real AIGovernancePolicyEngine/ApprovalPolicy. No HTTP endpoint, no
// network call — a deterministic, testable process boundary, chosen because it is
// the smallest bridge that actually crosses the JS/.NET runtime boundary instead of
// only sharing a paper contract between the two sides.
//
// This module never grants authorization and never touches a real database or
// external system — the CLI it invokes runs entirely in-memory (see
// InMemoryGovernedPlanStores.cs) and always reports LIVE_EXECUTION as blocked.

const path = require("path");
const { spawnSync } = require("child_process");
const fs = require("fs");

const repoRoot = path.resolve(__dirname, "../..");
const apiProjectDir = path.join(repoRoot, "applications/mais-compras/backend/src/BlueprintOS.Api");
const apiDllPath = path.join(apiProjectDir, "bin/Debug/net9.0/BlueprintOS.Api.dll");

function ensureBuilt({ apiProjectDir: projectDir = apiProjectDir, dllPath = apiDllPath } = {}) {
  if (fs.existsSync(dllPath)) return dllPath;
  const build = spawnSync("dotnet", ["build", projectDir, "--nologo", "-v", "quiet"], { encoding: "utf8" });
  if (build.status !== 0 || !fs.existsSync(dllPath)) {
    throw new Error(`GOVERNANCE_BRIDGE_BUILD_FAILED: could not build ${projectDir} (exit ${build.status}). stderr: ${build.stderr}`);
  }
  return dllPath;
}

// Invokes the governed-plan CLI with the given payload and returns the parsed
// JSON result. Never throws for a governed Blocked/RequiresApproval outcome —
// that is a correct result of this bridge, not a failure of it. Throws only if
// the process boundary itself is broken (dotnet missing, dll missing/unbuilt,
// malformed stdout).
function invokeGovernedPlanCli(payload, { dllPath = apiDllPath, timeoutMs = 30000 } = {}) {
  const resolvedDll = ensureBuilt({ dllPath });
  const result = spawnSync("dotnet", [resolvedDll, "governed-plan"], {
    input: JSON.stringify(payload),
    encoding: "utf8",
    timeout: timeoutMs,
  });
  if (result.error) {
    throw new Error(`GOVERNANCE_BRIDGE_PROCESS_ERROR: ${result.error.message}`);
  }
  const stdout = (result.stdout || "").trim();
  if (!stdout) {
    throw new Error(`GOVERNANCE_BRIDGE_EMPTY_OUTPUT: exit ${result.status}, stderr: ${result.stderr}`);
  }
  let parsed;
  try {
    parsed = JSON.parse(stdout.split("\n").pop());
  } catch (error) {
    throw new Error(`GOVERNANCE_BRIDGE_INVALID_OUTPUT: ${error.message}. stdout: ${stdout}`);
  }
  return { exitCode: result.status, response: parsed };
}

module.exports = { apiDllPath, apiProjectDir, ensureBuilt, invokeGovernedPlanCli };
