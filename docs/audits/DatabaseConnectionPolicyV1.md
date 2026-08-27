# Database Connection Policy V1 — Separação Linx Development/Production

Data: 2026-08-27
Escopo: fundação de conexões com bancos Linx/SOMA (DEV/PROD). **Não** retoma o caso funcional
PROG/OP/PED.

## Baseline (antes desta tarefa)

- Uma única chave `ConnectionStrings:ErpConnection`, sem separação semântica de ambiente.
- `B1ConnectivityValidator` validava `ErpConnection` (rotulada "ERP SOMA_DESENV") com `SELECT 1` +
  `SELECT SUSER_SNAME()`, sem comparar servidor/banco resolvidos contra nenhum valor esperado.
- Nenhuma chave/profile equivalente existia para Production — `SOMA`/`192.168.0.200` não era
  conhecido pelo código.
- `agents/linx-database-specialist-agent/agent.yaml` declarava dois profiles (`linx-erp-read-only`,
  `linx-erp-governed-write`) apontando ambos para a mesma `ConnectionStrings:ErpConnection`.
- `ConnectivityStatus` tinha `Ready | NotConfigured | Failed | PermissionDenied` — sem
  `EnvironmentMismatch` nem `VpnRequired` (rede indisponível era classificada como `Failed`,
  indistinguível de qualquer outro erro).
- 6+ consumidores (`SomaFilialReader`, `SomaFornecedorReader`, `SomaCentroCustoReader`,
  `LinxSchemaDiscoveryReader`, `ErpFornecedorDiscoveryRepository`,
  `SomaGarantirFornecedorErpAdapter`, `SomaDesenvolErpFornecedorAdapter`) leem
  `ErpConnection` diretamente, cada um com sua própria guarda de placeholder duplicada.

## Problema

Ambiguidade operacional: `ErpConnection` não deixava explícito se apontava para DEV ou PROD, não
havia proteção determinística contra apontar para o servidor errado, e nenhum estado distinguia
"VPN desconectada" de "credencial inválida".

## Arquitetura DEV

- Profile `linx-development`: servidor `192.168.9.98`, banco `SOMA_DESENV`, porta `1433`,
  `vpn_required: true`, ambiente `Development`.
- Chave: `ConnectionStrings:LinxDevelopmentConnection`.
- Laboratório técnico: SELECT/INSERT/UPDATE/DELETE/CREATE/ALTER/DROP/TRUNCATE/EXEC podem ser
  válidos com contexto claro, finalidade clara, permissão efetiva e rastreabilidade — nunca
  execução arbitrária.

## Arquitetura PROD

- Profile `linx-production`: servidor `192.168.0.200`, banco `SOMA`, porta `1433`,
  `vpn_required: true`, ambiente `Production`.
- Chave: `ConnectionStrings:LinxProductionConnection`.
- Governança reforçada e conservadora, sem alteração de nenhuma proteção existente (ver
  `agents/DATABASE_CONNECTION_POLICY.md` § 12).

## Profiles

Implementados em
[`backend/src/BlueprintOS.Infrastructure/Persistence/LinxConnectionProfile.cs`](../../backend/src/BlueprintOS.Infrastructure/Persistence/LinxConnectionProfile.cs)
como `LinxConnectionProfiles.Development` / `.Production` — metadado lógico (servidor, banco,
nome de chave, VPN), nunca credencial.

## Credenciais

Permanecem exclusivamente em User Secrets local, mesmo store existente (`UserSecretsId:
BlueprintOS-Development`), duas chaves distintas. Nenhum valor real foi commitado, logado ou
colocado em manifesto/YAML. Comandos exatos com placeholder documentados em
`agents/DATABASE_CONNECTION_POLICY.md` § 4.

## VPN

`ConnectivityStatus.VpnRequired` adicionado — classificação separada de `PermissionDenied` para
erros de rede (timeout, host inacessível, socket) em profiles com `vpn_required: true`. O CLI
`validate-b1-connectivity` agora imprime "Conecte-se à VPN corporativa e tente novamente." quando
esse estado ocorre.

## Proteção contra mismatch

`B1ConnectivityValidator` compara `SqlConnectionStringBuilder.DataSource`/`InitialCatalog` da
connection string configurada contra o profile esperado **antes de abrir qualquer conexão de
rede**. Divergência de servidor OU banco, em qualquer direção (DEV↔PROD), bloqueia como
`ConnectivityStatus.EnvironmentMismatch`. Coberto por 4 testes unitários que não abrem conexão
real (um deles mede que a resposta é imediata, não uma tentativa de rede com timeout).

## Governança DEV vs PROD

Sem mudança de comportamento de governança nesta tarefa — apenas a fundação de conexão/ambiente. As
regras de §11/§12 de `agents/DATABASE_CONNECTION_POLICY.md` documentam o que já era prática
esperada (Development mais permissivo com contexto; Production conservador), sem relaxar nenhuma
proteção de Production.

## Validator

`B1ConnectivityValidator.ValidateErpAsync(LinxEnvironment)` é o novo ponto de entrada explícito.
`ValidateErpAsync()` sem argumento é mantido, agora delegando para `Development` (compatibilidade).
Fallback: se `LinxDevelopmentConnection` não estiver configurada, tenta a chave legada
`ErpConnection` — ainda validada contra o profile Development (guarda de mismatch se aplica
igualmente ao fallback). Nunca imprime a connection string; expõe apenas `Server`, `Database`,
`EffectiveIdentity`.

