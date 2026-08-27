# Database Connection Policy v1 — Linx/SOMA (Development / Production)

Status: canonical, effective immediately.
Applies to: Codex, Claude, ChatGPT, all current and future agents/executors touching Linx/SOMA
connections (implementation: `agents/linx-database-specialist-agent/agent.yaml`).
Consolidates, does not replace: `agents/EXECUTION_POLICY.md` § Credenciais E Conexoes and
`agents/AGENT_CONTRACT.md` § `connections.credential_policy` remain canonical for cross-cutting
credential rules; this document is the canonical source for the Linx/SOMA environment split
specifically.

## 1. Environments

Two distinct Linx/SOMA ERP environments exist. Never infer one from the other.

| | Development | Production |
|---|---|---|
| Server | `192.168.9.98` | `192.168.0.200` |
| Database | `SOMA_DESENV` | `SOMA` |
| ASP.NET environment | `Development` | `Production` |
| VPN | required | required |
| Purpose | analysis, development, investigation, testing, experimentation | real ERP data and operations |
| Destructive ops (SELECT/INSERT/UPDATE/DELETE/CREATE/ALTER/DROP/TRUNCATE/EXEC/DDL/DML) | may be permitted with clear context and effective DB permission — SOMA_DESENV is our technical lab | conservative; governed writes only |

`SOMA_DESENV` is never confused with `SOMA`. Operations without clear context, ambiguous intent,
or a doubtful target stay blocked or must generate a question to the user, in both environments.

## 2. Profiles

Logical profile != credential. Profiles are versionable metadata (server, database, environment,
VPN requirement); user/password are never part of a profile.

- `linx-development` — environment `Development`, server `192.168.9.98`, database `SOMA_DESENV`,
  port `1433`, `vpn_required: true`, `credential_source: local`.
- `linx-production` — environment `Production`, server `192.168.0.200`, database `SOMA`,
  port `1433`, `vpn_required: true`, `credential_source: local`.
- `maiscompras-development` — environment `Development`, server `192.168.9.98`, database
  `MAISCOMPRAS`, port `1433`, `vpn_required: true`, `credential_source: local`. Same DEV server as
  `linx-development`; may resolve the same local identity as `linx-development` without either
  profile carrying the credential (§ 3.1).

Implemented as `LinxConnectionProfiles.Development` / `.Production` / `.MaisComprasDevelopment` in
[`backend/src/BlueprintOS.Infrastructure/Persistence/LinxConnectionProfile.cs`](../backend/src/BlueprintOS.Infrastructure/Persistence/LinxConnectionProfile.cs).

## 3. Connection strings

Canonical keys, semantically separated:

- `ConnectionStrings:LinxDevelopmentConnection`
- `ConnectionStrings:LinxProductionConnection`
- `ConnectionStrings:MaisComprasConnection` — DEV-only, same server as `LinxDevelopmentConnection`
  (`192.168.9.98`), different database (`MAISCOMPRAS`). See § 3.1.

All seven direct Linx/SOMA consumers (`SomaFilialReader`, `SomaFornecedorReader`,
`SomaCentroCustoReader`, `LinxSchemaDiscoveryReader`, `ErpFornecedorDiscoveryRepository`,
`SomaGarantirFornecedorErpAdapter`, `SomaDesenvolErpFornecedorAdapter`) now resolve their
connection string through `LinxConnectionStringResolver.Resolve(configuration,
LinxConnectionProfiles.Development)` — a single shared resolution point
(`backend/src/BlueprintOS.Infrastructure/Persistence/LinxConnectionStringResolver.cs`) instead of
each reader independently calling `configuration.GetConnectionString("ErpConnection")` and
duplicating its own placeholder/database guard. No consumer decides environment by inspecting the
connection string's contents — they declare the profile explicitly (`LinxConnectionProfiles.Development`
today; a future consumer needing Production would pass `LinxConnectionProfiles.Production`
explicitly, never infer it).

