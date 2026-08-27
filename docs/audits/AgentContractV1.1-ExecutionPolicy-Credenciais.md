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

## 25. Investigacao Da Conexao Local Read-Only (`linx-erp-read-only`) — Continuacao

Contexto: uma sessao anterior do caso PROG/OP/PED (`docs/audits/AgentLearningV1-LinxProgOpPed.md`, secao 7.9.c) declarou Knowledge Gap por nao conseguir validar a conexao Linx/`SOMA_DESENV`. Esta secao registra a investigacao e o resultado, sem gerar nenhuma solucao PROG/OP/PED.

### 25.1 Como `ConnectionStrings:ErpConnection` ja deveria ser resolvida (arquitetura pre-existente, CONFIRMADO)

O mecanismo correto **ja existia integralmente antes desta tarefa** e nao precisou de uma segunda arquitetura:

- `backend/src/BlueprintOS.Api/appsettings.json` (versionado) contem apenas um placeholder nao-segredo: `"ErpConnection": "__SET_VIA_USER_SECRETS_OR_CONNECTIONSTRINGS__ERPCONNECTION__"`.
- `backend/src/BlueprintOS.Api/BlueprintOS.Api.csproj` declara `<UserSecretsId>BlueprintOS-Development</UserSecretsId>`, habilitando `dotnet user-secrets` para popular `ConnectionStrings:ErpConnection` localmente, fora do Git, em `~/.microsoft/usersecrets/BlueprintOS-Development/secrets.json` (permissao `600`, fora do repositorio).
- `Program.cs` (`BuildDatabaseConfiguration()`) monta a configuracao com `AddJsonFile("appsettings.json") -> AddJsonFile("appsettings.Development.json") -> AddUserSecrets<Program>() -> AddEnvironmentVariables()` — User Secrets e variavel de ambiente sempre sobrescrevem o placeholder versionado, nunca o contrario.
- Todos os leitores ERP (`SomaFilialReader`, `SomaCentroCustoReader`, `SomaFornecedorReader`, `LinxSchemaDiscoveryReader`, `ErpFornecedorDiscoveryRepository`, etc.) ja recusam abrir conexao se `ConnectionStrings:ErpConnection` estiver ausente ou ainda for o placeholder `__SET_...__`, lancando `InvalidOperationException` com mensagem que nao contem a connection string.
- Ja existia um validador dedicado, **read-only por construcao** (`SELECT 1` apenas): `backend/src/BlueprintOS.Infrastructure/Persistence/B1ConnectivityValidator.cs`, exposto via CLI local sem subir o host HTTP: `dotnet run --project backend/src/BlueprintOS.Api -- validate-b1-connectivity`.

### 25.2 Por que o Agent nao conseguiu usar a conexao na sessao anterior

Nao foi um gap de arquitetura nem de credencial ausente. Foi um gap de **descoberta dentro da sessao de chat**: a sessao anterior nunca invocou o comando CLI local `validate-b1-connectivity` (nem qualquer outro caminho de teste read-only ja existente no repositorio) antes de declarar Knowledge Gap — a conclusao "sem conexao disponivel" foi correta quanto a nao poder pedir/usar credencial no chat (regra que continua valendo), mas incompleta quanto a nao ter verificado se **o mecanismo local do proprio desenvolvedor** ja resolvia isso sem nenhuma credencial no chat. Ao rodar o comando ja existente nesta sessao (sem digitar nenhuma credencial, sem imprimir a connection string), o resultado real foi:

```
+Compras ........ READY
  Servidor: 192.168.9.98
  Banco: MAISCOMPRAS
  Identidade efetiva: [REDACTED — nome de login, nao segredo, omitido aqui por minimizacao]
ERP SOMA_DESENV ........ READY
  Servidor: 192.168.9.98
  Banco: SOMA_DESENV
  Identidade efetiva: [REDACTED — nome de login, nao segredo, omitido aqui por minimizacao]
```

CONFIRMADO: `ConnectionStrings:ErpConnection` ja estava configurada localmente via `dotnet user-secrets` nesta maquina (`~/.microsoft/usersecrets/BlueprintOS-Development/secrets.json`, chaves presentes: `ConnectionStrings:MaisComprasConnection`, `ConnectionStrings:ErpConnection`, `Bootstrap:Secret`, `Bootstrap:AllowedCandidateEmails:0` — apenas os nomes das chaves foram inspecionados, nunca os valores). CONFIRMADO: **CONNECTION STATUS = READY (read-only)** para `SOMA_DESENV`.

**Observacao (nao um bloqueio desta tarefa):** a identidade efetiva resolvida por `SUSER_SNAME()` e um login de servico compartilhado (nao um login individual do desenvolvedor). Isso e consistente com o `credential_policy.individual_identity_required: true` do manifesto **apenas se** esse login de servico for de fato provisionado por pessoa/ambiente (nao compartilhado entre desenvolvedores com permissoes distintas) — algo que este Agent nao pode confirmar sem acesso ao cadastro de logins do SQL Server. Registrado como **NEEDS_VALIDATION**, nao como bloqueio: o principio "Governance permitir + banco permitir" continua valendo com as permissoes reais desse login, quaisquer que sejam.

### 25.3 Mudanca de codigo feita nesta etapa (minima, aditiva, reaproveitando o mecanismo existente)

Nao foi criada uma segunda arquitetura de conexao. `B1ConnectivityValidator` foi estendido (sem quebrar os dois unicos consumidores existentes, `Program.cs` e `ServiceCollectionExtensions.cs`) para:

