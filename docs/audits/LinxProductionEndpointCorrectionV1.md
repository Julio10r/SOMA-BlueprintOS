# Linx Production Endpoint Correction V1

Status: applied (code/docs corrected); pending local secret update + live validation by the Product Owner
Data: 2026-08-27
Escopo: `backend/src/BlueprintOS.Infrastructure/Persistence/LinxConnectionProfile.cs`, `backend/tests/BlueprintOS.UnitTests/Infrastructure/Persistence/B1ConnectivityValidatorTests.cs`, `agents/DATABASE_CONNECTION_POLICY.md`, `docs/audits/DatabaseConnectionPolicyV1.md`, `docs/audits/ProductionAuthoritativeInvestigationPolicyV1.md`, `docs/audits/LinxProgOpPed-ProductionInvestigation.md`, `docs/audits/AgentContractV1.1-ExecutionPolicy-Credenciais.md`, `docs/audits/AgentLearningV1-LinxProgOpPed.md`, `docs/audits/AgentLearningV1-LinxProgOpPed-Results.json`.

## 1. O Que Aconteceu

Uma etapa anterior (`docs/audits/LinxProgOpPed-ProductionInvestigation.md`) tentou validar conectividade read-only ao profile `linx-production`, configurado com servidor `192.168.0.200` / banco `SOMA`. O diagnostico de rede realizado naquela etapa foi tecnicamente correto para o host testado:

- ICMP para `192.168.0.200`: respondeu (host alcancavel).
- TCP `192.168.0.200:443`: abriu.
- TCP `192.168.0.200:1433` e `192.168.0.200:22`: nao responderam (timeout).
- Comparativamente, `192.168.9.98:1433` (Development) respondeu normalmente.

A conclusao registrada naquela etapa foi `CONNECTIVITYUNAVAILABLE` por bloqueio de firewall/porta especifico ao host de producao — uma inferencia razoavel dado o padrao observado (host alcancavel, porta SQL fechada, outra porta aberta), mas **nao era a causa raiz real**.

## 2. Evidencia Que Corrigiu O Diagnostico

O Product Owner forneceu evidencia de uma conexao real e funcional ao banco `SOMA`, obtida por ferramenta de cliente propria, executando (read-only) contra a conexao ja aberta:

```
SELECT @@SERVERNAME, DB_NAME(), CONNECTIONPROPERTY('local_net_address'),
       CONNECTIONPROPERTY('local_tcp_port'), CONNECTIONPROPERTY('net_transport')
```

Resultado:

| Propriedade | Valor |
|---|---|
| `@@SERVERNAME` | `SRV-SOMADB` |
| `MachineName` | `SRV-SOMADB` |
| `InstanceName` | `null` (sem instancia nomeada — instancia default) |
| `DB_NAME()` | `SOMA` |
| `CONNECTIONPROPERTY('local_net_address')` | `192.168.9.200` |
| `CONNECTIONPROPERTY('local_tcp_port')` | `1433` |
| `CONNECTIONPROPERTY('net_transport')` | `TCP` |

**Conclusao:** o endpoint SQL real e authoritative de producao e `192.168.9.200:1433` (TCP, instancia default). O valor `192.168.0.200`, documentado desde a fundacao original da separacao DEV/PROD (`docs/audits/DatabaseConnectionPolicyV1.md`), **nunca foi o servidor correto** — era um erro de configuracao/documentacao desde a origem, nao um problema de rede, firewall ou VPN.

## 3. Reclassificacao Da Causa Raiz

| | Antes da correcao | Depois da correcao |
|---|---|---|
| Causa raiz assumida | Firewall/porta bloqueada especificamente para SQL (1433) e SSH (22) no host de producao, com 443 liberado | Endpoint configurado estava errado — `192.168.0.200` provavelmente e outro host na rede (responde ICMP e HTTPS por outro motivo, nao e o `SRV-SOMADB`) |
| Diagnostico de rede em si | Correto para o host testado | Continua correto para o host testado — so nao era o host certo |
| Classificacao de status | `CONNECTIVITY_UNAVAILABLE` | Passa a `CONNECTIVITY_UNAVAILABLE` **por configuracao incorreta**, nao por bloqueio de rede legitimo — reavaliar apos a correcao local (secao 5) |