`ConnectionStrings:ErpConnection` is **DEPRECATED / LEGACY**. It is kept as a compatibility
fallback inside `LinxConnectionStringResolver`, for the Development profile only (never for
Production) — read only when `LinxDevelopmentConnection` is absent, and still validated against the
Development profile (mismatch protection applies identically to the fallback). No consumer reads
`ErpConnection` directly anymore; nothing decides environment from it. New code must use the
canonical keys. Planned removal: once every developer machine has `LinxDevelopmentConnection`
configured (§ 4), drop the fallback and the `ErpConnection` key entirely in a follow-up,
non-functional-scope change.

### 3.1 MAISCOMPRAS — sharing the DEV identity without duplicating a secret

The DEV server (`192.168.9.98`) hosts two databases: `SOMA_DESENV` (Linx ERP) and `MAISCOMPRAS`
(+Compras, BlueprintOS's own application database). Both are reachable with the same DEV identity.
BlueprintOS models this as two logical profiles that legitimately point at the same server and can
resolve the same local credential, without either profile carrying or duplicating the secret:

- `linx-development` → `ConnectionStrings:LinxDevelopmentConnection` → `192.168.9.98` / `SOMA_DESENV`
- `maiscompras-development` → `ConnectionStrings:MaisComprasConnection` → `192.168.9.98` / `MAISCOMPRAS`

The .NET stack requires one complete connection string per target database (`SqlConnection` cannot
target two databases through a single string), so this is implemented as two distinct
`ConnectionStrings` keys — not two secrets. The credential a developer configures for
`MaisComprasConnection` is the same DEV login already used for `LinxDevelopmentConnection` (same
`User Id`/`Password` pasted twice into two separate connection strings, since SQL Server connection
strings are self-contained); no new identity, no new secret store, no secret duplicated in Git. This
was already `BlueprintOSDbContext`'s connection (`MaisComprasConnection` pre-dates this policy); this
section only formalizes it as a first-class profile with the same mismatch/VPN protections as the
other two, via `LinxConnectionProfiles.MaisComprasDevelopment` and
`B1ConnectivityValidator.ValidateMaisComprasAsync()`.

## 4. Credentials

DEV and PROD have different servers, databases, users and passwords. Never reuse or infer a
credential between environments.

User/password:
- never in Git, manifests, versioned YAML, docs, logs, or chat.
- each developer configures their own credentials locally via `dotnet user-secrets`.

```bash
# Development — run locally, never paste a real password into chat or a versioned file
dotnet user-secrets set "ConnectionStrings:LinxDevelopmentConnection" \
  "Server=192.168.9.98;Database=SOMA_DESENV;User Id=<seu-usuario>;Password=<sua-senha>;Encrypt=True;TrustServerCertificate=True" \
  --project backend/src/BlueprintOS.Api/BlueprintOS.Api.csproj

# Production — run locally, only when you actually hold a Production credential
dotnet user-secrets set "ConnectionStrings:LinxProductionConnection" \
  "Server=192.168.0.200;Database=SOMA;User Id=<seu-usuario>;Password=<sua-senha>;Encrypt=True;TrustServerCertificate=True" \
  --project backend/src/BlueprintOS.Api/BlueprintOS.Api.csproj
```

Both keys live in the same existing User Secrets store (`UserSecretsId: BlueprintOS-Development`
in `backend/src/BlueprintOS.Api/BlueprintOS.Api.csproj`) — no new secret store was introduced.

## 5. Environment selection

- "Analyze the structure of table X" → `SOMA_DESENV` is acceptable by default.
- "Update table X" with the target environment unclear and a possible real effect →
  **ask the user**.
- "Update table X in production" → `SOMA` / `192.168.0.200`, full Production governance.

Validation that depends on real data may read Production read-only when necessary and governed.
Real execution always uses Production with full governance.

## 6. Environment mismatch protection

`B1ConnectivityValidator` compares the configured connection string's resolved server/database
against the expected profile **before opening any network connection**. Any of the following is
blocked deterministically as `ConnectivityStatus.EnvironmentMismatch`:

- `environment = Production`, configured target = Development server/database.
- `environment = Development`, configured target = Production server/database.
- expected database `SOMA`, configured database `SOMA_DESENV` (or vice versa).
- expected database `MAISCOMPRAS`, configured database `SOMA_DESENV` (or vice versa) — same DEV
  server does not exempt a profile from its own database check.

The same `LinxConnectionStringResolver.Resolve(configuration, profile)` enforces this for every
consumer, not just the connectivity validator — the guard lives in one place
(`backend/src/BlueprintOS.Infrastructure/Persistence/LinxConnectionStringResolver.cs`) and every
Linx/SOMA/+Compras reader/adapter goes through it. No write, and no SQL Server round-trip, happens
before this check passes.

## 7. VPN and transient connectivity — one automatic retry, never a diagnosis

All three profiles (`linx-development`, `linx-production`, `maiscompras-development`) require the
corporate VPN to already be connected — `vpn_required: true` is a **characteristic of the profile**
(this database is only reachable over VPN), never an automatic diagnosis of *why* a given
connection attempt failed. In practice, the corporate VPN can be connected and still drop the SQL
Server session momentarily (link blip, server-side reset) — a single failed attempt does not prove
"VPN disconnected", and BlueprintOS must not declare that prematurely.

Behavior on a network-level failure (DNS/TCP unreachable, connection timeout, socket exception,
`TimeoutException`, or a `SqlException` whose error text matches a network-unreachable pattern —
see § 13):

1. The first failure is not reported to the operator yet. Exactly **one** automatic retry is
   attempted, after a short fixed delay (750ms) — long enough to absorb a momentary blip, short
   enough to stay fast.
2. If the retry succeeds: the result is `Ready`, `RecoveredAfterRetry = true` is recorded
   internally (surfaced as a discreet CLI note), and nothing further is asked of the operator.
3. If the retry also fails: `ConnectivityStatus.ConnectivityUnavailable` is returned, and the
   operator guidance is exactly: **"Não foi possível acessar o servidor após uma tentativa de
   restabelecimento. Verifique/reconecte a VPN ou a conexão com o servidor e tente novamente."**
   No further retry is attempted from here — see § 13 for what happens next.

There is no loop: at most 1 retry, ever, per validation call. Credential/permission failures
(`PermissionDenied`), `EnvironmentMismatch`, and `NotConfigured` are **never** eligible for this
retry — they are not network instability, and retrying them would look like the system politely
waiting to try the same invalid login twice. `ConnectivityUnavailable` is a statement of observed
fact ("connectivity could not be reestablished"), not a claim about the root cause — it is never
conflated with `PermissionDenied` (SQL auth/authz error numbers `18456, 229, 230, 262, 4060`), and
it never asserts "the VPN is disconnected" as a diagnosis, only that the operator should check it.

## 8. Connection status

`ConnectivityStatus` (`backend/src/BlueprintOS.Infrastructure/Persistence/B1ConnectivityValidator.cs`):

`Ready | NotConfigured | Failed | PermissionDenied | EnvironmentMismatch | ConnectivityUnavailable`

## 9. Identity and permission

Authorization depends on two layers: BlueprintOS governance **and** the identity's effective
database permission.

- Governance allows + database denies → **blocked**.
- Governance denies + database allows → **blocked**.
- Never execute `GRANT`, `REVOKE`, `ALTER LOGIN`, `ALTER USER`, or a role change to work around a
  missing permission.

A credential technically having `DROP`/`UPDATE`/etc. does not mean BlueprintOS is authorized to use
that capability freely in Production.

## 10. First clone

```
git clone
  → BlueprintOS knows the linx-development / linx-production profiles
  → no credential ships with the repo
  → validator reports NotConfigured for each unset profile
  → developer runs the dotnet user-secrets commands in § 4 locally
  → validator tests connectivity, identifies the real server/database/identity
  → effective database permission is respected as-is (never escalated)
```

## 11. Development governance

Development is a governed technical lab, not a free-for-all. Destructive operations (CREATE, DROP,
TRUNCATE, ...) can be legitimate in `SOMA_DESENV` when: context is clear, purpose is clear, the
target is clearly `SOMA_DESENV`, the responsible agent has the matching capability, the identity
has permission, there is no risk of touching Production, and there is traceability.

Valid: *"Crie uma tabela de teste em SOMA_DESENV, valide a solução e depois remova."*
Not valid without more context: *"Trunque alguma tabela para testar"* — ask for the specific
resource/purpose, or block.

## 12. Production governance

Production stays conservative. No relaxation of any existing Production protection. Special
attention: bulk `UPDATE`, `DELETE`, `TRUNCATE`, `DROP`, `ALTER`, `MERGE`, mutating procedure
`EXEC`, data export, PII, sensitive personal data, secret credentials. Writes go through: Agent
responsible → Security/LGPD when applicable → ActionProposal → Policy Engine → Impact Analysis →
Approval when required → Tool Gateway → real identity → effective DB permission → execution →
post-validation → audit.

## 13. Validator

`B1ConnectivityValidator.ValidateErpAsync(LinxEnvironment environment)` validates the selected
profile explicitly:

1. Resolves the connection string for the profile's canonical key (Development additionally falls
   back to the deprecated `ErpConnection` key — § 3).
