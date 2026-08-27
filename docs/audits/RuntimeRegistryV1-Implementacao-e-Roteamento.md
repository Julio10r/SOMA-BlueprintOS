# Runtime Registry v1 - Implementacao e Roteamento

Data: 2026-08-27  
Repositorio: SOMA BlueprintOS  
Contrato: Agent Contract v1.1

## 1. Resumo Executivo

CONFIRMADO: o Runtime Registry v1 foi implementado como infraestrutura provider-agnostic e read-only em `tools/agents/runtime-registry.js`. Ele descobre manifests canônicos válidos, indexa ownership, resolve capabilities estruturadas, cria planos de routing e detecta conflitos e Capability Gaps.

CONFIRMADO: o Registry não executa tools, não autoriza operações, não instancia Agents, não acessa sistemas externos e não resolve credenciais. Tool Gateway e persistência de approvals não foram implementados.

## 2. Baseline

CONFIRMADO: foram lidos os quatro relatórios da Agent Factory v2, Agent Contract v1.1, Execution Policy, schema, os oito manifests, validator, Factory v2, runtime C#, AI Governance e contextos arquiteturais. O baseline possuía 8 Agents válidos e 12 warnings remanescentes: 8 `AFV2-GOV-001` e 4 `AFV2-GATEWAY-001`.

## 3. Arquitetura

```text
structured capability ids
  -> RuntimeRegistry
     -> canonical validateRepository()
     -> valid manifest discovery
     -> capability ownership index
     -> route / conflict / gap decision
  -> routing plan (stop)
```

CONFIRMADO: o Registry é infraestrutura, não um Agent. Criar `runtime-registry-agent` confundiria resolução arquitetural com ownership de domínio.

## 4. Localizacao Escolhida

CONFIRMADO: `tools/agents` foi escolhido porque o parser/validator canônico e a Factory provider-agnostic já vivem nessa fronteira. Isso evita dependência de UI, banco, DI C# e provider LLM.

## 5. Responsabilidades

CONFIRMADO: a API oferece `discoverAgents`, `getAgent`, `listCapabilities`, `getCapabilityOwners`, `resolveCapability`, `resolveCapabilities`, `buildRoutingPlan` e `detectConflicts`. A CLI read-only oferece `list`, `capabilities`, `resolve` e `plan`.

## 6. Relacao com Agent Factory

CONFIRMADO: Agent Factory v2 mantém CREATE, VALIDATE, AUDIT, UPDATE, REGISTER, CATALOG, TEST e SECURITY CHECK. O Registry somente consome manifests registrados e válidos; não duplica lifecycle nem escreve manifests.

## 7. Relacao com AgentFactory C#

CONFIRMADO: `backend/src/BlueprintOS.Core/Agents/AgentFactory.cs` continua instanciador runtime simples. O Registry descreve metadata de runtime e `factory_supported`, mas não chama nem substitui a Factory C#.

## 8. Relacao com Execution Policy

CONFIRMADO: ownership primário, complementar, `delegation_required`, no-bypass e Capability Gap são derivados do contrato. Toda decisão inclui `direct_bypass_allowed: false`.

## 9. Relacao com AI Governance

CONFIRMADO: routing não equivale a autorização. `ActionProposal`, `AIGovernancePolicyEngine`, `ApprovalPolicy`, identidade efetiva e controles futuros continuam responsáveis pela autorização operacional.

## 10. Relacao Futura com Tool Gateway

PROPOSTO: um Orchestrator poderá entregar a decisão do Registry ao Policy Engine e ao futuro Tool Gateway. A existência do Registry não intercepta SQL, browser, API, MCP ou scripts e não transforma enforcement documental/parcial em enforced.

## 11. Discovery

CONFIRMADO: a fonte única é `agents/*/agent.yaml`; não existe `registry.json` manual. `validateRepository` é reutilizado. Agents inválidos e IDs duplicados não entram. Referências desconhecidas em relationships geram finding de Registry.

## 12. Capability Index

CONFIRMADO: 14 capabilities foram indexadas automaticamente. O índice é reconstruído dos manifests e se expande quando um novo Agent válido é adicionado.

