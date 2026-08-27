# Agent Contract v1.1 - Execution Policy E Credenciais

Data: 2026-08-27
Repositorio: SOMA BlueprintOS
Baseline: `a3f5a0e feat(agents): establish canonical agent contract and manifests`

## 1. Resumo Executivo

CONFIRMADO: o Agent Contract evoluiu de v1 para v1.1 sem alterar runtime, Agent Factory, scripts, runbooks ou fluxos operacionais. A nova `agents/EXECUTION_POLICY.md` e provider/model agnostic e torna canonicos orquestracao por Agents, delegacao obrigatoria, no-direct-bypass, Capability Gap e seguranca de credenciais/conexoes.

CONFIRMADO: os sete Agents continuam registrados e nenhum recebeu capacidade de escrita, destruicao, bypass ou escalada de privilegio.

## 2. Mudancas v1 Para v1.1

CONFIRMADO:

- `contract_version: 1.1` foi adicionado mantendo `schema_version: 1` para compatibilidade do envelope;
- manifests passaram para `version: 1.1.0`;
- capability ownership, delegation, cross-cutting, gap policy e connections foram adicionados;
- least privilege e proibicao de privilege escalation tornaram-se validaveis;
- bootstrap por ponteiros foi criado em `AGENTS.md` e `CLAUDE.md`.

## 3. Execution Policy

CONFIRMADO: `agents/EXECUTION_POLICY.md` e a politica global de execucao para Codex, Claude, ChatGPT e qualquer executor futuro. Ela nao depende de memoria de chat nem de instrucao exclusiva de provider.

## 4. Capability Ownership

CONFIRMADO: cada capability possui ID kebab-case, `responsible_agent_id`, ownership `primary` ou `complementary`, `delegation_required` e `direct_execution_by_others_allowed`.

| Capability | Agent | Ownership |
| --- | --- | --- |
| `ai-runtime-echo` | `echo-agent` | primary |
| `organizational-knowledge-query` | `knowledge-agent` | primary |
| `security-privacy-review` | `security-lgpd-agent` | complementary/cross-cutting |
| `linx-erp-functional-analysis` | `linx-erp-specialist-agent` | primary |
| `linx-database-analysis` | `linx-database-specialist-agent` | primary |
| `wise-operational-analysis` | `wise-agent` | primary |
| `showcase-read-only-collection` | `showcase-agent` | primary |

## 5. Delegacao E No-Bypass

CONFIRMADO: todas as capabilities atuais exigem delegacao e proibem execucao direta por terceiros. Todos os manifests declaram `bypass_allowed: false`. SQL, MCP, shell, Python, `pyodbc`, HTTP, API e browser nao podem contornar o Agent responsavel.

## 6. Capability Gap

CONFIRMADO: falta de conhecimento, evidencia, tool governada ou permissao exige parada e registro de `CAPABILITY GAP`. A ordem canonica e evoluir Agent existente, verificar outro owner existente e somente entao propor novo Agent.

## 7. Evolucao E Criacao De Agent

CONFIRMADO: os manifests permitem proposta de aprendizado, mas mudanca material exige autorizacao humana. Novo Agent exige justificativa arquitetural e autorizacao humana explicita. Agent Factory v2 nao foi implementada.

## 8. Security/LGPD Cross-Cutting

CONFIRMADO: `security-lgpd-agent` foi marcado `cross_cutting: true`, com ownership complementar de `security-privacy-review`. Continua consultivo/contextual. O `AIGovernancePolicyEngine` continua responsavel pela decisao deterministica e o `ApprovalPolicy` pela autorizacao.

## 9. Connection Profiles

CONFIRMADO: profiles representam recursos logicos, nao credenciais:

| Agent | Profile | Referencia comprovada | Intent |
| --- | --- | --- | --- |
| Linx ERP Specialist | `linx-knowledge-store` | `ConnectionStrings:MaisComprasConnection` | read-only |
| Linx Database Specialist | `linx-erp-read-only` | `ConnectionStrings:ErpConnection` | read-only |
| WISE | `linx-wise-daily-read` | referencias locais `LINX_PROD_*` | read-only |
| Showcase | `showcase-authenticated-api-read` | `SHOWCASE_TOKEN` no processo local | read-only |

CONFIRMADO: nenhum host, usuario, senha, token ou connection string foi inserido nos manifests. Classificacao `Confidential` preserva a natureza interna/corporativa indicada pelas fontes atuais.

## 10. Estrategia De Credenciais E Secret Storage

CONFIRMADO: credencial pertence ao usuario/identidade executora. Development .NET reutiliza User Secrets; CI/Homologacao/Producao usam secret manager da plataforma/corporativo; variavel de ambiente local continua reconhecida onde ja existe.

PROPOSTO: adapters futuros devem preferir macOS Keychain, Windows Credential Manager ou secret store equivalente. A integracao completa nao foi implementada nesta etapa.

CONFIRMADO: fallback local so pode usar arquivo ignorado pelo Git e template versionado vazio. O repositorio ja ignora `.env`, `.env.*` e `secrets.json`, mantendo excecao apenas para `.env.example` sem segredo.

## 11. Novo Clone

CONFIRMADO: sem credencial local, o Agent identifica o profile, orienta cadastro no mecanismo seguro sem pedir segredo no chat e para. Ele nao procura segredo no Git, nao usa identidade compartilhada e somente continua depois da configuracao pelo usuario.

## 12. Least Privilege E Privilege Escalation

CONFIRMADO: os sete manifests declaram `least_privilege: true` e `privilege_escalation_allowed: false`. Permissao efetiva da identidade e aprovacao BlueprintOS sao independentes e ambas obrigatorias.