2. Missing/placeholder value → `NotConfigured` (no connection attempt).
3. Parses the connection string and compares server/database against the profile's expected
   values → mismatch blocks as `EnvironmentMismatch`, before any network I/O.
4. Opens the connection (via `ISqlConnectivityProbe`, a test seam — the production implementation
   is `SqlConnectivityProbe`), runs `SELECT 1;` as a read-only probe.
5. Best-effort `SELECT SUSER_SNAME();` to capture the effective login identity (never the
   credential itself).
6. Classifies failures: SQL auth/authz error numbers → `PermissionDenied` (never retried); a
   network-unreachable error (DNS/TCP failure, timeout, socket exception, or a `SqlException` whose
   message matches the network-unreachable pattern — including the `Number == 0` "server not found
   or not accessible" case some environments raise) → exactly 1 automatic retry after 750ms (§ 7);
   if the retry succeeds, `Ready` with `RecoveredAfterRetry = true`; if it also fails,
   `ConnectivityUnavailable`; anything else → `Failed`.

Never prints the connection string or password — only `Server`, `Database`, and
`EffectiveIdentity` (identity resolved by the DB itself) are exposed on the result.
`ValidateMaisComprasAsync()` runs the identical procedure against
`LinxConnectionProfiles.MaisComprasDevelopment`. Both `ValidateErpAsync` and
`ValidateMaisComprasAsync` are thin wrappers over the same
`LinxConnectionStringResolver.Resolve` used by every other Linx/SOMA consumer (§ 3), plus the
network round-trip and identity probe.

## 14. Agent Factory

`agents/linx-database-specialist-agent/agent.yaml` now declares two connection profiles
(`linx-development`, `linx-production`) instead of one ambiguous `linx-erp-*` pair, validated by
`tools/agents/validate-agent-manifests.js` against `agents/agent.schema.json`. New agents that use
the Linx/SOMA database must reference one of these two profiles by name and treat "logical profile
!= credential" as a baseline invariant — no material change to the Agent Factory audit rules was
needed beyond this manifest update.

## 15. Tests

`backend/tests/BlueprintOS.UnitTests/Infrastructure/Persistence/B1ConnectivityValidatorTests.cs`
covers, without any network/DB dependency: Development resolves only Development (never leaks into
Production and vice versa), missing Development/Production/MaisCompras secret → `NotConfigured`,
the `ErpConnection` legacy fallback is still validated against the Development profile, environment
mismatch is blocked deterministically before any connection attempt — SOMA ↔ SOMA_DESENV,
MAISCOMPRAS ↔ SOMA_DESENV, and DEV server ↔ PROD server, in both directions — and no result ever
carries the connection string or password. `LinxConnectionStringResolver` itself is covered
directly (not-configured and mismatch paths, both exception-based and synchronous — no async/DB
wait needed). `tests/BlueprintOS.UnitTests/Infrastructure/Integrations/ERP/ErpReadersReadOnlyTests.cs`
continues to pass unchanged after the reader migration (its mismatch-message assertion still holds
under the new resolver). The single-retry behavior (§ 7) is covered via a fake `ISqlConnectivityProbe`
(no real SQL Server or VPN involved): first attempt fails + retry succeeds → `Ready` with
`RecoveredAfterRetry`; both attempts fail → `ConnectivityUnavailable`; permission-denied, mismatch,
and not-configured are each asserted to make **zero** probe calls (never retried); and a guard test
asserts at most 2 probe calls regardless of how many failures a misbehaving fake queues, so a future
change can't accidentally turn the single retry into a loop. All tests complete in milliseconds
(the one retry test waits out the fixed 750ms delay, nothing more) — no live SQL Server or VPN is
required to run them.

## 16. On doubt

Ask the user. Never infer, never reuse a credential across environments, and no technical
capability of a Production credential substitutes for BlueprintOS governance.

## 17. Authoritative source policy (v1.1)

Canonical principle, effective for Codex, Claude, ChatGPT, and every current/future Agent or
executor: **Production is the authoritative source for investigating the current state of the
Linx/SOMA ERP. `SOMA_DESENV` is a development/test laboratory, not an automatic proxy for
production truth.**

`SOMA_DESENV` is usually structurally similar to Production, but its objects and — especially —
its data can be stale: schema/procedure changes in Development go through validation before they
reach Production, and objects in Development can be forgotten or left outdated by developers.
`SOMA_DESENV` alone therefore never settles a question about "what is true today in the real ERP."

When the intent is to understand current schema, a table, a view, a trigger, a procedure, a
business rule, cardinality, current data, a size grade, a relationship, or any other question about
the Linx ERP's real, present-day behavior: use `linx-production` (`192.168.0.200` / `SOMA`) by
default, read-only, per § 18. This supersedes no prior section of this document — § 1–§ 16 already
establish Production as conservative/governed for writes; this section makes it additionally the
default read target for investigation.

When the intent is to develop, experiment, reproduce a scenario, test a SQL/procedure change, build
a staging table, or validate a solution before it goes to Production: use `linx-development`
(`SOMA_DESENV`) per § 11, unchanged.

## 18. Production read-only investigation

Investigating in Production is not, by itself, authorization to write in Production. Unless a
write is explicitly requested and governed per § 12, Production investigation is limited to
operations equivalent to: `SELECT`, object/definition metadata (`sys.tables`, `sys.columns`,
`sys.indexes`, `sys.procedures`, `sys.sql_modules`, `OBJECT_DEFINITION`, `INFORMATION_SCHEMA.*`),
and identity/environment probes (`DB_NAME()`, `SUSER_SNAME()`, `SERVERPROPERTY()`). Do not execute a
mutating procedure just to observe its behavior — inspect its definition instead (as already done
for the 4 procedures in `docs/audits/AgentLearningV1-LinxProgOpPed.md` § 7.6, on `SOMA_DESENV`; the
same read-only technique applies to Production).

## 19. Evidence provenance by environment

Every piece of database-derived evidence must carry the environment it came from. Use labels
equivalent to `CONFIRMED_IN_PRODUCTION` / `CONFIRMED_IN_DEVELOPMENT` (or the existing
`CONFIRMED_BY_SCHEMA` / `CONFIRMED_BY_DATA_VALIDATION` / `CONFIRMED_BY_CODE_INSPECTION` labels,
qualified with the environment they were obtained from). Knowledge obtained only in `SOMA_DESENV`
must never be silently promoted to a claim about Production's current state. If the conclusion
depends on the ERP's real, current condition, use a status equivalent to
`CONFIRMED_IN_DEVELOPMENT` + `NEEDS_PRODUCTION_VALIDATION` (or `PENDING_PRODUCTION_VALIDATION`).
Only evidence confronted against Production may be promoted to authoritative current knowledge of
the Linx ERP.

## 20. Production unavailable — no silent fallback

If Production is temporarily unreachable, BlueprintOS must **not** silently substitute
`SOMA_DESENV` as if it answered the same question. Apply the existing single connectivity retry
(§ 7) first. If Production stays unavailable after that retry, report a status equivalent to
`PRODUCTION_VALIDATION_PENDING` and inform the user — do not fabricate a Production answer from
Development data. `SOMA_DESENV` may still be used as auxiliary investigation, but any resulting
evidence must be explicitly marked as not confirmed in Production (§ 19). This is the same
`ConnectivityUnavailable` mechanism already implemented by `B1ConnectivityValidator` (§ 7–§ 8) —
this section only forbids treating that outcome as license to swap environments.

## 21. Preparing Development objects before development/testing

When an existing object needs to be changed and tested, never assume the version currently in
`SOMA_DESENV` is the latest:

1. investigate the current version in Production (read-only, § 18);
2. obtain the object's current definition (`OBJECT_DEFINITION`, `sys.sql_modules`);
3. compare it against the `SOMA_DESENV` definition;
4. if `SOMA_DESENV` is stale, prepare/sync the needed version into `SOMA_DESENV` in a governed way
   (explicit context, explicit purpose, traceable);
5. only then start development/testing in `SOMA_DESENV` (§ 11, § 14).

## 22. Controlled Production -> Development data reproduction

BlueprintOS may propose bringing data from Production into `SOMA_DESENV` when needed to reproduce
or test a real scenario. This is a distinct, higher-scrutiny capability from ordinary Production
read-only investigation (§ 18) — it pairs a Production **read** with a Development **write**, and
Development's permissiveness (§ 11, § 14) does not remove governance over the origin of the data.
Conceptual flow: identify the minimum necessary dataset in Production -> classify the data ->
apply Security/LGPD review when personal/sensitive/secret data is involved -> propose the copy,
with origin, destination, purpose, volume, classification, filters, dependencies, impact, and
reversibility made explicit -> obtain governance approval -> copy into `SOMA_DESENV` -> validate the
reproduction -> test.

Minimization: never copy a full production table for convenience. Copy only the records and
dependencies needed to reproduce the specific scenario (**minimum necessary dataset**) — e.g. for a
77-row spreadsheet scenario, identify and copy only those 77 rows' dependencies, never the entire
source table.

LGPD / sensitive data: classify before copying. `PersonalData`, `SensitivePersonalData`, or
`SecretCredential` (or any other protected classification) triggers Security/LGPD review per
existing policy; prefer anonymization, masking, pseudonymization, or dropping unnecessary columns.
Passwords, tokens, secrets, and credentials are never copied into `SOMA_DESENV` under this flow —
this is an application of § 4, not an exception to it.

This is **on-demand controlled reproduction**, not replication: BlueprintOS does not build an
automatic mechanism to keep `SOMA_DESENV` mirrored to Production. Production stays authoritative;
`SOMA_DESENV` receives only what a specific, governed reproduction need requires. When data is
copied for a temporary/staging purpose, record origin, environment, purpose, filters, quantity,
tables, timestamp, responsible Agent/use case, and related `ActionProposal` where available, and
define an explicit cleanup strategy — never assume a `DELETE` is authorized without an explicit
governed decision for that specific case.

**Status of this section: DOCUMENTED, not yet ENFORCED in code.** No tool, Agent, or code path in
this repository currently executes a Production -> Development data copy; this section defines the
policy a future implementation must follow. No copy was performed as part of adding this section
(§ 26 of the governing task explicitly excluded it).

## 23. Agent Factory / new agents

Any Agent that touches a database, current or future, must be evaluated for whether it knows:
authoritative environment vs. test environment (§ 17), evidence provenance by environment (§ 19),
Development/Production drift (§ 19), and controlled PROD->DEV reproduction (§ 22). This is
inherited today via `agents/AGENT_CONTRACT.md` § "Politicas Canonicas Relacionadas" referencing this
document, and via each Agent's own `knowledge.update_rules` (see
`agents/linx-database-specialist-agent/agent.yaml` and
`agents/linx-erp-specialist-agent/agent.yaml`) — no `agent.schema.json` change was needed for this.