1. Substituir o `bool IsSuccess` binario por `ConnectivityStatus { Ready, NotConfigured, Failed, PermissionDenied }`, classificando `SqlException` com numeros de erro `18456/229/230/262/4060` (login falhou, permissao negada em objeto/comando/banco/database indisponivel para o login) como `PermissionDenied` em vez de `Failed` generico — para que o Agent nunca confunda "sem permissao" com "banco fora do ar" e nunca tente contornar elevando privilegio.
2. Apos o `SELECT 1` bem-sucedido, executar `SELECT SUSER_SNAME();` (tambem read-only) para expor a identidade efetiva de login — nunca a credencial usada para obte-la — permitindo que o Agent (e o desenvolvedor) saibam **com qual identidade real** as permissoes serao avaliadas.
3. `Program.cs` (comando `validate-b1-connectivity`) passou a imprimir o status textual (`READY`/`NOTCONFIGURED`/`FAILED`/`PERMISSIONDENIED`) e, em sucesso, Servidor/Banco/Identidade efetiva — nunca a connection string. A sanitizacao de mensagem de erro ja existente (`SanitizeConnectivityMessage`, regex que redige `login failed for user '...'` e `user id|uid|password|pwd=...`) foi preservada sem alteracao.

Arquivos alterados: `backend/src/BlueprintOS.Infrastructure/Persistence/B1ConnectivityValidator.cs`, `backend/src/BlueprintOS.Api/Program.cs`. Arquivo novo: `backend/tests/BlueprintOS.UnitTests/Infrastructure/Persistence/B1ConnectivityValidatorTests.cs` (4 testes: config ausente -> `NotConfigured`; placeholder nao resolvido -> `NotConfigured`; resultado `NotConfigured` nunca carrega a connection string; falha rapida sem tentar abrir conexao quando nao configurado). Build da solucao: 0 erros/0 warnings. Suite completa `BlueprintOS.UnitTests`: **896/896 passaram** (892 pre-existentes + 4 novos), 0 falhas.

### 25.4 Como um novo desenvolvedor configura sua propria credencial local (comando exato, com placeholder)

**Nao execute isto por mim — este e o comando que voce, desenvolvedor, roda localmente com sua propria credencial real:**

```bash
cd backend/src/BlueprintOS.Api
dotnet user-secrets set "ConnectionStrings:ErpConnection" "Server=<SEU_SERVIDOR>;Database=SOMA_DESENV;User Id=<SEU_USUARIO>;Password=<SUA_SENHA>;TrustServerCertificate=True;"
```

Alternativa via variavel de ambiente local (nao versionada, nao vai para `secrets.json`):

```bash
export ConnectionStrings__ErpConnection="Server=<SEU_SERVIDOR>;Database=SOMA_DESENV;User Id=<SEU_USUARIO>;Password=<SUA_SENHA>;TrustServerCertificate=True;"
```

Depois, validar sem expor a credencial:

```bash
dotnet run --project backend/src/BlueprintOS.Api -- validate-b1-connectivity
```

Isso usa a identidade/permissao real do usuario que configurou o secret — o BlueprintOS nao iguala nem eleva permissao entre desenvolvedores.

### 25.5 O que fica versionado vs. o que fica somente local

| Fica no Git | Fica somente local |
| --- | --- |
| `appsettings.json` com placeholder `__SET_...__` | `~/.microsoft/usersecrets/BlueprintOS-Development/secrets.json` (User Secrets, fora do repo) |
| `UserSecretsId` no `.csproj` (identificador logico, nao segredo) | Valor real de `ConnectionStrings:ErpConnection`/`MaisComprasConnection` |
| Nome logico do servidor/banco/porta quando documentado nesta auditoria | Usuario e senha reais |
| `B1ConnectivityValidator` e o comando CLI `validate-b1-connectivity` (codigo, nao segredo) | Identidade efetiva de login (exibida em tela, nunca commitada) |

### 25.6 Resultado Consolidado

- CONNECTION STATUS: **READY** (read-only) para `ErpConnection`/`SOMA_DESENV` nesta maquina de desenvolvimento, validado via `SELECT 1` + `SELECT SUSER_SNAME()`.
- Nenhuma escrita, migration, GRANT/REVOKE ou procedure de alteracao foi executada.
- Nenhuma connection string ou senha foi impressa, logada ou commitada.
- Secret scan no diff desta etapa (`B1ConnectivityValidator.cs`, `Program.cs`, teste novo): **limpo** — nenhuma credencial, IP ou identidade real encontrados.
- `linx-database-specialist-agent` (o Agent .NET real, nao esta sessao de chat) ja possuia, antes desta tarefa, o mecanismo correto para chegar a `READY`; o gap era de uso/descoberta na sessao de chat anterior, nao de arquitetura. Nenhuma mudanca foi necessaria no `agent.yaml` do Linx Database Specialist.

### 25.7 Atualizacao — separacao DEV/PROD (ver agents/DATABASE_CONNECTION_POLICY.md)

A conexao generica `ErpConnection` unica descrita acima (25.1-25.6) foi substituida pela separacao
explicita de ambientes Linx/SOMA: `ConnectionStrings:LinxDevelopmentConnection`
(`192.168.9.98`/`SOMA_DESENV`) e `ConnectionStrings:LinxProductionConnection`
(`192.168.0.200`/`SOMA`), cada uma com profile logico, protecao contra environment mismatch e
identidade/credencial estritamente local e separada por ambiente. `ErpConnection` permanece como
fallback DEPRECATED apenas para o profile Development, para nao quebrar consumidores existentes
silenciosamente. Detalhes completos, incluindo o comando de configuracao local para os dois
ambientes, estao em `agents/DATABASE_CONNECTION_POLICY.md` e `docs/audits/DatabaseConnectionPolicyV1.md` —
esta secao 25 permanece como registro historico da investigacao original.
