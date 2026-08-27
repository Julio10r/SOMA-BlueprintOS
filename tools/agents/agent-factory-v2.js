#!/usr/bin/env node

const fs = require("fs");
const path = require("path");
const {
  loadRepositoryManifests,
  parseManifest,
  validateManifest,
  validateRepository,
} = require("./validate-agent-manifests");

const OPERATIONS = Object.freeze([
  "CREATE", "VALIDATE", "AUDIT", "UPDATE", "REGISTER", "CATALOG", "TEST", "SECURITY_CHECK",
]);
const PROTECTED_CONTRACT_PATHS = new Set([
  "agents/AGENT_CONTRACT.md", "agents/EXECUTION_POLICY.md", "agents/agent.schema.json",
]);
const MATERIAL_PATHS = [
  "capability_ownership", "delegation", "connections", "data.systems", "data.classifications",
  "data.pii_allowed", "data.sensitive_pii_allowed", "data.secrets_allowed", "governance.can_execute_write",
  "governance.can_execute_destructive_operation", "governance.policy_engine_required",
  "governance.approval_required_for", "governance.enforcement_status",
];

function stable(value) {
  const normalize = (item) => {
    if (Array.isArray(item)) return item.map(normalize);
    if (item && typeof item === "object") return Object.fromEntries(Object.keys(item).sort().map((key) => [key, normalize(item[key])]));
    return item;
  };
  return JSON.stringify(normalize(value));
}

function get(value, dottedPath) {
  return dottedPath.split(".").reduce((current, key) => current && current[key], value);
}

function finding(agentId, id, severity, category, criterion, evidence, actual, expected, recommendation, options = {}) {
  return {
    id, agent_id: agentId, severity, category, criterion, evidence, actual, expected, recommendation,
    auto_fix_allowed: options.autoFixAllowed === true,
    requires_human_approval: options.requiresHumanApproval !== false,
  };
}

function statusFromFindings(findings) {
  if (findings.some((item) => item.severity === "ERROR")) return "FAIL";
  if (findings.some((item) => item.severity === "WARNING")) return "WARN";
  return "PASS";
}

function assertApproval(authorization, purpose) {
  if (!authorization || authorization.approved !== true || !authorization.approved_by || !authorization.approved_at) {
    throw new Error(`${purpose} requires explicit human approval with approved, approved_by and approved_at`);
  }
}

function assertSafeAgentTarget(relativePath) {
  const normalized = relativePath.replaceAll("\\", "/");
  if (PROTECTED_CONTRACT_PATHS.has(normalized)) throw new Error(`Agent Factory cannot modify protected contract source ${normalized}`);
  if (!/^agents\/[a-z0-9]+(?:-[a-z0-9]+)*\/agent\.yaml$/.test(normalized)) {
    throw new Error(`Agent Factory may only mutate canonical Agent manifests, received ${normalized}`);
  }
}

function assertSafeOutput(relativePath, kind) {
  if (!relativePath || path.isAbsolute(relativePath) || relativePath.split(/[\\/]/).includes("..")) {
    throw new Error(`Unsafe ${kind} output path`);
  }
  const normalized = relativePath.replaceAll("\\", "/");
  const allowed = kind === "catalog"
    ? /^docs\/agents\/[a-zA-Z0-9.-]+\.html$/
    : /^(docs\/audits|artifacts\/agent-audits)\/[a-zA-Z0-9.-]+\.json$/;
  if (!allowed.test(normalized)) throw new Error(`Unsupported ${kind} output path ${normalized}`);
  return normalized;
}

function yamlScalar(value) {
  if (value === null) return "null";
  if (typeof value === "boolean" || typeof value === "number") return String(value);
  return String(value);
}

function escapeHtml(value) {
  return String(value).replaceAll("&", "&amp;").replaceAll("<", "&lt;").replaceAll(">", "&gt;").replaceAll('"', "&quot;").replaceAll("'", "&#39;");
}

