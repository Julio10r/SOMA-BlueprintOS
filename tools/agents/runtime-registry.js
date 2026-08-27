#!/usr/bin/env node

const path = require("path");
const { validateRepository } = require("./validate-agent-manifests");

const ACTIVE_STATUS = new Set(["active"]);
const SAFE_PROFILE_FIELDS = ["configuration_reference", "environment", "access_intent", "classification"];

function clone(value) {
  return structuredClone(value);
}

function unique(values) {
  return [...new Set(values)].sort();
}

function safeProfiles(profiles = {}) {
  return Object.fromEntries(Object.entries(profiles).map(([id, profile]) => [
    id,
    Object.fromEntries(SAFE_PROFILE_FIELDS.filter((field) => field in profile).map((field) => [field, profile[field]])),
  ]));
}

function errorAgentId(error) {
  const match = error.match(/^agents\/([^/]+)\/agent\.yaml:/);
  return match ? match[1] : null;
}

class RuntimeRegistry {
  constructor(options = {}) {
    this.repoRoot = path.resolve(options.repoRoot || path.join(__dirname, "../.."));
    this.observer = options.observer || (() => {});
    this.state = null;
  }

  emit(event, details = {}) {
    this.observer({ event, ...details });
  }

  discoverAgents() {
    this.emit("registry.discovery.started");
    const validation = validateRepository(this.repoRoot);
    const duplicateIds = new Set();
    const counts = new Map();
    for (const record of validation.records) counts.set(record.manifest.id, (counts.get(record.manifest.id) || 0) + 1);
    for (const [id, count] of counts) if (count > 1) duplicateIds.add(id);

    const errorsByAgent = new Map();
    const repositoryErrors = [];
    for (const error of validation.errors) {
      const id = errorAgentId(error);
      if (!id) repositoryErrors.push(error);
      else {
        if (!errorsByAgent.has(id)) errorsByAgent.set(id, []);
        errorsByAgent.get(id).push(error);
      }
    }

    const knownIds = new Set(validation.records.map((record) => record.manifest.id));
    for (const record of validation.records) {
      const relationships = record.manifest.relationships || {};
      for (const field of ["upstream_agents", "downstream_agents"]) {
        for (const reference of relationships[field] || []) {
          if (!knownIds.has(reference)) {
            if (!errorsByAgent.has(record.manifest.id)) errorsByAgent.set(record.manifest.id, []);
            errorsByAgent.get(record.manifest.id).push(`${record.relativePath}: relationships.${field} references unknown Agent ${reference}`);
          }
        }
      }
    }

    const invalidAgents = [];
    const agents = new Map();
    const fatalRepositoryErrors = repositoryErrors.filter((error) => error !== "duplicate Agent id detected");
    for (const record of validation.records) {
      const errors = [...fatalRepositoryErrors, ...(errorsByAgent.get(record.manifest.id) || [])];
      if (duplicateIds.has(record.manifest.id)) errors.push(`duplicate Agent id ${record.manifest.id}`);
      if (errors.length) {
        invalidAgents.push({ agent_id: record.manifest.id, manifest: record.relativePath, errors: unique(errors) });
        continue;
      }
      agents.set(record.manifest.id, { manifest: clone(record.manifest), manifest_path: record.relativePath });
    }

    const capabilityIndex = new Map();
    for (const agent of agents.values()) {
      for (const [capability, ownership] of Object.entries(agent.manifest.capability_ownership)) {
        if (!capabilityIndex.has(capability)) capabilityIndex.set(capability, { primary: [], complementary: [] });
        capabilityIndex.get(capability)[ownership.ownership].push({
          agent_id: ownership.responsible_agent_id,
          delegation_required: ownership.delegation_required,
          manifest: agent.manifest_path,
        });
      }
    }
    for (const owners of capabilityIndex.values()) {
      owners.primary.sort((a, b) => a.agent_id.localeCompare(b.agent_id));
      owners.complementary.sort((a, b) => a.agent_id.localeCompare(b.agent_id));
    }

    this.state = { agents, capabilityIndex, invalidAgents, repositoryErrors };
    this.emit("registry.discovery.completed", {
      discovered_agents: agents.size,
      indexed_capabilities: capabilityIndex.size,
      invalid_agents: invalidAgents.length,
    });
    return this.describe();
  }

  ensureDiscovered() {
    if (!this.state) this.discoverAgents();
  }