## 13. Ownership

CONFIRMADO: foram encontrados 13 ownerships primários. Uma rota só é resolvida automaticamente quando existe exatamente um primary owner ativo.

## 14. Complementary Ownership

CONFIRMADO: foi encontrado 1 ownership complementar, `security-privacy-review` do `security-lgpd-agent`. Complementary nunca é promovido a primary e múltiplos complementares não são conflito.

## 15. Cross-Cutting

CONFIRMADO: `security-lgpd-agent` é o único cross-cutting Agent. Seus critérios são declarativos e dependem de contexto operacional estruturado ainda externo ao Registry.

AINDA_NAO_MAPEADO: resolução condicional completa dos critérios transversais. Nesta v1 o resultado usa `cross_cutting_candidates`; `cross_cutting_agents` permanece vazio sem evidência estruturada suficiente. O Registry não inclui Security/LGPD indiscriminadamente e não lhe transfere ownership de Linx, WISE, Showcase ou SOMA.

## 16. Routing

CONFIRMADO: cada decisão contém capability solicitada, primary, complementares, candidatos transversais, delegação, status, gap, conflitos, razões, evidência e próximo passo. O nome `routing_resolved` evita confusão com autorização de execução.

## 17. Capability Gap

CONFIRMADO: ausência de primary elegível produz `CAPABILITY_GAP`, Agents avaliados, motivo, capabilities semelhantes, próximos passos permitidos e `direct_bypass_allowed: false`. Novo Agent continua exigindo autorização humana explícita.

## 18. Routing Conflicts

CONFIRMADO: dois primary owners ativos da mesma capability produzem `ROUTING_CONFLICT`; não existe precedência artificial. Os manifests reais produziram zero conflito.

## 19. Agent Status

CONFIRMADO: somente `active` é elegível para routing automático. `planned`, `deprecated` e `retired` permanecem no catálogo válido, mas não são selecionados como owner ativo.

## 20. Runtime vs Operational Agents

CONFIRMADO: Echo, Knowledge, Linx Database, Linx ERP e Security/LGPD declaram runtime implementado. Agent Factory, Showcase e WISE existem no catálogo, mas não possuem runtime instanciável. Nenhuma classe C# artificial foi criada.

## 21. Relationships e Workflows

CONFIRMADO: upstream/downstream Agents, workflows e conflitos declarados são preservados na descrição e na rota. Integração diária Linx/WISE permanece workflow, não Agent, e não foi executada.

## 22. Connection Profiles

CONFIRMADO: somente IDs lógicos e os campos permitidos `configuration_reference`, `environment`, `access_intent` e `classification` são expostos. Registry não acessa armazenamento de segredo ou credencial.

## 23. Observabilidade

CONFIRMADO: observer injetável recebe `registry.discovery.started`, `registry.discovery.completed`, `registry.routing.resolved`, `registry.routing.gap` e `registry.routing.conflict`. Eventos contêm apenas IDs, contagens e categorias; não contêm prompt, texto livre, SQL, PII ou secrets.

## 24. Testes

CONFIRMADO: testes cobrem discovery dos 8 Agents, validade, IDs únicos, índice, primary/complementary/cross-cutting, delegação, gaps, bypass, conflitos, status, runtime operacional, relationships, workflows, perfis seguros, múltiplas capabilities, plano, ausência de execução/mutação e observabilidade redigida. Fixtures negativas cobrem primary duplicado, referência desconhecida, manifesto inválido, capability inexistente e bypass proibido.

## 25. Metricas Reais

| Metrica | Resultado |
| --- | ---: |
| Agents discovered | 8 |
| Capabilities indexed | 14 |
| Primary ownerships | 13 |
| Complementary ownerships | 1 |
| Cross-cutting Agents | 1 |
| Conflicts | 0 |
| Invalid Agents | 0 |

## 26. Simulacao SOMA

CONFIRMADO: `linx-database-analysis` resolveu para `linx-database-specialist-agent`, com delegação obrigatória, runtime implementado e enforcement `PARTIAL`. `security-lgpd-agent` apareceu como candidato transversal, não como owner.