function toYaml(value, indent = 0) {
  const prefix = " ".repeat(indent);
  if (Array.isArray(value)) {
    if (value.length === 0) return "[]";
    return value.map((item) => `${prefix}- ${yamlScalar(item)}`).join("\n");
  }
  const lines = [];
  for (const [key, child] of Object.entries(value)) {
    if (Array.isArray(child)) {
      if (child.length === 0) lines.push(`${prefix}${key}: []`);
      else lines.push(`${prefix}${key}:\n${toYaml(child, indent + 2)}`);
    } else if (child && typeof child === "object") {
      if (Object.keys(child).length === 0) lines.push(`${prefix}${key}: {}`);
      else lines.push(`${prefix}${key}:\n${toYaml(child, indent + 2)}`);
    } else {
      lines.push(`${prefix}${key}: ${yamlScalar(child)}`);
    }
  }
  return lines.join("\n");
}

class AgentFactoryV2 {
  constructor(options = {}) {
    this.repoRoot = path.resolve(options.repoRoot || path.join(__dirname, "../.."));
    this.clock = options.clock || (() => new Date());
  }

  discover() {
    return loadRepositoryManifests(this.repoRoot);
  }

  validate() {
    const result = validateRepository(this.repoRoot);
    return { operation: "VALIDATE", status: result.errors.length ? "FAIL" : "PASS", errors: result.errors, agents: result.records.map((item) => item.manifest.id) };
  }

  audit() {
    const validation = validateRepository(this.repoRoot);
    const validationByAgent = new Map();
    for (const error of validation.errors) {
      const match = error.match(/^agents\/([^/]+)\/agent\.yaml:/);
      const id = match ? match[1] : "repository";
      if (!validationByAgent.has(id)) validationByAgent.set(id, []);
      validationByAgent.get(id).push(error);
    }

    const agents = validation.records.map((record) => {
      const manifest = record.manifest;
      const findings = [];
      for (const error of validationByAgent.get(manifest.id) || []) {
        findings.push(finding(manifest.id, "AFV2-CONTRACT-001", "ERROR", "CONTRACT", "Manifest must satisfy Agent Contract v1.1", record.relativePath, error, "Valid canonical manifest", "Correct the Agent after explicit review; never weaken the contract."));
      }
      findings.push(...this.securityCheck(manifest).findings);

      if (["DOCUMENTAL", "PARTIAL", "PLANNED"].includes(manifest.governance.enforcement_status)) {
        findings.push(finding(manifest.id, "AFV2-GOV-001", "WARNING", "GOVERNANCE", "Enforcement must not be overstated", record.relativePath, manifest.governance.enforcement_status, "ENFORCED only after a technical control covers the declared flow", "Keep the current honest status and connect the flow to future governed enforcement.", { requiresHumanApproval: false }));
      }
      if (manifest.tests.safety.length === 0) {
        findings.push(finding(manifest.id, "AFV2-TEST-001", "WARNING", "TEST", "Safety coverage should be declared for governed behavior", `${record.relativePath}:tests.safety`, "No dedicated safety test declared", "Applicable safety test or documented non-applicability", "Design a focused safety test before expanding capability.", { requiresHumanApproval: false }));
      }
      if (manifest.observability.logs.length === 0 && manifest.observability.audit_events.length === 0) {
        findings.push(finding(manifest.id, "AFV2-OBS-001", "WARNING", "OBSERVABILITY", "Agent activity should have verifiable observability", `${record.relativePath}:observability`, "No logs or audit events declared", "Applicable redacted log/audit evidence or documented non-applicability", "Define minimum redacted observability appropriate to the Agent.", { requiresHumanApproval: false }));
      }
      if (Object.keys(manifest.connections.profiles).length > 0 && manifest.governance.enforcement_status !== "ENFORCED") {
        findings.push(finding(manifest.id, "AFV2-GATEWAY-001", "WARNING", "SECURITY", "External access must eventually be mediated by governed tools", `${record.relativePath}:connections`, "Connection profile exists without universal Tool Gateway enforcement", "Governed adapter plus policy, approval and audit where applicable", "Address in the future Tool Gateway; do not bypass the responsible Agent.", { requiresHumanApproval: false }));
      }
      return { id: manifest.id, status: statusFromFindings(findings), findings };
    });

    const repositoryFindings = (validationByAgent.get("repository") || []).map((error) =>
      finding("repository", "AFV2-REG-001", "ERROR", "REGISTRATION", "Canonical Agent set must validate as a repository", "agents/*/agent.yaml", error, "Repository validation passes", "Repair registration inconsistency after review."));

    return {
      contract_version: "1.1",
      timestamp: this.clock().toISOString(),
      operation: "AUDIT",
      status: statusFromFindings([...repositoryFindings, ...agents.flatMap((item) => item.findings)]),
      agents,
      findings: repositoryFindings,
    };
  }

