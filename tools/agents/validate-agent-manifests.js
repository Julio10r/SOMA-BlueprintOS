#!/usr/bin/env node

const fs = require("fs");
const path = require("path");

const repoRoot = path.resolve(__dirname, "../..");
const agentsRoot = path.join(repoRoot, "agents");
const schemaPath = path.join(agentsRoot, "agent.schema.json");

const expectedAgentIds = [
  "echo-agent", "knowledge-agent", "security-lgpd-agent", "linx-erp-specialist-agent",
  "linx-database-specialist-agent", "wise-agent", "showcase-agent",
];

const allowedTypes = new Set(["runtime", "knowledge", "operational", "hybrid"]);
const allowedStatus = new Set(["active", "planned", "deprecated", "retired"]);
const allowedRisks = new Set(["Green", "Yellow", "Red", "Unknown"]);
const allowedEnforcement = new Set(["ENFORCED", "DOCUMENTAL", "PARTIAL", "PLANNED"]);
const allowedOwnership = new Set(["primary", "complementary"]);
const allowedCredentialStrategies = new Set(["none", "user-managed", "service-identity-managed"]);
const allowedAccessIntents = new Set(["read-only", "governed-write", "AINDA_NAO_MAPEADO"]);
const allowedClassifications = new Set(["Public", "Internal", "Confidential", "AINDA_NAO_MAPEADO"]);

