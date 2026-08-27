#!/usr/bin/env node

const { RuntimeRegistry } = require("./runtime-registry");
const { validateActionContext } = require("./structured-action-context");

const MUTATING_INTENTS = new Set(["CREATE", "UPDATE", "DELETE", "TRUNCATE", "CONFIGURE", "EXECUTE_WORKFLOW"]);
const DESTRUCTIVE_INTENTS = new Set(["DELETE", "TRUNCATE"]);
const GOVERNED_CLASSIFICATIONS = new Set(["Unknown", "PersonalData", "SensitivePersonalData", "SecretCredential"]);

function unique(values) {
  return [...new Set(values.filter(Boolean))].sort();
}

class GovernedOrchestrator {
  constructor(options = {}) {
    this.registry = options.registry || new RuntimeRegistry({ repoRoot: options.repoRoot });
    this.observer = options.observer || (() => {});
  }

  emit(event, details = {}) {
    this.observer({ event, ...details });
  }

  validateActionContext(input) {
    return validateActionContext(input);
  }

  resolveRequestedCapabilities(context) {
    if (context.requested_capabilities.length) {
      return { capabilities: [...context.requested_capabilities], source: "EXPLICIT", context_gaps: [] };
    }
    const system = context.system.toLowerCase();
    const purpose = context.purpose.toLowerCase();
    if (system.includes("showcase") && context.operation_intent === "READ" && /collection|coleta/.test(purpose)) {
      return { capabilities: ["showcase-read-only-collection"], source: "DETERMINISTIC_RULE", context_gaps: [] };
    }
    if (system.includes("wise") && ["READ", "ANALYZE"].includes(context.operation_intent)) {
      return { capabilities: ["wise-operational-analysis"], source: "DETERMINISTIC_RULE", context_gaps: [] };
    }
    if ((system.includes("soma") || system.includes("linx")) && context.operation_intent === "ANALYZE") {
      return { capabilities: ["linx-database-analysis"], source: "DETERMINISTIC_RULE", context_gaps: [] };
    }
    return {
      capabilities: [],
      source: "UNRESOLVED",
      context_gaps: [{ field: "requested_capabilities", code: "CAPABILITY_RESOLUTION_CONTEXT_GAP" }],
    };
  }

  resolveCrossCutting(context, routes) {
    const candidateIds = unique(routes.flatMap((route) => route.cross_cutting_candidates || []).map((candidate) => candidate.agent_id));
    const reasons = [];
    const contextGaps = [];

    if (context.contains_personal_data || context.data_classifications.includes("PersonalData")) reasons.push("PERSONAL_DATA");
    if (context.contains_sensitive_personal_data || context.data_classifications.includes("SensitivePersonalData")) reasons.push("SENSITIVE_PERSONAL_DATA");
    if (context.contains_secrets || context.data_classifications.includes("SecretCredential")) reasons.push("SECRET_INVOLVEMENT");
    if (context.data_classifications.includes("Unknown")) reasons.push("UNKNOWN_DATA_CLASSIFICATION_POLICY_SIGNAL");
    if (MUTATING_INTENTS.has(context.operation_intent)) reasons.push("WRITE_OR_EXTERNAL_EFFECT");
    if (DESTRUCTIVE_INTENTS.has(context.operation_intent)) reasons.push("DESTRUCTIVE_OPERATION");
    if (context.operation_intent === "EXPORT") {
      if (context.expected_affected_rows === null) contextGaps.push({ field: "expected_affected_rows", code: "CROSS_CUTTING_EXPORT_IMPACT_UNKNOWN" });
      else if (context.expected_affected_rows > 10000 || context.contains_personal_data || context.contains_sensitive_personal_data) reasons.push("MATERIAL_OR_SENSITIVE_EXPORT");
    }

    const securityApplicable = candidateIds.includes("security-lgpd-agent") && reasons.length > 0;
    const agents = securityApplicable ? ["security-lgpd-agent"] : [];
    this.emit("orchestrator.crosscutting.resolved", {
      cross_cutting_agents: agents.length,
      reason_categories: unique(reasons),
      context_gaps: contextGaps.length,
    });
    return { agents, candidates: candidateIds, reasons: unique(reasons), context_gaps: contextGaps };
  }

  requiresActionProposal(context) {
    return MUTATING_INTENTS.has(context.operation_intent)
      || context.operation_intent === "EXPORT"
      || context.contains_personal_data
      || context.contains_sensitive_personal_data
      || context.contains_secrets
      || context.data_classifications.some((classification) => GOVERNED_CLASSIFICATIONS.has(classification));
  }