  securityCheck(manifest) {
    const findings = [];
    const source = `agents/${manifest.id}/agent.yaml`;
    if (manifest.delegation.bypass_allowed !== false) findings.push(finding(manifest.id, "AFV2-SEC-001", "ERROR", "SECURITY", "No direct bypass", source, "Bypass enabled", "bypass_allowed=false", "Disable bypass after explicit review."));
    if (manifest.connections.credential_policy.least_privilege !== true) findings.push(finding(manifest.id, "AFV2-CRED-001", "ERROR", "CREDENTIAL", "Least privilege", source, "Least privilege disabled", "least_privilege=true", "Restore least privilege."));
    if (manifest.connections.credential_policy.privilege_escalation_allowed !== false) findings.push(finding(manifest.id, "AFV2-CRED-002", "ERROR", "CREDENTIAL", "No privilege escalation", source, "Privilege escalation enabled", "privilege_escalation_allowed=false", "Remove privilege escalation."));
    if (manifest.governance.can_execute_write && manifest.governance.approval_required_for.length === 0) findings.push(finding(manifest.id, "AFV2-GOV-002", "ERROR", "GOVERNANCE", "Write execution requires explicit approval policy", source, "Write enabled without approval declaration", "Approval requirement declared", "Declare approval and route concrete actions through AI Governance."));
    if ((manifest.data.pii_allowed || manifest.data.sensitive_pii_allowed) && !manifest.governance.policy_engine_required) findings.push(finding(manifest.id, "AFV2-LGPD-001", "ERROR", "SECURITY", "PII access requires governance declaration", source, "PII allowed without Policy Engine requirement", "policy_engine_required=true", "Require deterministic policy evaluation."));
    return { operation: "SECURITY_CHECK", agent_id: manifest.id, status: statusFromFindings(findings), findings };
  }

  register(agentId) {
    const record = this.discover().find((item) => item.manifest.id === agentId);
    if (!record) return { operation: "REGISTER", agent_id: agentId, status: "FAIL", findings: [finding(agentId, "AFV2-REG-404", "ERROR", "REGISTRATION", "Agent must have a canonical manifest", `agents/${agentId}/agent.yaml`, "Manifest not found", "Manifest present and valid", "Use CREATE only after explicit human approval.")] };
    const validation = this.validate();
    const errors = validation.errors.filter((item) => item.startsWith(`${record.relativePath}:`));
    return { operation: "REGISTER", agent_id: agentId, status: errors.length ? "FAIL" : "PASS", findings: errors };
  }

  testPlan(agentId) {
    const record = this.discover().find((item) => item.manifest.id === agentId);
    if (!record) throw new Error(`Unknown Agent ${agentId}`);
    const declared = record.manifest.tests;
    const paths = [...declared.unit, ...declared.integration, ...declared.safety, ...declared.contract];
    return {
      operation: "TEST", agent_id: agentId,
      status: paths.every((item) => fs.existsSync(path.join(this.repoRoot, item))) ? "PASS" : "FAIL",
      declared, commands: ["node tools/agents/validate-agent-manifests.js", "node tools/agents/agent-factory-v2.test.js"],
      execution_performed: false,
    };
  }

