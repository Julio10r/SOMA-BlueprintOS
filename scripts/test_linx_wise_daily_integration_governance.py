#!/usr/bin/env python3
"""Offline tests for the governance-facing functions of
linx_wise_daily_integration.py: build_governed_plan(), consult_governed_plan_bridge()
and check_execution_approval(). No pyodbc connection, no real WISE/Linx access —
consult_governed_plan_bridge exercises the real .NET governed-plan CLI (in-memory,
no external connection), which must already be built
(`dotnet build backend/src/BlueprintOS.Api`) for the bridge assertions to run.
"""
import importlib.util
import json
import sys
import types
from pathlib import Path
from datetime import datetime, timedelta, timezone

ROOT = Path(__file__).resolve().parents[1]
MODULE_PATH = ROOT / "scripts" / "linx_wise_daily_integration.py"

# pyodbc is optional/native and irrelevant to the governance-only functions under
# test here; stub it so the module import never requires it to be installed.
if "pyodbc" not in sys.modules:
    sys.modules["pyodbc"] = types.ModuleType("pyodbc")

spec = importlib.util.spec_from_file_location("linx_wise_daily_integration", MODULE_PATH)
wise = importlib.util.module_from_spec(spec)
spec.loader.exec_module(wise)


class Args:
    id_campanha = "TEST-CAMP"
    data_limite = "2026-08-27"


def test_build_governed_plan_matches_bridge_contract():
    plan = wise.build_governed_plan(Args(), rows=[{"a": 1}] * 3, diff_summary={"x": 1})
    required_fields = {
        "requestId", "requestedBy", "agentId", "capability", "environment", "system",
        "resourceType", "resource", "operationIntent", "fields", "filterSummary",
        "expectedAffectedRows", "purpose", "dataClassification", "containsPersonalData",
        "containsSensitivePersonalData", "containsSecrets", "reversibility",
        "connectionProfile", "crossCuttingAgents",
    }
    missing = required_fields - plan.keys()
    assert not missing, f"plan missing required GovernedPlanPayload fields: {missing}"
    assert plan["agentId"] == "wise-agent"
    assert plan["capability"] == "wise-database-write-proposal"
    assert plan["connectionProfile"] == "wise-governed-write"
    print("PASS: build_governed_plan emits the full GovernedPlanPayload contract")


def test_bridge_consultation_and_approval_gate_end_to_end():
    if not wise.GOVERNED_PLAN_CLI_DLL.exists():
        print(f"SKIP: {wise.GOVERNED_PLAN_CLI_DLL} not built — run `dotnet build backend/src/BlueprintOS.Api` first")
        return

    plan = wise.build_governed_plan(Args(), rows=[{"a": 1}] * 3, diff_summary={"x": 1})
    plan["planHash"] = wise._plan_hash({k: v for k, v in plan.items() if k != "planHash"})

    response = wise.consult_governed_plan_bridge(plan)
    assert "error" not in response, f"bridge call failed: {response}"
    assert response["proposalBuild"]["succeeded"] is True
    assert response["policyDecision"]["status"] == "RequiresApproval"
    assert response["policyDecision"]["riskClassification"] == "Yellow"
    print("PASS: consult_governed_plan_bridge reaches the real AIGovernancePolicyEngine")

    # No approval env var / local record at all -> must stay blocked.
    approved, reason = wise.check_execution_approval(plan, response)
    assert approved is False
    assert "GOVERNANCE_APPROVED_EXECUTION" in reason
    print("PASS: check_execution_approval blocks without any approval evidence")

    # A Blocked/Red verdict from the bridge must never be overridable by the
    # local mirror, even if someone forges a GRANTED record + matching hash.
    forged_blocked_response = {
        "proposalBuild": {"succeeded": True},
        "policyDecision": {"status": "Blocked", "riskClassification": "Red", "reasons": ["forged"]},
    }
    approved, reason = wise.check_execution_approval(plan, forged_blocked_response)
    assert approved is False
    assert "blocked this plan" in reason
    print("PASS: a Blocked/Red policy verdict cannot be overridden by the local approval mirror")

    # Bridge unreachable/invalid output must also block, not silently pass.
    approved, reason = wise.check_execution_approval(plan, {"error": "GOVERNED_PLAN_BRIDGE_NOT_BUILT"})
    assert approved is False
    assert "unreachable" in reason
    print("PASS: an unreachable governed-plan bridge blocks execution rather than defaulting to allow")


if __name__ == "__main__":
    test_build_governed_plan_matches_bridge_contract()
    test_bridge_consultation_and_approval_gate_end_to_end()
    print("ALL PASS: linx_wise_daily_integration governance functions")