const secretPatterns = [
  /-----BEGIN [A-Z ]*PRIVATE KEY-----/i,
  /\bsk-[A-Za-z0-9_-]{16,}\b/,
  /\b[A-Za-z0-9_]*(PASSWORD|PASSWD|PWD|TOKEN|COOKIE|SECRET|API[_-]?KEY|CLIENT[_-]?SECRET)[A-Za-z0-9_]*\s*=\s*["']?[^"'\s<>*]+/i,
  /\b(Server|Data Source)=.*;.*\b(User Id|UID)=.*;.*\b(Password|PWD)=/i,
];

function parseScalar(value) {
  const trimmed = value.trim();
  if (trimmed === "null") return null;
  if (trimmed === "true") return true;
  if (trimmed === "false") return false;
  if (trimmed === "[]") return [];
  if (trimmed === "{}") return {};
  if (/^-?\d+(?:\.\d+)?$/.test(trimmed)) return Number(trimmed);
  return trimmed;
}

function parseManifest(text, filePath = "manifest") {
  const root = {};
  const stack = [{ indent: -1, value: root }];
  const lines = text.split(/\r?\n/);

  for (let index = 0; index < lines.length; index += 1) {
    const raw = lines[index];
    if (!raw.trim() || raw.trim().startsWith("#")) continue;
    const indent = raw.match(/^ */)[0].length;
    const line = raw.trim();
    while (stack.length > 1 && indent <= stack[stack.length - 1].indent) stack.pop();
    const parent = stack[stack.length - 1].value;

    if (line.startsWith("- ")) {
      if (!Array.isArray(parent)) throw new Error(`${filePath}:${index + 1}: list item without list parent`);
      parent.push(parseScalar(line.slice(2)));
      continue;
    }

    const separator = line.indexOf(":");
    if (separator === -1) throw new Error(`${filePath}:${index + 1}: expected key-value pair`);
    const key = line.slice(0, separator).trim();
    const value = line.slice(separator + 1).trim();
    if (!key) throw new Error(`${filePath}:${index + 1}: empty key`);

    if (value !== "") {
      parent[key] = parseScalar(value);
      continue;
    }

    const nextLine = lines.slice(index + 1).find((candidate) => candidate.trim() && !candidate.trim().startsWith("#"));
    const child = nextLine && nextLine.trim().startsWith("- ") ? [] : {};
    parent[key] = child;
    stack.push({ indent, value: child });
  }
  return root;
}

function validateManifest(manifest, text, relativePath, knownAgentIds, options = {}) {
  const errors = [];
  const add = (message) => errors.push(`${relativePath}: ${message}`);
  const object = (value, name) => {
    if (!value || typeof value !== "object" || Array.isArray(value)) add(`${name} must be an object`);
  };
  const array = (value, name) => {
    if (!Array.isArray(value)) add(`${name} must be an array`);
  };

  for (const pattern of secretPatterns) {
    if (pattern.test(text)) add(`potential secret material detected by ${pattern}`);
  }

  const requiredTopLevel = [
    "schema_version", "contract_version", "id", "name", "version", "type", "status", "owner",
    "responsibility", "implementation", "runtime", "capabilities", "capability_ownership", "delegation",
    "gap_policy", "connections", "data", "governance", "knowledge", "observability", "tests",
    "relationships", "catalog",
  ];
  for (const key of requiredTopLevel) if (!(key in manifest)) add(`missing ${key}`);

  if (manifest.schema_version !== 1) add("schema_version must be 1");
  if (manifest.contract_version !== 1.1) add("contract_version must be 1.1");
  if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(manifest.id || "")) add(`invalid id ${manifest.id}`);
  if (!/^\d+\.\d+\.\d+$/.test(manifest.version || "")) add(`invalid version ${manifest.version}`);
  if (!allowedTypes.has(manifest.type)) add(`invalid type ${manifest.type}`);
  if (!allowedStatus.has(manifest.status)) add(`invalid status ${manifest.status}`);

  for (const block of ["responsibility", "implementation", "runtime", "capabilities", "capability_ownership", "delegation", "gap_policy", "connections", "data", "governance", "knowledge", "observability", "tests", "relationships", "catalog"]) {
    object(manifest[block], block);
  }

  const listFields = [
    "implementation.code_paths", "implementation.prompt_paths", "implementation.context_paths", "implementation.runbook_paths",
    "implementation.script_paths", "implementation.docs_paths", "runtime.constructor_dependencies", "runtime.dependencies",
    "capabilities.tools", "capabilities.allowed_operations", "capabilities.forbidden_operations",
    "delegation.participation_criteria", "connections.credential_policy.secret_storage", "data.systems", "data.resources",
    "data.classifications", "governance.requires_action_proposal_for", "governance.approval_required_for",
    "knowledge.memory_paths", "knowledge.provenance_labels", "knowledge.update_rules", "observability.logs",
    "observability.metrics", "observability.audit_events", "tests.unit", "tests.integration", "tests.safety",
    "tests.contract", "relationships.upstream_agents", "relationships.downstream_agents", "relationships.workflows",
    "relationships.conflicts_with",
  ];
  for (const field of listFields) {
    const value = field.split(".").reduce((current, key) => current && current[key], manifest);
    array(value, field);
  }

  const ownership = manifest.capability_ownership || {};
  if (Object.keys(ownership).length === 0) add("capability_ownership must declare at least one capability");
  for (const [capabilityId, declaration] of Object.entries(ownership)) {
    if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(capabilityId)) add(`invalid capability id ${capabilityId}`);
    object(declaration, `capability_ownership.${capabilityId}`);
    if (!knownAgentIds.has(declaration.responsible_agent_id)) add(`${capabilityId} references unknown Agent ${declaration.responsible_agent_id}`);
    if (!allowedOwnership.has(declaration.ownership)) add(`${capabilityId} has invalid ownership ${declaration.ownership}`);
    if (typeof declaration.delegation_required !== "boolean") add(`${capabilityId}.delegation_required must be boolean`);
    if (declaration.direct_execution_by_others_allowed !== false) add(`${capabilityId}.direct_execution_by_others_allowed must be false without an approved architectural exception`);
    if (declaration.ownership === "primary" && declaration.responsible_agent_id !== manifest.id) add(`${capabilityId} primary owner must match manifest id`);
  }

  if (typeof manifest.delegation?.cross_cutting !== "boolean") add("delegation.cross_cutting must be boolean");
  if (manifest.delegation?.bypass_allowed !== false) add("delegation.bypass_allowed must be false without an approved architectural exception");
  if (manifest.id === "security-lgpd-agent" && manifest.delegation?.cross_cutting !== true) add("security-lgpd-agent must be cross-cutting");
  if (manifest.id !== "security-lgpd-agent" && manifest.delegation?.cross_cutting === true) add("cross-cutting status requires an explicit contract exception");

  if (manifest.gap_policy?.direct_bypass_allowed !== false) add("gap_policy.direct_bypass_allowed must be false");
  if (manifest.gap_policy?.explicit_human_approval_required_for_new_agent !== true) add("new Agent proposals require explicit human approval");
  if (manifest.gap_policy?.material_capability_change_requires_human_approval !== true) add("material capability changes require human approval");

  const credentialPolicy = manifest.connections?.credential_policy || {};
  if (!allowedCredentialStrategies.has(credentialPolicy.strategy)) add(`invalid credential strategy ${credentialPolicy.strategy}`);
  if (credentialPolicy.least_privilege !== true) add("connections.credential_policy.least_privilege must be true");
  if (credentialPolicy.privilege_escalation_allowed !== false) add("connections.credential_policy.privilege_escalation_allowed must be false");
  if (credentialPolicy.prompt_for_secret_allowed !== false) add("connections.credential_policy.prompt_for_secret_allowed must be false");
  for (const [profileId, profile] of Object.entries(manifest.connections?.profiles || {})) {
    if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(profileId)) add(`invalid connection profile id ${profileId}`);
    if (!allowedAccessIntents.has(profile.access_intent)) add(`${profileId} has invalid access_intent ${profile.access_intent}`);
    if (!allowedClassifications.has(profile.classification)) add(`${profileId} has invalid classification ${profile.classification}`);
    const serialized = JSON.stringify(profile);
    for (const pattern of secretPatterns) if (pattern.test(serialized)) add(`${profileId} contains potential secret material`);
  }

  if (!manifest.governance) add("sensitive Agent must declare governance");
  if (manifest.data?.secrets_allowed !== false) add("data.secrets_allowed must be false");
  if (typeof manifest.runtime?.implemented !== "boolean") add("runtime.implemented must be boolean");
  if (typeof manifest.runtime?.factory_supported !== "boolean") add("runtime.factory_supported must be boolean");
  if (typeof manifest.governance?.read_only !== "boolean") add("governance.read_only must be boolean");
  if (typeof manifest.governance?.can_propose_write !== "boolean") add("governance.can_propose_write must be boolean");
  if (typeof manifest.governance?.can_execute_write !== "boolean") add("governance.can_execute_write must be boolean");
  if (typeof manifest.governance?.can_execute_destructive_operation !== "boolean") add("governance.can_execute_destructive_operation must be boolean");
  if (typeof manifest.governance?.policy_engine_required !== "boolean") add("governance.policy_engine_required must be boolean");
  if (typeof manifest.governance?.audit_required !== "boolean") add("governance.audit_required must be boolean");
  if (!allowedRisks.has(manifest.governance?.default_risk)) add(`invalid default_risk ${manifest.governance?.default_risk}`);
  if (!allowedEnforcement.has(manifest.governance?.enforcement_status)) add(`invalid enforcement_status ${manifest.governance?.enforcement_status}`);
  if (typeof manifest.observability?.redaction_required !== "boolean") add("observability.redaction_required must be boolean");
  if (typeof manifest.catalog?.display_order !== "number") add("catalog.display_order must be number");

  if (options.validatePaths) {
    const pathGroups = [
      ...(manifest.implementation?.code_paths || []), ...(manifest.implementation?.prompt_paths || []),
      ...(manifest.implementation?.context_paths || []), ...(manifest.implementation?.runbook_paths || []),
      ...(manifest.implementation?.script_paths || []), ...(manifest.implementation?.docs_paths || []),
      ...(manifest.knowledge?.memory_paths || []), ...(manifest.tests?.unit || []),
      ...(manifest.tests?.integration || []), ...(manifest.tests?.safety || []), ...(manifest.tests?.contract || []),
    ];
    for (const candidate of pathGroups) {
      if (!fs.existsSync(path.join(repoRoot, candidate))) add(`referenced path does not exist: ${candidate}`);
    }
  }
  return errors;
}

