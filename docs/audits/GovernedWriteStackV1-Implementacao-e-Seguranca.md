# Governed Write Stack v1 — Implementação e Segurança

Status: accepted
Data: 2026-08-27
Escopo: retomada e conclusão da implementação do Governed Write Stack v1, incluindo a nova capability `soma-database-write-proposal` do `linx-database-specialist-agent`.

## Resumo Da Implementação

O Governed Write Stack v1 adiciona ao `linx-database-specialist-agent` a capacidade de produzir `ActionProposal` estruturada para necessidade de escrita SOMA/Linx, sem executar SQL. O fluxo completo é: proposta estruturada → `AIGovernancePolicyEngine` (classificação de risco Green/Yellow/Red) → `ApprovalPolicy` (grant vinculado a `ProposalHash`) → `ToolGateway` (validação final de identidade, ambiente, capability e política) → adapter `SomaLinxDryRunAdapter` (somente dry-run, nunca executa SQL real).

A capability é declarada em `agents/linx-database-specialist-agent/agent.yaml` como:

```yaml
soma-database-write-proposal:
  responsible_agent_id: linx-database-specialist-agent
  ownership: primary
  delegation_required: true
  direct_execution_by_others_allowed: false
```

Com `governance.can_execute_write: false`, `can_execute_destructive_operation: false`, `policy_engine_required: true`, `approval_required_for` cobrindo escrita Yellow, e `enforcement_status: PARTIAL` (honesto — não superestimado).

## Arquitetura Do Stack

Componentes em `backend/src/BlueprintOS.Core/AI/Governance/`:

- `Contracts/IGovernedWriteContracts.cs`: contratos (`IToolGateway`, `IGovernedToolAdapter`, `IGovernanceAuditStore`, etc.).
- `Models/GovernedWriteModels.cs`: modelos de proposta, requisição de gateway, resultado, preview dry-run.
- `StructuredActionProposalAdapter.cs`: constrói `ActionProposal` estruturada a partir do contexto do agente (capability e owner constantes).
- `ToolGateway.cs`: ponto único de validação antes de qualquer dry-run — verifica `LIVE_EXECUTION_DISABLED`, roteamento resolvido, capability registrada, owner correto, ambiente definido, exigência de Security/LGPD para escrita em produção, connection profile governado, identidade com permissão efetiva sem escalonamento, coerência do `PolicyDecision` com o `ProposalHash`, bloqueio Red, e validade do `ApprovalGrant` quando exigido.
- `SomaLinxDryRunAdapter.cs`: único adapter registrado para a capability; retorna um preview estruturado (`SqlGenerated: false`, `ExternalExecutionPerformed: false`, `GovernedExecutionMode.DryRun`), nunca abre conexão real nem gera SQL.

Estes componentes se integram aos componentes de governança pré-existentes: `AIGovernancePolicyEngine.cs`, `ApprovalPolicy.cs`, `InMemoryGovernanceAuditRecorder.cs`/`EfGovernanceStores.cs`.

## Persistência

`backend/src/BlueprintOS.Infrastructure/Persistence/Governance/` contém `GovernancePersistenceEntities.cs` e `EfGovernanceStores.cs`, mapeando três tabelas via EF Core:

- `AIGovernanceApprovalRequests` (Id, ActionProposalId, ProposalHash, RiskClassification, Reason, RequiredApprover, CreatedAt, ExpiresAt, Status).
- `AIGovernanceApprovalGrants` (Id, ApprovalRequestId → FK restrict, ProposalHash, ApprovedBy, ApprovedAt, ExpiresAt, Scope, Notes, RevokedAt).
- `AIGovernanceAuditEvents` (Id, EventType, RequestId, ActionProposalId, ProposalHash, AgentId, SubjectId, Outcome, CategoriesJson, CreatedAt).

A migration `20260827173342_AddGovernedWriteStackV1` foi verificada por **inspeção estática** (leitura direta do arquivo `.cs`, sem qualquer comando `dotnet ef` conectado): ela cria exclusivamente essas três tabelas, com FK `AIGovernanceApprovalGrants.ApprovalRequestId → AIGovernanceApprovalRequests.Id` (`ReferentialAction.Restrict`) e índices em `ProposalHash`, `(Status, ExpiresAt)`, `ActionProposalId` e `(RequestId, CreatedAt)`. Nenhuma tabela pré-existente é alterada. `BlueprintOSDbContext.cs` e `BlueprintOSDbContextModelSnapshot.cs` foram atualizados de forma consistente com essas três entidades.

## Resultado Dos Testes

Executados nesta retomada (todos passaram):

