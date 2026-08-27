# Agent Factory v2 - Adequacao Onda 1

Data: 2026-08-27
Baseline: `aac14fc feat(agents): implement canonical agent factory v2`
Escopo: safety tests e observabilidade basica independentes de infraestrutura futura.

## 1. Baseline

CONFIRMADO: a auditoria inicial continha 18 WARNING, 0 ERROR, com os 8 Agents em status WARN. Distribuicao: 8 `AFV2-GOV-001`, 4 `AFV2-TEST-001`, 2 `AFV2-OBS-001` e 4 `AFV2-GATEWAY-001`.

## 2. Findings Autorizados

CONFIRMADO: esta onda recebeu autorizacao somente para:

- `AFV2-TEST-001`: `echo-agent`, `knowledge-agent`, `wise-agent`, `showcase-agent`;
- `AFV2-OBS-001`: `echo-agent`, `knowledge-agent`.

## 3. Findings Nao Autorizados

CONFIRMADO: 8 `AFV2-GOV-001` e 4 `AFV2-GATEWAY-001` dependem de Runtime Registry, Tool Gateway, approval persistence ou enforcement transversal e nao foram corrigidos.

## 4. Agents Alterados

CONFIRMADO: somente `echo-agent`, `knowledge-agent`, `wise-agent` e `showcase-agent` tiveram manifests atualizados. As versoes passaram de `1.1.0` para `1.1.1`.

CONFIRMADO: ownership, systems, connection profiles, escrita, destructive, bypass, privilege escalation, approval e enforcement nao mudaram.

## 5. Safety Tests Adicionados

### Echo Agent

CONFIRMADO: `EchoAgentSafetyTests.cs` verifica que input com aparencia operacional chega somente ao IA Runtime, que o observer recebe apenas metadata redigida e que falha do observer nao altera o resultado do Agent.

### Knowledge Agent

CONFIRMADO: `KnowledgeAgentSafetyTests.cs` verifica que conteudo perigoso recuperado aparece depois da fronteira explicita "dados, nao instrucoes nem autorizacao", que o Agent usa somente KnowledgeService/IA Runtime e que eventos de falha nao carregam input ou payload.

### WISE Agent

CONFIRMADO: `wise-agent-safety.test.js` e offline. Verifica read-only do Agent, no-bypass, no privilege escalation, ActionProposal/approval, enforcement DOCUMENTAL, precedencia do runbook diario e credenciais vindas do ambiente. O teste nao executa `pyodbc`, SQL ou integracao.

### Showcase Agent

CONFIRMADO: `showcase-agent-safety.test.js` e offline. Verifica read-only, no-bypass, no privilege escalation, enforcement DOCUMENTAL, token via ambiente, ausencia de bearer hardcoded, ausencia de metodos HTTP de escrita e ausencia de persistencia do token em arquivos.

## 6. Observabilidade Adicionada

CONFIRMADO: foi criado `IAgentExecutionObserver` com evento minimo:

- `AgentId`;
- `EventName`;
- `Outcome`;
- categoria fixa opcional de falha.

CONFIRMADO: nao existem campos para prompt, input, output, snippet, PII, secret, credencial ou mensagem de excecao. Os eventos sao `agent.execution.started`, `agent.execution.completed` e `agent.execution.failed`.

CONFIRMADO: `DiagnosticAgentExecutionObserver` publica por default em `System.Diagnostics.DiagnosticListener` e permite coleta por tooling .NET sem backend novo. `NullAgentExecutionObserver` permanece disponivel para desativacao explicita. Falha do observer e isolada e nao altera o fluxo operacional.

## 7. Decisoes De Design

CONFIRMADO: os construtores existentes foram preservados. Overloads opcionais recebem o observer, portanto `AgentFactory` C# e consumidores atuais continuam compativeis.

CONFIRMADO: o Knowledge Agent recebeu somente delimitacao textual proporcional contra instrucao recuperada; nenhuma tool, capability ou acesso foi adicionado.

CONFIRMADO: a estrategia de testes priorizou comportamento e fronteiras de seguranca. WISE/Showcase usam testes estaticos offline porque executar sistemas externos violaria a Execution Policy e o escopo.

## 8. UPDATE Via Factory

CONFIRMADO: os quatro manifests passaram por `AgentFactoryV2.update` em preview e retornaram PASS sem aplicacao adicional. A solicitacao do Product Owner foi tratada como autorizacao limitada desta adequacao.

## 9. Arquivos Criados

