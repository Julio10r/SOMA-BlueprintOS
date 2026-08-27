#!/usr/bin/env node

const { RuntimeRegistry } = require("./runtime-registry");

function usage() {
  console.error("Usage: node tools/agents/runtime-registry-cli.js <list|capabilities|resolve|plan> [capability ...]");
  process.exitCode = 2;
}

const [, , command, ...args] = process.argv;
const registry = new RuntimeRegistry();

if (command === "list") console.log(JSON.stringify(registry.describe(), null, 2));
else if (command === "capabilities") console.log(JSON.stringify(registry.listCapabilities(), null, 2));
else if (command === "resolve" && args.length === 1) console.log(JSON.stringify(registry.resolveCapability(args[0]), null, 2));
else if (command === "plan" && args.length > 0) console.log(JSON.stringify(registry.buildRoutingPlan(args), null, 2));
else usage();