  buildGovernedPlan(input) {
    const validation = this.validateActionContext(input);
    const context = validation.context;
    this.emit("orchestrator.plan.started", { request_id: context.request_id || "MISSING" });

    const capabilityResolution = this.resolveRequestedCapabilities(context);
    const routes = capabilityResolution.capabilities.map((capability) => {
      const route = this.registry.resolveCapability(capability);
      this.emit(route.capability_gap ? "orchestrator.capability.gap" : "orchestrator.capability.resolved", {
        requested_capability: capability,
        status: route.status,
      });
      return route;
    });
    const crossCutting = this.resolveCrossCutting(context, routes);
    const contextGaps = [...validation.context_gaps, ...capabilityResolution.context_gaps, ...crossCutting.context_gaps];
    const capabilityGaps = routes.filter((route) => route.capability_gap).map((route) => route.capability_gap);
    const routingConflicts = routes.flatMap((route) => route.conflicts || []);
    const actionProposalRequired = this.requiresActionProposal(context);
    const sensitiveActionDetected = actionProposalRequired || DESTRUCTIVE_INTENTS.has(context.operation_intent);

    let executionStatus = "READ_ONLY_PLAN";
    if (validation.errors.length || contextGaps.length) executionStatus = "BLOCKED_CONTEXT_GAP";
    else if (routingConflicts.length) executionStatus = "BLOCKED_ROUTING_CONFLICT";
    else if (capabilityGaps.length) executionStatus = "BLOCKED_CAPABILITY_GAP";
    else if (actionProposalRequired) executionStatus = "READY_FOR_GOVERNANCE";

    const routeProfiles = routes.flatMap((route) => Object.keys(route.connection_profiles || {}));
    const connectionProfiles = unique([context.connection_profile, ...routeProfiles]);
    const workflows = unique([
      context.workflow_reference,
      ...routes.flatMap((route) => route.relationships?.workflows || []),
    ]);
    const plan = {
      request_id: context.request_id,
      context_summary: {
        environment: context.environment,
        system: context.system,
        resource_type: context.resource_type,
        resource: context.resource,
        operation_intent: context.operation_intent,
        purpose_present: Boolean(context.purpose),
        expected_affected_rows: context.expected_affected_rows,
        data_classifications: [...context.data_classifications],
        runbook_reference: context.runbook_reference,
        workflow_reference: context.workflow_reference,
      },
      capabilities: capabilityResolution.capabilities,
      capability_source: capabilityResolution.source,
      routes,
      primary_agents: unique(routes.map((route) => route.primary_agent)),
      complementary_agents: unique(routes.flatMap((route) => route.complementary_agents || [])),
      cross_cutting_agents: crossCutting.agents,
      cross_cutting_candidates: crossCutting.candidates,
      cross_cutting_reasons: crossCutting.reasons,
      capability_gaps: capabilityGaps,
      routing_conflicts: routingConflicts,
      context_gaps: contextGaps,
      validation_errors: validation.errors,
      workflows,
      runbook_reference: context.runbook_reference,
      connection_profiles: connectionProfiles,
      credential_resolution_required: connectionProfiles.length > 0,
      sensitive_action_detected: sensitiveActionDetected,
      action_proposal_required: actionProposalRequired,
      approval_required_candidate: actionProposalRequired,
      approval_granted: false,
      execution_status: executionStatus,
      execution_performed: false,
      direct_bypass_allowed: false,
      next_steps: this.nextSteps(executionStatus),
    };
    if (contextGaps.length) this.emit("orchestrator.context.gap", { request_id: context.request_id || "MISSING", gap_count: contextGaps.length });
    this.emit("orchestrator.plan.completed", {
      request_id: context.request_id || "MISSING",
      execution_status: executionStatus,
      routes: routes.length,
      capability_gaps: capabilityGaps.length,
      routing_conflicts: routingConflicts.length,
      context_gaps: contextGaps.length,
    });
    return plan;
  }

  orchestrate(input) {
    return this.buildGovernedPlan(input);
  }

  // WAVE A bridge: serializes a governed plan into the payload consumed by the
  // .NET GovernedPlanBridge (backend/src/BlueprintOS.Application/Governance/GovernedPlanBridge.cs),
  // which converts it into a real ActionProposal via GovernedWriteStack.PrepareAsync.
  // This method still grants no authorization — it only produces a serializable
  // plan; the Policy Engine and ApprovalPolicy remain the sole authorities.
  buildActionProposalPayload(input) {
    const plan = this.buildGovernedPlan(input);
    if (plan.execution_status !== "READY_FOR_GOVERNANCE") {
      return { eligible: false, reason: plan.execution_status, plan };
    }
    if (plan.capabilities.length !== 1 || plan.primary_agents.length !== 1) {
      return { eligible: false, reason: "AMBIGUOUS_CAPABILITY_OR_OWNER", plan };
    }
    const context = this.validateActionContext(input).context;
    return {
      eligible: true,
      reason: null,
      payload: {
        requestId: context.request_id,
        requestedBy: context.requested_by,
        agentId: plan.primary_agents[0],
        capability: plan.capabilities[0],
        environment: context.environment,
        system: context.system,
        resourceType: context.resource_type,
        resource: context.resource,
        operationIntent: context.operation_intent,
        fields: context.fields,
        filterSummary: context.filter_summary,
        expectedAffectedRows: context.expected_affected_rows,
        purpose: context.purpose,
        dataClassification: context.data_classifications[0] || "Unknown",
        containsPersonalData: context.contains_personal_data,
        containsSensitivePersonalData: context.contains_sensitive_personal_data,
        containsSecrets: context.contains_secrets,
        reversibility: context.reversibility,
        runbookReference: context.runbook_reference,
        connectionProfile: context.connection_profile,
        additionalContext: context.additional_context,
        crossCuttingAgents: plan.cross_cutting_agents,
      },
      plan,
    };
  }

  nextSteps(status) {
    if (status === "BLOCKED_CAPABILITY_GAP") return [
      "Evaluate evolution of an existing Agent.",
      "Verify an alternative natural owner.",
      "Propose a new Agent only if necessary and with explicit human authorization.",
    ];
    if (status === "BLOCKED_ROUTING_CONFLICT") return ["Correct ownership through an explicitly authorized architectural change."];
    if (status === "BLOCKED_CONTEXT_GAP") return ["Supply missing structured context without inventing values."];
    if (status === "READY_FOR_GOVERNANCE") return ["Prepare a concrete ActionProposal for deterministic policy evaluation; no authorization has been granted."];
    return ["The read-only routing plan may be reviewed by a future execution layer; no tool has been executed."];
  }
}

module.exports = { DESTRUCTIVE_INTENTS, GOVERNED_CLASSIFICATIONS, GovernedOrchestrator, MUTATING_INTENTS };