- `dotnet build` no backend — build limpo, 0 erros, 0 warnings.
- `dotnet test tests/BlueprintOS.UnitTests --filter "FullyQualifiedName~Governance"` — 31 testes passaram, incluindo os 11 cenários A–I focados em `GovernedWriteStackTests` (Scenario_A a Scenario_I, mais `Live_Execution_Should_Be_Blocked_Even_With_Valid_Yellow_Approval` e `Proposal_Context_Gap_Should_Not_Create_Proposal_Or_Policy_Decision`).
- `dotnet test tests/BlueprintOS.UnitTests` (suíte completa) — 877 testes, 0 falhas.
- `node tools/agents/validate-agent-manifests.js` — 8 manifests do Agent Contract v1.1 validados, capability ownership/delegação/gap/credenciais válidos, sem bypass/escalonamento/segredo detectado.
- `node tools/agents/validate-agent-manifests.test.js` — 7 cenários negativos rejeitados corretamente.
- `node tools/agents/governed-orchestrator.test.js` — inclui novo cenário cobrindo `soma-database-write-proposal` roteado para `linx-database-specialist-agent` com `security-lgpd-agent` como cross-cutting; passou.
- `node tools/agents/agent-factory-v2.test.js`, `node tools/agents/runtime-registry.test.js`, `node tools/agents/showcase-agent-safety.test.js`, `node tools/agents/wise-agent-safety.test.js` — todos passaram, confirmando que Showcase e WISE/Linx diário permanecem intactos.

Não executados nesta tarefa (por política explícita desta retomada — proibição de qualquer conexão real de banco):

- `backend/tests/BlueprintOS.IntegrationTests` (inclui `FornecedorRepositoryIntegrationTests` e testes que abrem conexão com banco/infra externa) — não executados. Motivo: exigem conexão de banco real e não pertencem ao escopo desta tarefa; risco de repetir o evento de conexão ocorrido na interrupção anterior.
- Qualquer comando `dotnet ef` (migrations list/database update/etc.) — não executado. Motivo: proibição explícita de abrir conexão real com banco nesta tarefa; verificação da migration foi feita por leitura estática do arquivo gerado.
- Secret scan com ferramenta dedicada (gitleaks/trufflehog) — não executado porque nenhuma dessas ferramentas está instalada/configurada neste ambiente. Em substituição, foi feita varredura manual por padrão regex (`password|pwd|secret|api[_-]?key|token|connectionstring` seguido de valor literal) sobre todos os arquivos novos/alterados desta tarefa; nenhuma ocorrência encontrada.

## Reauditoria — Agent Factory v2 AUDIT

Comparação entre o relatório anterior salvo em `docs/audits/AgentFactoryV2-AuditResults.json` e a nova execução de `node tools/agents/agent-factory-cli.js AUDIT` nesta retomada:

- Status geral: `WARN` antes e depois (nenhuma promoção artificial de enforcement).
- Findings antes: 18. Findings depois: 12.
- Resolvidos (não aparecem mais): `showcase-agent/AFV2-TEST-001`, `echo-agent/AFV2-OBS-001`, `echo-agent/AFV2-TEST-001`, `knowledge-agent/AFV2-TEST-001`, `knowledge-agent/AFV2-OBS-001`, `wise-agent/AFV2-TEST-001` — todos referentes a lacunas de observabilidade/teste já endereçadas em trabalho anterior a esta tarefa (não gerados por esta implementação).
- Novos findings introduzidos por esta tarefa: nenhum. O `linx-database-specialist-agent` mantém os mesmos dois findings de severidade WARNING já esperados e honestos: `AFV2-GOV-001` (enforcement `PARTIAL`, não `ENFORCED` — correto, pois ainda não há Tool Gateway universal cobrindo 100% do fluxo declarativo) e `AFV2-GATEWAY-001` (connection profile existe sem enforcement universal de Tool Gateway — também esperado nesta fase, já que o Tool Gateway atual cobre apenas o fluxo dry-run da nova capability, não todo acesso externo do Agent).
- Nenhum finding foi promovido, suprimido ou reclassificado artificialmente para "resolver" a auditoria; a Factory não corrige findings (comportamento read-only preservado).

## Transparência — Evento De Conexão Durante A Interrupção Anterior

Durante a interrupção anterior desta tarefa, um comando de CLI do EF Core relacionado à remoção/regeneração de uma migration leu automaticamente a connection string configurada para o ambiente `+Compras`. As únicas operações técnicas identificadas foram:

- `SELECT 1` (verificação de conectividade padrão do provedor EF Core/SQL Server).
- Leitura da tabela `__EFMigrationsHistory` (consulta padrão do EF Core para determinar o estado de migrations aplicadas).

Nenhuma escrita foi realizada. Nenhuma migration foi aplicada ao banco. Nenhum sistema ERP/SOMA foi acessado. Não houve exposição, uso ou tentativa de elevação de credencial além da leitura padrão de conectividade do próprio EF Core.