CONFIRMADO: não existe capability canônica de escrita no banco SOMA. `soma-database-write` produziu `CAPABILITY_GAP`; o plano ficou não resolvido, sem autorização e sem execução, com bypass proibido. Nenhum SQL foi executado.

## 27. Simulacao Showcase

CONFIRMADO: `showcase-read-only-collection` resolveu para `showcase-agent`, sem complementares, com Security/LGPD candidato, workflows de coleta/enriquecimento preservados, runtime não implementado e enforcement `DOCUMENTAL`. Nenhum browser/API foi acessado.

## 28. Simulacao WISE

CONFIRMADO: `wise-operational-analysis` resolveu para `wise-agent`, com relações declaradas a Linx, Showcase e Security/LGPD, workflows preservados, runtime não implementado e enforcement `DOCUMENTAL`. Nenhuma integração ou consulta foi executada.

## 29. Resultado da Reauditoria

CONFIRMADO: Agent Factory v2 AUDIT permaneceu `WARN`, com os mesmos 12 warnings. O auditor não foi alterado e nenhum finding foi maquiado.

## 30. Findings Antes e Depois

| Finding | Antes | Depois |
| --- | ---: | ---: |
| `AFV2-GOV-001` | 8 | 8 |
| `AFV2-GATEWAY-001` | 4 | 4 |
| Total WARNING | 12 | 12 |

## 31. Gaps

AINDA_NAO_MAPEADO: Tool Gateway universal, persistência de approvals e avaliação estruturada dos critérios cross-cutting. CONFIRMADO: escrita SOMA não possui capability declarada e permanece Capability Gap.

## 32. Riscos

INFERIDO: um consumidor futuro pode interpretar routing como autorização se ignorar os campos de segurança. A mitigação atual é `authorization_granted: false`, `execution_performed: false`, `direct_bypass_allowed: false` e documentação explícita; enforcement final pertence ao futuro gateway.

## 33. Arquivos Criados

- `tools/agents/runtime-registry.js`
- `tools/agents/runtime-registry-cli.js`
- `tools/agents/runtime-registry.test.js`
- `docs/audits/RuntimeRegistryV1-Implementacao-e-Roteamento.md`
- `docs/audits/RuntimeRegistryV1-ResolutionResults.json`

## 34. Arquivos Alterados

CONFIRMADO: nenhum arquivo existente foi alterado. Contract, schema e os oito manifests permaneceram intactos. Linx/WISE, Showcase e collectors permaneceram intactos.

## 35. Git Diff

CONFIRMADO: o escopo contém somente os cinco arquivos novos listados acima. Alterações preexistentes do worktree não pertencem a este trabalho e não serão staged.

## 36. Proximos Passos

1. Definir entrada estruturada para avaliar critérios cross-cutting sem NLP arbitrário.
2. Projetar Orchestrator consumidor do RoutingPlan.
3. Implementar Tool Gateway separadamente, com Policy Engine e approvals persistidos.
4. Avaliar a Capability Gap de escrita SOMA antes de evoluir Agent existente ou propor novo Agent.

## Validacao Final

```text
PASS: 8 Agent Contract v1.1 manifests validated
PASS: 7 negative validator scenarios rejected
PASS: Agent Factory v2 lifecycle, audit and safety tests
PASS: Runtime Registry v1 discovery, routing, gaps, conflicts and safety tests
PASS: WISE Agent offline safety invariants
PASS: Showcase Agent offline safety invariants
PASS: 17 .NET integration tests
PASS: 866 .NET unit tests
```

CONFIRMADO: o build realizado por `dotnet test` concluiu com 4 warnings preexistentes de nulabilidade em classes de Identity e sem erro. O primeiro comando .NET falhou no sandbox por restrição de pipe; a repetição autorizada fora do sandbox passou.

## Estado Final

CONFIRMADO: DISCOVER -> INDEX -> RESOLVE -> ROUTE -> DETECT GAP/CONFLICT -> REAUDIT -> PARAR foi cumprido. Nenhuma tool operacional foi executada pelo Registry.