**Nao removi o diagnostico de rede anterior** (`docs/audits/LinxProgOpPed-ProductionInvestigation.md`) — ele permanece registrado, com uma nota de correcao no topo apontando para este documento, exatamente para preservar o historico de que o endpoint configurado estava incorreto (nao o comportamento de rede em si).

## 4. Correcoes Aplicadas (Codigo E Documentacao)

Alterado `ExpectedServer` de `192.168.0.200` para `192.168.9.200` em:

- `backend/src/BlueprintOS.Infrastructure/Persistence/LinxConnectionProfile.cs` — `LinxConnectionProfiles.Production` (a fonte canonica unica de verdade; `LinxConnectionStringResolver`/`B1ConnectivityValidator` dependem dela para a protecao de environment mismatch).
- `backend/tests/BlueprintOS.UnitTests/Infrastructure/Persistence/B1ConnectivityValidatorTests.cs` — 6 fixtures que usavam `192.168.0.200` como representacao do servidor de producao (para testar `EnvironmentMismatch` e o fluxo de retry de conectividade). Sem essa correcao, os testes de retry (`ValidateErpAsync_Should_Recover_And_Report_Ready_When_The_Single_Retry_Succeeds` e afins) passariam a falhar com `EnvironmentMismatch` em vez de exercitar a logica de retry, porque o guard de mismatch roda antes de qualquer tentativa de conexao.
- `agents/DATABASE_CONNECTION_POLICY.md` — todas as 5 ocorrencias, com nota de correcao no topo do documento.
- `docs/audits/DatabaseConnectionPolicyV1.md`, `docs/audits/ProductionAuthoritativeInvestigationPolicyV1.md`, `docs/audits/LinxProgOpPed-ProductionInvestigation.md`, `docs/audits/AgentContractV1.1-ExecutionPolicy-Credenciais.md`, `docs/audits/AgentLearningV1-LinxProgOpPed.md`, `docs/audits/AgentLearningV1-LinxProgOpPed-Results.json` — notas de correcao adicionadas nos pontos relevantes, **sem apagar o corpo historico** desses documentos (que registra fielmente o que foi investigado/observado com o valor entao configurado).

**Usuario/senha nao foram alterados** — nenhum destes arquivos jamais continha credencial (User Secrets sempre foi o unico lugar da credencial real).

## 5. Acao Necessaria Do Product Owner (Local, Fora Do Git)

Este comando **nao foi executado por mim** — apenas o servidor precisa mudar; substitua os placeholders pela sua credencial real de producao (que voce ja possui, pois sua ferramenta local ja conecta):

```bash
dotnet user-secrets set "ConnectionStrings:LinxProductionConnection" \
  "Server=192.168.9.200;Database=SOMA;User Id=<SEU_USUARIO_PROD>;Password=<SUA_SENHA_PROD>;Encrypt=True;TrustServerCertificate=True" \
  --project backend/src/BlueprintOS.Api/BlueprintOS.Api.csproj
```

Apos confirmar a atualizacao local, a validacao read-only pode ser reexecutada com:

```bash
dotnet run --project backend/src/BlueprintOS.Api -- validate-b1-connectivity
```

Resultado esperado: `ERP Linx SOMA (Production) ........ READY`, com `Servidor: 192.168.9.200`, `Banco: SOMA`. **Esta validacao ainda nao foi executada nesta sessao** — aguardando a confirmacao do Product Owner de que o secret local foi atualizado, conforme solicitado explicitamente (regra 5 da tarefa: só validar apos a confirmacao).

## 6. Build E Testes Apos A Correcao

- `dotnet build backend/BlueprintOS.sln`: sucesso, 0 erros (warnings pre-existentes nao relacionados).
- `dotnet test backend/tests/BlueprintOS.UnitTests --filter FullyQualifiedName~B1ConnectivityValidatorTests`: **24/24 passaram** apos a correcao dos fixtures de teste (os testes de retry/mismatch continuam validando exatamente o mesmo comportamento, agora contra o endpoint correto).

## 7. Confirmacoes

- Nenhuma escrita em banco.
- Nenhuma migration.
- Nenhuma credencial alterada, exposta, logada ou commitada.
- Nenhum push.
- `SOMA_DESENV` nao foi tocado por esta correcao.
- O caso funcional PROG/OP/PED nao foi retomado — permanece bloqueado ate a validacao da secao 5 ser confirmada.