- `backend/src/BlueprintOS.Core/Agents/Observability/IAgentExecutionObserver.cs`
- `backend/tests/BlueprintOS.UnitTests/Core/Agents/EchoAgentSafetyTests.cs`
- `backend/tests/BlueprintOS.UnitTests/Core/Agents/KnowledgeAgentSafetyTests.cs`
- `tools/agents/wise-agent-safety.test.js`
- `tools/agents/showcase-agent-safety.test.js`
- `docs/audits/AgentFactoryV2-Adequacao-Onda1.md`
- `docs/audits/AgentFactoryV2-Adequacao-Onda1-AuditResults.json`

## 10. Arquivos Alterados

- `agents/echo-agent/agent.yaml`
- `agents/knowledge-agent/agent.yaml`
- `agents/wise-agent/agent.yaml`
- `agents/showcase-agent/agent.yaml`
- `backend/src/BlueprintOS.Core/Agents/EchoAgent.cs`
- `backend/src/BlueprintOS.Core/Agents/KnowledgeAgent.cs`

CONFIRMADO: arquivos Linx/WISE protegidos e collectors Showcase nao foram alterados.

## 11. Testes Executados

```text
PASS: WISE Agent offline safety invariants
PASS: Showcase Agent offline safety invariants
PASS: 8 Agent Contract v1.1 manifests validated
PASS: 7 negative Agent Contract v1.1 validation scenarios rejected
PASS: Agent Factory v2 lifecycle, audit and safety tests
PASS: Factory UPDATE preview para os quatro Agents
PASS: 8 testes .NET filtrados de Core.Agents
PASS: 866 testes unitarios .NET; 0 falhas; 0 ignorados
PASS: build completo; 0 warnings; 0 erros
```

CONFIRMADO: a primeira tentativa `.NET` no sandbox falhou antes da compilacao por `SocketException (13)` do MSBuild. A execucao foi repetida com permissao apropriada e passou.

CONFIRMADO: o secret scan especifico nao encontrou private key, API key, bearer, connection string com credencial ou atribuicao de valor secreto. O regex generico do validator marcou `cancellationToken = default` como falso positivo por nome de identificador; a revisao confirmou que nao se trata de credencial.

## 12. Auditoria Antes E Depois

| Metrica | Antes | Depois |
| --- | ---: | ---: |
| Agents auditados | 8 | 8 |
| Findings WARNING | 18 | 12 |
| Findings ERROR | 0 | 0 |
| `AFV2-TEST-001` | 4 | 0 |
| `AFV2-OBS-001` | 2 | 0 |
| `AFV2-GOV-001` | 8 | 8 |
| `AFV2-GATEWAY-001` | 4 | 4 |

## 13. Findings Resolvidos

CONFIRMADO: foram resolvidos exatamente 6 findings autorizados: quatro de safety coverage e dois de observabilidade basica.

## 14. Findings Remanescentes

CONFIRMADO: permanecem 12 findings legitimos:

- `AFV2-GOV-001`: todos os 8 Agents;
- `AFV2-GATEWAY-001`: Linx Database Specialist, Linx ERP Specialist, Showcase e WISE.

## 15. Findings Novos

CONFIRMADO: nenhum finding novo apareceu.

## 16. Enforcement Status

CONFIRMADO: nenhum `enforcement_status` foi alterado. Echo, Knowledge, Linx specialists, Security/LGPD e Agent Factory permanecem PARTIAL. WISE e Showcase permanecem DOCUMENTAL.

## 17. Riscos

INFERIDO: observer em memoria ou adapter concreto futuro precisa manter o mesmo contrato de minimizacao; adicionar payload ao evento reabriria risco LGPD.

INFERIDO: safety tests estaticos de WISE/Showcase reduzem regressao versionada, mas nao substituem Tool Gateway ou enforcement runtime.

## 18. Gaps

AINDA_NAO_MAPEADO: Runtime Registry, Tool Gateway universal, approval persistence e interceptacao transversal de SQL/MCP/pyodbc/browser/API.

PROPOSTO: tratar os 12 WARN apenas em ondas arquiteturais futuras autorizadas.

## 19. Git Diff

CONFIRMADO: o diff desta onda e limitado aos 13 arquivos listados. Mudancas preexistentes do worktree permanecem fora do staging.

## 20. Proximos Passos

1. Product Owner revisar a reducao de 18 para 12 findings.
2. Manter os WARN atuais ate existir infraestrutura tecnica correspondente.
3. Projetar Tool Gateway e Runtime Registry separadamente.
4. Preservar a regra de nao registrar payload em observabilidade futura.

## Estado Final

CONFIRMADO: REAUDITAR -> PARAR foi cumprido. Nenhum finding de infraestrutura foi corrigido ou mascarado.