## Clone / primeiro setup

`git clone` → nenhuma credencial vem junto → `validate-b1-connectivity` reporta `NotConfigured`
para qualquer profile sem secret → desenvolvedor roda o comando `dotnet user-secrets set` do
profile que precisa (DEV, PROD, ou ambos) → valida com o mesmo comando → identidade/permissão
efetiva do banco é respeitada como está.

## Testes

`B1ConnectivityValidatorTests.cs`: 12 testes (8 novos), todos sem rede/DB real, cobrindo:
- DEV resolve somente DEV (secret de DEV presente não vaza para resolução de PROD).
- Falta de secret DEV → `NotConfigured`; falta de secret PROD → `NotConfigured`.
- Fallback legado `ErpConnection` ainda validado contra o profile Development.
- Mismatch bloqueado nas duas direções (servidor e banco), sem tentativa de conexão de rede.
- Nenhum resultado (`NotConfigured` ou `EnvironmentMismatch`) carrega a connection string/senha.
- Suíte completa (904 testes) roda em ~2s, sem regressão.

## Secret scan

Revisado o diff completo desta tarefa (código, YAML, docs, JSON). Nenhuma senha/token/connection
string real presente — apenas valores de teste (`dev/dev`, `x/x`, `super-secret-user/password`
como fixture negativa) e placeholders `__SET_...__`. IPs e nomes de bancos (`192.168.9.98`,
`192.168.0.200`, `SOMA_DESENV`, `SOMA`) estão versionados conforme permitido pela política.

## Compatibilidade

`ErpConnection` mantida como fallback DEPRECATED/LEGACY apenas para Development — nenhum
consumidor existente (os 7 leitores/adapters listados no baseline) foi alterado ou quebrado
silenciosamente. Migração desses consumidores para a chave canônica fica para uma tarefa futura,
fora do escopo funcional desta correção de fundação.

## Validação real (read-only)

- **DEV** (`192.168.9.98`/`SOMA_DESENV`): secret já existente localmente (`ErpConnection`); nova
  chave canônica `LinxDevelopmentConnection` também configurada nesta máquina, mesmo valor.
  Resultado: **READY** via `dotnet run --project backend/src/BlueprintOS.Api --
  validate-b1-connectivity` (`SELECT 1` + `SELECT SUSER_SNAME()` → identidade `ti.n8n`). Nenhuma
  escrita.
- **PROD** (`192.168.0.200`/`SOMA`): secret **não configurado** nesta máquina. Resultado:
  **NOT_CONFIGURED**, reportado corretamente sem inventar nada. Comando exato para configurar,
  se e quando desejado, está em `agents/DATABASE_CONNECTION_POLICY.md` § 4 — aguardando o usuário.

## Arquivos alterados

- `agents/DATABASE_CONNECTION_POLICY.md` (novo) — política canônica.
- `backend/src/BlueprintOS.Infrastructure/Persistence/LinxConnectionProfile.cs` (novo) — enum
  `LinxEnvironment`, record `LinxConnectionProfile`, `LinxConnectionProfiles.Development/Production`.
- `backend/src/BlueprintOS.Infrastructure/Persistence/B1ConnectivityValidator.cs` — validação por
  profile, mismatch guard, `VpnRequired`/`EnvironmentMismatch`.
- `backend/src/BlueprintOS.Api/appsettings.json` — chaves `LinxDevelopmentConnection`/
  `LinxProductionConnection` (placeholder), `ErpConnection` mantida.
- `backend/src/BlueprintOS.Api/Program.cs` — `validate-b1-connectivity` valida DEV e PROD; mensagens
  de orientação VPN/mismatch.
- `backend/tests/BlueprintOS.UnitTests/Infrastructure/Persistence/B1ConnectivityValidatorTests.cs` —
  8 testes novos.
- `agents/linx-database-specialist-agent/agent.yaml` — profiles `linx-development`/`linx-production`.
- `docs/audits/AgentContractV1.1-ExecutionPolicy-Credenciais.md` — nota de atualização (§25.7).
- `docs/audits/AgentLearningV1-LinxProgOpPed.md` — nota pontual, caso funcional não reaberto.
- `docs/audits/DatabaseConnectionPolicyV1.md` (este arquivo).

## Riscos

- Consumidores não migrados (7 arquivos) continuam lendo `ErpConnection` diretamente, não o profile
  Development formal — dívida técnica documentada, não corrigida nesta tarefa por decisão explícita
  de minimizar blast radius.
- Classificação `VpnRequired` por número de erro SQL (`53`, `-2`, `-1`, `2`, `258`, `10060`) é uma
  heurística; alguns ambientes/drivers podem emitir números diferentes para "rede indisponível" —
  não testado contra um cenário real de VPN desconectada (fora do alcance desta sessão).

## Próximos passos (fora de escopo desta tarefa)

- Migrar os 7 consumidores diretos de `ErpConnection` para `LinxConnectionProfiles`/chaves
  canônicas.
- Adicionar comando CLI dedicado por ambiente (`validate-linx-development`,
  `validate-linx-production`) se o uso separado se mostrar necessário.
- Retomar o caso PROG/OP/PED (gaps 7.9.a/7.9.b) quando o Product Owner responder.