function loadRepositoryManifests() {
  const paths = fs.readdirSync(agentsRoot, { withFileTypes: true })
    .filter((entry) => entry.isDirectory())
    .map((entry) => path.join(agentsRoot, entry.name, "agent.yaml"))
    .filter(fs.existsSync)
    .sort();
  return paths.map((manifestPath) => {
    const relativePath = path.relative(repoRoot, manifestPath);
    const text = fs.readFileSync(manifestPath, "utf8");
    return { manifestPath, relativePath, text, manifest: parseManifest(text, relativePath) };
  });
}

function main() {
  const errors = [];
  if (!fs.existsSync(schemaPath)) errors.push("agents/agent.schema.json is missing");
  else {
    try { JSON.parse(fs.readFileSync(schemaPath, "utf8")); } catch (error) { errors.push(`invalid schema JSON: ${error.message}`); }
  }

  let records = [];
  try { records = loadRepositoryManifests(); } catch (error) { errors.push(error.message); }
  const ids = new Set(records.map((record) => record.manifest.id));
  if (ids.size !== records.length) errors.push("duplicate Agent id detected");
  for (const expected of expectedAgentIds) if (!ids.has(expected)) errors.push(`missing manifest for known Agent ${expected}`);

  for (const record of records) {
    if (record.manifest.id !== path.basename(path.dirname(record.manifestPath))) errors.push(`${record.relativePath}: manifest id must match directory name`);
    errors.push(...validateManifest(record.manifest, record.text, record.relativePath, ids, { validatePaths: true }));
  }

  if (errors.length) {
    for (const error of errors) console.error(`FAIL: ${error}`);
    process.exitCode = 1;
    return;
  }
  console.log(`PASS: ${records.length} Agent Contract v1.1 manifests validated`);
  console.log(`PASS: IDs unique: ${[...ids].sort().join(", ")}`);
  console.log("PASS: capability ownership, delegation, gap and credential policies valid");
  console.log("PASS: Agent references and required paths exist");
  console.log("PASS: no bypass, privilege escalation or secret values detected");
}

module.exports = { parseManifest, validateManifest, secretPatterns };
if (require.main === module) main();