  describeAgent(agent) {
    const manifest = agent.manifest;
    return {
      id: manifest.id,
      name: manifest.name,
      type: manifest.type,
      status: manifest.status,
      manifest: agent.manifest_path,
      runtime: clone(manifest.runtime),
      relationships: clone(manifest.relationships),
      connection_profiles: safeProfiles(manifest.connections?.profiles),
      governance: {
        read_only: manifest.governance.read_only,
        can_propose_write: manifest.governance.can_propose_write,
        can_execute_write: manifest.governance.can_execute_write,
        policy_engine_required: manifest.governance.policy_engine_required,
        enforcement_status: manifest.governance.enforcement_status,
      },
      cross_cutting: manifest.delegation.cross_cutting,
      participation_criteria: clone(manifest.delegation.participation_criteria),
    };
  }

  describe() {
    this.ensureDiscovered();
    const agents = [...this.state.agents.values()].map((agent) => this.describeAgent(agent)).sort((a, b) => a.id.localeCompare(b.id));
    const ownership = [...this.state.capabilityIndex.values()];
    return {
      discovered_agents: agents,
      indexed_capabilities: this.listCapabilities(),
      ownership_summary: {
        primary: ownership.reduce((total, item) => total + item.primary.length, 0),
        complementary: ownership.reduce((total, item) => total + item.complementary.length, 0),
      },
      cross_cutting_agents: agents.filter((agent) => agent.cross_cutting).map((agent) => agent.id),
      conflicts: this.detectConflicts(),
      invalid_agents: clone(this.state.invalidAgents),
      repository_errors: clone(this.state.repositoryErrors),
    };
  }

  getAgent(agentId) {
    this.ensureDiscovered();
    const agent = this.state.agents.get(agentId);
    return agent ? this.describeAgent(agent) : null;
  }

  listCapabilities() {
    this.ensureDiscovered();
    return [...this.state.capabilityIndex.keys()].sort();
  }

  getCapabilityOwners(capability) {
    this.ensureDiscovered();
    const owners = this.state.capabilityIndex.get(capability) || { primary: [], complementary: [] };
    return clone(owners);
  }

  detectConflicts() {
    this.ensureDiscovered();
    return [...this.state.capabilityIndex.entries()]
      .map(([capability, owners]) => [capability, {
        ...owners,
        primary: owners.primary.filter((owner) => ACTIVE_STATUS.has(this.state.agents.get(owner.agent_id)?.manifest.status)),
      }])
      .filter(([, owners]) => owners.primary.length > 1)
      .map(([capability, owners]) => ({
        type: "ROUTING_CONFLICT",
        capability,
        conflicting_agents: owners.primary.map((owner) => owner.agent_id),
        manifests: owners.primary.map((owner) => owner.manifest),
        evidence: "Multiple active primary ownership declarations",
        automatic_resolution_allowed: false,
      }));
  }

  similarCapabilities(requested) {
    const terms = new Set(requested.split("-").filter((term) => term.length > 2));
    return this.listCapabilities().map((capability) => ({
      capability,
      score: capability.split("-").filter((term) => terms.has(term)).length,
    })).filter((item) => item.score > 0).sort((a, b) => b.score - a.score || a.capability.localeCompare(b.capability)).slice(0, 5).map((item) => item.capability);
  }

