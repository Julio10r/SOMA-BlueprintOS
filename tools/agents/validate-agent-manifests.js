#!/usr/bin/env node

const fs = require("fs");
const path = require("path");

const repoRoot = path.resolve(__dirname, "../..");
const agentsRoot = path.join(repoRoot, "agents");
const schemaPath = path.join(agentsRoot, "agent.schema.json");

const expectedAgentIds = [
  "echo-agent",
  "knowledge-agent",
  "security-lgpd-agent",
  "linx-erp-specialist-agent",
  "linx-database-specialist-agent",
  "wise-agent",
  "showcase-agent",
];

const allowedTypes = new Set(["runtime", "knowledge", "operational", "hybrid"]);
const allowedStatus = new Set(["active", "planned", "deprecated", "retired"]);
const allowedRisks = new Set(["Green", "Yellow", "Red", "Unknown"]);
const allowedEnforcement = new Set(["ENFORCED", "DOCUMENTAL", "PARTIAL", "PLANNED"]);
const secretPatterns = [
  /-----BEGIN [A-Z ]*PRIVATE KEY-----/i,
  /\bsk-[A-Za-z0-9_-]{20,}\b/,
  /\b[A-Za-z0-9_]*PASSWORD[A-Za-z0-9_]*\s*=\s*["']?[^"'\s<>]+/i,
  /\b[A-Za-z0-9_]*TOKEN[A-Za-z0-9_]*\s*=\s*["']?[^"'\s<>]+/i,
  /\b[A-Za-z0-9_]*COOKIE[A-Za-z0-9_]*\s*=\s*["']?[^"'\s<>]+/i,
  /\b[A-Za-z0-9_]*SECRET[A-Za-z0-9_]*\s*=\s*["']?[^"'\s<>]+/i,
  /\b[A-Za-z0-9_]*API[_-]?KEY[A-Za-z0-9_]*\s*=\s*["']?[^"'\s<>]+/i,
  /Server=.*;.*(User Id|UID)=.*;.*(Password|PWD)=/i,
];

function fail(message) {
  console.error(`FAIL: ${message}`);
  process.exitCode = 1;
}

function parseScalar(value) {
  const trimmed = value.trim();
  if (trimmed === "null") return null;
  if (trimmed === "true") return true;
  if (trimmed === "false") return false;
  if (/^-?\d+$/.test(trimmed)) return Number(trimmed);
  return trimmed;
}

function parseManifest(text, filePath) {
  const root = {};
  const stack = [{ indent: -1, value: root }];
  const lines = text.split(/\r?\n/);

  for (let index = 0; index < lines.length; index += 1) {
    const raw = lines[index];
    if (!raw.trim() || raw.trim().startsWith("#")) continue;

    const indent = raw.match(/^ */)[0].length;
    const line = raw.trim();

    while (stack.length > 1 && indent <= stack[stack.length - 1].indent) {
      stack.pop();
    }

    const parent = stack[stack.length - 1].value;

    if (line.startsWith("- ")) {
      if (!Array.isArray(parent)) {
        throw new Error(`${filePath}:${index + 1}: list item without list parent`);
      }
      parent.push(parseScalar(line.slice(2)));
      continue;
    }

    const separator = line.indexOf(":");
    if (separator === -1) {
      throw new Error(`${filePath}:${index + 1}: expected key-value pair`);
    }

    const key = line.slice(0, separator).trim();
    const value = line.slice(separator + 1).trim();
    if (!key) throw new Error(`${filePath}:${index + 1}: empty key`);

    if (value === "[]") {
      parent[key] = [];
      continue;
    }

    if (value !== "") {
      parent[key] = parseScalar(value);
      continue;
    }

    const nextLine = lines.slice(index + 1).find((candidate) => candidate.trim() && !candidate.trim().startsWith("#"));
    const nextTrimmed = nextLine ? nextLine.trim() : "";
    const child = nextTrimmed.startsWith("- ") ? [] : {};
    parent[key] = child;
    stack.push({ indent, value: child });
  }

  return root;
}

function requireObject(value, name, file) {
  if (!value || typeof value !== "object" || Array.isArray(value)) {
    fail(`${file}: ${name} must be an object`);
  }
}

function requireArray(value, name, file) {
  if (!Array.isArray(value)) {
    fail(`${file}: ${name} must be an array`);
  }
}

function validateRequiredShape(manifest, relativePath) {
  const requiredTopLevel = [
    "schema_version",
    "id",
    "name",
    "version",
    "type",
    "status",
    "owner",
    "responsibility",
    "implementation",
    "runtime",
    "capabilities",
    "data",
    "governance",
    "knowledge",
    "observability",
    "tests",
    "relationships",
    "catalog",
  ];

  for (const key of requiredTopLevel) {
    if (!(key in manifest)) fail(`${relativePath}: missing ${key}`);
  }

  if (manifest.schema_version !== 1) fail(`${relativePath}: schema_version must be 1`);
  if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(manifest.id || "")) fail(`${relativePath}: invalid id ${manifest.id}`);
  if (!/^[0-9]+\.[0-9]+\.[0-9]+$/.test(manifest.version || "")) fail(`${relativePath}: invalid version ${manifest.version}`);
  if (!allowedTypes.has(manifest.type)) fail(`${relativePath}: invalid type ${manifest.type}`);
  if (!allowedStatus.has(manifest.status)) fail(`${relativePath}: invalid status ${manifest.status}`);

  requireObject(manifest.responsibility, "responsibility", relativePath);
  requireObject(manifest.implementation, "implementation", relativePath);
  requireObject(manifest.runtime, "runtime", relativePath);
  requireObject(manifest.capabilities, "capabilities", relativePath);
  requireObject(manifest.data, "data", relativePath);
  requireObject(manifest.governance, "governance", relativePath);
  requireObject(manifest.knowledge, "knowledge", relativePath);
  requireObject(manifest.observability, "observability", relativePath);
  requireObject(manifest.tests, "tests", relativePath);
  requireObject(manifest.relationships, "relationships", relativePath);
  requireObject(manifest.catalog, "catalog", relativePath);

  const listFields = [
    "implementation.code_paths",
    "implementation.prompt_paths",
    "implementation.context_paths",
    "implementation.runbook_paths",
    "implementation.script_paths",
    "implementation.docs_paths",
    "runtime.constructor_dependencies",
    "runtime.dependencies",
    "capabilities.tools",
    "capabilities.allowed_operations",
    "capabilities.forbidden_operations",
    "data.systems",
    "data.resources",
    "data.classifications",
    "governance.requires_action_proposal_for",
    "governance.approval_required_for",
    "knowledge.memory_paths",
    "knowledge.provenance_labels",
    "knowledge.update_rules",
    "observability.logs",
    "observability.metrics",
    "observability.audit_events",
    "tests.unit",
    "tests.integration",
    "tests.safety",
    "tests.contract",
    "relationships.upstream_agents",
    "relationships.downstream_agents",
    "relationships.workflows",
    "relationships.conflicts_with",
  ];

  for (const field of listFields) {
    const value = field.split(".").reduce((current, key) => current && current[key], manifest);
    requireArray(value, field, relativePath);
  }

  if (typeof manifest.runtime.implemented !== "boolean") fail(`${relativePath}: runtime.implemented must be boolean`);
  if (typeof manifest.runtime.factory_supported !== "boolean") fail(`${relativePath}: runtime.factory_supported must be boolean`);
  if (typeof manifest.governance.read_only !== "boolean") fail(`${relativePath}: governance.read_only must be boolean`);
  if (typeof manifest.governance.can_propose_write !== "boolean") fail(`${relativePath}: governance.can_propose_write must be boolean`);
  if (typeof manifest.governance.can_execute_write !== "boolean") fail(`${relativePath}: governance.can_execute_write must be boolean`);
  if (typeof manifest.governance.can_execute_destructive_operation !== "boolean") fail(`${relativePath}: governance.can_execute_destructive_operation must be boolean`);
  if (typeof manifest.governance.policy_engine_required !== "boolean") fail(`${relativePath}: governance.policy_engine_required must be boolean`);
  if (typeof manifest.governance.audit_required !== "boolean") fail(`${relativePath}: governance.audit_required must be boolean`);
  if (!allowedRisks.has(manifest.governance.default_risk)) fail(`${relativePath}: invalid default_risk ${manifest.governance.default_risk}`);
  if (!allowedEnforcement.has(manifest.governance.enforcement_status)) fail(`${relativePath}: invalid enforcement_status ${manifest.governance.enforcement_status}`);
  if (typeof manifest.observability.redaction_required !== "boolean") fail(`${relativePath}: observability.redaction_required must be boolean`);
  if (typeof manifest.catalog.display_order !== "number") fail(`${relativePath}: catalog.display_order must be number`);
}

function validatePaths(manifest, relativePath) {
  const pathGroups = [
    ...manifest.implementation.code_paths,
    ...manifest.implementation.prompt_paths,
    ...manifest.implementation.context_paths,
    ...manifest.implementation.runbook_paths,
    ...manifest.implementation.script_paths,
    ...manifest.implementation.docs_paths,
    ...manifest.knowledge.memory_paths,
    ...manifest.tests.unit,
    ...manifest.tests.integration,
    ...manifest.tests.safety,
  ];

  for (const candidate of pathGroups) {
    const absolute = path.join(repoRoot, candidate);
    if (!fs.existsSync(absolute)) {
      fail(`${relativePath}: referenced path does not exist: ${candidate}`);
    }
  }
}

function validateSecrets(text, relativePath) {
  for (const pattern of secretPatterns) {
    if (pattern.test(text)) {
      fail(`${relativePath}: potential secret material detected by ${pattern}`);
    }
  }
}

function main() {
  if (!fs.existsSync(schemaPath)) fail("agents/agent.schema.json is missing");
  JSON.parse(fs.readFileSync(schemaPath, "utf8"));

  const manifestPaths = fs
    .readdirSync(agentsRoot, { withFileTypes: true })
    .filter((entry) => entry.isDirectory())
    .map((entry) => path.join(agentsRoot, entry.name, "agent.yaml"))
    .filter((candidate) => fs.existsSync(candidate))
    .sort();

  const ids = new Set();
  const manifests = [];

  for (const manifestPath of manifestPaths) {
    const relativePath = path.relative(repoRoot, manifestPath);
    const text = fs.readFileSync(manifestPath, "utf8");
    validateSecrets(text, relativePath);

    let manifest;
    try {
      manifest = parseManifest(text, relativePath);
    } catch (error) {
      fail(error.message);
      continue;
    }

    validateRequiredShape(manifest, relativePath);
    validatePaths(manifest, relativePath);

    if (ids.has(manifest.id)) fail(`${relativePath}: duplicate id ${manifest.id}`);
    ids.add(manifest.id);
    manifests.push(manifest);
  }

  for (const expected of expectedAgentIds) {
    if (!ids.has(expected)) fail(`missing manifest for known Agent ${expected}`);
  }

  for (const manifest of manifests) {
    if (manifest.id !== path.basename(path.dirname(path.join(agentsRoot, manifest.id, "agent.yaml")))) {
      // Kept explicit below with actual path directory check.
    }
  }

  for (const manifestPath of manifestPaths) {
    const manifest = manifests.find((item) => item.id === path.basename(path.dirname(manifestPath)));
    if (!manifest) {
      fail(`${path.relative(repoRoot, manifestPath)}: manifest id must match directory name`);
    }
  }

  if (!process.exitCode) {
    console.log(`PASS: ${manifests.length} agent manifests validated`);
    console.log(`PASS: IDs unique: ${[...ids].sort().join(", ")}`);
    console.log("PASS: required paths exist");
    console.log("PASS: no secret values detected in manifests");
  }
}

main();
