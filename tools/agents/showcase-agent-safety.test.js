#!/usr/bin/env node

const assert = require("assert");
const fs = require("fs");
const path = require("path");
const { parseManifest } = require("./validate-agent-manifests");

const repoRoot = path.resolve(__dirname, "../..");
const read = (relativePath) => fs.readFileSync(path.join(repoRoot, relativePath), "utf8");
const manifest = parseManifest(read("agents/showcase-agent/agent.yaml"), "agents/showcase-agent/agent.yaml");
const collector = read("scripts/showcase_collector/collect.js");
const enricher = read("scripts/showcase_collector/enrich.js");
const excelBuilder = read("scripts/showcase_collector/build_excel.js");
const scripts = `${collector}\n${enricher}\n${excelBuilder}`;

assert.equal(manifest.governance.read_only, true);
assert.equal(manifest.governance.can_execute_write, false);
assert.equal(manifest.governance.can_execute_destructive_operation, false);
assert.equal(manifest.delegation.bypass_allowed, false);
assert.equal(manifest.connections.credential_policy.privilege_escalation_allowed, false);
assert.equal(manifest.governance.enforcement_status, "DOCUMENTAL");

assert(collector.includes("process.env.SHOWCASE_TOKEN"));
assert(enricher.includes("process.env.SHOWCASE_TOKEN"));
assert(!/SHOWCASE_TOKEN\s*=\s*["'][^"']+["']/.test(scripts), "No token literal may be assigned in scripts");
assert(!/Bearer\s+[A-Za-z0-9._-]{16,}/.test(scripts), "No bearer credential may be hardcoded");
assert(!/method\s*:\s*["'](?:POST|PUT|PATCH|DELETE)["']/i.test(scripts), "Showcase scripts must not use write HTTP methods");
assert(!/(?:writeFileSync|appendFileSync)\s*\([^\n]*SHOWCASE_TOKEN/.test(scripts), "Token must not be persisted in generated files");
assert(!/(?:writeFileSync|appendFileSync)\s*\([^\n]*\bTOKEN\b/.test(scripts), "Token variable must not be persisted in generated files");
assert(collector.includes("method: 'HEAD'"), "HEAD is the only explicit HTTP method expected");

console.log("PASS: Showcase Agent offline safety invariants");
