#!/usr/bin/env node

const fs = require("fs");
const path = require("path");
const { AgentFactoryV2, OPERATIONS, assertSafeOutput } = require("./agent-factory-v2");

function option(name) {
  const index = process.argv.indexOf(name);
  return index >= 0 ? process.argv[index + 1] : null;
}

function readJson(file) {
  return JSON.parse(fs.readFileSync(path.resolve(file), "utf8"));
}

const operation = (process.argv[2] || "").toUpperCase().replaceAll("-", "_");
const factory = new AgentFactoryV2();
let result;

switch (operation) {
  case "VALIDATE": result = factory.validate(); break;
  case "AUDIT": result = factory.audit(); break;
  case "REGISTER": result = factory.register(process.argv[3]); break;
  case "CATALOG": result = factory.catalog({ apply: process.argv.includes("--apply"), output: option("--output"), authorization: option("--authorization") ? readJson(option("--authorization")) : null }); break;
  case "TEST": result = factory.testPlan(process.argv[3]); break;
  case "SECURITY_CHECK": {
    const record = factory.discover().find((item) => item.manifest.id === process.argv[3]);
    if (!record) throw new Error(`Unknown Agent ${process.argv[3]}`);
    result = factory.securityCheck(record.manifest);
    break;
  }
  case "CREATE": result = factory.create({ ...readJson(option("--request")), apply: process.argv.includes("--apply") }); break;
  case "UPDATE": result = factory.update({ ...readJson(option("--request")), apply: process.argv.includes("--apply") }); break;
  default: throw new Error(`Operation required. Supported: ${OPERATIONS.join(", ")}`);
}

const output = option("--output");
const json = `${JSON.stringify(result, null, 2)}\n`;
if (output && operation !== "CATALOG") fs.writeFileSync(path.join(process.cwd(), assertSafeOutput(output, "audit")), json);
else process.stdout.write(json);

if (result.status === "FAIL") process.exitCode = 1;
