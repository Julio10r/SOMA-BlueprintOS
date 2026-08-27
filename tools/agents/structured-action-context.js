const ENVIRONMENTS = Object.freeze(["Unknown", "Development", "Homologation", "Production"]);
const OPERATION_INTENTS = Object.freeze([
  "UNKNOWN", "READ", "ANALYZE", "EXPORT", "CREATE", "UPDATE", "DELETE", "TRUNCATE",
  "EXECUTE_WORKFLOW", "CONFIGURE",
]);
const RESOURCE_TYPES = Object.freeze([
  "Unknown", "DatabaseTable", "DatabaseSchema", "ApiEndpoint", "FileExport", "Prompt",
  "Log", "Permission", "ExternalSystem",
]);
const DATA_CLASSIFICATIONS = Object.freeze([
  "Unknown", "Public", "Internal", "Confidential", "PersonalData", "SensitivePersonalData",
  "SecretCredential",
]);
const REVERSIBILITY = Object.freeze(["Unknown", "Reversible", "PartiallyReversible", "Irreversible"]);

function normalizedList(value) {
  return Array.isArray(value) ? [...new Set(value.map((item) => String(item).trim()).filter(Boolean))] : [];
}

function normalizeActionContext(input = {}) {
  return {
    request_id: typeof input.request_id === "string" ? input.request_id.trim() : "",
    requested_by: typeof input.requested_by === "string" ? input.requested_by.trim() : "",
    environment: input.environment || "Unknown",
    system: typeof input.system === "string" ? input.system.trim() : "",
    resource_type: input.resource_type || "Unknown",
    resource: typeof input.resource === "string" ? input.resource.trim() : "",
    operation_intent: input.operation_intent || "UNKNOWN",
    requested_capabilities: normalizedList(input.requested_capabilities),
    fields: normalizedList(input.fields),
    filter_summary: typeof input.filter_summary === "string" && input.filter_summary.trim() ? input.filter_summary.trim() : null,
    expected_affected_rows: Number.isInteger(input.expected_affected_rows) && input.expected_affected_rows >= 0
      ? input.expected_affected_rows : null,
    purpose: typeof input.purpose === "string" ? input.purpose.trim() : "",
    data_classifications: normalizedList(input.data_classifications),
    contains_personal_data: input.contains_personal_data === true,
    contains_sensitive_personal_data: input.contains_sensitive_personal_data === true,
    contains_secrets: input.contains_secrets === true,
    reversibility: input.reversibility || "Unknown",
    runbook_reference: typeof input.runbook_reference === "string" && input.runbook_reference.trim() ? input.runbook_reference.trim() : null,
    workflow_reference: typeof input.workflow_reference === "string" && input.workflow_reference.trim() ? input.workflow_reference.trim() : null,
    connection_profile: typeof input.connection_profile === "string" && input.connection_profile.trim() ? input.connection_profile.trim() : null,
    additional_context: typeof input.additional_context === "string" && input.additional_context.trim() ? input.additional_context.trim() : null,
  };
}

function validateActionContext(input) {
  const context = normalizeActionContext(input);
  const errors = [];
  const contextGaps = [];
  const enumCheck = (values, value, field) => {
    if (!values.includes(value)) errors.push({ field, code: "INVALID_VALUE", message: `${field} has unsupported value ${value}` });
  };

  enumCheck(ENVIRONMENTS, context.environment, "environment");
  enumCheck(OPERATION_INTENTS, context.operation_intent, "operation_intent");
  enumCheck(RESOURCE_TYPES, context.resource_type, "resource_type");
  enumCheck(REVERSIBILITY, context.reversibility, "reversibility");
  for (const classification of context.data_classifications) enumCheck(DATA_CLASSIFICATIONS, classification, "data_classifications");

  for (const field of ["request_id", "requested_by", "system", "resource"]) {
    if (!context[field]) contextGaps.push({ field, code: "REQUIRED_CONTEXT_MISSING" });
  }
  if (context.environment === "Unknown") contextGaps.push({ field: "environment", code: "ENVIRONMENT_UNKNOWN" });
  if (context.resource_type === "Unknown") contextGaps.push({ field: "resource_type", code: "RESOURCE_TYPE_UNKNOWN" });
  if (context.operation_intent === "UNKNOWN") contextGaps.push({ field: "operation_intent", code: "OPERATION_INTENT_UNKNOWN" });
  if (context.data_classifications.length === 0) contextGaps.push({ field: "data_classifications", code: "DATA_CLASSIFICATION_MISSING" });

  const mutation = ["CREATE", "UPDATE", "DELETE", "TRUNCATE", "CONFIGURE", "EXECUTE_WORKFLOW"].includes(context.operation_intent);
  if ((mutation || context.operation_intent === "EXPORT") && !context.purpose) {
    contextGaps.push({ field: "purpose", code: "SENSITIVE_PURPOSE_MISSING" });
  }
  if (context.operation_intent === "UPDATE" && !context.filter_summary) {
    contextGaps.push({ field: "filter_summary", code: "UPDATE_FILTER_MISSING" });
  }
  if (["UPDATE", "EXPORT"].includes(context.operation_intent) && context.expected_affected_rows === null) {
    contextGaps.push({ field: "expected_affected_rows", code: "EXPECTED_IMPACT_UNKNOWN" });
  }

  return { valid: errors.length === 0, context, errors, context_gaps: contextGaps };
}

module.exports = {
  DATA_CLASSIFICATIONS,
  ENVIRONMENTS,
  OPERATION_INTENTS,
  RESOURCE_TYPES,
  REVERSIBILITY,
  normalizeActionContext,
  validateActionContext,
};