## 13. Manifests Atualizados

CONFIRMADO: foram atualizados `echo-agent`, `knowledge-agent`, `security-lgpd-agent`, `linx-erp-specialist-agent`, `linx-database-specialist-agent`, `wise-agent` e `showcase-agent`.

CONFIRMADO: Linx Database Specialist continua sem autorizacao para DML/DDL; WISE continua read-only por padrao; Showcase continua read-only e com token efemero fora do Git.

## 14. Schema Atualizado

CONFIRMADO: `agents/agent.schema.json` valida v1.1, ownership, delegacao, gap policy, profiles e credential policy sem enfraquecer blocos v1.

## 15. Validator E Testes

CONFIRMADO: `tools/agents/validate-agent-manifests.js` valida IDs, paths, ownership, Agent referenciado, cross-cutting, no-bypass, gap policy, profiles, least privilege, privilege escalation e material secreto.

CONFIRMADO: `tools/agents/validate-agent-manifests.test.js` rejeita sete casos negativos: password ficticio, token ficticio, escalada de privilegio, Agent inexistente, bypass, execucao direta por terceiro e Agent sem governance.

Resultados:

```text
PASS: 7 Agent Contract v1.1 manifests validated
PASS: 7 negative Agent Contract v1.1 validation scenarios rejected
```

Testes `.NET`: NAO APLICAVEL. Nenhum arquivo `.NET` foi alterado nesta etapa; a baseline v1 registrou 861 testes unitarios aprovados.

## 16. Quatro Cenarios Normativos

CONFIRMADO - UPDATE SOMA producao: owner -> analise -> Capability Gap quando necessario -> Security/LGPD -> ActionProposal -> Policy Engine -> Approval -> profile -> permissao efetiva -> Tool/Adapter governado -> validacao -> auditoria. Sem Tool/Adapter governado, parar.

CONFIRMADO - Agent nao sabe: parar, registrar gap e propor evolucao, outro owner ou novo Agent autorizado.

CONFIRMADO - dev sem permissao: mesmo com policy aprovada, `permission denied` encerra a tentativa sem elevacao.

CONFIRMADO - clone novo: orientar configuracao local segura sem solicitar segredo no chat e aguardar o usuario.

## 17. Enforcement Tecnico Versus Documental

CONFIRMADO: schema, validator e testes negativos fornecem enforcement tecnico sobre manifests. AI Governance Onda 1 fornece enforcement parcial nos fluxos que a utilizam.

CONFIRMADO: delegacao universal e interceptacao de tools ainda sao documentais/parciais porque Runtime Registry e Tool Gateway universais nao existem. WISE e Showcase permanecem predominantemente `DOCUMENTAL`; os demais mantem seus status anteriores.

## 18. Gaps Remanescentes

AINDA_NAO_MAPEADO: classificacao corporativa formal e centralizada para host/database de todos os ambientes.

AINDA_NAO_MAPEADO: identidade de servico versus identidade individual para futuros workers nao interativos.

PROPOSTO: adicionar secret scanning leve ao CI e pre-commit em etapa futura; nenhuma ferramenta pesada foi introduzida agora.

PROPOSTO: criar registry de connection profiles quando houver Tool Gateway, evitando duplicacao de metadados.

## 19. Impacto Futuro

PROPOSTO: Agent Factory v2 devera criar manifests v1.1, detectar ownership conflitante, exigir aprovacao para novo Agent e bloquear autoexpansao de privilegios.

PROPOSTO: Tool Gateway devera resolver capability -> owner -> policy -> identidade -> connection profile -> adapter, negar bypass e produzir auditoria.

## 20. Arquivos Criados

- `AGENTS.md`
- `CLAUDE.md`
- `agents/EXECUTION_POLICY.md`
- `tools/agents/validate-agent-manifests.test.js`
- `docs/audits/AgentContractV1.1-ExecutionPolicy-Credenciais.md`

## 21. Arquivos Alterados

- `agents/AGENT_CONTRACT.md`
- `agents/README.md`
- `agents/agent.schema.json`
- `agents/echo-agent/agent.yaml`
- `agents/knowledge-agent/agent.yaml`
- `agents/security-lgpd-agent/agent.yaml`
- `agents/linx-erp-specialist-agent/agent.yaml`
- `agents/linx-database-specialist-agent/agent.yaml`
- `agents/wise-agent/agent.yaml`
- `agents/showcase-agent/agent.yaml`
- `tools/agents/validate-agent-manifests.js`

## 22. Git Diff Resumido

CONFIRMADO: o diff e restrito aos 16 arquivos listados acima. Alteracoes preexistentes de fornecedores, frontend, dashboard, assets e auditorias locais permaneceram fora do escopo e do staging.

## 23. Confirmacoes De Seguranca

CONFIRMADO:

- nenhum secret ou credencial foi adicionado;
- nenhum Agent recebeu privilegio novo;
- nenhum bypass foi habilitado;
- nenhuma connection string com segredo foi versionada;
- nenhum fluxo operacional existente foi alterado;
- nenhuma execucao real Linx/WISE, Showcase ou SQL ocorreu.

## 24. Proximos Passos

1. Projetar Runtime Registry sobre IDs e ownership v1.1.
2. Projetar Tool Gateway com resolucao de profiles e dupla autorizacao efetiva.
3. Implementar adapters de secret store por ambiente conforme necessidade comprovada.
4. Integrar validator e secret scan leve ao CI.
5. Evoluir enforcement documental de WISE/Showcase sem alterar comportamento ate existir Work Order aprovada.