  resolveCapability(capability) {
    this.ensureDiscovered();
    if (typeof capability !== "string" || !capability.trim()) throw new Error("A structured capability id is required");
    const requested = capability.trim();
    const owners = this.getCapabilityOwners(requested);
    const eligiblePrimary = owners.primary.filter((owner) => ACTIVE_STATUS.has(this.state.agents.get(owner.agent_id)?.manifest.status));
    const eligibleComplementary = owners.complementary.filter((owner) => ACTIVE_STATUS.has(this.state.agents.get(owner.agent_id)?.manifest.status));
    const crossCuttingCandidates = [...this.state.agents.values()]
      .filter((agent) => agent.manifest.delegation.cross_cutting && ACTIVE_STATUS.has(agent.manifest.status))
      .map((agent) => ({
        agent_id: agent.manifest.id,
        participation_criteria: clone(agent.manifest.delegation.participation_criteria),
        reason: "Manifest criteria require external structured operation context; candidacy is not authorization or mandatory participation.",
      }));

    if (eligiblePrimary.length > 1) {
      const conflict = this.detectConflicts().find((item) => item.capability === requested);
      const decision = {
        requested_capability: requested,
        status: "ROUTING_CONFLICT",
        routing_resolved: false,
        primary_agent: null,
        complementary_agents: eligibleComplementary.map((owner) => owner.agent_id),
        cross_cutting_agents: [],
        cross_cutting_candidates: crossCuttingCandidates,
        delegation_required: true,
        capability_gap: null,
        conflicts: [conflict],
        direct_bypass_allowed: false,
        reasons: ["More than one active primary owner is declared; arbitrary precedence is forbidden."],
        evidence: eligiblePrimary.map((owner) => owner.manifest),
        next_step: "Correct ownership declarations through an explicitly authorized architectural change.",
      };
      this.emit("registry.routing.conflict", { requested_capability: requested, conflicting_agents: eligiblePrimary.length });
      return decision;
    }

    if (eligiblePrimary.length !== 1) {
      const knownOwners = [...owners.primary, ...owners.complementary];
      const gap = {
        type: "CAPABILITY_GAP",
        requested_capability: requested,
        agents_evaluated: [...this.state.agents.keys()].sort(),
        reason: knownOwners.length ? "No active primary owner is eligible." : "No matching capability is declared.",
        similar_capabilities: this.similarCapabilities(requested),
        allowed_next_steps: [
          "Verify an existing Agent and capability declaration.",
          "Evaluate evolution of an existing Agent.",
          "Evaluate another natural owner.",
          "Propose a new Agent only if necessary and with explicit human authorization.",
        ],
        direct_bypass_allowed: false,
      };
      const decision = {
        requested_capability: requested,
        status: "CAPABILITY_GAP",
        routing_resolved: false,
        primary_agent: null,
        complementary_agents: eligibleComplementary.map((owner) => owner.agent_id),
        cross_cutting_agents: [],
        cross_cutting_candidates: crossCuttingCandidates,
        delegation_required: knownOwners.some((owner) => owner.delegation_required),
        capability_gap: gap,
        conflicts: [],
        direct_bypass_allowed: false,
        reasons: [gap.reason],
        evidence: knownOwners.map((owner) => owner.manifest),
        next_step: gap.allowed_next_steps[0],
      };
      this.emit("registry.routing.gap", { requested_capability: requested, reason_category: knownOwners.length ? "NO_ELIGIBLE_PRIMARY" : "NOT_DECLARED" });
      return decision;
    }

    const primary = eligiblePrimary[0];
    const agent = this.state.agents.get(primary.agent_id);
    const decision = {
      requested_capability: requested,
      status: "ROUTING_RESOLVED",
      routing_resolved: true,
      primary_agent: primary.agent_id,
      complementary_agents: eligibleComplementary.map((owner) => owner.agent_id),
      cross_cutting_agents: [],
      cross_cutting_candidates: crossCuttingCandidates.filter((candidate) => candidate.agent_id !== primary.agent_id),
      delegation_required: primary.delegation_required || eligibleComplementary.some((owner) => owner.delegation_required),
      capability_gap: null,
      conflicts: [],
      direct_bypass_allowed: false,
      runtime: clone(agent.manifest.runtime),
      relationships: clone(agent.manifest.relationships),
      connection_profiles: safeProfiles(agent.manifest.connections?.profiles),
      enforcement_status: agent.manifest.governance.enforcement_status,
      reasons: ["Exactly one active primary owner is declared by a valid canonical manifest."],
      evidence: [primary.manifest],
      next_step: "Pass this routing decision to a future orchestrator; operational authorization remains separate.",
    };
    this.emit("registry.routing.resolved", { requested_capability: requested, primary_agent: primary.agent_id });
    return decision;
  }

  resolveCapabilities(capabilities) {
    if (!Array.isArray(capabilities)) throw new Error("capabilities must be an array of structured ids");
    return capabilities.map((capability) => this.resolveCapability(capability));
  }

  buildRoutingPlan(capabilities) {
    const routes = this.resolveCapabilities(capabilities);
    return {
      requested_capabilities: clone(capabilities),
      routes,
      gaps: routes.filter((route) => route.capability_gap).map((route) => route.capability_gap),
      conflicts: routes.flatMap((route) => route.conflicts),
      routing_resolved: routes.every((route) => route.routing_resolved),
      direct_bypass_allowed: false,
      execution_performed: false,
      authorization_granted: false,
      next_step: "Submit resolved routes to orchestration and governance components; this Registry does not execute or authorize tools.",
    };
  }
}

module.exports = { ACTIVE_STATUS, RuntimeRegistry, SAFE_PROFILE_FIELDS, safeProfiles };
