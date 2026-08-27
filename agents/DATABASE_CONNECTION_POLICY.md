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

Implemented as `LinxConnectionProfiles.Development` / `LinxConnectionProfiles.Production` in
[`backend/src/BlueprintOS.Infrastructure/Persistence/LinxConnectionProfile.cs`](../backend/src/BlueprintOS.Infrastructure/Persistence/LinxConnectionProfile.cs).

## 3. Connection strings

Canonical keys, semantically separated:

- `ConnectionStrings:LinxDevelopmentConnection`
- `ConnectionStrings:LinxProductionConnection`

`ConnectionStrings:ErpConnection` is **DEPRECATED / LEGACY**. It is kept as a compatibility
fallback for the Development profile only (never for Production) so existing consumers
(`SomaFilialReader`, `SomaFornecedorReader`, `SomaCentroCustoReader`, `LinxSchemaDiscoveryReader`,
`ErpFornecedorDiscoveryRepository`, `SomaGarantirFornecedorErpAdapter`,
`SomaDesenvolErpFornecedorAdapter`, and the `ErpIntegration:BusinessUnits:*` config) are not
broken silently. `B1ConnectivityValidator.ValidateErpAsync(LinxEnvironment.Development)` reads
`LinxDevelopmentConnection` first and falls back to `ErpConnection` only if the canonical key is
absent — the fallback is still validated against the Development profile (mismatch protection
applies identically). New code must use the canonical keys. `ErpConnection` should be migrated out
of the individual readers in a follow-up, non-functional-scope change; it is not removed here to
avoid an unrelated, silent break to running consumers.

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

No write, and no SQL Server round-trip, happens before this check passes.

## 7. VPN

Both environments require the corporate VPN to already be connected. BlueprintOS does not manage
the VPN; it must correctly detect its absence rather than misreport it as an invalid credential.

`ConnectivityStatus.VpnRequired` is returned when the underlying failure is network-level (DNS/TCP
unreachable, connection timeout — SQL error `53`, socket exceptions, `TimeoutException`) on a
profile with `vpn_required: true`. It is never conflated with `PermissionDenied` (SQL auth/authz
error numbers `18456, 229, 230, 262, 4060`). When `VpnRequired` is reported, the operator guidance
is: **"Conecte-se à VPN corporativa e tente novamente."** — no further connection detail is
printed.

## 8. Connection status

`ConnectivityStatus` (`backend/src/BlueprintOS.Infrastructure/Persistence/B1ConnectivityValidator.cs`):

`Ready | NotConfigured | Failed | PermissionDenied | EnvironmentMismatch | VpnRequired`

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
4. Opens the connection, runs `SELECT 1;` as a read-only probe.
5. Best-effort `SELECT SUSER_SNAME();` to capture the effective login identity (never the
   credential itself).
6. Classifies failures: SQL auth/authz error numbers → `PermissionDenied`; network-unreachable
   errors on a VPN-required profile → `VpnRequired`; anything else → `Failed`.

Never prints the connection string or password — only `Server`, `Database`, and
`EffectiveIdentity` (identity resolved by the DB itself) are exposed on the result.

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
Production and vice versa), missing Development/Production secret → `NotConfigured`, the
`ErpConnection` legacy fallback is still validated against the Development profile, environment
mismatch is blocked deterministically before any connection attempt (server- or database-level, in
both directions), and no result ever carries the connection string or password. All new tests
complete in milliseconds — no live SQL Server or VPN is required to run them.

## 16. On doubt

Ask the user. Never infer, never reuse a credential across environments, and no technical
capability of a Production credential substitutes for BlueprintOS governance.