  catalog(options = {}) {
    const records = this.discover().sort((a, b) => a.manifest.catalog.display_order - b.manifest.catalog.display_order);
    const rows = records.map(({ manifest }) => `<tr><td>${escapeHtml(manifest.id)}</td><td>${escapeHtml(manifest.name)}</td><td>${escapeHtml(manifest.version)}</td><td>${escapeHtml(manifest.type)}</td><td>${escapeHtml(manifest.status)}</td><td>${escapeHtml(Object.keys(manifest.capability_ownership).join(", "))}</td><td>${escapeHtml(manifest.governance.enforcement_status)}</td></tr>`).join("\n");
    const html = `<!doctype html>\n<html lang="pt-BR"><head><meta charset="utf-8"><title>SOMA BlueprintOS Agents</title></head><body><h1>SOMA BlueprintOS Agents</h1><p>Generated from canonical manifests. Human editorial context remains in docs/agents/AgentsCatalog.html.</p><table><thead><tr><th>ID</th><th>Name</th><th>Version</th><th>Type</th><th>Status</th><th>Capabilities</th><th>Enforcement</th></tr></thead><tbody>${rows}</tbody></table></body></html>\n`;
    if (options.apply) {
      assertApproval(options.authorization, "CATALOG write");
      const output = assertSafeOutput(options.output || "docs/agents/AgentsCatalog.generated.html", "catalog");
      fs.writeFileSync(path.join(this.repoRoot, output), html);
      return { operation: "CATALOG", status: "PASS", output, generated: true };
    }
    return { operation: "CATALOG", status: "PASS", generated: false, preview: html };
  }

  create(request) {
    assertApproval(request.authorization, "CREATE");
    if (!request.capability_gap_evidence || !Array.isArray(request.existing_agents_evaluated) || request.existing_agents_evaluated.length === 0) {
      throw new Error("CREATE requires Capability Gap evidence and evaluated existing Agents");
    }
    const manifest = structuredClone(request.manifest);
    manifest.delegation = { ...manifest.delegation, bypass_allowed: false };
    manifest.connections.credential_policy = { ...manifest.connections.credential_policy, least_privilege: true, privilege_escalation_allowed: false, prompt_for_secret_allowed: false };
    manifest.governance = { ...manifest.governance, can_execute_write: manifest.governance.can_execute_write === true && request.authorization.allow_write === true, can_execute_destructive_operation: false };
    const target = `agents/${manifest.id}/agent.yaml`;
    assertSafeAgentTarget(target);
    if (fs.existsSync(path.join(this.repoRoot, target))) throw new Error(`Agent ${manifest.id} already exists`);
    const yaml = `${toYaml(manifest)}\n`;
    const known = new Set([...this.discover().map((item) => item.manifest.id), manifest.id]);
    const errors = validateManifest(manifest, yaml, target, known, { validatePaths: false, repoRoot: this.repoRoot });
    if (errors.length) throw new Error(`CREATE manifest is invalid: ${errors.join(" | ")}`);
    if (request.apply === true) {
      fs.mkdirSync(path.dirname(path.join(this.repoRoot, target)), { recursive: true });
      fs.writeFileSync(path.join(this.repoRoot, target), yaml);
    }
    return { operation: "CREATE", status: "PASS", target, applied: request.apply === true, manifest, yaml };
  }

  update(request) {
    const record = this.discover().find((item) => item.manifest.id === request.agent_id);
    if (!record) throw new Error(`Unknown Agent ${request.agent_id}`);
    const target = record.relativePath;
    assertSafeAgentTarget(target);
    const next = structuredClone(request.manifest);
    if (next.id !== request.agent_id) throw new Error("UPDATE cannot change Agent id");
    const materialChanges = MATERIAL_PATHS.filter((item) => stable(get(record.manifest, item)) !== stable(get(next, item)));
    if (materialChanges.length) assertApproval(request.authorization, "Material UPDATE");
    if (next.delegation.bypass_allowed || next.connections.credential_policy.privilege_escalation_allowed) throw new Error("UPDATE cannot enable bypass or privilege escalation");
    const yaml = `${toYaml(next)}\n`;
    const known = new Set(this.discover().map((item) => item.manifest.id));
    const errors = validateManifest(next, yaml, target, known, { validatePaths: true, repoRoot: this.repoRoot });
    if (errors.length) throw new Error(`UPDATE manifest is invalid: ${errors.join(" | ")}`);
    if (request.apply === true) fs.writeFileSync(path.join(this.repoRoot, target), yaml);
    return { operation: "UPDATE", status: "PASS", target, applied: request.apply === true, material_changes: materialChanges };
  }
}

module.exports = { AgentFactoryV2, MATERIAL_PATHS, OPERATIONS, PROTECTED_CONTRACT_PATHS, assertSafeAgentTarget, assertSafeOutput, escapeHtml, statusFromFindings, toYaml };