A partir da retomada desta tarefa, ficou explicitamente proibido qualquer comando que abra conexão real com banco — isso inclui qualquer `dotnet ef` contra uma connection string real. Toda validação da migration `20260827173342_AddGovernedWriteStackV1` nesta retomada foi feita exclusivamente por inspeção estática do código gerado (leitura direta dos arquivos `.cs`), sem nenhum comando conectado. Nenhum comando `dotnet ef`, nenhuma nova conexão de banco e nenhum acesso a ERP/SOMA ocorreram durante esta retomada.

## Confirmações De Segurança (Verificação Manual Por Código)

| Item | Confirmado | Evidência |
|---|---|---|
| LIVE_EXECUTION continua desabilitado | Sim | `ToolGateway.Validate`: `if (request.ExecutionMode == GovernedExecutionMode.LiveExecution) reasons.Add("LIVE_EXECUTION_DISABLED")`; teste `Live_Execution_Should_Be_Blocked_Even_With_Valid_Yellow_Approval` |
| Tool Gateway somente DRY_RUN | Sim | `ToolGateway.InvokeAsync` sempre chama `adapter.DryRunAsync`; retorno inclui `"DRY_RUN_ONLY"`, `"NO_EXTERNAL_EXECUTION"` |
| Adapter SOMA/Linx não executa SQL | Sim | `SomaLinxDryRunAdapter.DryRunAsync` monta apenas um preview; `SqlGenerated: false`, `ExternalExecutionPerformed: false` |
| Nenhuma credencial é resolvida | Sim | Preview expõe `CredentialResolutionRequired: true` como estado declarativo, não resolve segredo nenhum; nenhuma leitura de secret no código |
| Identity permission é apenas requisito/estado estruturado | Sim | `request.Identity.HasEffectivePermission` é um booleano de entrada da requisição, checado sem lógica de obtenção/elevação de credencial |
| No privilege escalation | Sim | `if (request.Identity.PrivilegeEscalationAllowed) reasons.Add("PRIVILEGE_ESCALATION_FORBIDDEN")`; teste `Scenario_G_Denied_Identity_Should_Block_Without_Privilege_Escalation` |
| ProposalHash continua vinculando approval | Sim | `ToolGateway`: `POLICY_DECISION_PROPOSAL_MISMATCH` quando hash diverge; `ApprovalPolicy.IsGrantValidFor` compara hash |
| Approval expirado bloqueia | Sim | Teste `Scenario_E_Expired_Approval_Should_Block` |
| Approval revogado bloqueia | Sim | Teste `Scenario_F_Revoked_Persisted_Approval_Should_Block` |
| Mudança de proposal invalida approval | Sim | Teste `Scenario_D_Changed_Filter_Should_Invalidate_ProposalHash_Approval` |
| UPDATE sem filtro é bloqueado/classificado corretamente | Sim | Teste `Scenario_B_Update_Without_Where_Should_Be_Red_And_Blocked`; `AIGovernancePolicyEngineTests.Update_Without_Context_Should_Be_Red_And_Blocked` |
| TRUNCATE é bloqueado | Sim | Teste `Scenario_C_Truncate_Should_Be_Red_And_Blocked`; `AIGovernancePolicyEngineTests.Destructive_Or_Privilege_Operations_Should_Be_Red(Truncate)` |
| SecretCredential é redigido/bloqueado | Sim | Teste `Scenario_H_Secret_Should_Be_Red_And_Audit_Should_Be_Redacted`; `AIGovernancePolicyEngineTests.Secret_Exposure_Should_Be_Red` |
| Export PII segue governança | Sim | Teste `Scenario_I_Massive_Pii_Export_Should_Be_Red_And_Live_Always_Blocked`; `AIGovernancePolicyEngineTests.Massive_Pii_Export_Should_Be_Red` |
| Security/LGPD exigido para escrita em produção | Sim | `ToolGateway.Validate`: `SECURITY_LGPD_REVIEW_REQUIRED` quando ambiente Production e operação de escrita sem `security-lgpd-agent` entre os cross-cutting agents |
| Linx/WISE diário continua intacto | Sim | `node tools/agents/wise-agent-safety.test.js` passou sem alteração de comportamento; nenhum arquivo do fluxo diário (`scripts/linx_wise_daily_integration.py`, runbook) foi incluído nesta tarefa/commit |
| Showcase continua intacto | Sim | `node tools/agents/showcase-agent-safety.test.js` passou |

## Divergências Encontradas Vs. Estado Conhecido

Nenhuma divergência material foi encontrada entre o estado descrito como "já implementado" e o código real. Único ponto de esclarecimento: a contagem de "15 capabilities" citada refere-se ao total de entradas `capability_ownership` somadas em todos os 8 manifests (`agent-factory`: 7, `linx-database-specialist-agent`: 2, demais: 1 cada) — confirmado por inspeção direta dos YAMLs, não a 15 manifests.
